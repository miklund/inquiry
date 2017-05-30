module Color

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Color should have property furnituregold`` () =
    // act
    let color = pim.Color.furnituregold
    // assert
    test <@ color.value |> Map.find "en" = "Gold" @>