///
/// Tests to verify that we can set and get default properties
///
module DefaultEntityProperties

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

[<Fact>]
let ``Can get ChangeSet property`` () =
    // arrange
    let expectedChangeSet = 123
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.ChangeSet <- expectedChangeSet
    // assert
    test <@ instance.ChangeSet = expectedChangeSet @>

[<Fact>]
let ``Can get Completeness property`` () =
    // arrange
    let expectedCompleteness = 7
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.Completeness <- System.Nullable(expectedCompleteness)
    // assert
    test <@ instance.Completeness = Some expectedCompleteness @>

[<Fact>]
let ``Can get CreatedBy property`` () =
    // arrange
    let expectedCreatedBy = "pimuser1"
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.CreatedBy <- expectedCreatedBy
    // assert
    test <@ instance.CreatedBy = expectedCreatedBy @>

[<Fact>]
let ``Can get DateCreated property`` () =
    // arrange
    let expectedDateCreated = System.DateTime.Now
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.DateCreated <- expectedDateCreated
    // assert
    test <@ instance.DateCreated = expectedDateCreated @>

[<Fact>]
let ``Can get FieldSetId property`` () =
    // arrange
    let expectedFieldSetId = "General"
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.FieldSetId <- expectedFieldSetId
    // assert
    test <@ instance.FieldSetId = expectedFieldSetId @>

[<Fact>]
let ``Can get Id property`` () =
    // arrange
    let expectedId = 123
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.Id <- expectedId
    // assert
    test <@ instance.Id = expectedId @>

[<Fact>]
let ``Can get LastModified property`` () =
    // arrange
    let expectedLastModified = System.DateTime.Now
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.LastModified <- expectedLastModified
    // assert
    test <@ instance.LastModified = expectedLastModified @>

[<Fact>]
let ``Can get LoadLevel property`` () =
    // arrange
    let expectedLoadLevel = Objects.LoadLevel.DataOnly
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.LoadLevel <- expectedLoadLevel
    // assert
    test <@ instance.LoadLevel = expectedLoadLevel @>

[<Fact>]
let ``Can get Locked property`` () =
    // arrange
    let expectedLocked = "Locked"
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.Locked <- expectedLocked
    // assert
    test <@ instance.Locked = expectedLocked @>

[<Fact>]
let ``Can get MainPictureId property`` () =
    // arrange
    let expectedMainPictureId = 1337
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.MainPictureId <- System.Nullable(expectedMainPictureId)
    // assert
    test <@ instance.MainPictureId = Some expectedMainPictureId @>

[<Fact>]
let ``Can get ModifiedBy property`` () =
    // arrange
    let expectedModifiedBy = "pimuser1"
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.ModifiedBy <- expectedModifiedBy
    // assert
    test <@ instance.ModifiedBy = expectedModifiedBy @>

[<Fact>]
let ``Can get Version property`` () =
    // arrange
    let expectedVersion = 5
    let instance = pim.Test("Required Value")
    // act
    instance.Entity.Version <- expectedVersion
    // assert
    test <@ instance.Version = expectedVersion @>