namespace FsIter.Benchmarks

open BenchmarkDotNet.Attributes
open FsIter

module Helpers =
    let multipleOfThree n = n % 3 = 0

[<MemoryDiagnoser>]
type Filter () =
    [<Benchmark(Baseline = true)>]
    [<ArgumentsSource("Elements")>]
    member _.Array(elements: int[]) = Array.filter Helpers.multipleOfThree elements

    [<Benchmark>]
    [<ArgumentsSource("Elements")>]
    member _.Iter(elements: int[]) =
        Iter.fromArray elements
        |> Iter.filter Helpers.multipleOfThree
        |> Iter.toArray

    
    [<Benchmark>]
    [<ArgumentsSource("Elements")>]
    member _.Seq(elements: int[]) = // Might not be a good comparison to make
        Array.toSeq elements
        |> Seq.filter Helpers.multipleOfThree
        |> Seq.toArray

    member _.Elements: int[][] =
        [|
            [||]
            [| 3 |]
            [| 1..10 |]
            [| 1..100 |]
        |]
