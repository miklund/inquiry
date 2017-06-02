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
    test <@ instance.Name = Some bundleName @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word bundle and use camel case`` () =
    // act
    let instance = pim.Bundle(name = "The bundle name")
    // assert
    test <@ instance.GetType() = typeof<pim.Product> @>

[<Fact>]
let ``Should be able to set bundle description`` () =
    // arrange
    let bundle = pim.Bundle("Buy a bike get a head")
    // act
                 |> set (fun b -> b.Description <- [("en", "Buy bike, get head"); ("sv", "Köp cykel, kom först")] |> Map.ofList)
    // assert
    test <@ bundle.Description |> Map.find "en" = "Buy bike, get head" @>
    test <@ bundle.Description |> Map.find "sv" = "Köp cykel, kom först" @>