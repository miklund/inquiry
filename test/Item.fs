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

[<Fact>]
let ``Item industry should be None by default`` () =
    // act
    let item = pim.Item("ABC123", "<size/>")
    // assert
    test <@ item.Industry = None @>

[<Fact>]
let ``Can set item industry to fashion retail`` () =
    // act
    let item = pim.Item("ABC123", "<size/>", Industry = Some pim.Industry.fashionretail)
    // assert
    test <@ item.Industry = Some pim.Industry.fashionretail @>

[<Fact>]
let ``Can set item color`` () =
    // arrange
    let item = pim.Item("ABC123", "")
    // act
               |> set (fun i -> i.Color <- Some pim.Color.furniturewheat)
    // assert
    test <@ item.Color =  Some pim.Color.furniturewheat @>

[<Fact>]
let ``Can change item status`` () =
    // arrange
    let item = pim.Item("ABC123", "")
    // act
               |> set (fun i -> i.Status <- Some pim.ItemStatus.``new``)
    // assert
    test <@ item.Status = Some pim.ItemStatus.``new`` @>

[<Fact>]
let ``Can set fashion season`` () =
    // act
    let item = pim.Item("ABC123", "<size/>", FashionSeason = Some pim.ItemSeason.FW2017)
    // assert
    test <@ item.FashionSeason = Some pim.ItemSeason.FW2017 @>

[<Fact>]
let ``Can set DIY market to us`` () =
    // arrange
    let item = pim.Item("ABC123", "<size/>")
    // act
               |> set (fun i -> i.DIYMarket <- Some pim.Market.us)
    // assert
    test <@ item.DIYMarket = Some pim.Market.us @>
