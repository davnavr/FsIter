module FsIter.Tests.Program

open System.Collections.Immutable
open Expecto
open FsIter

[<EntryPoint>]
let main argv =
    testList "all" [
        testList "map" [
            testProperty "has same length" <| fun (elements: int[]) (mapping: _ -> int) ->
                let actual = Iter.map mapping (Iter.from elements)
                elements.Length = Iter.length actual

            testProperty "equivalent when identity is used" <| fun (elements: int[]) ->
                let expected = ImmutableArray.Create(items = elements)
                let mutable actual = ImmutableArray.CreateBuilder(elements.Length)

                Iter.from elements
                |> Iter.map id
                |> Iter.appendToCollection actual

                expected = actual.ToImmutable()
        ]

        testList "filter" [
            testProperty "length less than or equal to original" <| fun (elements: int[]) (filter: _ -> bool) ->
                let actual = Iter.filter filter (Iter.from elements)
                Iter.length actual <= elements.Length

            testProperty "even numbers" <| fun (elements: int[]) ->
                let isEven num = num % 2 = 0
                let expected = (Seq.filter isEven elements).ToImmutableArray()
                let mutable actual = ImmutableArray.CreateBuilder(elements.Length)

                Iter.from(elements)
                |> Iter.filter isEven
                |> Iter.appendToCollection actual

                expected = actual.ToImmutable()
        ]
    ]
    |> runTestsWithCLIArgs [] argv
