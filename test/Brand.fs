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

//
// Testing the tryParse function on CVL
//

[<Fact>]
let ``Brand should parse schreiber to Brand.schreiber`` () =
    // act
    let brand = pim.Brand.tryParse "schreiber"
    // assert
    test <@ brand = Some pim.Brand.schreiber @>

[<Fact>]
let ``Brand should parse blackdecker to Brand.blackdecker`` () =
    // act
    let brand = pim.Brand.tryParse "blackdecker"
    // assert
    test <@ brand = Some pim.Brand.blackdecker @>

[<Fact>]
let ``Brand should parse makita to Brand.makita`` () =
    // act
    let brand = pim.Brand.tryParse "makita"
    // assert
    test <@ brand = Some pim.Brand.makita @>

[<Fact>]
let ``Brand should parse dremel to Brand.dremel`` () =
    // act
    let brand = pim.Brand.tryParse "dremel"
    // assert
    test <@ brand = Some pim.Brand.dremel @>

[<Fact>]
let ``Brand should parse bosch to Brand.bosch`` () =
    // act
    let brand = pim.Brand.tryParse "bosch"
    // assert
    test <@ brand = Some pim.Brand.bosch @>
