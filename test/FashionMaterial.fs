///
/// Used to test the LocaleString CVL functionality
///
module FashionMaterial

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Should generate cotton property of FashionMaterial CVL`` () =
    // act
    let cvlValue = FashionMaterial.cotton
    // assert
    test <@ cvlValue.GetType() = typeof<FashionMaterial> @>

[<Fact>]
let ``Should generate syntethic property of FashionMaterial CVL`` () =
    // act
    let cvlValue = FashionMaterial.syntethic
    // assert
    test <@ cvlValue.GetType() = typeof<FashionMaterial> @>

[<Fact>]
let ``Should generate wool property of FashionMaterial CVL`` () =
    // act
    let cvlValue = FashionMaterial.wool
    // assert
    test <@ cvlValue.GetType() = typeof<FashionMaterial> @>

[<Fact>]
let ``Should generate other property of FashionMaterial CVL`` () =
    // act
    let cvlValue = FashionMaterial.other
    // assert
    test <@ cvlValue.GetType() = typeof<FashionMaterial> @>

[<Fact>]
let ``FashonMaterial CVL should have LocaleString as data type`` () =
    // act
    let cvlValue = FashionMaterial.cotton
    // assert
    test <@ cvlValue.DataType = DataType.LocaleString @>

[<Fact>]
let ``FashionMaterial.cotton will translate to Baumwolle in german`` () =
    // arrange
    let cvlValue = FashionMaterial.cotton
    // act
    let localeString = cvlValue.value
    // assert
    test <@ localeString |> Map.find "de" = "Baumwolle" @>
    
[<Fact>]
let ``FashionMaterial.wool will translate to Laine in french`` () =
    // act
    let value = FashionMaterial.wool.value
    // assert
    test <@ value |> Map.find "fr" = "Laine" @>