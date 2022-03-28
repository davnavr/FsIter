module FsIter.Tests.Program

open System.Collections.Immutable
open Expecto
open Expecto.ExpectoFsCheck
open FsIter

[<EntryPoint>]
let main argv =
    testList "all" [
        testList "map" [
            testProperty "same length" <| fun (elements: int[]) (mapping: int -> int) ->
                let actual =
                    Iter.from elements
                    |> Iter.map mapping
                    |> Iter.length

                elements.Length = actual

            testProperty "mapping with id returns equivalent sequence" <| fun (elements: int[]) ->
                let expected = ImmutableArray.Create(items = elements)
                let mutable actual = ImmutableArray.CreateBuilder(elements.Length)

                Iter.from elements
                |> Iter.map id
                |> Iter.appendToCollection actual

                expected = actual.MoveToImmutable()
        ]
    ]
    |> runTestsWithCLIArgs [] argv
