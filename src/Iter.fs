module FsIter.Iter

open System.Collections.Generic
open System.Collections.Immutable

type iter<'T> = IEnumerator<'T>

let fromSeq<'T, 'C when 'C :> seq<'T>> (source: 'C) = source.GetEnumerator()

[<Struct>]
type ArrayIterator<'T> =
    val internal array: 'T[]
    val mutable internal index: int32

    new (array) = { array = array; index = -1 }

    member this.Current = this.array.[this.index]

    interface iter<'T> with
        member this.Current = this.Current
        member this.Current = box this.Current
        member _.Dispose() = ()
        member this.Reset() = this.index <- -1
        member this.MoveNext() =
            this.index <- this.index + 1
            this.index < this.array.Length

let inline fromArray source = new ArrayIterator<'T>(source)

let length<'T, 'I when 'I :> iter<'T>> (source: 'I) =
    let mutable count: int32 = 0
    let mutable enumerator = source
    try
        while enumerator.MoveNext() do
            count <- Checked.(+) count 1
        count
    finally
        enumerator.Dispose()

let appendToCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'I :> iter<'T>> (collection: 'C) (source: 'I) =
    let mutable enumerator = source
    try
        while enumerator.MoveNext() do
            collection.Add(enumerator.Current)
    finally
        enumerator.Dispose()

let inline toCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'C : (new : unit -> 'C) and 'I :> iter<'T>> (source: 'I) =
    let mutable collection = new 'C()
    appendToCollection<'C, 'T, 'I> collection source
    collection

let inline toArrayList<'T, 'I when 'I :> iter<'T>> (source: 'I) = toCollection<List<'T>, 'T, 'I> source

let toImmutableArray<'T, 'I when 'I :> iter<'T>> (source: 'I) =
    let builder = ImmutableArray.CreateBuilder()
    appendToCollection<_, 'T,' I> builder source
    if builder.Capacity = builder.Count
    then builder.MoveToImmutable()
    else builder.ToImmutable()

module Struct =
    [<Interface>]
    type IClosure<'I, 'O> =
        abstract member Call : 'I -> 'O

    type clo<'I, 'O> = IClosure<'I, 'O>

    // TODO: Consider using AggressiveInlining for most of these methods.

    [<Struct>]
    type WrappedClosure<'I, 'O> (closure: 'I -> 'O) =
        interface clo<'I, 'O> with
            member _.Call(input) = closure(input)

    [<Struct>]
    type Mapping<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> =
        val mutable source: 'I
        val mutable mapping: 'M

        new (source: 'I, mapping: 'M) = { source = source; mapping = mapping }

        member this.Current = this.mapping.Call(this.source.Current)

        interface iter<'U> with
            member this.Current = this.Current
            member this.Current = box this.Current
            member this.Dispose() = this.source.Dispose()
            member this.MoveNext() = this.source.MoveNext()
            member this.Reset() = this.source.Reset()

    let map<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> (mapping: 'M) (source: 'I) =
        new Mapping<'T, 'U, 'I, 'M>(source, mapping)

    [<Struct>]
    type Filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val mutable source: 'I
        val mutable filter: 'F

        new (source: 'I, filter: 'F) = { source = source; filter = filter }

        member this.MoveNext() =
            if this.source.MoveNext() then
                if this.filter.Call(this.source.Current)
                then true
                else this.MoveNext()
            else false

        interface iter<'T> with
            member this.Current = this.source.Current
            member this.Current = box this.source.Current
            member this.Dispose() = this.source.Dispose()
            member this.Reset() = this.source.Reset()
            member this.MoveNext() = this.MoveNext()

    let filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> (filter: 'F) (source: 'I) =
        new Filter<'T, 'I, 'F>(source, filter)

    [<Struct>]
    type TakeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val mutable source: 'I
        val mutable filter: 'F
        val mutable ended: bool

        new (source: 'I, filter: 'F) = { source = source; filter = filter; ended = false }

        interface iter<'T> with
            member this.Current = this.source.Current
            member this.Current = box this.source.Current
            member this.Dispose() = this.source.Dispose()

            member this.MoveNext() =
                if not this.ended && this.source.MoveNext() then
                    this.ended = this.filter.Call(this.source.Current)
                else
                    false

            member this.Reset() =
                this.source.Reset()
                this.ended <- false

    let takeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> (predicate: 'F) (source: 'I) =
        new TakeWhile<'T, 'I, 'F>(source, predicate)

    let iter<'T, 'I, 'A when 'I :> iter<'T> and 'A :> clo<'T, unit>> (action: 'A) (source: 'I) =
        let mutable enumerator = source
        try
            while enumerator.MoveNext() do
                action.Call(enumerator.Current)
        finally
            enumerator.Dispose()

type Mapping<'T, 'U, 'I when 'I :> iter<'T>> = Struct.Mapping<'T, 'U, 'I, Struct.WrappedClosure<'T, 'U>>
let map mapping source = Struct.map (Struct.WrappedClosure(mapping)) source

type Filter<'T, 'I when 'I :> iter<'T>> = Struct.Filter<'T, 'I, Struct.WrappedClosure<'T, bool>>
let filter filter source = Struct.filter (Struct.WrappedClosure(filter)) source

type TakeWhile<'T, 'I when 'I :> iter<'T>> = Struct.TakeWhile<'T, 'I, Struct.WrappedClosure<'T, bool>>
let takeWhile predicate source = Struct.takeWhile (Struct.WrappedClosure(predicate)) source

let iter<'T, 'I when 'I :> iter<'T>> action (source: 'I) = Struct.iter (Struct.WrappedClosure(action)) source
