namespace FsIter.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

[<MemoryDiagnoser>]
type Map =
    let items = [|  |]
