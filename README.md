# FsIter
[![Project Status: Abandoned – Initial development has started, but there has not yet been a stable, usable release; the project has been abandoned and the author(s) do not intend on continuing development.](https://www.repostatus.org/badges/latest/abandoned.svg)](https://www.repostatus.org/#abandoned)
Developed has stopped until further notice as preliminary benchmarks show that built-in functions such as `Array.filter` are faster than what `FsIter` is able to provide.

F# library for operating on enumerators in a slightly faster and rust-like way

```fsharp
open FsIter

// Only allocations here is the array and closures, no seq objects are created.
[| 5; 2; 7; 8; 9; 10; 0; 2 |]
|> Iter.fromArray
|> Iter.map (fun num -> num + 1)
|> Iter.filter (fun num -> num > 2)
|> Iter.iter (printfn "%i")
```

`FsIter` makes operations on enumerators, and by extension, sequences faster by avoiding allocations on the heap. This is
possible thanks to the pervasive use of generic structs, which may result in more code being generated at runtime. As always, be
sure to benchmark your code to see if there is any improvement.

Currently, the most basic of benchmarks show that `FsIter` works best for sequences with a large number of elements.
