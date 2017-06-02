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
