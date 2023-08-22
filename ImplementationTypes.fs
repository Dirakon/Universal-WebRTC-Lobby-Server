module WebSocket_Matchmaking_Server.ImplementationTypes

open System.Collections.Concurrent
open WebSocket_Matchmaking_Server.Domain



type ConnectionId = int

type EventNotification = LobbyDisbandedEvent

type ConnectionInfo =
    {| eventsMissed: ConcurrentQueue<EventNotification>
       connectionState: ConnectionState |}

module ConnectionInfo =
    let create () =
        {| connectionState = ConnectionState.JustJoined
           eventsMissed = ConcurrentQueue() |}
