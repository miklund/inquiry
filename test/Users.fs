module Users

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://DESKTOP-MI0QP1I:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Can parse user cert9`` () =
    // arrange
    let user = pim.Users.tryParse "cert9"
    // assert
    test <@ user = Some pim.Users.cert9 @>

[<Fact>]
let ``Can parse user pimuser1`` () =
    // arrange
    let user = pim.Users.tryParse "pimuser1"
    // assert
    test <@ user <> None @>

[<Fact>]
let ``Can parse user demoa1`` () =
    // arrange
    let user = pim.Users.tryParse "demoa1"
    // assert
    test <@ user = Some pim.Users.demoa1 @>

[<Fact>]
let ``Cannot parse unknown user`` () =
    // arrange
    let user = pim.Users.tryParse "unknown"
    // assert
    test <@ user = None @>