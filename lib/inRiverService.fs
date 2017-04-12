namespace inQuiry

open inRiver.Remoting

// just a wrapper for inRiver.Remoting
type inRiverService(host : string, username : string, password : string) =

    // initialize the RemoteManager
    do RemoteManager.CreateInstance(host, username, password) |> ignore
    
    new() = inRiverService(System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverHost"], System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverUserName"], System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverPassword"])

    
    // return entity type
    member this.GetEntityTypes () =
        RemoteManager.ModelService.GetAllEntityTypes() :> Objects.EntityType seq
    
    // get an entity type by id
    member this.GetEntityTypeById id =
        RemoteManager.ModelService.GetEntityType(id)