module Milestone

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Can update value of NodeName and Description`` () =
    let nodeName = Some "Basic"
    let description = Some "Basic clothing that can be used to combine offers"
    // arrange
    let milestone = pim.Milestone(NodeName = None, Description = None)
    // act
                    |> set (fun m -> m.NodeName <- nodeName; m.Description <- description)
    // assert
    test <@ milestone.NodeName = nodeName && milestone.Description = description @>

[<Fact>]
let ``Can set milestone start date`` () =
    // arrange
    let today = System.DateTime.Today
    // act
    let milestone = pim.Milestone(StartDate = Some today)
    // assert
    test <@ milestone.StartDate = Some today @>

[<Fact>]
let ``Can set milestone end date`` () =
    // arrange
    let tomorrow = System.DateTime.Today.AddDays(1.0)
    // act
    let milestone = pim.Milestone(EndDate = Some tomorrow)
    // assert
    test <@ milestone.EndDate = Some tomorrow @>

[<Fact>]
let ``Milestone status should default to new`` () =
    // act
    let milestone = pim.Milestone()
    // assert
    test <@ milestone.Status = Some pim.MilestoneStatus.New @>

[<Fact>]
let ``Can change the milestone status to ongoing`` () =
    // arrange
    let milestone = pim.Milestone()
    // act
                    |> set (fun m -> m.Status <- Some pim.MilestoneStatus.Ongoing)
    // assert
    test <@ milestone.Status = Some pim.MilestoneStatus.Ongoing @>