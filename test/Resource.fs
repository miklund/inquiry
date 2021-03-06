﻿module Resource

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
    let file = NewFile ("test.dat", data)
    // act
    let instance = pim.Resource(fileData = file)
    // assert
    test <@ instance.FileData =  Some (NewFile ("test.dat", data)) @>

[<Fact>]
let ``Creating a resource will not save the file`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("test.dat", data)
    // act
    let instance = pim.Resource(fileData = file)
    // assert
    test <@ instance.Entity.GetField("ResourceFileId").Data = null @>

[<Fact>]
let ``Saving a Resource will also save the file data to utility service`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("test.dat", data)
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
    let file = NewFile ("test.dat", data)
    // act
    let resource = pim.Resource(file, Name = Some "Test data")
    // assert
    test <@ resource.Name = Some "Test data" @>

[<Fact>]
let ``Resource filename is set from constructor`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("test.dat", data)
    // act
    let resource = pim.Resource(file, Filename = Some "test.dat")
    // assert
    test <@ resource.Filename = Some "test.dat" @>

[<Fact>]
let ``Can set the resource mime type`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("test.dat", data)
    // act
    let resource = pim.Resource(file)
                   |> set (fun r -> r.MimeType <- Some "base64/text")
    // assert
    test <@ resource.MimeType = Some "base64/text" @>

[<Fact>]
let ``Can set the resource description`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("test.dat", data)
    // act
    let resource = pim.Resource(file)
                   |> set (fun r -> r.Description <- Some "This is a test file!")
    // assert
    test <@ resource.Description = Some "This is a test file!" @>

[<Fact>]
let ``Can set the resource image map`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("test.dat", data)
    // act
    let resource = pim.Resource(file, ImageMap = Some "test.dat.map")
    // assert
    test <@ resource.ImageMap = Some "test.dat.map" @>

[<Fact>]
let ``Can set the media type to sketch`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("first.dat", data)
    // act
    let resource = pim.Resource(file, MediaType = Some pim.MediaType.sketch)
    // assert
    test <@ resource.MediaType = Some pim.MediaType.sketch @>


//
// Needed to test the File set property, but these no longer work because ResourceFileId is read-only
// Might remove these tests unless we need them again.
//

//[<Fact>]
//let ``Can update the file property with a new file`` () =
//    // arrange
//    let data1 = [| for i in [65..74] -> byte(i) |]
//    let data2 = [| for i in [75..84] -> byte(i) |]
//    let file = New ("first.dat", data1)
//    let resource = pim.Resource(file)
//    // act
//                   |> set (fun r -> r.FileData <- Some (New ("second.dat", data2)))
//    // assert
//    test <@ resource.FileData = Some (New ("second.dat", data2)) @>

//[<Fact>]
//let ``After updating the file and saving, the fileId will be updated on entity`` () =
//    // arrange
//    let data1 = [| for i in [65..74] -> byte(i) |]
//    let data2 = [| for i in [75..84] -> byte(i) |]
//    let resource1 = match pim.Resource(New ("first.dat", data1)) |> pim.Resource.save with
//                    | Ok resource -> resource
//                    | Error e -> failwith e.Message
//    // act
//    let resource2 = match resource1 |> set (fun r -> r.FileData <- Some (New ("second.dat", data2))) |> pim.Resource.save with
//                    | Ok resource -> resource
//                    | Error e -> failwith e.Message
//    // assert
//    test <@ resource1.Entity.GetField("ResourceFileId") <> resource2.Entity.GetField("ResourceFileId") @>

//[<Fact>]
//let ``Should remove orphaned files after exchanging a file on a unique field`` () =
//    // arrange
//    let data1 = [| for i in [65..74] -> byte(i) |]
//    let data2 = [| for i in [75..84] -> byte(i) |]
//    let resource1 = match pim.Resource(New ("first.dat", data1)) |> pim.Resource.save with
//                    | Ok resource -> resource
//                    | Error e -> failwith e.Message
//    // act
//    ignore <| (resource1 |> set (fun r -> r.FileData <- Some (New ("second.dat", data2))) |> pim.Resource.save)

//    // assert
//    test <@ null = inRiverService.getFile (resource1.Entity.GetField("ResourceFileId").Data :?> int) @>

[<Fact>]
let ``Can save a new resource and read back its file`` () =
    // arrange
    let data = [| for i in [65..74] -> byte(i) |]
    let file = NewFile ("first.dat", data)
    // act
    let resource1 =
        match pim.Resource(file) |> pim.Resource.save with
        | Ok resource -> resource
        | Error e -> failwith e.Message
    // assert
    match pim.Resource.get (resource1.Id) with
    | Ok resource ->
        match resource.FileData with
        | Some (PersistedFile persistedFileData) -> test <@ persistedFileData = data @>
        | _ -> failwith "Failed to store file data in PIM"
    | Error _ -> failwith "Saved resource was not found"

[<Fact>]
let ``Can get resource by filename`` () =
    // act
    match pim.Resource.getByFilename "A001001_1_2.jpg" with
    // assert
    | Ok resource -> test <@ resource.Id = 178 @>
    | Error e -> failwith e.Message

[<Fact>]
let ``Can get resource by file id`` () =
    // act
    match pim.Resource.getByFileId 1 with
    // assert
    | Ok resource -> test <@ resource.Id = 178 @>
    | Error e -> failwith e.Message