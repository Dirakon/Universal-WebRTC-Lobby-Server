module WebSocket_Matchmaking_Server.MultiLock

open System
open System.Threading
open WebSocket_Matchmaking_Server.Domain
open WebSocket_Matchmaking_Server.Utils


type MultiLock() =
    let criticalLock = Lock.create ()
    let mutable accumulatedLocks: Lock list = []

    member this.addLock(newLock: Lock) =
        criticalLock |> Lock.lockSync (fun _ ->
            Monitor.Enter newLock
            accumulatedLocks <- accumulatedLocks |> List.append (List.singleton newLock))

    member this.tryRemoveLock(existingLock: Lock) =
        lock criticalLock (fun _ ->
            if accumulatedLocks |> Seq.contains existingLock then
                Monitor.Exit existingLock
                accumulatedLocks <- accumulatedLocks |> List.except [ existingLock ]
                true
            else
                false)

    member this.unlockAll =
        lock criticalLock (fun _ ->
            accumulatedLocks |> Seq.map Monitor.Exit |> ignore
            accumulatedLocks <- [])

    interface IDisposable with
        member this.Dispose() = this.unlockAll
