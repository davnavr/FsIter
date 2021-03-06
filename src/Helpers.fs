namespace FsIter.Helpers

open System
open System.Collections.Immutable
open System.Runtime.CompilerServices

[<IsByRefLike; Struct; NoComparison; NoEquality>]
type internal ArrayBuilder<'T> =
    val mutable buffer : 'T[]
    val mutable count : int

    new (capacity: int) =
        { buffer = if capacity > 0 then Array.zeroCreate capacity else Array.Empty()
          count = 0 }

    member this.Capacity = this.buffer.Length

    member inline private this.EnsureBufferExists() =
        if this.buffer.Length = 0 then
            this.buffer <- Array.zeroCreate 1

    member this.Add(item: 'T) =
        this.EnsureBufferExists()

        if this.buffer.Length = this.count then
            Array.Resize(&this.buffer, this.buffer.Length * 2)

        this.buffer.[this.count] <- item
        this.count <- this.count + 1

    member this.ToArray() =
        let length = this.count
        let mutable buffer = this.buffer

        this.count <- 0
        this.buffer <- Array.Empty()

        if length < buffer.Length then
            Array.Resize(&buffer, length)

        buffer

    member this.ToImmutableArray() =
        // Safe, since the only place where the array will be referenced is in the returned ImmutableArray instance.
        let mutable array = this.ToArray()
        Unsafe.As<'T[], ImmutableArray<'T>>(&array)
