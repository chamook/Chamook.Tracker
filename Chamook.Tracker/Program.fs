open HttpClient
open System
open System.Threading
open Newtonsoft.Json

//todo: load this from a config file or something
let home = Haversine.pos(0.0<Haversine.deg>, 0.0<Haversine.deg>)

let login username pin = 
    let data = "username=" + username + "&pin=" + pin + "&image.x=0&image.y=0"
    createRequest Post "http://79.123.36.5/mmlogin2.asp"
    |> withHeader (Referer "http://www.mindme.care")
    |> withHeader (ContentType "application/x-www-form-urlencoded")
    |> withHeader (Pragma "no-cache")
    |> withAutoFollowRedirectsDisabled
    |> withBody data
    |> getResponse

let openTrackingPage cookieValue = 
    createRequest Get "http://79.123.36.5/mmuserlocation.asp"
    |> withHeader (Referer "http://www.mindme.care")
    |> withHeader (Pragma "no-cache")
    |> withKeepAlive true
    |> withCookie {name="ASPSESSIONIDSAQAQDQS";value=cookieValue}
    |> getResponse

let parsePosition text = 
    JsonConvert.DeserializeObject<Haversine.pos> text

let filterTrackingResponse (response:string) = 
    let startPoint = response.IndexOf("goMap(") + 6
    let endPoint = response.IndexOf(",streetViewControl")
    response.Substring(startPoint, endPoint - startPoint) + "}"

let loginPrompt ()= 
    Console.WriteLine "Enter Username:"
    let user = Console.ReadLine()
    Console.WriteLine "Enter PIN:"
    let pin = Console.ReadLine()
    user, pin

type historicalPosition = {position:Haversine.pos;distanceFromHome:float<Haversine.km>}

type positionAnalysis = 
    |AwayFromHomeFirstTime
    |AwayFromHomeMoving
    |AwayFromHomeStationary
    |AtHomeReturned
    |AtHome


let rangeFromHome = 0.2<Haversine.km>
let stationaryRange = 0.1<Haversine.km>


let comparePositions previousPosition currentPosition = 
    if currentPosition.distanceFromHome < rangeFromHome then
        if previousPosition.distanceFromHome > rangeFromHome then
            AtHomeReturned
        else
            AtHome
    else
        if previousPosition.distanceFromHome < rangeFromHome then
            AwayFromHomeFirstTime
        else
            if Haversine.hsDist currentPosition.position previousPosition.position < stationaryRange then
                AwayFromHomeStationary
            else
                AwayFromHomeMoving
    

let processPosition (pos:Haversine.pos, history:list<historicalPosition>) = 
    let distance = pos |> Haversine.hsDist home
    let consoleMessage = sprintf "Found Position - distance from home: %f" (float distance)
    System.Console.WriteLine consoleMessage
    let newPosition = {position=pos;distanceFromHome=distance}
    match history with
    | [] -> [newPosition]
    | head::tail -> match comparePositions head newPosition with
                    |AtHome -> newPosition::history
                    |AwayFromHomeMoving ->  match tail with
                                            |[] -> newPosition::history
                                            |second::_ ->   match comparePositions head second with
                                                            |AwayFromHomeStationary ->  Pushbullet.sendPush "" "Movement Alert" "Away from home moving, after being stationary" |> ignore
                                                                                        newPosition::history
                                                            |_ -> newPosition::history
                    |AwayFromHomeFirstTime ->   Pushbullet.sendPush "" "Movement Alert" "Away from home first time" |> ignore
                                                newPosition::history
                    |AwayFromHomeStationary ->  match tail with
                                                |[] ->  Pushbullet.sendPush "" "Movement Alert" "Away from home stationary" |> ignore
                                                        newPosition::history
                                                |second::_ ->   match comparePositions head second with
                                                                |AwayFromHomeStationary ->  newPosition::history
                                                                |_ ->   Pushbullet.sendPush "" "Movement Alert" "Away from home stationary" |> ignore
                                                                        newPosition::history

                    |AtHomeReturned ->  Pushbullet.sendPush "" "Movement Alert" "Returned to home" |> ignore
                                        newPosition::history
                     
    


let rec track (history:list<historicalPosition>, user:string, password:string) = 
    let loginResponse =  login user password
    let trackingResponse = loginResponse.Cookies.["ASPSESSIONIDSAQAQDQS"] |> openTrackingPage

    let newHistory = 
        try
            match trackingResponse.EntityBody with
            |Some s -> 
                let parsedPosition = filterTrackingResponse s |> parsePosition
                processPosition(parsedPosition,history)
            |None -> history
        with
        | ex -> sprintf "Exception occured: %s" ex.Message |> Console.WriteLine
                Console.WriteLine "Resetting history data, this will prevent the next position from sending an alert because there is no comparison data..."
                []

    Thread.Sleep 240000

    track(newHistory,user,password)

[<EntryPoint>]
let main argv = 
    //there has to be a less dumb way to do this
    let userData = loginPrompt()
    let username = fst userData
    let pwd = snd userData

    track([],username,pwd) |> ignore


    Console.ReadLine |> ignore
    0