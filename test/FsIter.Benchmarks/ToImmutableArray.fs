namespace FsIter.Benchmarks

open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open BenchmarkDotNet.Attributes
open FsIter

[<MemoryDiagnoser>]
type ToImmutableArray () =
    [<Benchmark(Baseline = true)>]
    [<ArgumentsSource("Elements")>]
    member _.ToImmutableArray(elements: List<int>) = elements.ToImmutableArray()

    [<Benchmark>]
    [<ArgumentsSource("Elements")>]
    member _.Iter(elements: List<int>) = Iter.toImmutableArray (Iter.fromArrayList elements)

    member _.Elements: List<int>[] =
        [|
            List();
            (Seq.singleton 1).ToList()
            [| 1..100 |].ToList()
            [| 1..1000 |].ToList()
        |]
