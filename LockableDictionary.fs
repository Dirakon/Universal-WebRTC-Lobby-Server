module WebSocket_Matchmaking_Server.LockableDictionary

open System.Collections.Concurrent
open WebSocket_Matchmaking_Server.Domain



module ConcurrentDictionary =
    let tryFind<'key, 'value> (key: 'key) (dictionary: ConcurrentDictionary<'key, 'value>) =
        match dictionary.TryGetValue key with
        | true, value -> Some(value)
        | _ -> None

type LockableDictionary<'key, 'value> =
    {| underlyingDictionary: ConcurrentDictionary<'key, Lock * 'value>
       criticalLock: Lock
       defaultValueFactory: unit -> 'value |}

module LockableDictionary =

    let create<'key, 'value> (defaultValueFactory: unit -> 'value) : LockableDictionary<'key, 'value> =
        {| underlyingDictionary = ConcurrentDictionary()
           criticalLock = Lock.create()
           defaultValueFactory = defaultValueFactory |}

    let withLockOnKeyAsync<'key, 'value, 'returnValue>
        (key: 'key)
        (someFunction: 'value -> Async<'returnValue * 'value>)
        (dictionary: LockableDictionary<'key, 'value>)
        =
        let keyLock =
            lock dictionary.criticalLock (fun _ ->
                match dictionary.underlyingDictionary |> ConcurrentDictionary.tryFind key with
                | None ->
                    let newLock = Lock.create()

                    assert
                        (dictionary.underlyingDictionary.TryAdd(key, (newLock, dictionary.defaultValueFactory ())) = true)

                    newLock
                | Some(lock, value) -> lock)

        lock keyLock (fun _ ->
            async {
                let oldValue =
                    match dictionary.underlyingDictionary |> ConcurrentDictionary.tryFind key with
                    | None ->
                        failwith
                            "Logical error! Not supposed to happen since we create this entry above. For this reason, never remove an entry, only add new ones"
                    | Some(key, value) -> value

                let! returnValue, newValueToSet = someFunction oldValue

                lock dictionary.criticalLock (fun _ ->
                    dictionary.underlyingDictionary.[key] <- (keyLock, newValueToSet))

                return returnValue
            })

    let withLockOnKey<'key, 'value, 'returnValue>
        (key: 'key)
        (someFunction: 'value -> 'returnValue * 'value)
        (dictionary: LockableDictionary<'key, 'value>)
        =
        dictionary
        |> withLockOnKeyAsync key (fun input -> async { return someFunction input })
        |> Async.RunSynchronously