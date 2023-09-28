module WebSocket_Matchmaking_Server.MessagingDomain
//
// open Suave.Sockets
// open WebSocket_Matchmaking_Server.Domain
//
//
//
// let toLobbyDto (lobby: Lobby) : SingleLobbyInfo =
//     {| currentPlayers = lobby.players.Count
//        hasPassword = lobby.password.IsSome
//        lobbyId = lobby.id
//        lobbyName = lobby.name
//        maxPlayers = lobby.maxPlayers |}
//
// let lobbyCreationRequestInvalid (request: LobbyCreationRequest) =
//     if request.maxPlayers <= 1 then false else true
