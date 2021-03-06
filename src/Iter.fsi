/// Contains functions for performing operations on enumerators.
[<RequireQualifiedAccess>]
module FsIter.Iter

open System
open System.Collections.Generic
open System.Runtime.CompilerServices

[<IsReadOnly; Struct>]
type CountEstimate =
    { Lower: int
      Upper: int voption }

    static member Default : CountEstimate

/// Iterates over the elements of a sequence.
[<Interface>]
type Iterator<'T> =
    inherit IDisposable

    /// <summary>Gets the next element in the sequence.</summary>
    /// <returns>
    /// <see langword="true"/> if an element was successfully retrieved; otherwise, <see langword="false"/> if the end of the
    /// sequence was reached.
    /// </returns>
    abstract member Next : element: outref<'T> -> bool

    /// Gets a lower and upper estimate of the remaining number of elements in the sequence.
    abstract member RemainingCount : CountEstimate

type iter<'T> = Iterator<'T>

[<Struct; NoComparison; NoEquality>]
type SeqIterator<'T, 'E when 'E :> IEnumerator<'T>> =
    val mutable internal inner: 'E

    new : 'E -> SeqIterator<'T, 'E>

    interface iter<'T>

/// <summary>
/// Creates an iterator from the <param name="source"/>. It is recommended to use this function by suppling the value returned by
/// calling <see cref="M:System.Collections.Generic.IEnumerable`1.GetEnumerator()"/> on the source sequence.
/// </summary>
val inline fromEnumerator<'T, 'E when 'E :> IEnumerator<'T>> : source: 'E -> SeqIterator<'T, 'E>

/// <summary>
/// Gets an iterator used to iterate over the items in the <param name="source"/> sequence.
/// Note that this allocates a <see cref="T:System.Collections.Generic.IEnumerator`1"/>, so consider one of the more specialized
/// constructor functions.
/// </summary>
val inline fromSeq<'T, 'C when 'C :> seq<'T>> : source: 'C -> SeqIterator<'T, IEnumerator<'T>>

/// <summary>
/// Gets an iterator over the elements of a <see cref="T:System.Collections.Generic.List`1"/>.
/// </summary>
/// <exception cref="T:System.ArgumentNullException"/>
val fromResizeArray<'T> : source: List<'T> -> SeqIterator<'T, List<'T>.Enumerator> // TODO: Return iterate with size estimate

[<Struct; NoComparison; NoEquality>]
type StringCharIterator =
    val mutable internal next: int
    val mutable internal string: string

    new : string -> StringCharIterator

    interface iter<char>

/// Gets an iterator over the characters of a string.
val inline fromStringChars : source: string -> StringCharIterator

//[<Struct; NoComparison; NoEquality>]
//type StringRuneIterator =
//    val mutable internal source: System.Text.StringRuneEnumerator

[<Struct; NoComparison; NoEquality>]
type ArrayIterator<'T> =
    val internal array: 'T[]
    val mutable internal index: int32

    new : array: 'T[] -> ArrayIterator<'T>

    interface iter<'T>

/// <summary>Gets an iterator used to iterate over the items in the <param name="source"/> array.</summary>
val inline fromArray<'T> : source: 'T[] -> ArrayIterator<'T>



/// <summary>
/// Returns the number of elements returned by the <param name="source"/> iterator as a <see cref="T:System.Int32"/>.
/// </summary>
val length<'T, 'I when 'I :> iter<'T>> : source: 'I -> int32

/// <summary>
/// Appends the elements returned by the <param name="source"/> iterator to the specified <param name="collection"/>.
/// </summary>
val appendToCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'I :> iter<'T>> : collection: 'C -> source: 'I -> unit

