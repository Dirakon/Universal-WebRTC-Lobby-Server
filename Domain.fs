module WebSocket_Matchmaking_Server.Domain

open System.Collections.Generic
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

and Player = {| webSocket: WebSocket |} // TODO: take language and translate errors to this language

and Lock = Object

and PeerId = int

and LobbyId = int

let hostPeerId: PeerId = 0

type ConnectionState =
    | JustJoined
    | ChosenDomain of {| domain: Domain |}
    | InsideLobby of
        {| domain: Domain
           lobby: Lobby
           peerId: PeerId |}

module Lock =
    let create () : Lock = Object
