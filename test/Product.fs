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
    let instance = pim.Product("ABC123", pim.ProductStatus.webready, false)
    // assert
    test <@ instance.GetType() = typeof<pim.Product> @>

[<Fact>]
let ``Mandatory parameter ProductNumber injected in constructor should be set to entity`` () =
    // act
    let instance = pim.Product("ABC123", pim.ProductStatus.printready, false)
    // assert
    test <@ instance.Number = Some "ABC123" @>

[<Fact>]
let ``Mandatory parameter ProductApproved injected in constructor should be set to entity`` () =
    // arrange
    let productApproved = true
    // act
    let instance = pim.Product("ABC123", pim.ProductStatus.underenrichment, productApproved)
    // assert
    test <@ instance.Approved = Some productApproved @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word product and use camel case`` () =
    // act
    let instance = pim.Product(number = "ABC123", status = pim.ProductStatus.webready, approved = true)
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
    let product = pim.Product("ABC123", status = pim.ProductStatus.underenrichment)
    // assert
    test <@ product.Status = Some(pim.ProductStatus.underenrichment) @>
   
[<Fact>]
let ``Default value of ProductStatus should be new`` () =
    // act
    let product = pim.Product("ABC123")
    // assert
    test <@ product.Status = Some(pim.ProductStatus.``new``) @>

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