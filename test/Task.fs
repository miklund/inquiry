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

[<Fact>]
let ``Email property should be None by default`` () =
    // act
    let task = pim.Task(pim.Users.cert9)
    // assert
    test <@ task.Email = None @>

[<Fact>]
let ``Should be able to set email property to value`` () =
    // arrange
    let task = pim.Task(pim.Users.cert9)
    // act
               |> set (fun t -> t.Email <- Some true)
    // assert
    test <@ task.Email = Some true @>


[<Fact>]
let ``Should be able to set email property to None`` () =
    // arrange
    let task = pim.Task(pim.Users.cert9, Email = Some true)
    // act
               |> set (fun t -> t.Email <- None)
    // assert
    test <@ task.Email = None @>

[<Fact>]
let ``Default value for task status is None`` () =
    // act
    let task = pim.Task(pim.Users.cert9)
    // assert
    test <@ task.Status = None @>

[<Fact>]
let ``Can set task status to new`` () =
    // act
    let task = pim.Task(pim.Users.cert9, Status = Some pim.TaskStatus.``new``)
    // assert
    test <@ task.Status = Some pim.TaskStatus.``new`` @>