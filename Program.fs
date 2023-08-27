open Microsoft.FSharp.Core
open Suave
open Suave.Logging



open WebSocket_Matchmaking_Server

[<EntryPoint>]
let main _ =
    startWebServer
        { defaultConfig with
            logger = Targets.create Verbose [||] }
        WebApp.app

    0
