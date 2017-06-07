module Section

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Default value of Id should be guid`` () =
    // act
    let section = pim.Section()
    // assert
    test <@ section.SectionId <> Some System.Guid.Empty @>

[<Fact>]
let ``Can set section Id`` () =
    // arrange
    let guid = System.Guid.NewGuid()
    // act
    let section = pim.Section(SectionId = Some guid)
    // assert
    test <@ section.SectionId = Some guid @>

[<Fact>]
let ``Can set section Name`` () =
    // act
    let section = pim.Section(Name = Some "Section 13")
    // assert
    test <@ section.Name = Some "Section 13" @>
    