module FsIter.Tests.Program

open Expecto
open Expecto.ExpectoFsCheck
open FsIter

[<EntryPoint>]
let main argv =
    testList "all" [
        testList "map" [
            testProperty "same number of elements" <| fun (elements: int[]) (mapping: int -> int) ->
                let actual =
                    Iter.from elements
                    |> Iter.map mapping
                    |> Iter.length

                elements.Length = actual

            //testProperty "id function returns equivalent sequence"
        ]
    ]
    |> runTestsWithCLIArgs [] argv
