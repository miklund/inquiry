module CampaignType

open inQuiry.TypeProvider
open inQuiry.Model
open Xunit
open Swensen.Unquote
open inRiver.Remoting
open inQuiry

[<Fact>]
let ``CampaignType should have property campaign`` () =
    // act
    let campaignType = CampaignType.campaign
    // assert
    test <@ campaignType.value |> Map.find "en" = "Campaign" @>
    test <@ campaignType.value |> Map.find "sv" = "Kampanj" @>
    test <@ campaignType.value |> Map.find "nl" = "Campagne" @>
    test <@ campaignType.value |> Map.find "fr" = "Campagne" @>
    test <@ campaignType.value |> Map.find "de" = "Kampagne" @>

[<Fact>]
let ``CampaignType should have property sale`` () =
    // act
    let campaignType = CampaignType.sale
    // assert
    test <@ campaignType.value |> Map.find "en" = "Sale" @>
    test <@ campaignType.value |> Map.find "sv" = "Försäljning" @>
    test <@ campaignType.value |> Map.find "nl" = "Verkoop" @>
    test <@ campaignType.value |> Map.find "fr" = "Vente" @>
    test <@ campaignType.value |> Map.find "de" = "Verkauf" @>

[<Fact>]
let ``CampaignType should have property release`` () =
    // act
    let campaignType = CampaignType.release
    // assert
    test <@ campaignType.value |> Map.find "en" = "Product Release" @>
    test <@ campaignType.value |> Map.find "sv" = "Produktlansering" @>
    test <@ campaignType.value |> Map.find "nl" = "Productrelease" @>
    test <@ campaignType.value |> Map.find "fr" = "Sortie du produit" @>
    test <@ campaignType.value |> Map.find "de" = "Produktfreigabe" @>