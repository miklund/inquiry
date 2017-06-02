module Assortment

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to set assortment name from constructor as a property expression`` () =
    // arrange
    let assortmentName = "My assortment"
    // act
    let assortment = pim.Assortment(Name = Some assortmentName)
    // assert
    test <@ assortment.Name = Some assortmentName @>