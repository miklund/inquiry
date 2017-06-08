module Product

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to create a new instance of product`` () =
    // act
    let instance = pim.Product("ABC123", [pim.ProductStatus.webready], false)
    // assert
    test <@ instance.GetType() = typeof<pim.Product> @>

[<Fact>]
let ``Mandatory parameter ProductNumber injected in constructor should be set to entity`` () =
    // act
    let instance = pim.Product("ABC123", [pim.ProductStatus.printready], false)
    // assert
    test <@ instance.Number = Some "ABC123" @>

[<Fact>]
let ``Mandatory parameter ProductApproved injected in constructor should be set to entity`` () =
    // arrange
    let productApproved = true
    // act
    let instance = pim.Product("ABC123", [pim.ProductStatus.underenrichment], productApproved)
    // assert
    test <@ instance.Approved = Some productApproved @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word product and use camel case`` () =
    // act
    let instance = pim.Product(number = "ABC123", status = [pim.ProductStatus.webready], approved = true)
    // assert
    test <@ instance.GetType() = typeof<pim.Product> @>

[<Fact>]
let ``Saving a product will return an updated Product with new Id`` () =
    // arrange
    let productNumber = "SKU" + DateTime.Now.Ticks.ToString()
    let product = pim.Product(number = productNumber)
    // act
    let newProduct = match pim.Product.save(product) with
                     | Ok entity -> entity
                     | Error ex -> failwith ex.Message
    // assert
    test <@ newProduct.Number = Some productNumber @>
    test <@ newProduct.Id > 0 @>

[<Fact>]
let ``A product should have localized product name`` () =
    // arrange
    let entity = RemoteManager.DataService.GetEntityByUniqueValue("ProductNumber", "A001", Objects.LoadLevel.DataOnly)
    // act
    let product = pim.Product.create entity
    // assert
    test <@ product.Name.["en"] = "City Jacket" @>

[<Fact>]
let ``ProductStatus CVL should be set in the constructor`` () =
    // act
    let product = pim.Product("ABC123", status = [pim.ProductStatus.underenrichment])
    // assert
    test <@ product.Status = [pim.ProductStatus.underenrichment] @>
   
[<Fact>]
let ``Default value of ProductStatus should be new`` () =
    // act
    let product = pim.Product("ABC123")
    // assert
    test <@ product.Status = [pim.ProductStatus.``new``] @>

[<Fact>]
let ``ProductNumber is the DisplayName`` () =
    // arrange
    let productNumber = "ABC321"
    // act
    let product = pim.Product(productNumber)
    // assert
    test <@ product.Number = product.DisplayName @>

[<Fact>]
let ``Should be able to update the product name with translation`` () =
    // arrange
    let product = pim.Product("ABC123")
                  |> set (fun p -> p.Name <- [("en", "Translate ABC123"); ("de", "Translate ABC123")] |> Map.ofList)
    // act
                  |> set (fun p -> p.Name <- p.Name.Add ("en", "My Little Pony: Twilight Sparkle Swim Suit"))
    // assert
    test <@ product.Name |> Map.find "en" = "My Little Pony: Twilight Sparkle Swim Suit" @>

[<Fact>]
let ``Default value of product name should be an empty translation map`` () =
    // act
    let product = pim.Product("ABC123")
    // assert
    test <@ product.Name = Map.empty @>

[<Fact>]
let ``Should be able to set the short description from constructor`` () =
    // act
    let product = pim.Product("ABC123", ShortDescription = ([("en", "Localized short description in english")] |> Map.ofList))
    // assert
    test <@ product.ShortDescription |> Map.find "en" = "Localized short description in english" @>

[<Fact>]
let ``Should be able to add translation to the long product description`` () =
    // arrange
    let product = pim.Product("ABC123", LongDescription = ([("en", "Localized long description in english")] |> Map.ofList))
    // act
                  |> set (fun p -> p.LongDescription <- p.LongDescription.Add ("sv", "En lång beskrivning på svenska"))
    // assert
    test <@ product.LongDescription |> Map.find "sv" = "En lång beskrivning på svenska" @>

[<Fact>]
let ``Default value of product approved should be false`` () =
    // act
    let product = pim.Product("ABC123")
    // assert
    test <@ product.Approved = Some false @>

