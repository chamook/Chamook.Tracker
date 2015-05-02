module Pushbullet

open HttpClient
open Newtonsoft.Json

//seriously, don't leave this in when you commit
let accessToken = ""

type emailPushRequest = {
    email:string;
    [<JsonProperty(PropertyName="type")>] 
    Type:string; 
    title:string; 
    body:string}

let sendPush recipientMail heading message = 
    let consoleMessage = sprintf "sending a push to %s with heading %s and message %s" recipientMail heading message
    System.Console.WriteLine consoleMessage
    let authHeader = "Bearer " + accessToken
    let requestBody = {email=recipientMail;Type="note";title=heading;body=message}
    let bodyText = JsonConvert.SerializeObject(requestBody)
    createRequest Post "https://api.pushbullet.com/v2/pushes"
    |> withHeader (Authorization authHeader)
    |> withHeader (ContentType "application/json")
    |> withBody bodyText
    |> getResponse

let spam recipientList heading message =
    recipientList |> List.map (fun i -> sendPush i heading message)