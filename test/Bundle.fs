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
    test <@ instance.BundleName = Some bundleName @>