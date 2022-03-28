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
                let actual = Iter.map id (Iter.from elements)
                ImmutableArray.Create(items = elements) = Iter.toImmutableArray actual
        ]

        testList "filter" [
            testProperty "length less than or equal to original" <| fun (elements: int[]) (filter: _ -> bool) ->
                let actual = Iter.filter filter (Iter.from elements)
                Iter.length actual <= elements.Length

            let isEven num = num % 2 = 0

            testProperty "even numbers" <| fun (elements: int[]) ->
                let expected = Seq.filter isEven elements
                let actual = Iter.filter isEven (Iter.from elements)
                expected.ToImmutableArray() = Iter.toImmutableArray actual

            let alwaysFalse (_: int) = false

            testProperty "empty when always false" <| fun (elements: int[]) ->
                let iterator = Iter.filter alwaysFalse (Iter.from elements)
                Iter.length iterator = 0
        ]
    ]
    |> runTestsWithCLIArgs [] argv
