module Product

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Should be able to create a new instance of product`` () =
    // act
    let instance = Product("ABC123", ProductStatus.webready, false)
    // assert
    test <@ instance.GetType() = typeof<Product> @>

[<Fact>]
let ``Mandatory parameter ProductNumber injected in constructor should be set to entity`` () =
    // act
    let instance = Product("ABC123", ProductStatus.printready, false)
    // assert
    test <@ instance.Number = "ABC123" @>

[<Fact>]
let ``Mandatory parameter ProductApproved injected in constructor should be set to entity`` () =
    // arrange
    let productApproved = true
    // act
    let instance = Product("ABC123", ProductStatus.underenrichment, productApproved)
    // assert
    test <@ instance.Approved = productApproved @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word product and use camel case`` () =
    // act
    let instance = Product(number = "ABC123", status = ProductStatus.webready, approved = true)
    // assert
    test <@ instance.GetType() = typeof<Product> @>

[<Fact>]
let ``Saving a product will return an updated Product with new Id`` () =
    // arrange
    let productNumber = "SKU" + DateTime.Now.Ticks.ToString()
    let product = Product(number = productNumber)
    // act
    let newProduct = match Product.Save(product) with
                     | Ok entity -> entity
                     | Error ex -> failwith ex.Message
    // assert
    test <@ newProduct.Number = productNumber @>
    test <@ newProduct.Id > 0 @>

[<Fact>]
let ``A product should have localized product name`` () =
    // arrange
    let entity = RemoteManager.DataService.GetEntityByUniqueValue("ProductNumber", "A001", Objects.LoadLevel.DataOnly)
    // act
    let product = Product.Create(entity)
    // assert
    test <@ product.Name.["en"] = "City Jacket" @>

[<Fact>]
let ``ProductStatus CVL should be set in the constructor`` () =
    // act
    let product = Product("ABC123", status = ProductStatus.underenrichment)
    // assert
    test <@ product.Status = ProductStatus.underenrichment @>
   
[<Fact>]
let ``Default value of ProductStatus should be new`` () =
    // act
    let product = Product("ABC123")
    // assert
    test <@ product.Status = ProductStatus.``new`` @>

[<Fact>]
let ``ProductNumber is the DisplayName`` () =
    // arrange
    let productNumber = "ABC321"
    // act
    let product = Product(productNumber)
    // assert
    test <@ product.Number = product.DisplayName @>