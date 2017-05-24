module Color

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Color should have property furnituregold`` () =
    // act
    let color = Color.furnituregold
    // assert
    test <@ color.value |> Map.find "en" = "Gold" @>