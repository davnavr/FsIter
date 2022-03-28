# FsIter
F# library for operating on sequences in a slightly faster and rust-like way

```fsharp
open FsIter

// Only allocations here is the list and closures, no seq objects are created.
[ 5; 2; 7; 8; 9; 10; 0; 2 ]
|> Iter.map (fun num -> num + 1)
|> Iter.filter (fun num -> num > 2)
|> Iter.over (printfn "%i")
```
