# FsIter
F# library for operating on enumerators in a slightly faster and rust-like way

```fsharp
open FsIter

// Only allocations here is the list and closures, no seq objects are created.
[ 5; 2; 7; 8; 9; 10; 0; 2 ]
|> Iter.from
|> Iter.map (fun num -> num + 1)
|> Iter.filter (fun num -> num > 2)
|> Iter.iter (printfn "%i")
```

`FsIter` makes operations on enumerators, and by extension, sequences faster by avoiding allocations on the heap. This is
possible thanks to the pervasive use of generic structs, which may result in more code being generated at runtime. As always, be
sure to benchmark your code to see if there is any improvement.
