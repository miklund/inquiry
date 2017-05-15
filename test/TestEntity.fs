///
/// This entity is not part of the demo database.
/// This entity is only here to test some of the internal logic of the type provider
///
module TestEntity

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``When field TestName is conflicting with field Name then only camel case convention should apply in constructor`` () =
    // act
    let instance = Test(testName = "Hello", name = "World")
    // assert
    test <@ instance <> null @>

[<Fact>]
let ``When field TestName is conflicting with field Name then no conventions should apply to properties`` () =
    // act
    let instance = Test(testName = "Hello", name = "World")
    // assert
    test <@ instance.TestName = "Hello" @>
    test <@ instance.Name = "World" @>

[<Fact>]
let ``When field TestCreatedBy is conflicting with Entity property CreatedBy then no conventions should apply to property`` () =
    // act
    let instance = Test("", "")
    // assert
    test <@ instance.TestCreatedBy = None @>

[<Fact>]
let ``When field type is mandatory but a default value has been supplied then constructor parameter is optional and default value is set`` () =
    // arrange
    let defaultValue = "Hello World!"
    // act
    let instance = Test("")
    // assert
    test <@ instance.TestName = defaultValue @>

[<Fact>]
let ``When field type is mandatory and has a default value but is supplied with another value from constructor then that constructor value should be used`` () =
    // arrange
    let testNameValue = "Another World!"
    // act
    let instance = Test("", testName = testNameValue)
    // assert
    test <@ instance.TestName = testNameValue @>

[<Fact>]
let ``Setting a non mandatory field TestDescription will set Some value at property TestDescription`` () =
    // arrange
    let testData = "This is description of this Test entity"
    let entityType = inRiverService().GetEntityTypeById("Test")
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("TestDescription").Data <- testData
    // act
    let instance = Test.Create(entity)
    // assert
    test <@ instance.Description = Some testData @>