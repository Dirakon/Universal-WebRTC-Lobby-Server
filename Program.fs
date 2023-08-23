open System.Collections.Generic
open Microsoft.FSharp.Core
open Suave
open Suave.Operators
open Suave.Filters
open Suave.RequestErrors
open Suave.Logging

open System

open Suave.Sockets.Control
open Suave.WebSocket

open WebSocket_Matchmaking_Server
open WebSocket_Matchmaking_Server.Domain
open WebSocket_Matchmaking_Server.LockableDictionary
open WebSocket_Matchmaking_Server.MessagingDomain
open WebSocket_Matchmaking_Server.MultiLock
open WebSocket_Matchmaking_Server.WebsocketMessagingImplementation
open WebSocket_Matchmaking_Server.Utils
open YoLo




let domains: Domain ResizeArray =
    ResizeArray(
        [| {| domainName = "chess-with-connect-4" // TODO: load from file (.env or similar)
              lobbies = ResizeArray()
              domainLock = Lock.create () |} |]
    )

let connectionsInformation =
    LockableDictionary.create<ConnectionId, ConnectionInfo> ConnectionInfo.create

let mutable lastConnectionId: ConnectionId = 0
let connectionIdLock = Lock.create ()

let getConnectionId () =
    lock connectionIdLock (fun _ ->
        lastConnectionId <- lastConnectionId + 1
        lastConnectionId - 1)


let handleCreateLobbyRequest
    (connectionInfo: ConnectionInfo)
    (request: LobbyCreationRequest)
    (webSocket: WebSocket)
    (connectionId: ConnectionId)
    =
    socket {
        let response =
            match connectionInfo.connectionState with
            | JustJoined -> LobbyCreationFailure {| comment = Some("Did not join a domain first") |}
            | ChosenDomain stateInfo ->
                if lobbyCreationRequestInvalid request then
                    LobbyCreationFailure {| comment = Some("Invalid lobby info") |}
                else
                    lock stateInfo.domain.domainLock (fun _ ->
                        let biggestId =
                            stateInfo.domain.lobbies
                            |> Seq.map (fun lobby -> lobby.id)
                            |> Seq.maxTotal
                            |> Option.defaultValue 0

                        let hostPlayer: Player =
                            {| webSocket = webSocket
                               connectionId = connectionId |}

                        let newLobby: Lobby =
                            {| name = request.lobbyName
                               password = request.lobbyPassword
                               host = hostPeerId
                               id = biggestId + 1
                               maxPlayers = request.maxPlayers
                               players = Dictionary([ KeyValuePair(hostPeerId, hostPlayer) ])
                               lobbyLock = Lock.create () |}

                        stateInfo.domain.lobbies.Add newLobby
                        LobbyCreationSuccess {| assignedPeerId = hostPeerId |})
            | InsideLobby stateInfo -> LobbyCreationFailure {| comment = Some("Already inside a lobby") |} // TODO: maybe force leave instead?


        do! websocketSend webSocket response

        return connectionInfo
    }

let handleListLobbiesRequest (connectionInfo: ConnectionInfo) (webSocket: WebSocket) =
    socket {
        let lobbies, domainLock =
            match connectionInfo.connectionState with
            | JustJoined -> ResizeArray(), Lock.create ()
            | ChosenDomain stateInfo -> stateInfo.domain.lobbies, stateInfo.domain.domainLock
            | InsideLobby stateInfo -> stateInfo.domain.lobbies, stateInfo.domain.domainLock

        let lobbyDtos = lock domainLock (fun _ -> lobbies |> Seq.map toLobbyDto)

        do! websocketSend webSocket (LobbyListResponse {| lobbies = lobbyDtos |})

        return connectionInfo
    }

let handleDomainJoinRequest (connectionInfo: ConnectionInfo) (domainName: string) (webSocket: WebSocket) =
    socket {
        let domain = domains |> Seq.tryFind (fun domain -> domain.domainName = domainName)

        let response =
            match connectionInfo.connectionState with
            | JustJoined ->
                if domain.IsSome then
                    DomainJoinSuccess
                else
                    DomainJoinFailure {| comment = Some("Domain not found") |}
            | _ -> DomainJoinFailure {| comment = Some("Already inside a domain") |}

        let newConnectionInfo =
            match (domain, response) with
            | Some(domain), DomainJoinSuccess ->
                {| connectionInfo with
                    connectionState = ChosenDomain {| domain = domain |} |}
            | _ -> connectionInfo

        do! websocketSend webSocket response
        return newConnectionInfo
    }