/// <summary>Returns a collection containing the elements returned by the <param name="source"/> iterator.</summary>
val inline toCollection<'C, 'T, 'I when 'C :> ICollection<'T> and 'C : (new : unit -> 'C) and 'I :> iter<'T>> : source: 'I -> 'C

/// <summary>
/// Returns a <see cref="T:System.Collections.Generic.List`1"/> containing the elements returned by the <param name="source"/>
/// iterator.
/// </summary>
val toResizeArray<'T, 'I when 'I :> iter<'T>> : source: 'I -> List<'T>

/// <summary>
/// Returns an <see cref="T:System.Collections.Immutable.ImmutableArray`1"/> containing the elements returned by the
/// <param name="source"/> iterator.
/// </summary>
val toImmutableArray<'T, 'I when 'I :> iter<'T>> : source: 'I -> System.Collections.Immutable.ImmutableArray<'T>

/// <summary>
/// Returns an array containing the elements returned by the <param name="source"/> iterator.
/// </summary>
val toArray<'T, 'I when 'I :> iter<'T>> : source: 'I -> 'T[]

/// <summary>
/// Returns a <see cref="T:System.Text.StringBuilder"/> containing the characters returned by the <param name="source"/>
/// iterator.
/// </summary>
val toStringBuilder<'I when 'I :> iter<char>> : source: 'I -> System.Text.StringBuilder

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

    [<Struct; NoComparison; NoEquality>]
    type Mapping<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> =
        val mutable internal source: 'I
        val internal mapping: 'M
        interface iter<'U>

    val map<'T, 'U, 'I, 'M when 'I :> iter<'T> and 'M :> clo<'T, 'U>> : mapping: 'M -> source: 'I -> Mapping<'T, 'U, 'I, 'M>

    [<Struct; NoComparison; NoEquality>]
    type Filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val mutable internal source: 'I
        val internal filter: 'F
        interface iter<'T>

    val filter<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> : filter: 'F -> source: 'I -> Filter<'T, 'I, 'F>

    [<Struct; NoComparison; NoEquality>]
    type TakeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> =
        val internal filter: 'F
        val mutable internal source: 'I
        val mutable internal ended: bool
        interface iter<'T>

    val takeWhile<'T, 'I, 'F when 'I :> iter<'T> and 'F :> clo<'T, bool>> : predicate: 'F -> source: 'I -> TakeWhile<'T, 'I, 'F>

    // TODO: Could optimize by using Nullable for small structs for Choose

    [<Struct; NoComparison; NoEquality>]
    type Choose<'T, 'U, 'I, 'C when 'I :> iter<'T> and 'C :> clo<'T, 'U voption>> =
        val internal chooser: 'C
        val mutable internal source: 'I

        interface iter<'U>

    val choose<'T, 'U, 'I, 'C when 'I :> iter<'T> and 'C :> clo<'T, 'U voption>> : chooser: 'C -> source: 'I -> Choose<'T, 'U, 'I, 'C>

    val iter<'T, 'I, 'A when 'I :> iter<'T> and 'A :> clo<'T, unit>> : action: 'A -> source: 'I -> unit

[<Struct; NoComparison; NoEquality>]
type Append<'T, 'I1, 'I2 when 'I1 :> iter<'T> and 'I2 :> iter<'T>> =
    val mutable internal first: 'I1
    val mutable internal second: 'I2
    val mutable internal halfway: bool

    interface iter<'T>

/// Appends two iterators together.
val append<'T, 'I1, 'I2 when 'I1 :> iter<'T> and 'I2 :> iter<'T>> : first: 'I1 -> second: 'I2 -> Append<'T, 'I1, 'I2>

type Mapping<'T, 'U, 'I when 'I :> iter<'T>> = Struct.Mapping<'T, 'U, 'I, Struct.WrappedClosure<'T, 'U>>

/// <summary>
/// Returns an enumerator whose elements are the results of applying the <param name="mapping"/> function the elements returned
/// by the <param name="source"/> enumerator.
/// </summary>
val inline map<'T, 'U, 'I when 'I :> iter<'T>> : mapping: ('T -> 'U) -> source: 'I -> Mapping<'T, 'U, 'I>

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

// TODO: Return voption if sequence is empty.
val inline average<'T, 'I when 'I :> iter<'T> and 'T : (static member (+) : 'T * 'T -> 'T) and 'T : (static member Zero : 'T) and 'T : (static member (/) : 'T * 'T -> 'T) and 'T : (static member One : 'T)>
    : source: 'I -> 'T
