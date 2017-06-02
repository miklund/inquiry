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
let ``Should be  able to change the fashion size`` () =
    // arrange
    let item = pim.Item("ABC123", "<size/>", FashionSize = Some "Smallest")
    // act
               |> set (fun i -> i.FashionSize <- Some "Big and Beautiful")
    // assert
    test <@ item.FashionSize = Some "Big and Beautiful" @>
    