namespace inQuiry

// just a wrapper for inRiver.Remoting
module inRiverService =

    //
    // configuration
    //
    type ConnectionDetails = {
            host : string
            userName : string
            password : string
        }

    let mutable connection = {
            host = System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverHost"]
            userName = System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverUserName"]
            password = System.Configuration.ConfigurationManager.AppSettings.["inQuiry:inRiverPassword"]
        }

    // initialize the RemoteManager
    // This has some weird behaviours. It will only execute before "first access of a value that has observable initialization"
    // This means that a simple `let` statement does not garantuee the do statement to run
    type RemoteManager = { 
        modelService : inRiver.Remoting.ModelService
        dataService : inRiver.Remoting.DataService
        utilService : inRiver.Remoting.UtilityService
    }

    let remoteManager =
        Lazy (fun () ->
            inRiver.Remoting.RemoteManager.CreateInstance(connection.host, connection.userName, connection.password) |> ignore
            {
                modelService = inRiver.Remoting.RemoteManager.ModelService
                dataService = inRiver.Remoting.RemoteManager.DataService
                utilService = inRiver.Remoting.RemoteManager.UtilityService
            }
        )
 

    // this is a cache of entity types. We only need to get them once, they will not change
    let private entityTypes =
        lazy (
            remoteManager.Force().modelService.GetAllEntityTypes()
                |> Seq.map (fun entityType -> entityType.Id, entityType)
                |> Map.ofSeq
        )
        
    // this is a cache of cvls. We only need to get them once, they will not change
    let private cvlTypes =
        lazy (
            remoteManager.Force().modelService.GetAllCVLs()
                |> Seq.map (fun cvlType -> cvlType.Id, cvlType)
                |> Map.ofSeq
        )

    // map of cvlId * cvlValues
    let private cvlValues =
        lazy (
            remoteManager.Force().modelService.GetAllCVLValues()
                |> Seq.groupBy (fun cvlValue -> cvlValue.CVLId)
                |> Map.ofSeq
        )

    // get entity
    let getEntity id =
        remoteManager.Force().dataService.GetEntity(id, inRiver.Remoting.Objects.LoadLevel.DataOnly)
        |> Option.ofObj

    let getEntityByUniqueValue fieldTypeID value =
        remoteManager.Force().dataService.GetEntityByUniqueValue(fieldTypeID, value, inRiver.Remoting.Objects.LoadLevel.DataOnly)
        |> Option.ofObj

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
        match cvlValues.Force() |> Map.tryFind cvlId with
        | Some cvlValues -> cvlValues
        | None -> Seq.empty
    
    let getCvlValueByKey cvlId key =
        getCvlValues cvlId
        |> Seq.tryFind (fun cvlValue -> cvlValue.Key = key)
        
    // get file
    let getFile fileId =
        remoteManager.Force().utilService.GetFile(fileId, "Original")

    let createFile fileName data =
        remoteManager.Force().utilService.AddFile(fileName, data)

    let deleteFile fileId =
        remoteManager.Force().utilService.DeleteFile(fileId)

    // save entity to inriver
    let save (entity : inRiver.Remoting.Objects.Entity) =
        if entity.Id > 0 then
            // updated entity
            remoteManager.Force().dataService.UpdateEntity(entity)
        else
            // new entity
            remoteManager.Force().dataService.AddEntity(entity)