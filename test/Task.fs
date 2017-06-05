module Task

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Can update task name`` () =
    // arrange
    let task = pim.Task(pim.Users.cert9, Name = Some "My task")
    // act
               |> set (fun t -> t.Name <- Some "Translate product items")
    // assert
    test <@ task.Name = Some "Translate product items" @>

[<Fact>]
let ``Can update task description`` () =
    // arrange
    let description = "All products should have translations in en, dk, sv, no, fi"
    let task = pim.Task(pim.Users.cert9)
    // act
    let newTask = task |> set (fun t -> t.Description <- Some description)
    // assert
    test <@ newTask.Description = Some description @>

[<Fact>]
let ``Can set task due date`` () =
    // arrange
    let tomorrow = System.DateTime.Today.AddDays(1.0)
    // act
    let task = pim.Task(pim.Users.cert9, DueDate = Some tomorrow)
    // assert
    test <@ task.DueDate = Some tomorrow @>