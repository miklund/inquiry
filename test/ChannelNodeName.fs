module ChannelNode

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to change name of channel node`` () =
    // arrange
    let channelNode = pim.ChannelNode(Name = Some "Summer Stock")
    // act
                      |> set (fun cn -> cn.Name <- Some "Autumn")
    // assert
    test <@ channelNode.Name = Some "Autumn" @>
