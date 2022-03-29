module FsIter.Iter

open System
open System.Collections.Generic
open System.Collections.Immutable

#nowarn "77" // Special member constraint names

[<Struct>]
type CountEstimate =
    { Lower: int; Upper: int voption }

    member this.Estimate =
        match this.Upper with
        | ValueNone -> this.Lower
        | ValueSome(upper) -> upper

    static member Default = { Lower = 0; Upper = ValueNone }

[<Interface>]
type Iterator<'T> =
    inherit IDisposable

    abstract member Next : element: outref<'T> -> bool
    abstract member RemainingCount : CountEstimate

type iter<'T> = Iterator<'T>

[<Struct; NoComparison; NoEquality>]
type SeqIterator<'T, 'E when 'E :> IEnumerator<'T>> =
    val mutable inner: 'E

    new (enumerator: 'E) = { inner = enumerator }

    interface iter<'T> with
        member this.Next(element: outref<'T>) =
            let result = this.inner.MoveNext()
            if result then element <- this.inner.Current
            result

        member this.Dispose() = this.inner.Dispose()
        member _.RemainingCount = CountEstimate.Default

let inline fromEnumerator<'T, 'E when 'E :> IEnumerator<'T>> (source: 'E) = new SeqIterator<'T, 'E>(source)

let inline fromSeq<'T, 'C when 'C :> seq<'T>> (source: 'C) = source.GetEnumerator() |> fromEnumerator

let fromResizeArray<'T> (source: List<'T>) =
    if isNull source then nullArg (nameof source)
    fromEnumerator (source.GetEnumerator())

[<Struct; NoComparison; NoEquality>]
type ArrayIterator<'T> =
    val array: 'T[]
    val mutable index: int32

    new (array) =
        { array = if isNull array then Array.Empty() else array
          index = -1 }

    interface iter<'T> with
        member this.Next(element: outref<'T>) =
            if this.index < this.array.Length - 1 then
                this.index <- this.index + 1
                element <- this.array.[this.index]
                true
            else
                false

        member this.RemainingCount =
            let count = this.array.Length - (this.index + 1)
            { Lower = count; Upper = ValueSome count }

        member _.Dispose() = ()

let inline fromArray source = new ArrayIterator<'T>(source)

[<Struct; NoComparison; NoEquality>]
type StringCharIterator =
    val mutable internal next: int
    val mutable internal string: string

    new (s: string) =
        { string = if isNull s then String.Empty else s
          next = -1 }

    interface iter<char> with
        member _.Dispose() = ()

        member this.RemainingCount =
            let count = this.string.Length - (this.next + 1)
            { Lower = count; Upper = ValueSome count }

        member this.Next(element: outref<char>) =
            if this.next < this.string.Length - 1 then
                this.next <- this.next + 1
                element <- this.string.[this.next]
                true
            else
                false

let inline fromStringChars source = new StringCharIterator(source)

let length<'T, 'I when 'I :> iter<'T>> (source: 'I) =
    let mutable count: int32 = 0
    let mutable iterator = source
    let mutable sink = Unchecked.defaultof<'T>
    try
        while iterator.Next(&sink) do
            count <- Checked.(+) count 1
        count
    finally
        iterator.Dispose()

let appendToCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'I :> iter<'T>> (collection: 'C) (source: 'I) =
    let mutable iterator = source
    let mutable item = Unchecked.defaultof<'T>
    try
        while iterator.Next(&item) do
            collection.Add(item)
    finally
        iterator.Dispose()

let inline toCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'C : (new : unit -> 'C) and 'I :> iter<'T>> (source: 'I) =
    let mutable collection = new 'C()
    appendToCollection<'C, 'T, 'I> collection source
    collection

let toArrayBuilder (source: 'I when 'I :> iter<'T>) =
    let mutable builder = Helpers.ArrayBuilder(source.RemainingCount.Estimate)
    let mutable iterator = source
    let mutable item = Unchecked.defaultof<'T>
    try
        while iterator.Next(&item) do
            builder.Add(item)
    finally
        iterator.Dispose()
    builder

let toResizeArray<'T, 'I when 'I :> iter<'T>> (source: 'I) =
    let items = List<'T>(source.RemainingCount.Estimate)
    appendToCollection items source
    items

let toArray<'T, 'I when 'I :> iter<'T>> (source: 'I) = toArrayBuilder(source).ToArray()

let toImmutableArray<'T, 'I when 'I :> iter<'T>> (source: 'I) = toArrayBuilder(source).ToImmutableArray()

let toStringBuilder<'I when 'I :> iter<char>> (source: 'I) =
    let builder = System.Text.StringBuilder(source.RemainingCount.Estimate)
    let mutable iterator = source
    let mutable character = Unchecked.defaultof<char>
    try
        while iterator.Next(&character) do
            builder.Append(character) |> ignore
    finally
        iterator.Dispose()
    builder

module Struct =
    [<Interface>]
    type IClosure<'I, 'O> =
        abstract member Call : 'I -> 'O

    type clo<'I, 'O> = IClosure<'I, 'O>

    // TODO: Consider using AggressiveInlining for most of these methods.

    [<Struct; NoComparison; NoEquality>]
    type WrappedClosure<'I, 'O> (closure: 'I -> 'O) =
        interface clo<'I, 'O> with
            member _.Call(input) = closure(input)

    // TODO: Avoid struct copying when passing parameters to constructors by jsut calling the default constructor and setting the fields directly.

    [<Struct; NoComparison; NoEquality>]
    type Mapping<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> =
        val mutable source: 'I
        val mapping: 'M

        new (source: 'I, mapping: 'M) = { source = source; mapping = mapping }

        interface iter<'U> with
            member this.Dispose() = this.source.Dispose()

            member this.Next(element: outref<'U>) =
                let mutable item = Unchecked.defaultof<'T>
                if this.source.Next(&item) then
                    element <- this.mapping.Call item
                    true
                else
                    false

            member this.RemainingCount = this.source.RemainingCount

    let map<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> (mapping: 'M) (source: 'I) =
        new Mapping<'T, 'U, 'I, 'M>(source, mapping)

    [<Struct; NoComparison; NoEquality>]
    type Filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val mutable source: 'I
        val filter: 'F

        new (source: 'I, filter: 'F) = { source = source; filter = filter }

        interface iter<'T> with
            member this.Dispose() = this.source.Dispose()

            member this.RemainingCount = { this.source.RemainingCount with Lower = 0 }

            member this.Next(element: outref<'T>) =
                let mutable moved = this.source.Next(&element)
                while moved && not(this.filter.Call element) do
                    moved <- this.source.Next(&element)
                moved

    let filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> (filter: 'F) (source: 'I) =
        new Filter<'T, 'I, 'F>(source, filter)

    [<Struct; NoComparison; NoEquality>]
    type TakeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val filter: 'F
        val mutable source: 'I
        val mutable ended: bool

        new (source: 'I, filter: 'F) = { source = source; filter = filter; ended = false }

        interface iter<'T> with
            member this.Dispose() = this.source.Dispose()

            member this.RemainingCount = { this.source.RemainingCount with Lower = 0 }

            member this.Next(element: outref<'T>) =
                if not this.ended && this.source.Next(&element) then
                    let continuing = this.filter.Call element
                    this.ended <- not continuing
                    continuing
                else
                    false

    let takeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> (predicate: 'F) (source: 'I) =
        new TakeWhile<'T, 'I, 'F>(source, predicate)

    [<Struct; NoComparison; NoEquality>]
    type Choose<'T, 'U, 'I, 'C when 'I :> iter<'T> and 'C :> clo<'T, 'U voption>> =
        val internal chooser: 'C
        val mutable internal source: 'I

        new(source, chooser) = { source = source; chooser = chooser }

        interface iter<'U> with
            member this.Dispose() = this.source.Dispose()

            member this.RemainingCount = { this.source.RemainingCount with Lower = 0 }

            member this.Next(element: outref<'U>) =
                let mutable value = Unchecked.defaultof<'T>
                let mutable moved = this.source.Next(&value)
                let mutable chooserReturnValue = this.chooser.Call value

                while moved && chooserReturnValue.IsNone do
                    moved <- this.source.Next(&value)
                    chooserReturnValue <- this.chooser.Call value

                match chooserReturnValue with
                | ValueSome(value) -> element <- value
                | ValueNone -> ()

                moved

    let choose<'T, 'U, 'I, 'C when 'I :> iter<'T> and 'C :> clo<'T, 'U voption>> (chooser: 'C) (source: 'I) =
        new Choose<'T, 'U, 'I, 'C>(source, chooser)

    let iter<'T, 'I, 'A when 'I :> iter<'T> and 'A :> clo<'T, unit>> (action: 'A) (source: 'I) =
        let mutable iterator = source
        let mutable element = Unchecked.defaultof<'T>
        try
            while iterator.Next(&element) do
                action.Call element
        finally
            iterator.Dispose()

[<Struct; NoComparison; NoEquality>]
type Append<'T, 'I1, 'I2 when 'I1 :> iter<'T> and 'I2 :> iter<'T>> =
    val mutable first: 'I1
    val mutable second: 'I2
    val mutable halfway: bool

    new (first, second) = { first = first; second = second; halfway = false }

    interface iter<'T> with
        member this.Dispose() =
            this.first.Dispose()
            this.second.Dispose()

        member this.RemainingCount =
            let second = this.second.RemainingCount
            if not this.halfway then
                let first = this.first.RemainingCount
                { Lower = first.Lower + second.Lower
                  Upper =
                    match struct(first.Upper, second.Upper) with
                    | ValueSome(upper1), ValueSome(upper2) -> ValueSome(upper1 + upper2)
                    | _ -> ValueNone }
            else
                second

        member this.Next(element: outref<'T>) =
            if not this.halfway then
                if not(this.first.Next(&element)) then
                    this.halfway <- true
                    this.second.Next(&element)
                else
                    true
            else
                this.second.Next(&element)

let append<'T, 'I1, 'I2 when 'I1 :> iter<'T> and 'I2 :> iter<'T>> first second = new Append<'T, 'I1, 'I2>(first, second)

type Mapping<'T, 'U, 'I when 'I :> iter<'T>> = Struct.Mapping<'T, 'U, 'I, Struct.WrappedClosure<'T, 'U>>
let inline map mapping source = Struct.map (Struct.WrappedClosure(mapping)) source

type Filter<'T, 'I when 'I :> iter<'T>> = Struct.Filter<'T, 'I, Struct.WrappedClosure<'T, bool>>
let filter filter source = Struct.filter (Struct.WrappedClosure(filter)) source

type TakeWhile<'T, 'I when 'I :> iter<'T>> = Struct.TakeWhile<'T, 'I, Struct.WrappedClosure<'T, bool>>
let takeWhile predicate source = Struct.takeWhile (Struct.WrappedClosure(predicate)) source

let iter<'T, 'I when 'I :> iter<'T>> action (source: 'I) = Struct.iter (Struct.WrappedClosure(action)) source

let inline average<'T, 'I when 'I :> iter<'T> and 'T : (static member (+) : 'T * 'T -> 'T) and 'T : (static member Zero : 'T) and 'T : (static member (/) : 'T * 'T -> 'T) and 'T : (static member One : 'T)>
    (source: 'I) : 'T =
    let mutable average = LanguagePrimitives.GenericZero<'T>
    let mutable count = LanguagePrimitives.GenericZero<'T>
    let mutable iterator = source
    let mutable element = Unchecked.defaultof<'T>

    try
        while iterator.Next(&element) do
            average <- (^T : (static member (+) : 'T * 'T -> 'T) (average, element))
            count <- (^T : (static member (+) : 'T * 'T -> 'T) (count, LanguagePrimitives.GenericOne<'T>))
    finally
        iterator.Dispose()
    (^T : (static member op_Division : 'T * 'T -> 'T) (average, count))
