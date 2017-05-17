namespace inQuiry

open inRiver.Remoting

// just a wrapper for inRiver.Remoting
module inRiverService =

    // initialize the RemoteManager
    //do RemoteManager.CreateInstance(System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverHost"], System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverUserName"], System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverPassword"]) |> ignore
    do RemoteManager.CreateInstance("http://localhost:8080", "pimuser1", "pimuser1") |> ignore

    // this is a cache of entity types. We only need to get them once, they will not change
    let private entityTypes =
        lazy (
            RemoteManager.ModelService.GetAllEntityTypes()
                |> Seq.map (fun entityType -> entityType.Id, entityType)
                |> Map.ofSeq
        )
        
    // return entity type
    let getEntityTypes () = 
        entityTypes.Force()
        |> Map.toSeq
        |> Seq.map (fun (_, entityType) -> entityType)
    
    // get an entity type by id
    let getEntityTypeById id =
        entityTypes.Force()
        |> Map.tryFind id

    // save entity to inriver
    let save (entity : Objects.Entity) =
        if entity.Id > 0 then
            // updated entity
            RemoteManager.DataService.UpdateEntity(entity)
        else
            // new entity
            RemoteManager.DataService.AddEntity(entity)