namespace FsIter.Benchmarks

open BenchmarkDotNet.Attributes
open FsIter

[<MemoryDiagnoser>]
type Map () =
    let mapping n = n + 42
    let nothing _ = ()

    [<Benchmark(Baseline = true)>]
    [<ArgumentsSource("Elements")>]
    member _.SeqMap(elements: int[]) =
        Seq.map mapping elements |> Seq.iter nothing

    [<Benchmark>]
    [<ArgumentsSource("Elements")>]
    member _.IterMap(elements: int[]) =
        Iter.fromSeq elements
        |> Iter.map mapping
        |> Iter.iter nothing

    [<Benchmark>]
    [<ArgumentsSource("Elements")>]
    member _.ForLoop(elements: int[]) =
        for elem in elements do
            nothing elem

    member _.Elements: int[][] =
        [|
            [||]
            [| 1 |]
            [| 1..100 |]
            [| 1..10000 |]
        |]
