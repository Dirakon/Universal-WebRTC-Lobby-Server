module WebSocket_Matchmaking_Server.Utils

open FSharp.Core



module Seq =
    let maxTotal (source: seq<'a>) =
        if Seq.isEmpty source then None else Some(source |> Seq.max)

module SocketMonad =
    let map mapFunction = Async.map (Choice.map mapFunction)
    let mapValue value = Async.map (Choice.map (fun _ -> value))

    let saveWith failValue =
        Async.map (fun res ->
            match res with
            | Choice1Of2 success -> Choice1Of2 success
            | Choice2Of2 fail -> Choice1Of2 failValue)

// module Lock =
//     let create () : Lock = new RWLock<unit>()
//
//     let lockAsync (someAsyncFunction: unit -> Async<'a>) (lock: Lock) =
//         async {
//             use! locking = lock.Write().AsTask() |> Async.AwaitTask
//
//             return! someAsyncFunction ()
//         }
//
//     let lockSync (someSyncFunction: unit -> 'a) (lock: Lock) = failwith "TODO"
