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
        Iter.from elements
        |> Iter.map mapping
        |> Iter.iter nothing

    member _.Elements: int[][] =
        [|
            [||]
            [| 1 |]
            [| 1..100 |]
            [| 1..1000 |]
            [| 1..10000 |]
        |]
