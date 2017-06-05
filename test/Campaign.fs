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
    test <@ instance.Name = Some name @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word campaign and use camel case`` () =
    // act
    let instance = pim.Campaign(name = "Spring sale!", ``type`` = pim.CampaignType.sale)
    // assert
    test <@ instance.GetType() = typeof<pim.Campaign> @>
   
[<Fact>]
let ``Should be able to set the description property`` () =
    // arrange
    let description = "Half price on snakes when you buy a plane."
    // act
    let instance = pim.Campaign(name = "Sprint sale!", Description = Some description)
    // assert
    test <@ instance.Description = Some description @>

[<Fact>]
let ``Trying to set a mandatory property to null should cause an error`` () =
    let instance = pim.Campaign(name = "Sprint sale!")
    // act
    let code = fun () -> instance.Name <- None
    // assert
    ignore <| Assert.Throws(code)

[<Fact>]
let ``Can set campaign start date`` () =
    // arrange
    let today = System.DateTime.Today
    // act
    let campaign = pim.Campaign("Sprint Sale", StartDate = Some today)
    // assert
    test <@ campaign.StartDate = Some today @>

[<Fact>]
let ``Campaign start date is None when not set`` () =
    // act
    let campaign = pim.Campaign("Spring sale")
    // assert
    test <@ campaign.StartDate = None @>

[<Fact>]
let ``Can set campaign start date to None`` () =
    // arrange
    let campaign = pim.Campaign("Spring Sale", StartDate = Some System.DateTime.Now)
    // act
                   |> set (fun c -> c.StartDate <- None)
    // assert
    test <@ campaign.StartDate = None @>

[<Fact>]
let ``Can set campaign end date`` () =
    // assert
    let tomorrow = System.DateTime.Today.AddDays(1.0)
    // arrange
    let campaign = pim.Campaign("Spring sale", EndDate = Some tomorrow)
    // assert
    test <@ campaign.EndDate = Some tomorrow @>

[<Fact>]
let ``Should be able to set demo`` () =
    // act
    let campaign = pim.Campaign("Spring Sale", Demo = Some 1)
    // assert
    test <@ campaign.Demo = Some 1 @>