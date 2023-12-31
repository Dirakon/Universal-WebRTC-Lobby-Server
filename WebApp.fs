module WebSocket_Matchmaking_Server.WebApp

open FSharp.Core
open FsToolkit.ErrorHandling
open Suave
open Suave.Operators
open Suave.Filters
open Suave.RequestErrors

open System

open Suave.Sockets.Control
open Suave.WebSocket

open WebSocket_Matchmaking_Server.Domain
open WebSocket_Matchmaking_Server.WebsocketMessagingImplementation
open WebSocket_Matchmaking_Server.Utils
open YoLo


//
//
// let domains: Domain list =
//     [ ] TODO: do this instead of one domain // TODO: load from file (.env or similar)
let initialDomain: Domain =
    {| lobbies = new RWLock<LobbyInfo list>([])
       domainName = "chess-with-connect-4" |}
//
// let connectionsInformation =
//     LockableDictionary.create<ConnectionId, ConnectionInfo> ConnectionInfo.create
//
// let mutable lastConnectionId: ConnectionId = 0
// let connectionIdLock = Lock.create ()
//
// let getConnectionId () =
//     connectionIdLock
//     |> Lock.lockSync (fun _ ->
//         lastConnectionId <- lastConnectionId + 1
//         lastConnectionId - 1)
//
//
// let handleCreateLobbyRequest
//     (connectionInfo: ConnectionInfo)
//     (request: LobbyCreationRequest)
//     (webSocket: WebSocket)
//     (connectionId: ConnectionId)
//     =
//     socket {
//         let response =
//             match connectionInfo.connectionState with
//             | JustJoined -> LobbyCreationFailure {| comment = Some("Did not join a domain first") |}
//             | ChosenDomain stateInfo ->
//                 if lobbyCreationRequestInvalid request then
//                     LobbyCreationFailure {| comment = Some("Invalid lobby info") |}
//                 else
//                     stateInfo.domain.domainLock
//                     |> Lock.lockSync (fun _ ->
//                         let biggestId =
//                             stateInfo.domain.lobbies
//                             |> Seq.map (fun lobby -> lobby.id)
//                             |> Seq.maxTotal
//                             |> Option.defaultValue 0
//
//                         let hostPlayer: Player =
//                             {| webSocket = webSocket
//                                connectionId = connectionId |}
//
//                         let newLobby: Lobby =
//                             {| name = request.lobbyName
//                                password = request.lobbyPassword
//                                host = hostPeerId
//                                id = biggestId + 1
//                                maxPlayers = request.maxPlayers
//                                players = Dictionary([ KeyValuePair(hostPeerId, hostPlayer) ])
//                                lobbyLock = Lock.create () |}
//
//                         stateInfo.domain.lobbies.Add newLobby
//                         LobbyCreationSuccess {| assignedPeerId = hostPeerId |})
//             | InsideLobby stateInfo -> LobbyCreationFailure {| comment = Some("Already inside a lobby") |} // TODO: maybe force leave instead?
//
//
//         do! websocketSend webSocket response
//
//         return connectionInfo
//     }
//
// let handleListLobbiesRequest (connectionInfo: ConnectionInfo) (webSocket: WebSocket) =
//     socket {
//         let lobbies, domainLock =
//             match connectionInfo.connectionState with
//             | JustJoined -> ResizeArray(), Lock.create ()
//             | ChosenDomain stateInfo -> stateInfo.domain.lobbies, stateInfo.domain.domainLock
//             | InsideLobby stateInfo -> stateInfo.domain.lobbies, stateInfo.domain.domainLock
//
//         let lobbyDtos = domainLock |> Lock.lockSync (fun _ -> lobbies |> Seq.map toLobbyDto)
//
//         do! websocketSend webSocket (LobbyListResponse {| lobbies = lobbyDtos |})
//
//         return connectionInfo
//     }
//
// let handleDomainJoinRequest (connectionInfo: ConnectionInfo) (domainName: string) (webSocket: WebSocket) =
//     socket {
//         let domain = domains |> Seq.tryFind (fun domain -> domain.domainName = domainName)
//
//         let response =
//             match connectionInfo.connectionState with
//             | JustJoined ->
//                 if domain.IsSome then
//                     DomainJoinSuccess
//                 else
//                     DomainJoinFailure {| comment = Some("Domain not found") |}
//             | _ -> DomainJoinFailure {| comment = Some("Already inside a domain") |}
//
//         let newConnectionInfo =
//             match (domain, response) with
//             | Some(domain), DomainJoinSuccess ->
//                 {| connectionInfo with
//                     connectionState = ChosenDomain {| domain = domain |} |}
//             | _ -> connectionInfo
//
//         do! websocketSend webSocket response
//         return newConnectionInfo
//     }
//
//
// let handleRelayRequest
//     (connectionInfo: ConnectionInfo)
//     (destinationPeerId: PeerId)
//     (message: string)
//     (webSocket: WebSocket)
//     =
//     socket {
//         let destinationWebSocket =
//             match connectionInfo.connectionState with
//             | InsideLobby stateInfo ->
//                 stateInfo.lobby.lobbyLock
//                 |> Lock.lockSync (fun _ ->
//                     stateInfo.lobby.players
//                     |> Seq.tryFind (fun (KeyValue(id, _)) -> id = destinationPeerId)
//                     |> Option.map (fun (KeyValue(_, player)) -> player.webSocket))
//             | _ -> None
//
//         let! response =
//             match destinationWebSocket with
//             | None ->
//                 socket { return (MessageRelayFailure {| comment = Some("Unable to find requested peer in lobby") |}) }
//             | Some(peerWebSocket) ->
//                 socket {
//                     return!
//                         websocketSend peerWebSocket (MessageRelayedNotification {| message = message |})
//                         |> SocketMonad.mapValue MessageRelaySuccess
//                         |> SocketMonad.saveWith (
//                             MessageRelayFailure {| comment = Some("Unable to communicate with requested peer") |}
//                         )
//                 }
//
//         do! websocketSend webSocket response
//
//
//         return connectionInfo
//     }
//
// let lockOnLobbyAndAllNonHostPlayers (lobby: Lobby) (multiLock: MultiLock) =
//     let rec lockOnPlayersInner (playersLockedOn: Player list) =
//         multiLock.addLock lobby.lobbyLock
//         let playersToUnlock, playersToLock = failwith "TODO: some filtering"
//
//         if playersToUnlock = [] && playersToLock = [] then
//             ()
//         else
//             assert multiLock.tryRemoveLock lobby.lobbyLock
//
//             playersToUnlock
//             |> Seq.map (fun player ->
//                 assert multiLock.tryRemoveLock (failwith "TODO: GET LOCK STRAIGHT FROM LOCKABLE DICTIONARY SOMEHOW!"))
//             |> ignore
//
//             playersToLock
//             |> Seq.map (fun player ->
//                 multiLock.addLock (failwith "TODO: GET LOCK STRAIGHT FROM LOCKABLE DICTIONARY SOMEHOW!"))
//             |> ignore
//
//             lockOnPlayersInner (playersLockedOn |> List.except playersToUnlock |> List.append playersToLock)
//
//     lockOnPlayersInner []
//
// let handleLeaveLobbyRequest (connectionInfo: ConnectionInfo) (webSocket: WebSocket) =
//     socket {
//         match connectionInfo.connectionState with
//         | InsideLobby stateInfo ->
//             let lobby = stateInfo.lobby
//             let peerId = stateInfo.peerId
//
//             if peerId = hostPeerId then
//                 use allPlayerLock = new MultiLock()
//                 lockOnLobbyAndAllNonHostPlayers lobby allPlayerLock
//             // TODO: you have full control now. Destroy the lobby, push leave notifications to all!
//             else
//                 do!
//                     stateInfo.lobby.lobbyLock
//                     |> Lock.lockAsync (fun _ ->
//                         socket {
//                             for KeyValue(otherPlayerPeerId, otherPlayer) in lobby.players do
//                                 do!
//                                     websocketSend
//                                         otherPlayer.webSocket
//                                         (PlayerLeaveNotification {| leaverId = peerId |})
//                                     |> SocketMonad.saveWith () // TODO: nothing good can come out of people not responding. maybe fail now? maybe kick them?
//                         })
//
//             do! websocketSend webSocket (LobbyLeaveNotification {| comment = Some("Successfully left the lobby") |})
//
//             return
//                 {| connectionInfo with
//                     connectionState = (ChosenDomain {| domain = stateInfo.domain |}) |}
//         | _ ->
//             do! websocketSend webSocket (LobbyLeaveFailure {| comment = Some("Not inside a lobby to leave") |})
//             return connectionInfo
//     }
//
// let handleJoinLobbyRequest
//     (connectionInfo: ConnectionInfo)
//     (lobbyId: LobbyId)
//     (lobbyPassword: Option<string>)
//     (webSocket: WebSocket)
//     (connectionId: ConnectionId)
//     =
//     socket {
//         match connectionInfo.connectionState with
//         | InsideLobby stateInfo ->
//             do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Cannot join from inside lobby") |})
//             return connectionInfo
//         | JustJoined ->
//             do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Cannot join outside the domain") |})
//             return connectionInfo
//         | ChosenDomain stateInfo ->
//             let chosenLobby =
//                 stateInfo.domain.domainLock
//                 |> Lock.lockSync (fun _ -> stateInfo.domain.lobbies |> Seq.tryFind (fun lobby -> lobby.id = lobbyId))
//
//             match chosenLobby with
//             | None ->
//                 do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Cannot find requested lobby") |})
//                 return connectionInfo
//             | Some(lobby: Lobby) ->
//                 return!
//                     lobby.lobbyLock
//                     |> Lock.lockAsync (fun _ ->
//                         socket {
//                             if lobby.players.Count >= lobby.maxPlayers then
//                                 do!
//                                     websocketSend
//                                         webSocket
//                                         (LobbyJoinFailure {| comment = Some("Lobby already has max players") |})
//
//                                 return connectionInfo
//                             else if lobby.password.IsSome && lobby.password <> lobbyPassword then
//                                 do!
//                                     websocketSend
//                                         webSocket
//                                         (LobbyJoinFailure {| comment = Some("Incorrect password") |})
//
//                                 return connectionInfo
//                             else if lobby.players.Count = 0 then
//                                 do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Invalid lobby") |})
//                                 return connectionInfo
//                             else
//                                 let assignedPeerId = (lobby.players.Keys |> Seq.max) + 1
//                                 do! websocketSend webSocket (LobbyJoinSuccess {| assignedPeerId = assignedPeerId |})
//
//                                 for KeyValue(otherPlayerPeerId, otherPlayer) in lobby.players do
//                                     do!
//                                         websocketSend
//                                             webSocket
//                                             (PlayerJoinNotification {| joineeId = otherPlayerPeerId |})
//
//                                     do!
//                                         websocketSend
//                                             otherPlayer.webSocket
//                                             (PlayerJoinNotification {| joineeId = assignedPeerId |})
//                                         |> SocketMonad.saveWith () // TODO: nothing good can come out of people not responding. maybe fail now? maybe kick them?
//
//                                 lobby.players.[assignedPeerId] <-
//                                     {| webSocket = webSocket
//                                        connectionId = connectionId |}
//
//                                 return
//                                     {| connectionInfo with
//                                         connectionState =
//                                             ConnectionState.InsideLobby
//                                                 {| domain = stateInfo.domain
//                                                    lobby = lobby
//                                                    peerId = assignedPeerId |} |}
//                         })
//     }
//
// let rec messageHandlingWorkflow
//     (message: WebsocketClientMessage)
//     (connectionInfo: ConnectionInfo)
//     (webSocket: WebSocket)
//     (connectionId: ConnectionId)
//     =
//     match message with
//     | LobbyCreationRequest request -> handleCreateLobbyRequest connectionInfo request webSocket connectionId
//     | LobbyJoinRequest request ->
//         handleJoinLobbyRequest connectionInfo request.lobbyId request.password webSocket connectionId
//     | LobbyListRequest -> handleListLobbiesRequest connectionInfo webSocket
//     | DomainJoinRequest request -> handleDomainJoinRequest connectionInfo request.domainName webSocket
//     | LobbySealRequest -> handleLeaveLobbyRequest connectionInfo webSocket // TODO: maybe something unique instead?
//     | LobbyLeaveRequest -> handleLeaveLobbyRequest connectionInfo webSocket
//     | MessageRelayRequest request ->
//         handleRelayRequest connectionInfo request.destinationPeerId request.message webSocket
//

