module WebSocket_Matchmaking_Server.WebsocketMessagingImplementation

open Microsoft.FSharp.Core
open Suave

open System.Text

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

open Newtonsoft.Json
open WebSocket_Matchmaking_Server.Domain
open WebSocket_Matchmaking_Server.MessagingDomain
open YoLo

let websocketListen (webSocket: WebSocket) (context: HttpContext) (handleMessage: WebSocketMessageHandling) =
    socket {
        // if `loop` is set to false, the server will stop receiving messages
        let mutable loop = true

        while loop do
            // the server will wait for a message to be received without blocking the thread
            let! msg = webSocket.read ()

            match msg with
            // the message has type (Opcode * byte [] * bool)
            //
            // Opcode type:
            //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
            //
            // byte [] contains the actual message
            //
            // the last element is the FIN byte, explained later
            | Text, data, true ->
                // the message can be converted to a string
                let message =
                    Encoding.UTF8.GetString data
                    |> JsonConvert.DeserializeObject<WebsocketClientMessage>

                do! handleMessage message |> Async.map (fun _ -> Choice1Of2())

            | Close, _, _ ->
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true

                // after sending a Close message, stop the loop
                loop <- false

            | _ -> ()
    }

let websocketSend (webSocket: WebSocket) (message: WebsocketServerMessage) =
    socket {
        let byteMessage =
            JsonConvert.SerializeObject(message) |> Encoding.ASCII.GetBytes |> ByteSegment

        do! webSocket.send Text byteMessage true
    }
