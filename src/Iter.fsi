/// Contains functions for performing operations on sequences.
[<RequireQualifiedAccess>]
module FsIter.Iter

// TODO: How to ensure struct enumerators are used if possible?

// TODO: Move 'T and 'U type parameters before all others.

/// Variants of functions to perform operations on sequences without allocating closures.
module Struct =
    [<Interface>]
    type IClosure<'I, 'O> =
        abstract member Call : 'I -> 'O

    /// Custom interface type used to represent closures. Since F# closures are always instances of classes allocated on the heap,
    /// this interface allows "allocation of closures" on the stack with value types.
    type clo<'I, 'O> = IClosure<'I, 'O>

    /// <summary>
    /// Enables interoperation between <see cref="T:FsIter.Iter.IClosure`2"/> and actual F# closures.
    /// </summary>
    [<Struct>]
    type WrappedClosure<'I, 'O> =
        new : ('I -> 'O) -> WrappedClosure<'I, 'O>

        interface clo<'I, 'O>

    [<Struct>]
    type Mapping<'I, 'T, 'U, 'M when 'I :> seq<'T> and 'M :> clo<'T, 'U>> =
        interface seq<'U>

    val map<'I, 'T, 'U, 'M when 'I :> seq<'T> and 'M :> clo<'T, 'U>> : mapping: 'M -> source: 'I -> Mapping<'I, 'T, 'U, 'M>

    [<Struct>]
    type Filter<'I, 'T, 'F when 'I :> seq<'T> and 'F :> clo<'T, bool>> =
        interface seq<'T>

    val filter<'I, 'T, 'F when 'I :> seq<'T> and 'F :> clo<'T, bool>> : filter: 'F -> source: 'I -> Filter<'I, 'T, 'F>

    val over<'I, 'T, 'A when 'I :> seq<'T> and 'A :> clo<'T, unit>> : action: 'A -> source: 'I -> unit

type Mapping<'I, 'T, 'U when 'I :> seq<'T>> = Struct.Mapping<'I, 'T, 'U, Struct.WrappedClosure<'T, 'U>>

/// <summary>
/// Returns a sequence whose elements are the results of applying the <param name="mapping"/> function the elements of the
/// <param name="source"/> sequence.
/// </summary>
val map<'I, 'T, 'U when 'I :> seq<'T>> : mapping: ('T -> 'U) -> source: 'I -> Mapping<'I, 'T, 'U>

type Filter<'I, 'T when 'I :> seq<'T>> = Struct.Filter<'I, 'T, Struct.WrappedClosure<'T, bool>>

/// <summary>
/// Returns a sequence containing only the elements in the <param name="source"/> sequence for which the <param name="filter"/>
/// function returned <see langword="true"/> for.
/// </summary>
val filter<'I, 'T when 'I :> seq<'T>> : filter: ('T -> bool) -> source: 'I -> Filter<'I, 'T>
