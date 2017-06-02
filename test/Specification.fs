module Specification

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Can set specification name, oh my god! why is this not mandatory!!!!!?!?!?!!!oneone`` () =
    // arrange
    let specification = pim.Specification()
    // act
                        |> set (fun s -> s.Name <- Some "Technical Details")
    // assert
    test <@ specification.Name = Some "Technical Details" @>