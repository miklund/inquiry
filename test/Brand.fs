module Brand

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Brand should have property schreiber`` () =
    // act
    let brand = pim.Brand.schreiber
    // assert
    test <@ brand.value = "Schreiber" @>

[<Fact>]
let ``Brand should have property blackdecker`` () =
    // act
    let brand = pim.Brand.blackdecker
    // assert
    test <@ brand.value = "Black & Decker" @>

[<Fact>]
let ``Brand should have property makita`` () =
    // act
    let brand = pim.Brand.makita
    // assert
    test <@ brand.value = "Makita" @>

[<Fact>]
let ``Brand should have property dremel`` () =
    // act
    let brand = pim.Brand.dremel
    // assert
    test <@ brand.value = "Dremel" @>

[<Fact>]
let ``Brand should have property bosch`` () =
    // act
    let brand = pim.Brand.bosch
    // assert
    test <@ brand.value = "Bosch" @>