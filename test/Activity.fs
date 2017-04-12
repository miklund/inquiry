module Activity

open inQuiry.TypeProvider
open inQuiry.Model
open Fuchu
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Tests>]
let tests = 
    testList "Activity" [
        testCase "should be able to create an instance" <| 
            fun _ ->
                // act
                let instance = Activity()
                // assert
                test <@ instance.GetType() = typeof<Activity> @>

        testCase "should be able to create instance based on an Entity instance" <|
            fun _ ->
                // arrange
                let entity = new Objects.Entity()
                entity.CreatedBy <- "Mikael Lundin"
                // act
                let instance = Activity.Create(entity)
                // assert
                test <@ instance.CreatedBy = "Mikael Lundin" @>

        testCase "should get activity start date (System.DateTime) through generated property" <|
            fun _ ->
                // arrange
                let testData = System.DateTime.Now
                let entityType = inRiverService().GetEntityTypeById("Activity")
                let entity = Objects.Entity.CreateEntity(entityType)
                entity.GetField("ActivityStartDate").Data <- testData
                // act
                let instance = Activity.Create(entity)
                // assert
                test <@ instance.ActivityStartDate = testData @>
        
        testCase "should get activity length (System.Int32) through generated property" <|
            fun _ ->
                // arrange
                let testData = 123
                let entityType = inRiverService().GetEntityTypeById("Activity")
                let entity = Objects.Entity.CreateEntity(entityType)
                entity.GetField("ActivityLength").Data <- testData
                // act
                let instance = Activity.Create(entity)
                // assert
                test <@ instance.ActivityLength = testData @>

        //testCase "should set the default value ActivityStatus to new" <|
        //    fun _ ->
        //        // act
        //        let instance = Activity()
        //        // assert
        //        test <@ (instance.ActivityStatus :?> Objects.CVLValue).CVLId = "new" @>
        ]