[<Fact>]
let ``Can set product approved in constructor to true`` () =
    // act
    let product = pim.Product("ABC123", approved = true)
    // assert
    test <@ product.Approved = Some true @>

[<Fact>]
let ``Can set product approved property to true`` () =
    // arrange
    let product = pim.Product("ABC123")
    // act
                  |> set (fun p -> p.Approved <- Some true)
    // assert
    test <@ product.Approved = Some true @>

[<Fact>]
let ``Cannot set product approved to None because it is mandatory`` () =
    // arrange
    let product = pim.Product "ABC123"
    // act
    let code = fun () -> product |> set (fun p -> p.Approved <- None) |> ignore
    // assert
    Assert.Throws(code) |> ignore

[<Fact>]
let ``Can set product industry to manufacturing`` () =
    // act
    let product = pim.Product("ABC123", Industry = Some pim.Industry.manufacturing)
    // assert
    test <@ product.Industry = Some pim.Industry.manufacturing @>

[<Fact>]
let ``Can set product main category to jacket`` () =
    // act
    let product = pim.Product("ABC123", MainCategory = Some pim.MainCategory.jacket)
    // assert
    test <@ product.MainCategory = Some pim.MainCategory.jacket @>

[<Fact>]
let ``Can set product sub category to casual`` () =
    // act
    let product = pim.Product("ABC123", SubCategory = Some pim.SubCategory.casual)
    // assert
    test <@ product.SubCategory = Some pim.SubCategory.casual @>

[<Fact>]
let ``Can set product market to sweden`` () =
    // act
    let product = pim.Product("ABC123", Market = [pim.Market.se])
    // assert
    test <@ product.Market = [pim.Market.se] @>

[<Fact>]
let ``Can set product market to sweden and us`` () =
    // act
    let product = pim.Product("ABC123", Market = [pim.Market.se; pim.Market.us])
    // assert
    test <@ product.Market = [pim.Market.se; pim.Market.us] @>

[<Fact>]
let ``Market is de;dk;fi;gb;nl;no;se;us by default`` () =
    // act
    let product = pim.Product("ABC123")
    // assert
    test <@ product.Market = [pim.Market.de;pim.Market.dk;pim.Market.fi;pim.Market.gb;pim.Market.nl;pim.Market.no;pim.Market.se;pim.Market.us] @>

[<Fact>]
let ``Can set product brand to bosch`` () =
    // act
    let product = pim.Product("ABC123", Brand = Some pim.Brand.bosch)
    // assert
    test <@ product.Brand = Some pim.Brand.bosch @>

[<Fact>]
let ``Default product status shall be new`` () =
    // act
    let product = pim.Product("ABC123")
    // assert
    test <@ product.Status = [pim.ProductStatus.``new``] @>

[<Fact>]
let ``Can update the product status to web ready`` () =
    // act
    let product = pim.Product("ABC123", Status = [pim.ProductStatus.webready])
    // assert
    test <@ product.Status = [pim.ProductStatus.webready] @>

[<Fact>]
let ``Cannot set the product status to None`` () =
    // arrange
    let product = pim.Product("ABC123")
    // act
    let code = fun () -> product |> set (fun p -> p.Status <- []) |> ignore
    // assert
    ignore <| Assert.Throws(code)

[<Fact>]
let ``Can set the product fashion gender to unisex`` () =
    // act
    let product = pim.Product("ABC123", FashionGender = Some pim.Gender.unisex)
    // assert
    test <@ product.FashionGender = Some pim.Gender.unisex @>

[<Fact>]
let ``Can set the product translation status complete to Swedish`` () =
    // act
    let product = pim.Product("ABC123", TranslationStatus = [pim.ProductTranslationComplete.Swedish])
    // assert
    test <@ product.TranslationStatus = [pim.ProductTranslationComplete.Swedish] @>

[<Fact>]
let ``Can set the product translation status complete to Swedish and Dannish`` () =
    // act
    let product = pim.Product("ABC123", TranslationStatus = [pim.ProductTranslationComplete.Swedish; pim.ProductTranslationComplete.Dannish])
    // assert
    test <@ product.TranslationStatus = [pim.ProductTranslationComplete.Swedish; pim.ProductTranslationComplete.Dannish] @>