let handleRelayRequest
    (connectionInfo: ConnectionInfo)
    (destinationPeerId: PeerId)
    (message: string)
    (webSocket: WebSocket)
    =
    socket {
        let destinationWebSocket =
            match connectionInfo.connectionState with
            | InsideLobby stateInfo ->
                lock stateInfo.lobby.lobbyLock (fun _ ->
                    stateInfo.lobby.players
                    |> Seq.tryFind (fun (KeyValue(id, _)) -> id = destinationPeerId)
                    |> Option.map (fun (KeyValue(_, player)) -> player.webSocket))
            | _ -> None

        let! response =
            match destinationWebSocket with
            | None ->
                socket { return (MessageRelayFailure {| comment = Some("Unable to find requested peer in lobby") |}) }
            | Some(peerWebSocket) ->
                socket {
                    return!
                        websocketSend peerWebSocket (MessageRelayedNotification {| message = message |})
                        |> SocketMonad.mapValue MessageRelaySuccess
                        |> SocketMonad.saveWith (
                            MessageRelayFailure {| comment = Some("Unable to communicate with requested peer") |}
                        )
                }

        do! websocketSend webSocket response


        return connectionInfo
    }

let lockOnLobbyAndAllNonHostPlayers (lobby: Lobby) (multiLock: MultiLock) =
    let rec lockOnPlayersInner (playersLockedOn: Player list) =
        multiLock.addLock lobby.lobbyLock
        let playersToUnlock, playersToLock = failwith "TODO: some filtering"

        if playersToUnlock = [] && playersToLock = [] then
            ()
        else
            assert multiLock.tryRemoveLock lobby.lobbyLock

            playersToUnlock
            |> Seq.map (fun player ->
                assert multiLock.tryRemoveLock (failwith "TODO: GET LOCK STRAIGHT FROM LOCKABLE DICTIONARY SOMEHOW!"))
            |> ignore

            playersToLock
            |> Seq.map (fun player ->
                multiLock.addLock (failwith "TODO: GET LOCK STRAIGHT FROM LOCKABLE DICTIONARY SOMEHOW!"))
            |> ignore

            lockOnPlayersInner (playersLockedOn |> List.except playersToUnlock |> List.append playersToLock)

    lockOnPlayersInner []

let handleLeaveLobbyRequest (connectionInfo: ConnectionInfo) (webSocket: WebSocket) =
    socket {
        match connectionInfo.connectionState with
        | InsideLobby stateInfo ->
            let lobby = stateInfo.lobby
            let peerId = stateInfo.peerId

            if peerId = hostPeerId then
                use allPlayerLock = new MultiLock()
                lockOnLobbyAndAllNonHostPlayers lobby allPlayerLock
                // TODO: you have full control now. Destroy the lobby, push leave notifications to all!
            else
                do!
                    lock stateInfo.lobby.lobbyLock (fun _ ->
                        socket {
                            for KeyValue(otherPlayerPeerId, otherPlayer) in lobby.players do
                                do!
                                    websocketSend
                                        otherPlayer.webSocket
                                        (PlayerLeaveNotification {| leaverId = peerId |})
                                    |> SocketMonad.saveWith () // TODO: nothing good can come out of people not responding. maybe fail now? maybe kick them?
                        })

            do! websocketSend webSocket (LobbyLeaveNotification {| comment = Some("Successfully left the lobby") |})

            return
                {| connectionInfo with
                    connectionState = (ChosenDomain {| domain = stateInfo.domain |}) |}
        | _ ->
            do! websocketSend webSocket (LobbyLeaveFailure {| comment = Some("Not inside a lobby to leave") |})
            return connectionInfo
    }

