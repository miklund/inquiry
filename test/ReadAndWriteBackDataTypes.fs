module ReadAndWriteBackDataTypes

// Reading a value and writing it back should not change the original value

// One part of having a statically typed API is that each database value is
// transformed to a statically typed value. In this process the value might
// change slightly, so when writing it back, it might not be exactly the same
// value as was read. An example of this could be, reading an unset CVL
// multivalue would yield `null` but writing it back would change `null` to
// empty string. The consequences might not be huge, but it is an unwanted
// side effect that we will try to avoid.

// Since we're testing side effects here, we actually need to save the entities
// to database that we're testing this on.

open System
open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

type pim = inRiverProvider<"http://localhost:8080", "pimuser1", "pimuser1">

// DATATYPE: String, set value
[<Fact>]
let ``Reading a set string value and writing it back again will not affect the value`` () =
    // arrange
    let original = "A quick brown fox jumps over the lazy dog"
    let activity = pim.Activity(Description = Some original) |> pim.Activity.save |> Result.get
    let expected = pim.Activity.get(activity.Id) |> Result.get |> (fun a -> a.Entity.GetField("ActivityDescription").Data) :?> string
    // act
    pim.Activity.get(activity.Id) |> Result.get
    // - read the Description property and write it to itself, testing both read and write for side effects
    |> set (fun a -> a.Description <- a.Description)
    |> pim.Activity.save
    |> ignore
    // assert
    let actual = pim.Activity.get(activity.Id) |> Result.get |> (fun a -> a.Entity.GetField("ActivityDescription").Data) :?> string
    test <@ original = expected && expected = actual @>

// DATATYPE: String, unset value
[<Fact>]
let ``Reading an unset string value and writing it back again will not affect the value`` () =
    // arrange
    let activity = pim.Activity() |> pim.Activity.save |> Result.get
    let expected = pim.Activity.get(activity.Id) |> Result.get |> (fun a -> a.Entity.GetField("ActivityDescription").Data) :?> string
    // act
    pim.Activity.get(activity.Id) |> Result.get
    |> set (fun a -> a.Description <- a.Description)
    |> pim.Activity.save
    |> ignore
    // assert
    let actual = pim.Activity.get(activity.Id) |> Result.get |> (fun a -> a.Entity.GetField("ActivityDescription").Data) :?> string
    test <@ expected = actual @>

// DATATYPE: Guid, set value
[<Fact>]
let ``Reading a set guid value and writing it back again will not affect the value`` () =
    // arrange
    let original = Guid.NewGuid().ToString()
    let section = pim.Section()
    section.Entity.GetField("SectionId").Data <- original
    let sectionId = section |> pim.Section.save |> Result.get |> (fun s -> s.Id)
    // act
    pim.Section.get(sectionId) |> Result.get
    |> set (fun s -> s.SectionId <- s.SectionId)
    |> pim.Section.save
    |> ignore
    // assert
    let actual = pim.Section.get(sectionId) |> Result.get |> (fun s -> s.Entity.GetField("SectionId").Data) :?> string
    test <@ original = actual @>

// DATATYPE: Guid, unset value
[<Fact>]
let ``Reading an unset guid value and writing it back again will not affect the value`` () =
    // arrange
    let section = pim.Section() |> pim.Section.save |> Result.get
    let expected = pim.Section.get(section.Id) |> Result.get |> (fun s -> s.Entity.GetField("SectionId").Data) :?> string
    // act
    pim.Section.get(section.Id) |> Result.get
    |> set (fun s -> s.SectionId <- s.SectionId)
    |> pim.Section.save
    |> ignore
    // assert
    let actual = pim.Section.get(section.Id) |> Result.get |> (fun s -> s.Entity.GetField("SectionId").Data) :?> string
    test <@ actual = expected @>

