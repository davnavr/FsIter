module FsIter.Benchmarks.Program

type Marker = struct end

[<EntryPoint>]
let main argv =
    BenchmarkDotNet.Running.BenchmarkSwitcher.FromAssembly(typeof<Marker>.Assembly).Run(argv) |> ignore
    0
