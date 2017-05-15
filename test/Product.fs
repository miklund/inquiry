module Product

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Should be able to create a new instance of product`` () =
    // act
    let instance = Product("ABC123", null, false)
    // assert
    test <@ instance.GetType() = typeof<Product> @>

[<Fact>]
let ``Mandatory parameter ProductNumber injected in constructor should be set to entity`` () =
    // act
    let instance = Product("ABC123", null, false)
    // assert
    test <@ instance.Number = Some "ABC123" @>

[<Fact>]
let ``Mandatory parameter ProductApproved injected in constructor should be set to entity`` () =
    // arrange
    let productApproved = true
    // act
    let instance = Product("ABC123", null, productApproved)
    // assert
    test <@ instance.Approved = Some productApproved @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word product and use camel case`` () =
    // act
    let instance = Product(number = "ABC123", status = null, approved = true)
    // assert
    test <@ instance.GetType() = typeof<Product> @>
