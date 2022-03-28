module FsIter.Iter

open System.Collections.Generic
open System.Runtime.CompilerServices

type iter<'T> = IEnumerator<'T>

let from<'T, 'C when 'C :> seq<'T>> (source: 'C) = source.GetEnumerator()

let longerLength<'T, 'I when 'I :> iter<'T>> (source: 'I) =
    let mutable count: uint64 = 0UL
    use mutable enumerator = source
    while enumerator.MoveNext() do
        count <- count + 1UL
    count

let length<'T, 'I when 'I :> iter<'T>> (source: 'I) =
    Checked.int32 (longerLength source)

module Struct =
    [<Interface>]
    type IClosure<'I, 'O> =
        abstract member Call : 'I -> 'O

    type clo<'I, 'O> = IClosure<'I, 'O>

    // TODO: Consider using AggressiveInlining for most of these methods.

    [<IsReadOnly; Struct>]
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

    let filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> (filter: 'F) (source: 'I) =
        new Filter<'T, 'I, 'F>(source, filter)

    let iter<'T, 'I, 'A when 'I :> iter<'T> and 'A :> clo<'T, unit>> (action: 'A) (source: 'I) =
        use mutable enumerator = source
        while enumerator.MoveNext() do
            action.Call(enumerator.Current)

type Mapping<'T, 'U, 'I when 'I :> iter<'T>> = Struct.Mapping<'T, 'U, 'I, Struct.WrappedClosure<'T, 'U>>
let map mapping source =
    Struct.map (Struct.WrappedClosure(mapping)) source

type Filter<'T, 'I when 'I :> iter<'T>> = Struct.Filter<'T, 'I, Struct.WrappedClosure<'T, bool>>
let filter filter source =
    Struct.filter (Struct.WrappedClosure(filter)) source

let iter<'T, 'I when 'I :> iter<'T>> action (source: 'I) =
    Struct.iter (Struct.WrappedClosure(action)) source
