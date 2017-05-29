module Resource

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Creating a resource should make the data available through its property`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let instance = Resource(fileData = file)
    // assert
    test <@ instance.FileData = data @>

[<Fact>]
let ``Creating a resource will not save the file`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let instance = Resource(fileData = file)
    // assert
    test <@ instance.Entity.GetField("ResourceFileId").Data = null @>

[<Fact>]
let ``Saving a Resource will also save the file data to utility service`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    let instance = Resource(fileData = file)
    // act
    ignore <| Resource.Save instance
    let fileId = instance.Entity.GetField("ResourceFileId").Data :?> int
    // assert
    test <@ inRiverService.getFile fileId = data @>