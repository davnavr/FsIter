namespace FsIter.Benchmarks

open BenchmarkDotNet.Attributes
open FsIter

[<MemoryDiagnoser>]
type Map () =
    let mapping = fun n -> n + 42
    let mutable sink = 0
    let action = fun n -> sink <- n

    [<Benchmark(Baseline = true)>]
    [<ArgumentsSource("Elements")>]
    member _.SeqMap(elements: int[]) =
        Seq.map mapping elements |> Seq.iter action

    [<Benchmark>]
    [<ArgumentsSource("Elements")>]
    member _.IterMap(elements: int[]) =
        Iter.fromArray elements
        |> Iter.map mapping
        |> Iter.iter action

    //[<Benchmark>]
    //[<ArgumentsSource("Elements")>]
    //member _.ForLoop(elements: int[]) =
    //    for elem in elements do
    //        nothing elem

    member _.Elements: int[][] =
        [|
            [||]
            [| 1 |]
            [| 1..100 |]
            [| 1..1000 |]
        |]