// DATATYPE: LocaleString, set value
[<Fact>]
let ``Reading a set localestring and writing it back again will not affect the value`` () =
    // arrange
    let languages = [System.Globalization.CultureInfo.GetCultureInfo("en"); System.Globalization.CultureInfo.GetCultureInfo("sv")]
    let original = new Objects.LocaleString(new Collections.Generic.List<System.Globalization.CultureInfo>(languages))
    original.[System.Globalization.CultureInfo.GetCultureInfo("en")] <- "This is a test bundle"
    original.[System.Globalization.CultureInfo.GetCultureInfo("sv")] <- "Detta är en testbundel"

    let bundle = pim.Bundle(name = "Test Bundle")
    bundle.Entity.GetField("BundleDescription").Data <- original
    let bundleId, expected = bundle |> pim.Bundle.save |> Result.get |> (fun b -> b.Id, b.Entity.GetField("BundleDescription").Data :?> Objects.LocaleString)
    // act
    pim.Bundle.get(bundleId) |> Result.get
    |> set (fun b -> b.Description <- b.Description)
    |> pim.Bundle.save
    |> ignore
    // assert
    let actual = pim.Bundle.get(bundleId) |> Result.get |> (fun b -> b.Entity.GetField("BundleDescription").Data) :?> Objects.LocaleString
    test <@ actual.ToString() = expected.ToString() @>

// DATATYPE: LocaleString, unset value
[<Fact>]
let ``Reading an unset localestring and writing it back again will not affect the value`` () =
    // arrange
    let bundle = pim.Bundle(name = "Test bundle") |> pim.Bundle.save |> Result.get
    let expected = pim.Bundle.get(bundle.Id) |> Result.get |> (fun b -> b.Entity.GetField("BundleDescription").Data) :?> Objects.LocaleString
    // act
    pim.Bundle.get(bundle.Id) |> Result.get
    |> set (fun b -> b.Description <- b.Description)
    |> pim.Bundle.save
    |> ignore
    // assert
    let actual = pim.Bundle.get(bundle.Id) |> Result.get |> (fun b -> b.Entity.GetField("BundleDescription").Data) :?> Objects.LocaleString
    test <@ actual = expected @>


// DATATYPE: Integer, set value
[<Fact>]
let ``Reading a set integer and writing it back again will not affect the value`` () =
    // arrange
    let original = 123
    let campaign = pim.Campaign(name = "Test campaign")
    campaign.Entity.GetField("CampaignDemo").Data <- original
    let campaignId, expected = campaign |> pim.Campaign.save |> Result.get |> (fun c -> c.Id, c.Entity.GetField("CampaignDemo").Data :?> int)
    // act
    pim.Campaign.get(campaignId) |> Result.get
    |> set (fun c -> c.Demo <- c.Demo)
    |> pim.Campaign.save
    |> ignore
    // assert
    let actual = pim.Campaign.get(campaignId) |> Result.get |> (fun c -> c.Entity.GetField("CampaignDemo").Data) :?> int
    test <@ expected = original && actual = expected @>

// DATATYPE: Integer, unset value
[<Fact>]
let ``Reading an unset integer and writing it back again will not affect the value`` () =
    // arrange
    let campaign = pim.Campaign(name = "Test campaign") |> pim.Campaign.save |> Result.get
    let expected = pim.Campaign.get(campaign.Id) |> Result.get |> (fun c -> c.Entity.GetField("CampaignDemo").Data)
    // act
    pim.Campaign.get(campaign.Id) |> Result.get
    |> set (fun c -> c.Demo <- c.Demo)
    |> pim.Campaign.save
    |> ignore
    // assert
    let actual = pim.Campaign.get(campaign.Id) |> Result.get |> (fun c -> c.Entity.GetField("CampaignDemo").Data)
    test <@ actual = expected @>


