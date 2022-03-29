/// Contains functions for performing operations on enumerators.
[<RequireQualifiedAccess>]
module FsIter.Iter

open System.Collections.Generic
open System.Runtime.CompilerServices

type iter<'T> = IEnumerator<'T>

(*
[<Interface>]
type Iterator<'T> =
    abstract member Next : element: outref<'T> -> bool

    abstract member RemainingCount : struct(int, int voption)
*)

// TODO: Maybe an inline function could call correct GetEnumerator?
// TODO: How to ensure struct enumerators are used if possible?

/// <summary>
/// Gets an enumerator used to iterate over the items in the <param name="source"/> sequence.
/// Note that this allocates an iterator, consider explicitly calling
/// <see cref="M:System.Collections.Generic.IEnumerator`2.GetEnumerator()`"/> on the collection if a struct enumerator is
/// available, or using one of the more specialized constructor functions.
/// </summary>
val fromSeq<'T, 'C when 'C :> seq<'T>> : source: 'C -> iter<'T>

[<Struct>]
type ArrayIterator<'T> =
    val internal array: 'T[]
    val mutable internal index: int32

    new : array: 'T[] -> ArrayIterator<'T>

    interface iter<'T>

/// <summary>Gets an enumerator used to iterate over the items in the <param name="source"/> array.</summary>
val inline fromArray<'T> : source: 'T[] -> ArrayIterator<'T>

/// <summary>
/// Returns the number of elements returned by the <param name="source"/> enumerator as a <see cref="T:System.Int32"/>.
/// </summary>
val length<'T, 'I when 'I :> iter<'T>> : source: 'I -> int32

/// <summary>
/// Appends the elements returned by the <param name="source"/> enumerator to the specified <param name="collection"/>.
/// </summary>
val appendToCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'I :> iter<'T>> : collection: 'C -> source: 'I -> unit

/// <summary>Returns a collection containing the elements returned by the <param name="source"/> enumerator.</summary>
val inline toCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'C : (new : unit -> 'C) and 'I :> iter<'T>> : source: 'I -> 'C

/// <summary>
/// Returns a <see cref="T:System.Collections.Generic.List`1"/> containing the elements returned by the enumerator.
/// </summary>
val inline toArrayList<'T, 'I when 'I :> iter<'T>> : source: 'I -> List<'T>

/// <summary>
/// Returns an <see cref="T:System.Collections.Immutable.ImmutableArray`1"/> containing the elements returned by the enumerator.
/// </summary>
val toImmutableArray<'T, 'I when 'I :> iter<'T>> : source: 'I -> System.Collections.Immutable.ImmutableArray<'T>

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
    [<IsReadOnly; Struct; NoEquality; NoComparison>]
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
        interface iter<'T>

    val filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> : filter: 'F -> source: 'I -> Filter<'T, 'I, 'F>

    [<Struct>]
    type TakeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val mutable internal source: 'I
        val mutable internal filter: 'F
        val mutable internal ended: bool
        interface iter<'T>

    val takeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> : predicate: 'F -> source: 'I -> TakeWhile<'T, 'I, 'F>

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

type TakeWhile<'T, 'I when 'I :> iter<'T>> = Struct.TakeWhile<'T, 'I, Struct.WrappedClosure<'T, bool>>
val takeWhile<'T, 'I when 'I :> iter<'T>> : predicate: ('T -> bool) -> source: 'I -> TakeWhile<'T, 'I>

/// <summary>Consumes the <param name="source"/> enumerator, applying the given <param name="action"/> to each element.</summary>
val iter<'T, 'I when 'I :> iter<'T>> : action: ('T -> unit) -> source: 'I -> unit
