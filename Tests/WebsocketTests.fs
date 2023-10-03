module Tests.WebsocketTests

open System.Linq
open Newtonsoft.Json
open Suave
open Suave.Testing

open NUnit.Framework
open FsUnit

open WebSocket_Matchmaking_Server


open System
open WebSocket_Matchmaking_Server.Domain
open Websocket.Client
open System.Threading



type DisposableSeq(disposables: IDisposable seq) =
    interface IDisposable with
        member this.Dispose() =
            disposables |> Seq.map (fun disposable -> disposable.Dispose()) |> ignore



let runWithConfig = runWith defaultConfig

let ip = "localhost"
let port = 8080

[<SetUp>]
let Setup () =
    task {
        use! locking = WebApp.initialDomain.lobbies.Write().AsTask()
        let mutable lobbies = locking.Value
        lobbies <- []
    }

[<Test>]
let Test1 () =
    async {
        let res = runWithConfig WebApp.app |> req HttpMethod.POST "/" None

        printfn $"{res}"
        Assert.Pass()
    }

let messageAsText = JsonConvert.SerializeObject
let textAsMessage = JsonConvert.DeserializeObject<WebsocketServerMessage>

let awaitMessages (count: int) (messageQueue: Channels.Channel<WebsocketServerMessage>) =
    Enumerable.Range(0, count)
    |> Seq.map (fun _ -> messageQueue.Reader.ReadAsync().AsTask() |> Async.AwaitTask)
    |> Async.Sequential
    
let appendMessages (count: int) (messageQueue: Channels.Channel<WebsocketServerMessage>) (initialMessages: WebsocketServerMessage array)=
    Enumerable.Range(0, count)
    |> Seq.map (fun _ -> messageQueue.Reader.ReadAsync().AsTask() |> Async.AwaitTask)
    |> Async.Sequential
    |> Async.map (fun newMessages -> Array.concat [initialMessages; newMessages])

let createMessageQueue (websocket: WebsocketClient) =
    let messageQueue =
        System.Threading.Channels.Channel.CreateUnbounded<WebsocketServerMessage>()

    websocket.MessageReceived.Subscribe(fun e ->
        messageQueue.Writer.WriteAsync(textAsMessage (e.Text)).AsTask() |> ignore)
    |> ignore

    messageQueue

let startWebSockets (websockets: WebsocketClient list) =
    websockets
    |> Seq.map (fun websocket -> websocket.Start() |> Async.AwaitTask |> Async.StartChild |> Async.Ignore)
    |> Async.Sequential
    |> Async.Ignore

let stopWebSockets (websockets: WebsocketClient list) =
    websockets
    |> Seq.map (fun websocket ->
        websocket.Stop(Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing")
        |> Async.AwaitTask)
    |> Async.Sequential
    |> Async.Ignore

let sendMessage message (websocket: WebsocketClient) =
    websocket.Send(messageAsText <| message)

let ``players can create and join lobbies`` (twoClientWebsockets: WebsocketClient list) =
    async {
        let clientWebsocket = twoClientWebsockets[0]
        let clientMessageQueue = createMessageQueue (clientWebsocket)

        let hostWebsocket = twoClientWebsockets[1]
        let hostMessageQueue = createMessageQueue (hostWebsocket)

        do! startWebSockets twoClientWebsockets

        hostWebsocket
        |> sendMessage (WebsocketClientMessage.LobbyCreationRequest {| lobbyName = "test"; maxPlayers = 2; password = None |})

        let! hostMessages = awaitMessages 1 hostMessageQueue

        clientWebsocket
        |> sendMessage (WebsocketClientMessage.LobbyJoinRequest {| lobbyId = 1; password = None |})

        let! hostMessages = appendMessages 1 hostMessageQueue hostMessages
        let! clientMessages = awaitMessages 2 clientMessageQueue
        
        hostMessages |> should equivalent [LobbyCreationSuccess {|assignedLobbyId = 1|};
                                       WebsocketServerMessage.PlayersChangeNotification {| updatedPlayers = [1;2] |}]
        
        clientMessages |> should equivalent [LobbyJoinSuccess {|lobbyId = 1|};
                                        WebsocketServerMessage.PlayersChangeNotification {| updatedPlayers = [1;2] |}]
        
        printfn "%s" (hostMessages.ToString())
        printfn "%s" (clientMessages.ToString())
        do! stopWebSockets twoClientWebsockets
    }


let testCase webSocketUrl subprotocols (fn: WebsocketClient list -> Async<unit>) websocketsCount =

    let clientFactory _ =
        let client = new System.Net.WebSockets.ClientWebSocket()

        for proto in subprotocols do
            client.Options.AddSubProtocol proto

        client

    let websockets =
        Enumerable.Range(0, websocketsCount)
        |> Seq.map (fun _ -> new WebsocketClient(Uri $"ws://{ip}:{port}{webSocketUrl}", clientFactory))
        |> Seq.toList

    use disposableSockets = new DisposableSeq(websockets |> Seq.cast<IDisposable>)

    websockets |> Seq.map (fun ws -> ws.IsReconnectionEnabled <- false) |> ignore

    let ctx = runWithConfig WebApp.app
    withContext (fun _ -> (fn websockets) |> Async.RunSynchronously) ctx

[<Test>]
let ``players can create and join lobbies test`` () =
    testCase "/websocket" [||] ``players can create and join lobbies`` 2
