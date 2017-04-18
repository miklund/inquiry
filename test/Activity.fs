﻿module Activity

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry


[<Fact>]
let ``Should be able to create a new instance of activity`` () =
    // act
    let instance = Activity()
    // assert
    test <@ instance.GetType() = typeof<Activity> @>

[<Fact>]
let ``Should be able to create new instance of Activity based on existing entity`` () =
    // arrange
    let entity = new Objects.Entity()
    entity.CreatedBy <- "Mikael Lundin"
    // act
    let instance = Activity.Create(entity)
    // assert
    test <@ instance.CreatedBy = "Mikael Lundin" @>

[<Fact>]
let ``Should get activity start date (System.DateTime) through generated property`` () =
    // arrange
    let testData = System.DateTime.Now
    let entityType = inRiverService().GetEntityTypeById("Activity")
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("ActivityStartDate").Data <- testData
    // act
    let instance = Activity.Create(entity)
    // assert
    test <@ instance.ActivityStartDate = Some testData @>


[<Fact>]
let ``Should get value None when value of generated property ActivityDescription is null`` () =
    // arrange
    let entityType = inRiverService().GetEntityTypeById("Activity")
    let entity = Objects.Entity.CreateEntity(entityType)
    // act
    let instance = Activity.Create(entity)
    // assert
    test <@ instance.ActivityDescription = None @>

[<Fact>]
let ``Should get Some(value) when value of generated property ActivityDescription is not null`` () =
    // arrange
    let testData = "Activity Description"
    let entityType = inRiverService().GetEntityTypeById("Activity")
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("ActivityDescription").Data <- testData
    // act
    let instance = Activity.Create(entity)
    // assert
    test <@ instance.ActivityDescription = Some testData @>

[<Fact>]
let ``Should get activity length (System.Int32) through generated property ActivityLength`` () =
    // arrange
    let testData = 123
    let entityType = inRiverService().GetEntityTypeById("Activity")
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("ActivityLength").Data <- testData
    // act
    let instance = Activity.Create(entity)
    // assert
    test <@ instance.ActivityLength = Some testData @>
