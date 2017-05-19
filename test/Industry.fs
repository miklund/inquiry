///
/// Used to test the nested CVL functionality
///

module Industry

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Should generate a manufacturing property for Industry`` () =
    // act
    let manufacturing = Industry.manufacturing
    // assert
    test <@ manufacturing.value |> Map.find "en" = "Manufacturing" @>

[<Fact>]
let ``Should generate an electronics property for Industry`` () =
    // act
    let electronics = Industry.electronics
    // assert
    test <@ electronics.value |> Map.find "en" = "Electronics" @>

[<Fact>]
let ``Should generate a fashionretail property for Industry`` () =
    // act
    let fashionRetail = Industry.fashionretail
    // assert
    test <@ fashionRetail.value |> Map.find "en" = "Fashion/Retail" @>

[<Fact>]
let ``Should generate a diy property for Industry`` () =
    // act
    let diy = Industry.diy
    // assert
    test <@ diy.value |> Map.find "en" = "DIY" @>

[<Fact>]
let ``Should generate a furniture property for Industry`` () =
    // act
    let furniture = Industry.furniture
    // assert
    test <@ furniture.value |> Map.find "en" = "Furniture" @>
