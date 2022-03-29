module FsIter.Tests.Program

open System.Collections.Immutable
open Expecto
open FsIter

type Always<'T> () =
    static member val True = fun (_: 'T) -> true
    static member val False = fun (_: 'T) -> false

[<EntryPoint>]
let main argv =
    testList "all" [
        testList "map" [
            testProperty "has same length" <| fun (elements: int[]) (mapping: _ -> int) ->
                let actual = Iter.map mapping (Iter.fromArray elements)
                elements.Length = Iter.length actual

            testProperty "equivalent when identity is used" <| fun (elements: int[]) ->
                let actual = Iter.map id (Iter.fromArray elements)
                ImmutableArray.Create(items = elements) = Iter.toImmutableArray actual
        ]

        testList "filter" [
            testProperty "length less than or equal to original" <| fun (elements: int[]) (filter: _ -> bool) ->
                let actual = Iter.filter filter (Iter.fromArray elements)
                Iter.length actual <= elements.Length

            let isEven num = num % 2 = 0

            testProperty "even numbers" <| fun (elements: int[]) ->
                let expected = Seq.filter isEven elements
                let actual = Iter.filter isEven (Iter.fromArray elements)
                expected.ToImmutableArray() = Iter.toImmutableArray actual

            testProperty "empty when always false" <| fun (elements: int[]) ->
                let iterator = Iter.filter Always.True (Iter.fromArray elements)
                Iter.length iterator = 0
        ]

        testList "takeWhile" [
            testProperty "length less than or equal to original" <| fun (elements: int[]) (filter: _ -> bool) ->
                let actual = Iter.takeWhile filter (Iter.fromArray elements)
                Iter.length actual <= elements.Length

            testProperty "empty when always false" <| fun (elements: int[]) ->
                let iterator = Iter.takeWhile Always.False (Iter.fromArray elements)
                Iter.length iterator = 0

            testProperty "equivalent when always true" <| fun (elements: int[]) ->
                let actual = Iter.takeWhile Always.True (Iter.fromArray elements)
                elements.ToImmutableArray() = Iter.toImmutableArray actual

            //original sequence always starts with the takeWhile one
        ]
    ]
    |> runTestsWithCLIArgs [] argv
