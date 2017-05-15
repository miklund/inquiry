module Bundle

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Should be able to create a new instance of Bundle and set the name`` () =
    // arrange
    let bundleName = "My new Bundle"
    // act
    let instance = Bundle(bundleName)
    // assert
    test <@ instance.Name = bundleName @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word bundle and use camel case`` () =
    // act
    let instance = Bundle(name = "The bundle name")
    // assert
    test <@ instance.GetType() = typeof<Product> @>