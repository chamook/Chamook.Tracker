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
    |> withCookie {name="ASPSESSIONIDACSCBRTS";value=cookieValue}
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
    |AtHome

let comparePositions previousPosition currentPosition = 
    if currentPosition.distanceFromHome < 0.4<Haversine.km> then
        AtHome
    else
        if previousPosition.distanceFromHome < 0.4<Haversine.km> then
            AwayFromHomeFirstTime
        else
            if currentPosition.distanceFromHome - previousPosition.distanceFromHome < 0.1<Haversine.km> then
                AwayFromHomeStationary
            else
                AwayFromHomeMoving
    

let processPosition (pos:Haversine.pos, history:list<historicalPosition>) = 
    let distance = pos |> Haversine.hsDist home
    let newPosition = {position=pos;distanceFromHome=distance}
    match history with
    | [] -> [newPosition]
    | head::_ ->    match comparePositions head newPosition with
                    |AtHome -> newPosition::history
                    |AwayFromHomeMoving -> newPosition::history
                    |AwayFromHomeFirstTime ->   Pushbullet.sendPush "chamookdk@gmail.com" "Movement Alert" "Away from home first time"
                                                newPosition::history
                    |AwayFromHomeStationary ->  Pushbullet.sendPush "chamookdk@gmail.com" "Movement Alert" "Away from home stationary"
                                                newPosition::history

                     
    


let rec track (history:list<historicalPosition>, user:string, password:string) = 
    let loginResponse =  login user password
    let trackingResponse = loginResponse.Cookies.["ASPSESSIONIDACSCBRTS"] |> openTrackingPage

    let newHistory =    match trackingResponse.EntityBody with
                        |Some s -> 
                            let parsedPosition = filterTrackingResponse s |> parsePosition
                            processPosition(parsedPosition,history)
                        |None -> history

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