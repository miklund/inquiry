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
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item(number = "ABC123", sizeXML = xmlDoc)
    // act
               |> set (fun i -> i.Number <- Some "BCD234")
    // assert
    test <@ item.Number = Some "BCD234" @>

[<Fact>]
let ``Should be able to change the fashion size`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", xmlDoc, FashionSize = Some "Smallest")
    // act
               |> set (fun i -> i.FashionSize <- Some "Big and Beautiful")
    // assert
    test <@ item.FashionSize = Some "Big and Beautiful" @>
    
[<Fact>]
let ``Default value of fashion weight should be none, meaning not set`` () =
    // act
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", xmlDoc)
    // assert
    test <@ item.FashionWeight = None @>

[<Fact>]
let ``Should be able to set fashion weight to a value`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", xmlDoc)
    // act
               |> set (fun i -> i.FashionWeight <- Some 0.5)
    // assert
    test <@ item.FashionWeight = Some 0.5 @>

[<Fact>]
let ``Should be able to set fashion weight to None`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", xmlDoc, FashionWeight = Some 0.5)
    // act
               |> set (fun i -> i.FashionWeight <- None)
    // assert
    test <@ item.FashionWeight = None @>

[<Fact>]
let ``Item industry should be None by default`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    // act
    let item = pim.Item("ABC123", xmlDoc)
    // assert
    test <@ item.Industry = None @>

[<Fact>]
let ``Can set item industry to fashion retail`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    // act
    let item = pim.Item("ABC123", xmlDoc, Industry = Some pim.Industry.fashionretail)
    // assert
    test <@ item.Industry = Some pim.Industry.fashionretail @>

[<Fact>]
let ``Can set item color`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", null)
    // act
               |> set (fun i -> i.Color <- Some pim.Color.furniturewheat)
    // assert
    test <@ item.Color =  Some pim.Color.furniturewheat @>

[<Fact>]
let ``Can change item status`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", xmlDoc)
    // act
               |> set (fun i -> i.Status <- Some pim.ItemStatus.``new``)
    // assert
    test <@ item.Status = Some pim.ItemStatus.``new`` @>

[<Fact>]
let ``Can set fashion season`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    // act
    let item = pim.Item("ABC123", xmlDoc, FashionSeason = [pim.ItemSeason.FW2017])
    // assert
    test <@ item.FashionSeason = [pim.ItemSeason.FW2017] @>

[<Fact>]
let ``Can set several fashion seasons`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    // act
    let item = pim.Item("ABC123", xmlDoc, FashionSeason = [pim.ItemSeason.FW2015; pim.ItemSeason.FW2016])
    // assert
    test <@ item.FashionSeason = [pim.ItemSeason.FW2015; pim.ItemSeason.FW2016] @>

[<Fact>]
let ``Can set DIY market to us`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", xmlDoc)
    // act
               |> set (fun i -> i.DIYMarket <- [pim.Market.us])
    // assert
    test <@ item.DIYMarket = [pim.Market.us] @>

[<Fact>]
let ``Can set DIY market to se and us`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size/>")
    let item = pim.Item("ABC123", xmlDoc, DIYMarket = [pim.Market.se; pim.Market.us])
    // assert
    test <@ item.DIYMarket = [pim.Market.se; pim.Market.us] @>

[<Fact>]
let ``Can set SizeXML by constructor`` () =
    // arrange
    let xmlDoc = System.Xml.Linq.XDocument.Parse("<size><medium>38</medium></size>");
    // act
    let item = pim.Item("ABC123", xmlDoc)
    // assert
    test <@ item.SizeXML.Value.ToString() = xmlDoc.ToString() @>
    