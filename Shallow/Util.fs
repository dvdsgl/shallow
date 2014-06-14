[<AutoOpen>]
module Shallow.Util

open System
open System.Collections.Generic

module Seq =
    let rec cycle xs = seq {
        yield! xs
        yield! cycle xs
    }

type Dictionary<'Key, 'Value> with
    member this.MaybeGet(k) =
        match this.TryGetValue(k) with
        | true, v -> Some v
        | _ -> None

let memoize f =
    let cache = Dictionary<_,_>(HashIdentity.Structural)
    fun x ->
        match cache.MaybeGet(x) with
        | Some y -> y
        | None ->
            let y = f x
            cache.[x] <- y
            y