// DATATYPE: DateTime, set value
[<Fact>]
let ``Reading a set datetime and writing it back again will not affect the value`` () =
    // arrange
    let today = System.DateTime.Today
    let campaign = pim.Campaign(name = "Test campaign")
    campaign.Entity.GetField("CampaignStartDate").Data <- today
    let campaignId, expected = campaign |> pim.Campaign.save |> Result.get |> (fun c -> c.Id, c.Entity.GetField("CampaignStartDate").Data :?> System.DateTime)
    // act
    pim.Campaign.get(campaignId) |> Result.get
    |> set (fun c -> c.StartDate <- c.StartDate)
    |> pim.Campaign.save
    |> ignore
    // assert
    let actual = pim.Campaign.get(campaignId) |> Result.get |> (fun c -> c.Entity.GetField("CampaignStartDate").Data) :?> System.DateTime
    test <@ expected = today && actual = expected @>

// DATATYPE: DateTime, unset value
[<Fact>]
let ``Reading an unset datetime and writing it back again will not affect the value`` () =
    // arrange
    let campaign = pim.Campaign(name = "Test campaign") |> pim.Campaign.save |> Result.get
    let expected = pim.Campaign.get(campaign.Id) |> Result.get |> (fun c -> c.Entity.GetField("CampaignStartDate").Data)
    // act
    pim.Campaign.get(campaign.Id) |> Result.get
    |> set (fun c -> c.Demo <- c.Demo)
    |> pim.Campaign.save
    |> ignore
    // assert
    let actual = pim.Campaign.get(campaign.Id) |> Result.get |> (fun c -> c.Entity.GetField("CampaignStartDate").Data)
    test <@ actual = expected @>


// DATATYPE: Double, set value
[<Fact>]
let ``Reading a set double and writing it back again will not affect the value`` () =
    // arrange
    let original = 1.23
    let item = pim.Item(number = Guid.NewGuid().ToString())
    item.Entity.GetField("ItemFashionWeight").Data <- original
    let itemId, expected = item |> pim.Item.save |> Result.get |> (fun i -> i.Id, i.Entity.GetField("ItemFashionWeight").Data :?> double)
    // act
    pim.Item.get(itemId) |> Result.get
    |> set (fun i -> i.FashionWeight <- i.FashionWeight)
    |> pim.Item.save
    |> ignore
    // assert
    let actual = pim.Item.get(itemId) |> Result.get |> (fun i -> i.Entity.GetField("ItemFashionWeight").Data) :?> double
    test <@ expected = original && actual = expected @>


// DATATYPE: Double, unset value
[<Fact>]
let ``Reading an unset double and writing it back again will not affect the value`` () =
    // arrange
    let item = pim.Item(number = Guid.NewGuid().ToString()) |> pim.Item.save |> Result.get
    let expected = pim.Item.get(item.Id) |> Result.get |> (fun i -> i.Entity.GetField("ItemFashionWeight").Data)
    // act
    pim.Item.get(item.Id) |> Result.get
    |> set (fun i -> i.FashionWeight <- i.FashionWeight)
    |> pim.Item.save
    |> ignore
    // assert
    let actual = pim.Item.get(item.Id) |> Result.get |> (fun i -> i.Entity.GetField("ItemFashionWeight").Data)
    test <@ actual = expected @>


// DATATYPE: Boolean, set value
[<Fact>]
let ``Reading a set value and writing it back again will not affect the value`` () =
    // arrange
    let original = true
    let task = pim.Task()
    task.Entity.GetField("TaskEmail").Data <- original
    let taskId, expected = task |> pim.Task.save |> Result.get |> (fun c -> c.Id, c.Entity.GetField("TaskEmail").Data :?> bool)
    // act
    pim.Task.get(taskId) |> Result.get
    |> set (fun c -> c.Email <- c.Email)
    |> pim.Task.save
    |> ignore
    // assert
    let actual = pim.Task.get(taskId) |> Result.get |> (fun c -> c.Entity.GetField("TaskEmail").Data) :?> bool
    test <@ expected = original && actual = expected @>


