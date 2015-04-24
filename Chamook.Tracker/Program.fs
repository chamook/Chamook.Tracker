open HttpClient
open System.Threading

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

let filterTrackingResponse (response:string) = 
    let startPoint = response.IndexOf("latitude:")
    let endPoint = response.IndexOf(",streetViewControl")
    response.Substring(startPoint, endPoint - startPoint)

let loginPrompt = 
    System.Console.WriteLine "Enter Username:"
    let user = System.Console.ReadLine()
    System.Console.WriteLine "Enter PIN:"
    let pin = System.Console.ReadLine()
    user, pin

let rec track user password = 
    let loginResponse =  login user password
    let trackingResponse = loginResponse.Cookies.["ASPSESSIONIDACSCBRTS"] |> openTrackingPage

    match trackingResponse.EntityBody with
    |Some s -> filterTrackingResponse s |> System.Console.WriteLine
    |None -> System.Console.WriteLine "No data!"

    Thread.Sleep 240000

    track user password
    

[<EntryPoint>]
let main argv = 
    loginPrompt ||> track

    System.Console.ReadLine |> ignore
    0