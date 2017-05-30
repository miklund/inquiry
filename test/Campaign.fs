module Campaign

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to create a new instance of Campaign and set the name`` () =
    // arrange
    let name = "Spring Sale!"
    // act
    let instance = pim.Campaign(name, pim.CampaignType.release)
    // assert
    test <@ instance.Name = name @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word campaign and use camel case`` () =
    // act
    let instance = pim.Campaign(name = "Spring sale!", ``type`` = pim.CampaignType.sale)
    // assert
    test <@ instance.GetType() = typeof<pim.Campaign> @>
    