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