// DATATYPE: Boolean, unset value
[<Fact>]
let ``Reading an unset boolean and writing it back again will not affect the value`` () =
    // arrange
    let task = pim.Task() |> pim.Task.save |> Result.get
    let expected = pim.Task.get(task.Id) |> Result.get |> (fun t -> t.Entity.GetField("TaskEmail").Data)
    // act
    pim.Task.get(task.Id) |> Result.get
    |> set (fun t -> t.Email <- t.Email)
    |> pim.Task.save
    |> ignore
    // assert
    let actual = pim.Task.get(task.Id) |> Result.get |> (fun t -> t.Entity.GetField("TaskEmail").Data)
    test <@ actual = expected @>


// DATATYPE: Cvl, set value
[<Fact>]
let ``Reading a set cvl and writing it back again will not affect the value`` () =
    // arrange
    let channel = pim.Channel()
    channel.Entity.GetField("ChannelIndustry").Data <- "furniture"
    let channelId, expected = channel |> pim.Channel.save |> Result.get |> (fun c -> c.Id, c.Entity.GetField("ChannelIndustry").Data :?> string)
    // act
    pim.Channel.get(channelId) |> Result.get
    |> set (fun c -> c.Industry <- c.Industry)
    |> pim.Channel.save
    |> ignore
    // assert
    let actual = pim.Channel.get(channelId) |> Result.get |> (fun c -> c.Entity.GetField("ChannelIndustry").Data) :?> string
    test <@ expected = actual @>


// DATATYPE: Cvl, unset value
[<Fact>]
let ``Reading an unset cvl and writing it back again will not affect the value`` () =
    // arrange
    let channel = pim.Channel() |> pim.Channel.save |> Result.get
    let expected = pim.Channel.get(channel.Id) |> Result.get |> (fun c -> c.Entity.GetField("ChannelIndustry").Data)
    // act
    pim.Channel.get(channel.Id) |> Result.get
    |> set (fun c -> c.Industry <- c.Industry)
    |> pim.Channel.save
    |> ignore
    // assert
    let actual = pim.Channel.get(channel.Id) |> Result.get |> (fun c -> c.Entity.GetField("ChannelIndustry").Data)
    test <@ expected = actual @>


// DATATYPE: Multivalue Cvl, set value
[<Fact>]
let ``Reading a set multivalue cvl and writing it back again will not affect the value`` () =
    // arrange
    let original = "de;dk;no;se;us"
    let item = pim.Item(number = System.Guid.NewGuid().ToString())
    item.Entity.GetField("ItemDIYMarket").Data <- original
    let itemId, expected = item |> pim.Item.save |> Result.get |> (fun i -> i.Id, i.Entity.GetField("ItemDIYMarket").Data :?> string)
    // act
    pim.Item.get(itemId) |> Result.get
    |> set (fun i -> i.DIYMarket <- i.DIYMarket)
    |> pim.Item.save
    |> ignore
    // assert
    let actual = pim.Item.get(itemId) |> Result.get |> (fun i -> i.Entity.GetField("ItemDIYMarket").Data) :?> string
    test <@ original = expected && actual = expected @>


// DATATYPE: Multivalue Cvl, unset value
[<Fact>]
let ``Reading an unset multivalue cvl and writing it back again will not affect the value`` () =
    // arrange
    let item = pim.Item(number = System.Guid.NewGuid().ToString()) |> pim.Item.save |> Result.get
    let expected = pim.Item.get(item.Id) |> Result.get |> (fun i -> i.Entity.GetField("ItemFashionSeason").Data)
    // act
    pim.Item.get(item.Id) |> Result.get
    |> set (fun i -> i.FashionSeason <- i.FashionSeason)
    |> pim.Item.save
    |> ignore
    // assert
    let actual = pim.Item.get(item.Id) |> Result.get |> (fun i -> i.Entity.GetField("ItemFashionSeason").Data)
    test <@ expected = actual @>
