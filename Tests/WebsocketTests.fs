module Tests.WebsocketTests

open Newtonsoft.Json
open Suave
open Suave.Testing

open NUnit.Framework

open WebSocket_Matchmaking_Server


open System
open Websocket.Client
open System.Threading



let runWithConfig = runWith defaultConfig

let ip = "localhost"
let port = 8080

[<SetUp>]
let Setup () = ()

[<Test>]
let Test1 () =

    let res = runWithConfig WebApp.app |> req HttpMethod.POST "/" None

    printfn $"{res}"
    Assert.Pass()





// TODO: setup infrastracture for multiple concurrent async websocket clients, each making some assertions:
// use MAILBOX for sync (subscribe) -> to async?
// that's a cool idea

let someTestFunction (mre: ManualResetEvent) (clientWebSocket: WebsocketClient) =
    let message =
        JsonConvert.SerializeObject
        <| MessagingDomain.DomainJoinRequest {| domainName = "Bruv" |}

    let echo: string ref = ref ""

    clientWebSocket.MessageReceived.Subscribe(fun e ->
        echo.Value <- e.Text
        mre.Set() |> ignore
        failwith "TODO: propogate errors from this subscribe function (assertion fails).")
    |> ignore

    clientWebSocket.Start() |> ignore

    clientWebSocket.Send(message)
    mre.WaitOne() |> ignore

    let stop =
        clientWebSocket.Stop(Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing")

    stop.Wait()

// Assert.AreEqual(message, echo.Value)


let testCase webSocketUrl subprotocols fn =
    use mre = new ManualResetEvent(false)

    let clientFactory _ =
        let client = new System.Net.WebSockets.ClientWebSocket()

        for proto in subprotocols do
            client.Options.AddSubProtocol proto

        client

    use clientWebSocket =
        new WebsocketClient(new Uri(sprintf "ws://%s:%i%s" ip port webSocketUrl), clientFactory)

    clientWebSocket.IsReconnectionEnabled <- false
    let ctx = runWithConfig WebApp.app
    withContext (fun _ -> fn mre clientWebSocket) ctx

[<Test>]
let Test2 () =
    testCase "/websocket" [||] someTestFunction
