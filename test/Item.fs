module Item

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to change the item number`` () =
    // arrange
    let item = pim.Item(number = "ABC123", sizeXML = "<size/>")
    // act
               |> set (fun i -> i.Number <- Some "BCD234")
    // assert
    test <@ item.Number = Some "BCD234" @>

[<Fact>]
let ``Should be able to change the fashion size`` () =
    // arrange
    let item = pim.Item("ABC123", "<size/>", FashionSize = Some "Smallest")
    // act
               |> set (fun i -> i.FashionSize <- Some "Big and Beautiful")
    // assert
    test <@ item.FashionSize = Some "Big and Beautiful" @>
    
[<Fact>]
let ``Default value of fashion weight should be none, meaning not set`` () =
    // act
    let item = pim.Item("ABC123", "<size/>")
    // assert
    test <@ item.FashionWeight = None @>

[<Fact>]
let ``Should be able to set fashion weight to a value`` () =
    // arrange
    let item = pim.Item("ABC123", "<size/>")
    // act
               |> set (fun i -> i.FashionWeight <- Some 0.5)
    // assert
    test <@ item.FashionWeight = Some 0.5 @>

[<Fact>]
let ``Should be able to set fashion weight to None`` () =
    // arrange
    let item = pim.Item("ABC123", "<size/>", FashionWeight = Some 0.5)
    // act
               |> set (fun i -> i.FashionWeight <- None)
    // assert
    test <@ item.FashionWeight = None @>