let handleJoinLobbyRequest
    (connectionInfo: ConnectionInfo)
    (lobbyId: LobbyId)
    (lobbyPassword: Option<string>)
    (webSocket: WebSocket)
    (connectionId: ConnectionId)
    =
    socket {
        match connectionInfo.connectionState with
        | InsideLobby stateInfo ->
            do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Cannot join from inside lobby") |})
            return connectionInfo
        | JustJoined ->
            do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Cannot join outside the domain") |})
            return connectionInfo
        | ChosenDomain stateInfo ->
            let chosenLobby =
                lock stateInfo.domain.domainLock (fun _ ->
                    stateInfo.domain.lobbies |> Seq.tryFind (fun lobby -> lobby.id = lobbyId))

            match chosenLobby with
            | None ->
                do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Cannot find requested lobby") |})
                return connectionInfo
            | Some(lobby) ->
                return!
                    lock lobby.lobbyLock (fun _ ->
                        socket {
                            if lobby.players.Count >= lobby.maxPlayers then
                                do!
                                    websocketSend
                                        webSocket
                                        (LobbyJoinFailure {| comment = Some("Lobby already has max players") |})

                                return connectionInfo
                            else if lobby.password.IsSome && lobby.password <> lobbyPassword then
                                do!
                                    websocketSend
                                        webSocket
                                        (LobbyJoinFailure {| comment = Some("Incorrect password") |})

                                return connectionInfo
                            else if lobby.players.Count = 0 then
                                do! websocketSend webSocket (LobbyJoinFailure {| comment = Some("Invalid lobby") |})
                                return connectionInfo
                            else
                                let assignedPeerId = (lobby.players.Keys |> Seq.max) + 1
                                do! websocketSend webSocket (LobbyJoinSuccess {| assignedPeerId = assignedPeerId |})

                                for KeyValue(otherPlayerPeerId, otherPlayer) in lobby.players do
                                    do!
                                        websocketSend
                                            webSocket
                                            (PlayerJoinNotification {| joineeId = otherPlayerPeerId |})

                                    do!
                                        websocketSend
                                            otherPlayer.webSocket
                                            (PlayerJoinNotification {| joineeId = assignedPeerId |})
                                        |> SocketMonad.saveWith () // TODO: nothing good can come out of people not responding. maybe fail now? maybe kick them?

                                lobby.players.[assignedPeerId] <-
                                    {| webSocket = webSocket
                                       connectionId = connectionId |}

                                return
                                    {| connectionInfo with
                                        connectionState =
                                            ConnectionState.InsideLobby
                                                {| domain = stateInfo.domain
                                                   lobby = lobby
                                                   peerId = assignedPeerId |} |}
                        })
    }

let rec messageHandlingWorkflow
    (message: WebsocketClientMessage)
    (connectionInfo: ConnectionInfo)
    (webSocket: WebSocket)
    (connectionId: ConnectionId)
    =
    match message with
    | LobbyCreationRequest request -> handleCreateLobbyRequest connectionInfo request webSocket connectionId
    | LobbyJoinRequest request ->
        handleJoinLobbyRequest connectionInfo request.lobbyId request.password webSocket connectionId
    | LobbyListRequest -> handleListLobbiesRequest connectionInfo webSocket
    | DomainJoinRequest request -> handleDomainJoinRequest connectionInfo request.domainName webSocket
    | LobbySealRequest -> handleLeaveLobbyRequest connectionInfo webSocket // TODO: maybe something unique instead?
    | LobbyLeaveRequest -> handleLeaveLobbyRequest connectionInfo webSocket
    | MessageRelayRequest request ->
        handleRelayRequest connectionInfo request.destinationPeerId request.message webSocket



/// An example of explictly fetching websocket errors and handling them in your codebase.
let wsWithErrorHandling (webSocket: WebSocket) (context: HttpContext) =

    use _ =
        { new IDisposable with
            member _.Dispose() =
                printfn "Resource needed by websocket connection disposed" }

    let connectionId = getConnectionId ()

    let rec messageHandling (message: WebsocketClientMessage) =
        connectionsInformation
        |> LockableDictionary.withLockOnKeyAsync connectionId (fun connectionInfo ->
            messageHandlingWorkflow message connectionInfo webSocket connectionId
            |> Async.map (fun res ->
                match res with
                | Choice1Of2 newConnectionInfo -> ((Choice1Of2()), newConnectionInfo)
                // NOTE: putting default connectionInfo back since I didn't figure out how to safely remove entries from this lockable dictionary yet
                // TODO: figure this out ^
                | Choice2Of2 error -> ((Choice2Of2 error), ConnectionInfo.create ())))


    let websocketWorkflow = websocketListen webSocket context messageHandling

    async {
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

[<EntryPoint>]
let main _ =
    startWebServer
        { defaultConfig with
            logger = Targets.create Verbose [||] }
        app

    0
