module Channel

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to set channel name as property expression and modify it afterward`` () =
    // arrange
    let channel = pim.Channel(published = false, Name = Some "Web")
    // act
    let newChannel = channel |> set (fun c -> c.Name <- Some "Print")
    // assert
    test <@ newChannel.Name = Some "Print" @>