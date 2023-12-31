module Tests.LockableDictionaryTests
//
//
// open System.Collections.Concurrent
// open System.Diagnostics
// open System.Linq
//
// open NUnit.Framework
//
// open WebSocket_Matchmaking_Server
// open WebSocket_Matchmaking_Server.LockableDictionary
//
//
//
// open System
//
//
// let defaultValue = 0
// let mutable dictionary: Option<LockableDictionary<int, int>> = None
// let mutable globalSw: Option<Stopwatch> = None
//
// [<SetUp>]
// let Setup () =
//     dictionary <- Some(LockableDictionary.create (fun _ -> defaultValue))
//     globalSw <- Some(Stopwatch.StartNew())
//
//
// [<Test>]
// [<TestCase(1)>]
// [<TestCase(2)>]
// let ``LockableDictionary locks on key`` (someKey: int) =
//     let dictionary =
//         dictionary |> Option.defaultWith (fun _ -> failwith "Dictionary not set")
//
//     async {
//         // Arrange
//         let mutable lockIsSupposedToBeSet = false
//         let valuesSeen: ConcurrentBag<int> = ConcurrentBag()
//
//         let makeAsyncTask (waitingTimeMs: int) ownValue =
//             dictionary
//             |> LockableDictionary.withLockOnKeyAsync someKey (fun previousValue ->
//                 async {
//                     Assert.AreEqual(lockIsSupposedToBeSet, false)
//                     lockIsSupposedToBeSet <- true
//
//                     valuesSeen.Add(previousValue)
//                     do! Async.Sleep(waitingTimeMs)
//
//                     Assert.AreEqual(lockIsSupposedToBeSet, true)
//                     lockIsSupposedToBeSet <- false
//                     return ((), ownValue)
//                 })
//
//
//         let oneTaskTimeMs = 1000
//         let values = [ 1; 2; 3; 4 ]
//
//         // Act
//         do!
//             values
//             |> Seq.map (fun value -> makeAsyncTask oneTaskTimeMs value)
//             |> Async.Parallel
//             |> Async.Ignore
//
//         // Assert
//         dictionary
//         |> LockableDictionary.withLockOnKey someKey (fun previousValue ->
//             valuesSeen.Add(previousValue)
//             (), previousValue)
//
//         Assert.AreEqual(lockIsSupposedToBeSet, false)
//         Assert.That(valuesSeen, Is.EquivalentTo([ 0; 1; 2; 3; 4 ]))
//     }
//
// [<Test>]
// let ``LockableDictionary is on on-key basis`` () =
//     async {
//         // Arrange
//         let dictionary = dictionary.Value
//         let keys = Enumerable.Range(0, 40) |> Seq.toList
//         let oneTestTime: int64 = 4000
//
//         let sw = Stopwatch()
//         sw.Start()
//
//         // Act
//         do!
//             Async.Parallel(keys |> Seq.map (fun key -> ``LockableDictionary locks on key`` key))
//             |> Async.Ignore
//
//         // Assert
//         sw.Stop()
//
//         let finalValues =
//             keys
//             |> Seq.map (fun key -> dictionary |> LockableDictionary.withLockOnKey key (fun value -> value, value))
//
//         printfn $"Time in ms: {sw.ElapsedMilliseconds}"
//         Assert.GreaterOrEqual(sw.ElapsedMilliseconds, oneTestTime)
//         Assert.Less(sw.ElapsedMilliseconds, (int64 keys.Length) * oneTestTime / (int64 2))
//
//         printfn $"{String.Join(',', finalValues)}"
//         Assert.That(finalValues, Is.All.Not.EqualTo(defaultValue))
//     }
