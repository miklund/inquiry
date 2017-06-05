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

[<Fact>]
let ``Should set published to false as default value`` () =
    // act
    let channel = pim.Channel()
    // test
    test <@ channel.Published = Some false @>

[<Fact>]
let ``Should be able to set published to true`` () =
    // act
    let channel = pim.Channel(published = true)
    // assert
    test <@ channel.Published = Some  true @>