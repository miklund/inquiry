///
/// This entity is not part of the demo database.
/// This entity is only here to test some of the internal logic of the type provider
///
module TestEntity

open System.Xml.Linq
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
    let entityType = Option.get (inRiverService.getEntityTypeById("Test"))
    let entity = Objects.Entity.CreateEntity(entityType)
    entity.GetField("TestDescription").Data <- testData
    // act
    let instance = Test.Create(entity)
    // assert
    test <@ instance.Description = Some testData @>

[<Fact>]
let ``Saving an entity will persist the entity in inRiver`` () =
    // arrange
    let name = "Test-" + System.DateTime.Now.Ticks.ToString()
    let instance = Test(name)
    // act
    let savedInstance = match Test.Save(instance) with
                        | Ok entity -> entity
                        | Error ex -> failwith ex.Message
    // assert
    let entity = RemoteManager.DataService.GetEntity(id = savedInstance.Id, level = Objects.LoadLevel.DataOnly)
    test <@ (entity.GetField("Name").Data :?> string) = name @>

[<Fact>]
let ``Name is DisplayDescription`` () =
    // arrange
    let name = "Once upon a time"
    // act
    let instance = Test(name)
    // assert
    test <@ instance.Name = instance.DisplayDescription @>

[<Fact>]
let ``TestXML should be able to set XML value from constructor`` () =
    // arrange
    let xml = "<root><name>Bertil</name></root>"
    // act
    let instance = Test("XmlTest", xml = xml)
    // assert
    test <@ instance.Xml = xml @>