module Bundle

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to create a new instance of Bundle and set the name`` () =
    // arrange
    let bundleName = "My new Bundle"
    // act
    let instance = pim.Bundle(bundleName)
    // assert
    test <@ instance.Name = bundleName @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word bundle and use camel case`` () =
    // act
    let instance = pim.Bundle(name = "The bundle name")
    // assert
    test <@ instance.GetType() = typeof<pim.Product> @>