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
    test <@ instance.Name = Some name @>

[<Fact>]
let ``Constructor parameters should apply naming conventions removing the word campaign and use camel case`` () =
    // act
    let instance = Campaign(name = "Spring sale!", ``type`` = null)
    // assert
    test <@ instance.GetType() = typeof<Campaign> @>
    