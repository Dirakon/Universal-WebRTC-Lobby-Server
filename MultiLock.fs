module WebSocket_Matchmaking_Server.MultiLock
//
// open System
// open WebSocket_Matchmaking_Server.Domain
// open WebSocket_Matchmaking_Server.Utils
//
//
// type MultiLock() =
//     let criticalLock = Lock.create ()
//     let mutable accumulatedLocks: Lock list = []
//
//     member this.addLock(newLock: Lock) = failwith "TODO"
//     // criticalLock |> Lock.lockSync (fun _ ->
//     //     Monitor.Enter newLock
//     //     accumulatedLocks <- accumulatedLocks |> List.append (List.singleton newLock))
//
//     member this.tryRemoveLock(existingLock: Lock) = failwith "TODO"
//     // criticalLock |> Lock.lockSync (fun _ ->
//     //     if accumulatedLocks |> Seq.contains existingLock then
//     //         Monitor.Exit existingLock
//     //         accumulatedLocks <- accumulatedLocks |> List.except [ existingLock ]
//     //         true
//     //     else
//     //         false)
//
//     member this.unlockAll = failwith "TODO"
//     // criticalLock |> Lock.lockSync (fun _ ->
//     //     accumulatedLocks |> Seq.map Monitor.Exit |> ignore
//     //     accumulatedLocks <- [])
//
//     interface IDisposable with
//         member this.Dispose() = this.unlockAll
