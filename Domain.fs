module WebSocket_Matchmaking_Server.Domain

open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading
open Suave.WebSocket


type Domain =
    {| domainName: string
       lobbies: Lobby ResizeArray
       domainLock: Lock |}

and Lobby =
    {| id: LobbyId
       players: Dictionary<PeerId, Player>
       host: PeerId
       password: Option<string>
       name: string
       maxPlayers: int
       lobbyLock: Lock |}

and Player =
    {| webSocket: WebSocket
       connectionId: ConnectionId |} // TODO: take language and translate errors to this language

and Lock = Semaphore

and PeerId = int

and LobbyId = int

and ConnectionId = int



let hostPeerId: PeerId = 0

type ConnectionState =
    | JustJoined
    | ChosenDomain of {| domain: Domain |}
    | InsideLobby of
        {| domain: Domain
           lobby: Lobby
           peerId: PeerId |}

type EventNotification = LobbyDisbandedEvent

type ConnectionInfo =
    {| eventsMissed: ConcurrentQueue<EventNotification>
       connectionState: ConnectionState |}

module ConnectionInfo =
    let create () =
        {| connectionState = ConnectionState.JustJoined
           eventsMissed = ConcurrentQueue() |}
