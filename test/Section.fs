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
    let success, guid = System.Guid.TryParse(section.SectionId.Value)
    test <@ success @>

[<Fact>]
let ``Can set section Id`` () =
    // act
    let section = pim.Section(SectionId = Some "1")
    // assert
    test <@ section.SectionId = Some "1" @>

[<Fact>]
let ``Can set section Name`` () =
    // act
    let section = pim.Section(Name = Some "Section 13")
    // assert
    test <@ section.Name = Some "Section 13" @>
    