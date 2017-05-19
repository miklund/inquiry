///
/// Used to test the String CVL functionality
///

module ActivityStatus

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``Should generate new property of ActivityStatus`` () =
    // act
    let value = ActivityStatus.``new``
    // assert
    test <@ value.GetType() = typeof<ActivityStatus> @>

[<Fact>]
let ``Should generate ongoing property of ActivityStatus`` () =
    // act
    let value = ActivityStatus.ongoing
    // assert
    test <@ value.GetType() = typeof<ActivityStatus> @>

[<Fact>]
let ``Should generate complete property of ActivityStatus`` () =
    // act
    let value = ActivityStatus.complete
    // assert
    test <@ value.GetType() = typeof<ActivityStatus> @>

[<Fact>]
let ``ActivityStatus should have String as DataType`` () =
    // act
    let value = ActivityStatus.complete
    // assert
    test <@ value.DataType = DataType.String @>

[<Fact>]
let ``ActivityStatus should have Ongoing as string value for property ongoing`` () =
    // act
    let ongoing = ActivityStatus.ongoing
    // assert
    test <@ ongoing.value = "Ongoing" @>