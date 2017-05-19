namespace inQuiry

open inRiver.Remoting

// just a wrapper for inRiver.Remoting
module inRiverService =

    // initialize the RemoteManager
    // This has some weird behaviours. It will only execute before "first access of a value that has observable initialization"
    // This means that a simple `let` statement does not garantuee the do statement to run
    //do RemoteManager.CreateInstance(System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverHost"], System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverUserName"], System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverPassword"]) |> ignore
    do RemoteManager.CreateInstance("http://localhost:8080", "pimuser1", "pimuser1") |> ignore

    // this is a cache of entity types. We only need to get them once, they will not change
    let private entityTypes =
        lazy (
            RemoteManager.ModelService.GetAllEntityTypes()
                |> Seq.map (fun entityType -> entityType.Id, entityType)
                |> Map.ofSeq
        )
        
    // this is a cache of cvls. We only need to get them once, they will not change
    let private cvlTypes =
        lazy (
            RemoteManager.ModelService.GetAllCVLs()
                |> Seq.map (fun cvlType -> cvlType.Id, cvlType)
                |> Map.ofSeq
        )

    let private cvlValues =
        lazy (
            RemoteManager.ModelService.GetAllCVLValues()
                |> Seq.map (fun cvlValue -> cvlValue.Id, cvlValue)
                |> Map.ofSeq
        )

    // return entity type
    let getEntityTypes () = 
        entityTypes.Force()
        |> Map.toSeq
        |> Seq.map snd
    
    // get an entity type by id
    let getEntityTypeById id =
        entityTypes.Force()
        |> Map.tryFind id

    // get cvl type by id
    let getCvlTypeById id =
        cvlTypes.Force()
        |> Map.tryFind id

    // get cvl types
    let getCvlTypes () =
        cvlTypes.Force()
        |> Map.toSeq
        |> Seq.map snd
    
    // get cvl values
    let getCvlValues cvlId =
        cvlValues.Force()
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.filter (fun cvlValue -> cvlValue.CVLId = cvlId)

    let getCvlValueById valueId =
        cvlValues.Force()
        |> Map.tryFind valueId

    // save entity to inriver
    let save (entity : Objects.Entity) =
        if entity.Id > 0 then
            // updated entity
            RemoteManager.DataService.UpdateEntity(entity)
        else
            // new entity
            RemoteManager.DataService.AddEntity(entity)