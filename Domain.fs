module WebSocket_Matchmaking_Server.Domain

open FSharp.Core
open Suave.Sockets
open Suave.WebSocket


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
       players: Map<PeerId, Player>
       |}
and Player =
    {| webSocket: WebSocket
       inbox: MailboxProcessor<InboxMessage> |} // TODO: take language and translate errors to this language

and PeerId = int

and LobbyId = int



and ConnectionState =
    | JustJoined
    | ChosenDomain of {| domain: Domain |}
    | InsideLobby of
        {| domain: Domain
           lobby: LobbyReplica
           peerId: PeerId |}

and InboxRequest =
    | LobbyConnectionRequest of {| player: Player
                                   lobbyId : LobbyId |}
and InboxNotification =
    | LobbyReplicaUpdate of LobbyReplica
    | WebsocketClientMessage of WebsocketClientMessage

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
           destinationPeerId: PeerId |}

and WebsocketServerMessage =
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

and WebSocketMessageHandling = WebsocketClientMessage -> Async<Choice<unit, Error>>

let hostPeerId: PeerId = 0

// module ConnectionInfo =
//     let create () =
//         {| connectionState = ConnectionState.JustJoined
//            inbox = MailboxProcessor() |}
