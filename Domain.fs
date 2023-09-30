module WebSocket_Matchmaking_Server.Domain

open FSharp.Core
open Suave.Sockets


type Domain =
    {| domainName: string
       lobbies: RWLock<LobbyInfo ResizeArray> |} // TODO: maybe list is sufficient because of RWLock's power?
and LobbyInfo =
    {| host: Player
       id: LobbyId 
       name: string 
       password: Option<string>
       maxPlayers: int 
       |}
and LobbyReplica =
    {| info : LobbyInfo
       players: Map<PlayerId, Player>
       |}
and Player =
    {| inbox: MailboxProcessor<InboxMessage>
       playerId: PlayerId |} // TODO: take language and translate errors to this language

and PlayerId = int

and LobbyId = int



and ConnectionState =
    | JustJoined
    | ChosenDomain of {| domain: Domain |}
    | InsideLobby of
        {| domain: Domain
           lobby: LobbyReplica |}

and InboxRequest =
    | LobbyConnectionRequest of {| player: Player
                                   lobbyId : LobbyId |}
and InboxNotification =
    | LobbyReplicaUpdate of LobbyReplica
    | WebsocketClientMessage of WebsocketClientMessage
    | LobbyJoinSuccess of {| lobbyReplica : LobbyReplica |}
    | LobbyJoinFailure of {| comment: Option<string> |}

and InboxMessage =
    | InboxNotification of InboxNotification
    | InboxRequest of
        {|
           request: InboxRequest
           callbackProcessor : MailboxProcessor<InboxMessage>
           |}

and OutboxMessage = 
    | WebsocketServerMessage of WebsocketServerMessage
    
and ConnectionInfo =
    {| websocketOutbox: MailboxProcessor<OutboxMessage>
       connectionState: ConnectionState
       player : Player |}

and SingleLobbyInfo =
    {| lobbyId: LobbyId
       lobbyName: string
       currentPlayers: int
       maxPlayers: int
       hasPassword: bool |}

and LobbyCreationRequest =
    {| lobbyName: string
       maxPlayers: int
       lobbyPassword: Option<string> |}

and WebsocketClientMessage =
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
           destinationPlayerId: PlayerId |}

and WebsocketServerMessage =
    | LobbyCreationSuccess of {| assignedLobbyId: LobbyId |}
    | LobbyCreationFailure of {| comment: Option<string> |}
    | LobbyJoinSuccess of {| lobbyId: LobbyId |}
    | LobbyJoinFailure of {| comment: Option<string> |}
    | DomainJoinSuccess
    | DomainJoinFailure of {| comment: Option<string> |}
    | MessageRelaySuccess
    | MessageRelayFailure of {| comment: Option<string> |}
    | MessageRelayedNotification of {| message: string |}
    | LobbyListResponse of {| lobbies: SingleLobbyInfo seq |}
    | PlayerLeaveNotification of {| leaverId: PlayerId; lobbyId: LobbyId |}
    | PlayerJoinNotification of {| joineeId: PlayerId; lobbyId: LobbyId |}
    | LobbyLeaveNotification of {| comment: Option<string> |}

and WebSocketMessageHandling = WebsocketClientMessage -> Async<Choice<unit, Error>>

// module ConnectionInfo =
//     let create () =
//         {| connectionState = ConnectionState.JustJoined
//            inbox = MailboxProcessor() |}
