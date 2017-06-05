module Activity

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Should be able to create a new instance of activity`` () =
    // act
    let instance = pim.Activity()
    // assert
    test <@ instance.GetType() = typeof<pim.Activity> @>

[<Fact>]
let ``Should be able to create new instance of Activity based on existing entity`` () =
    // arrange
    let entity = new Objects.Entity()
    entity.EntityType <- new Objects.EntityType("Activity")
    entity.CreatedBy <- "Mikael Lundin"
    // act
    let instance = pim.Activity.create entity
    // assert
    test <@ instance.CreatedBy = "Mikael Lundin" @>

[<Fact>]
let ``Should get activity start date (System.DateTime) through generated property`` () =
    // arrange
    let testData = System.DateTime.Now
    let entityType = Option.get (inRiverService.getEntityTypeById("Activity"))
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("ActivityStartDate").Data <- testData
    // act
    let instance = pim.Activity.create entity
    // assert
    test <@ instance.StartDate = Some testData @>


[<Fact>]
let ``Should get value None when value of generated property ActivityDescription is null`` () =
    // arrange
    let entityType = Option.get (inRiverService.getEntityTypeById("Activity"))
    let entity = Objects.Entity.CreateEntity(entityType)
    // act
    let instance = pim.Activity.create entity
    // assert
    test <@ instance.Description = None @>

[<Fact>]
let ``Should be able to set ActivityDescription to None`` () =
    // arrange
    let instance = pim.Activity(Description = Some "Activity Description")
    // act
                   |> set (fun a -> a.Description <- None)
    // assert
    test <@ instance.Description = None @>

[<Fact>]
let ``Should get Some(value) when value of generated property ActivityDescription is not null`` () =
    // arrange
    let testData = "Activity Description"
    let entityType = Option.get (inRiverService.getEntityTypeById("Activity"))
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("ActivityDescription").Data <- testData
    // act
    let instance = pim.Activity.create entity
    // assert
    test <@ instance.Description = Some testData @>

[<Fact>]
let ``Should get activity length (System.Int32) through generated property ActivityLength`` () =
    // arrange
    let testData = 123
    let entityType = Option.get (inRiverService.getEntityTypeById("Activity"))
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("ActivityLength").Data <- testData
    // act
    let instance = pim.Activity.create entity
    // assert
    test <@ instance.Length = Some testData @>

[<Fact>]
let ``Should be able to update ActivityDescription with a value`` () =
    // arrange
    let description = "My new activity"
    // act
    let activity = pim.Activity() |> set (fun a -> a.Description <- Some description)
    // assert
    test <@ activity.Description = Some description @>

[<Fact>]
let ``Should be able to set StartDate`` () =
    // arrange
    let now = System.DateTime.Now
    // act
    let activity = pim.Activity(StartDate = Some now)
    // assert
    test <@ activity.StartDate = Some now @>

[<Fact>]
let ``Should be able to update EndDate`` () =
    // arrange
    let tomorrow = System.DateTime.Today.AddDays(1.0)
    let activity = pim.Activity(EndDate = Some System.DateTime.Today)
    // act
                   |> set (fun a -> a.EndDate <- Some tomorrow)
    // assert
    test <@ activity.EndDate = Some tomorrow @>

[<Fact>]
let ``StartDate should be None if not set`` () =
    // act
    let activity = pim.Activity()
    // assert
    test <@ activity.StartDate = None @>

[<Fact>]
let ``Should be able to set StartDate to None`` () =
    // arrange
    let now = System.DateTime.Now
    let activity = pim.Activity(StartDate = Some now)
    // act
                   |> set (fun d -> d.StartDate <- None)
    // assert
    test <@ activity.StartDate = None @>

[<Fact>]
let ``Activity length should be None unless set`` () =
    // act
    let activity = pim.Activity()
    // assert
    test <@ activity.Length = None @>

[<Fact>]
let ``Should be able to set activity Length value`` () =
    // act
    let activity = pim.Activity(Length = Some 123)
    // assert
    test <@ activity.Length = Some 123 @>

[<Fact>]
let ``Should be able to set activity Length to None`` () =
    // arrange
    let activity = pim.Activity(Length = Some 123)
    // act
                   |> set (fun a -> a.Length <- None)
    // assert
    test <@ activity.Length = None @>

[<Fact>]
let ``Should be able to set relative start`` () =
    // act
    let activity = pim.Activity(RelStart = Some 321)
    // assert
    test <@ activity.RelStart = Some 321 @>

[<Fact>]
let ``Can set activity type to MilestoneType.Translation`` () =
    // act
    let activity = pim.Activity(Type = Some pim.MilestoneType.Translation)
    // assert
    test <@ activity.Type = Some pim.MilestoneType.Translation @>

[<Fact>]
let ``Can change activity type to MilestoneType.SelectImages`` () =
    // arrange
    let activity = pim.Activity(Type = Some pim.MilestoneType.Translation)
    // act
                   |> set (fun a -> a.Type <- Some pim.MilestoneType.SelectImages)
    // assert
    test <@ activity.Type = Some pim.MilestoneType.SelectImages @>

[<Fact>]
let ``Can set activity type to None`` () =
    // arrange
    let activity = pim.Activity()
    // act
                   |> set (fun a -> a.Type <- None)
    // assert
    test <@ activity.Type = None @>

[<Fact>]
let ``Activity status should be new by default`` () =
    // act
    let activity = pim.Activity()
    // assert
    test <@ activity.Status = Some pim.ActivityStatus.``new`` @>

[<Fact>]
let ``Can change activity status to ongoing`` () =
    // arrange
    let activity = pim.Activity()
    // act
                   |> set (fun a -> a.Status <- Some pim.ActivityStatus.ongoing)
    // assert
    test <@ activity.Status = Some pim.ActivityStatus.ongoing @>

[<Fact>]
let ``Activity responsible should have None as default value`` () =
    // act
    let activity = pim.Activity()
    // assert
    test <@ activity.Responsible = None @>

[<Fact>]
let ``Can set activity responsible to pimuser1`` () =
    // arrange
    let activity = pim.Activity()
    // act
                   |> set (fun a -> a.Responsible <- Some pim.Users.cert9)
    // arrange
    test <@ activity.Responsible = Some pim.Users.cert9 @>