module Campaign

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Should be able to create a new instance of Campaign and set the name`` () =
    // arrange
    let name = "Spring Sale!"
    // act
    let instance = Campaign(name, null)
    // assert
    test <@ instance.CampaignName = Some name @>

// need a test here that checks the CVL