let standardMailboxTimeoutMs = 15 * 60000

let maxPlayerId: RWLock<PlayerId> = new RWLock<PlayerId>(1)

let getNewPlayerId () =
    async {
        use! locking = maxPlayerId.Write().AsTask() |> Async.AwaitTask
        let mutable writer = locking
        let newId = writer.Value

        writer.Value <- (newId + 1)

        return newId
    }

let maxLobbyId: RWLock<LobbyId> = new RWLock<LobbyId>(1)

let getNewLobbyId () =
    async {
        use! locking = maxLobbyId.Write().AsTask() |> Async.AwaitTask
        let mutable writer = locking
        let newId = writer.Value

        writer.Value <- (newId + 1)

        return newId
    }

let lobbyCreationRequestValid (request: LobbyCreationRequest) =
    if request.maxPlayers <= 1 then false else true

/// An example of explicitly fetching websocket errors and handling them in your codebase.
let wsWithErrorHandling (webSocket: WebSocket) (context: HttpContext) =

    use _ =
        { new IDisposable with
            member _.Dispose() =
                printfn "Resource needed by websocket connection disposed" }

    let outbox =
        new MailboxProcessor<OutboxMessage>(fun processor ->
            let rec messageLoop () =
                async {
                    let! msg = processor.Receive(standardMailboxTimeoutMs)

                    let! sendingSuccess =
                        match msg with
                        | WebsocketServerMessage websocketServerMessage ->
                            (websocketSend webSocket websocketServerMessage)

                    match sendingSuccess with
                    | Choice1Of2 unit -> return! messageLoop ()
                    | Choice2Of2 error -> () // TODO: log websocket communication error, try repeat?
                }

            messageLoop ())

    outbox.Start()

    async {
        let! thisPlayerId = getNewPlayerId ()

        let rec inbox =
            new MailboxProcessor<InboxMessage>(fun processor ->
                let thisPlayer: Player =
                    {| playerId = thisPlayerId
                       inbox = inbox |}

                let rec messageLoop (currentState: ConnectionState) =
                    async {
                        let! msg = processor.Receive(standardMailboxTimeoutMs)

                        let! newState =
                            match currentState with
                            | ChosenDomain chosenDomainState ->
                                match msg with
                                | InboxNotification inboxNotification ->
                                    match inboxNotification with
                                    | WebsocketClientMessage websocketClientMessage ->
                                        match websocketClientMessage with
                                        | LobbyCreationRequest requestPayload when
                                            not (lobbyCreationRequestValid requestPayload)
                                            ->
                                            outbox.Post(
                                                WebsocketServerMessage(
                                                    WebsocketServerMessage.LobbyCreationFailure
                                                        {| comment = Some($"Invalid lobby creation request") |}
                                                )
                                            )

                                            currentState |> Async.result
                                        | LobbyCreationRequest requestPayload ->
                                            async {
                                                use! locking =
                                                    initialDomain.lobbies.Write().AsTask() |> Async.AwaitTask

                                                let mutable lobbies = locking
                                                let! newLobbyId = getNewLobbyId ()

                                                let newLobbyInfo: LobbyInfo =
                                                    {| host = thisPlayer
                                                       id = newLobbyId
                                                       maxPlayers = requestPayload.maxPlayers
                                                       name = requestPayload.lobbyName
                                                       password = requestPayload.password |}

                                                lobbies.Value <- List.Cons(newLobbyInfo, lobbies.Value)

                                                let newLobbyReplica: LobbyReplica =
                                                    {| info = newLobbyInfo
                                                       players = Map [ thisPlayerId, thisPlayer ] |}

                                                outbox.Post(
                                                    WebsocketServerMessage(
                                                        WebsocketServerMessage.LobbyCreationSuccess
                                                            {| assignedLobbyId = newLobbyId |}
                                                    )
                                                )

                                                return
                                                    InsideLobby
                                                        {| domain = chosenDomainState.domain
                                                           lobby = newLobbyReplica |}
                                            }
                                        | LobbyJoinRequest requestPayload ->
                                            async {
                                                use! lobbies = initialDomain.lobbies.Read().AsTask() |> Async.AwaitTask

                                                let searchedLobby =
                                                    lobbies.Value
                                                    |> Seq.tryFind (fun lobby -> lobby.id = requestPayload.lobbyId)

                                                match searchedLobby with
                                                | None ->
                                                    outbox.Post(
                                                        WebsocketServerMessage(
                                                            WebsocketServerMessage.LobbyJoinFailure
                                                                {| comment =
                                                                    Some(
                                                                        $"Could not find the lobby with id {requestPayload.lobbyId}"
                                                                    ) |}
                                                        )
                                                    )
                                                | Some lobby ->
                                                    lobby.host.inbox.Post(
                                                        InboxRequest(
                                                            {| callbackProcessor = inbox
                                                               request =
                                                                InboxRequest.LobbyConnectionRequest
                                                                    {| lobbyId = lobby.id
                                                                       player = thisPlayer |} |}
                                                        )
                                                    )

                                                return currentState
                                            }

                                        | LobbyLeaveRequest ->
                                            failwith "invalid message to get while NOT in lobby: LOG IT SOMEHOW?"
                                    | InboxNotification.LobbyJoinSuccess messagePayload ->
                                        let initialLobbyReplica = messagePayload.lobbyReplica

                                        outbox.Post(
                                            WebsocketServerMessage(
                                                WebsocketServerMessage.LobbyJoinSuccess
                                                    {| lobbyId = initialLobbyReplica.info.id |}
                                            )
                                        )

                                        outbox.Post(
                                            WebsocketServerMessage(
                                                WebsocketServerMessage.PlayersChangeNotification
                                                    {| updatedPlayers = initialLobbyReplica.players.Keys |}
                                            )
                                        )

                                        InsideLobby
                                            {| domain = chosenDomainState.domain
                                               lobby = initialLobbyReplica |}
                                        |> Async.result
                                    | InboxNotification.LobbyJoinFailure commentPayload ->
                                        outbox.Post(
                                            WebsocketServerMessage(
                                                WebsocketServerMessage.LobbyJoinFailure commentPayload
                                            )
                                        )

                                        currentState |> Async.result
                                    | LobbyReplicaUpdate requestPayload ->
                                        failwith "invalid message to get while NOT in lobby: LOG IT SOMEHOW?"
                                | InboxRequest requestDescription ->
                                    match requestDescription.request with
                                    | LobbyConnectionRequest _ ->
                                        requestDescription.callbackProcessor.Post(
                                            InboxNotification(
                                                InboxNotification.LobbyJoinFailure
                                                    {| comment =
                                                        Some("Lobby host is no longer host in the requested lobby") |}
                                            )
                                        )

                                        currentState |> Async.result
                            | InsideLobby insideLobbyState ->
                                match msg with
                                | InboxNotification inboxNotification ->
                                    match inboxNotification with
                                    | LobbyReplicaUpdate newLobbyReplica when
                                        newLobbyReplica.info.id = insideLobbyState.lobby.info.id
                                        ->
                                        outbox.Post(
                                            WebsocketServerMessage(
                                                WebsocketServerMessage.PlayersChangeNotification
                                                    {| updatedPlayers = newLobbyReplica.players.Keys |}
                                            )
                                        )

                                        InsideLobby
                                            {| insideLobbyState with
                                                lobby = newLobbyReplica |}
                                        |> Async.result
                                    | LobbyReplicaUpdate _ -> failwith "invalid lobbyId! LOG IT SOMEHOW?"
                                    | WebsocketClientMessage websocketClientMessage -> failwith "todo"
                                    | InboxNotification.LobbyJoinSuccess _
                                    | InboxNotification.LobbyJoinFailure _ ->
                                        failwith "invalid message to get while in lobby: LOG IT SOMEHOW?"
                                | InboxRequest requestDescription ->
                                    match requestDescription.request with
                                    | LobbyConnectionRequest lobbyConnectionRequest when
                                        (insideLobbyState.lobby.info.id = lobbyConnectionRequest.lobbyId)
                                        && (thisPlayer.playerId = insideLobbyState.lobby.info.host.playerId)
                                        ->
                                        if
                                            insideLobbyState.lobby.players.Count
                                            >= insideLobbyState.lobby.info.maxPlayers
                                        then
                                            requestDescription.callbackProcessor.Post(
                                                InboxNotification(
                                                    InboxNotification.LobbyJoinFailure
                                                        {| comment = Some("Player count is full") |}
                                                )
                                            )

                                            currentState |> Async.result
                                        elif
                                            insideLobbyState.lobby.players.Values
                                            |> Seq.exists (fun p ->
                                                p.playerId = lobbyConnectionRequest.player.playerId)
                                        then
                                            requestDescription.callbackProcessor.Post(
                                                InboxNotification(
                                                    InboxNotification.LobbyJoinFailure
                                                        {| comment = Some("Player already in the lobby") |}
                                                )
                                            )

                                            currentState |> Async.result
                                        else
                                            let oldLobbyReplica = insideLobbyState.lobby

                                            let newLobbyReplica: LobbyReplica =
                                                {| oldLobbyReplica with
                                                    players =
                                                        oldLobbyReplica.players.Add(
                                                            lobbyConnectionRequest.player.playerId,
                                                            lobbyConnectionRequest.player
                                                        ) |}

                                            for KeyValue(playerId, player) in oldLobbyReplica.players do
                                                player.inbox.Post(
                                                    InboxNotification(
                                                        InboxNotification.LobbyReplicaUpdate newLobbyReplica
                                                    )
                                                )

                                            requestDescription.callbackProcessor.Post(
                                                InboxNotification(
                                                    InboxNotification.LobbyJoinSuccess
                                                        {| lobbyReplica = newLobbyReplica |}
                                                )
                                            )

                                            InsideLobby
                                                {| insideLobbyState with
                                                    lobby = newLobbyReplica |}
                                            |> Async.result
                                    | LobbyConnectionRequest _ ->
                                        requestDescription.callbackProcessor.Post(
                                            InboxNotification(
                                                InboxNotification.LobbyJoinFailure
                                                    {| comment =
                                                        Some("Lobby host is no longer host in the requested lobby") |}
                                            )
                                        )

                                        currentState |> Async.result



                        //  | JustJoined
                        // | ChosenDomain _
                        // | InsideLobby _ ->
                        //     requestDescription.callbackProcessor.Post(
                        //         InboxNotification(
                        //             InboxNotification.LobbyJoinFailure
                        //                 {| comment =
                        //                     Some("Lobby host is no longer host in the requested lobby") |}
                        //         )
                        //     )
                        //
                        //  currentState



                        return! messageLoop newState
                    }

                messageLoop (ConnectionState.ChosenDomain {| domain = initialDomain |}))

        inbox.Start()

        let rec messageHandling (message: WebsocketClientMessage) =
            socket { inbox.Post(InboxNotification(InboxNotification.WebsocketClientMessage message)) }


        let websocketWorkflow = websocketListen webSocket context messageHandling

        let! successOrError = websocketWorkflow

        match successOrError with
        | Choice1Of2 unit -> ()
        | Choice2Of2 error ->
            // Example error handling logic here
            printfn $"Error: [%A{error}]"

        return successOrError
    }

let app: WebPart =
    choose
        [ path "/websocket" >=> handShake wsWithErrorHandling
          NOT_FOUND "Found no handlers." ]
