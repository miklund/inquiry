module Resource

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Creating a resource should make the data available through its property`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let instance = pim.Resource(fileData = file)
    // assert
    test <@ instance.FileData = data @>

[<Fact>]
let ``Creating a resource will not save the file`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let instance = pim.Resource(fileData = file)
    // assert
    test <@ instance.Entity.GetField("ResourceFileId").Data = null @>

[<Fact>]
let ``Saving a Resource will also save the file data to utility service`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    let instance = pim.Resource(fileData = file)
    // act
    ignore <| pim.Resource.save instance
    let fileId = instance.Entity.GetField("ResourceFileId").Data :?> int
    // assert
    test <@ inRiverService.getFile fileId = data @>

[<Fact>]
let ``Can set resource name`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let resource = pim.Resource(file, Name = Some "Test data")
    // assert
    test <@ resource.Name = Some "Test data" @>

[<Fact>]
let ``Resource filename is set from constructor`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let resource = pim.Resource(file, Filename = Some "test.dat")
    // assert
    test <@ resource.Filename = Some "test.dat" @>

[<Fact>]
let ``Can set the resource mime type`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let resource = pim.Resource(file)
                   |> set (fun r -> r.MimeType <- Some "base64/text")
    // assert
    test <@ resource.MimeType = Some "base64/text" @>

[<Fact>]
let ``Can set the resource description`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let resource = pim.Resource(file)
                   |> set (fun r -> r.Description <- Some "This is a test file!")
    // assert
    test <@ resource.Description = Some "This is a test file!" @>

[<Fact>]
let ``Can set the resource image map`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = New ("test.dat", data)
    // act
    let resource = pim.Resource(file, ImageMap = Some "test.dat.map")
    // assert
    test <@ resource.ImageMap = Some "test.dat.map" @>