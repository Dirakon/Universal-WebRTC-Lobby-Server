module WebSocket_Matchmaking_Server.MessagingDomain

open Suave.Sockets
open WebSocket_Matchmaking_Server.Domain


type SingleLobbyInfo =
    {| lobbyId: LobbyId
       lobbyName: string
       currentPlayers: int
       maxPlayers: int
       hasPassword: bool |}

type LobbyCreationRequest =
    {| lobbyName: string
       maxPlayers: int
       lobbyPassword: Option<string> |}

type WebsocketClientMessage =
    | LobbyCreationRequest of LobbyCreationRequest
    | LobbyJoinRequest of
        {| lobbyId: LobbyId
           password: Option<string> |}
    | DomainJoinRequest of {| domainName: string |}
    | LobbyListRequest
    | LobbySealRequest
    | LobbyLeaveRequest
    | MessageRelayRequest of
        {| message: string
           destinationPeerId: PeerId |}

type WebsocketServerMessage =
    | LobbyCreationSuccess of {| assignedPeerId: PeerId |}
    | LobbyCreationFailure of {| comment: Option<string> |}
    | LobbyJoinSuccess of {| assignedPeerId: PeerId |}
    | LobbyJoinFailure of {| comment: Option<string> |}
    | DomainJoinSuccess
    | DomainJoinFailure of {| comment: Option<string> |}
    | MessageRelaySuccess
    | MessageRelayFailure of {| comment: Option<string> |}
    | MessageRelayedNotification of {| message: string |}
    | LobbyListResponse of {| lobbies: SingleLobbyInfo seq |}
    | PlayerLeaveNotification of {| leaverId: PeerId |}
    | PlayerJoinNotification of {| joineeId: PeerId |}
    | LobbyLeaveFailure of {| comment: Option<string> |}
    | LobbyLeaveNotification of {| comment: Option<string> |}

type WebSocketMessageHandling = WebsocketClientMessage -> Async<Choice<unit, Error>>

let toLobbyDto (lobby: Lobby) : SingleLobbyInfo =
    {| currentPlayers = lobby.players.Count
       hasPassword = lobby.password.IsSome
       lobbyId = lobby.id
       lobbyName = lobby.name
       maxPlayers = lobby.maxPlayers |}

let lobbyCreationRequestInvalid (request: LobbyCreationRequest) =
    if request.maxPlayers <= 1 then false else true
