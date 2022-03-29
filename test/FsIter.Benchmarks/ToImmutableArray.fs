namespace FsIter.Benchmarks

open System.Collections.Immutable
open BenchmarkDotNet.Attributes
open FsIter

[<MemoryDiagnoser>]
type ToImmutableArray () =
    [<Benchmark(Baseline = true)>]
    [<ArgumentsSource("Elements")>]
    member _.Create(elements: int[]) = ImmutableArray.Create(items = elements)

    [<Benchmark>]
    [<ArgumentsSource("Elements")>]
    member _.Iter(elements: int[]) = Iter.toImmutableArray (Iter.fromArray elements)

    member _.Elements: int[][] =
        [|
            [||]
            [| 1 |]
            [| 1..100 |]
            [| 1..1000 |]
        |]
