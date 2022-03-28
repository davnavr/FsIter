/// Contains functions for performing operations on enumerators.
[<RequireQualifiedAccess>]
module FsIter.Iter

type iter<'T> = System.Collections.Generic.IEnumerator<'T>

(*
[<Interface>]
type Iterator<'T> =
    abstract member Next : element: outref<'T> -> bool
*)

// TODO: Maybe an inline function could call correct GetEnumerator?
// TODO: How to ensure struct enumerators are used if possible?

/// <summary>
/// Gets an enumerator used to iterate over the items in the <param name="source"/> collection.
/// Note that this allocates an iterator, consider explicitly calling
/// <see cref="M:System.Collections.Generic.IEnumerator`2.GetEnumerator()`"/> on the collection if a struct enumerator is
/// available.
/// </summary>
val from<'T, 'C when 'C :> seq<'T>> : source: 'C -> iter<'T>

/// <summary>Returns the number of elements returned by the <param name="source"/> enumerator as a <see cref="T:System.UInt64"/>.</summary>
val longerLength<'T, 'I when 'I :> iter<'T>> : source: 'I -> uint64

/// <summary>Returns the number of elements returned by the <param name="source"/> enumerator as a <see cref="T:System.Int32"/>.</summary>
val length<'T, 'I when 'I :> iter<'T>> : source: 'I -> int32

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
    [<Struct; NoEquality; NoComparison>]
    type WrappedClosure<'I, 'O> =
        new : closure: ('I -> 'O) -> WrappedClosure<'I, 'O>

        interface clo<'I, 'O>

    [<Struct>]
    type Mapping<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> =
        val mutable internal source: 'I
        val mutable internal mapping: 'M

        interface iter<'U>

    val map<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> : mapping: 'M -> source: 'I -> Mapping<'T, 'U, 'I, 'M>

    [<Struct>]
    type Filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val mutable internal source: 'I
        val mutable internal filter: 'F
        val mutable internal ended: bool

        interface iter<'T>

    val filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> : filter: 'F -> source: 'I -> Filter<'T, 'I, 'F>

    val iter<'T, 'I, 'A when 'I :> iter<'T> and 'A :> clo<'T, unit>> : action: 'A -> source: 'I -> unit

type Mapping<'T, 'U, 'I when 'I :> iter<'T>> = Struct.Mapping<'T, 'U, 'I, Struct.WrappedClosure<'T, 'U>>

/// <summary>
/// Returns an enumerator whose elements are the results of applying the <param name="mapping"/> function the elements returned
/// by the <param name="source"/> enumerator.
/// </summary>
val map<'T, 'U, 'I when 'I :> iter<'T>> : mapping: ('T -> 'U) -> source: 'I -> Mapping<'T, 'U, 'I>

type Filter<'T, 'I when 'I :> iter<'T>> = Struct.Filter<'T, 'I, Struct.WrappedClosure<'T, bool>>

/// <summary>
/// Returns a sequence containing only the elements in the <param name="source"/> sequence for which the <param name="filter"/>
/// function returned <see langword="true"/> for.
/// </summary>
val filter<'T, 'I when 'I :> iter<'T>> : filter: ('T -> bool) -> source: 'I -> Filter<'T, 'I>

/// <summary>Consumes the <param name="source"/> enumerator, applying the given <param name="action"/> to each element.</summary>
val iter<'T, 'I when 'I :> iter<'T>> : action: ('T -> unit) -> source: 'I -> unit
