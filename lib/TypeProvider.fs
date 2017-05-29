module inQuiry.TypeProvider

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open inQuiry
open inRiver.Remoting

// remove entity ID from field name
let fieldNamingConvention entityID (fieldName : string) =
    // try get configuration value if this naming convention is active
    let foundConfigValue, value = bool.TryParse(Configuration.ConfigurationManager.AppSettings.["inQuiry:fieldNamingConvention"])
    // apply fieldNamingConvention if configuration not found or unparsable, otherwise use configuration value
    if (not foundConfigValue) || value then
        fieldName.Replace(entityID, String.Empty)
    else
        fieldName

// make first letter lower case
// > camelCase "ProductName"
// -> "productName"
let toCamelCase = function
| s when s = null -> null
| s when s = String.Empty -> s
| s when s.Length = 1 -> s.ToLower()
| s -> System.Char.ToLower(s.[0]).ToString() + s.Substring(1)

// turn a value expression to an option value expression
let optionPropertyExpression<'a when 'a : equality> (valueExpression : Expr<'a>) =
    <@@
        let value = (%valueExpression : 'a)
        if value = Unchecked.defaultof<'a> then
            None
        else
            Some(value)
        @@>

let uncheckedExpression (valueExpression : Expr<'a>) =
    <@@ (% valueExpression : 'a ) @@>

// simplify the Objects.LocaleString to an immutable map
type LocaleString = Map<string, string>

// the file description is used when creating a file reference
type File = 
    // a new file is a fileName and file data
    | New of string * byte array
    // an already persisted file is just data
    | Persisted of byte array
    
type FileCache = Map<string, File>

// the available field data types
type DataType = 
    | Boolean 
    | CVL of string 
    | DateTime 
    | Double 
    | File 
    | Integer 
    | LocaleString 
    | String 
    | Xml

type DataType with
    // Data type is represented as a string in inRiver
    static member parse = function
        | "Boolean", _ -> Boolean
        | "CVL", id -> CVL id
        | "DateTime", _ -> DateTime
        | "Double", _ -> Double
        | "File", _ -> File
        | "Integer", _ -> Integer
        | "LocaleString", _ -> LocaleString
        | "String", _ -> String
        | "Xml", _ -> Xml
        | dt, _ -> failwith (sprintf "Unknown field data type: %s" dt)


// remove the fileId suffix
let fileNameFieldConvention (fieldType : Objects.FieldType) (input : string) =
    // if field type is  File
    if (fieldType.DataType, fieldType.CVLId) |> DataType.parse = File then
        // replace the Id suffix with Data
        System.Text.RegularExpressions.Regex.Replace(input, "Id$", "Data")
    else
        // otherwise return as it is
        input

// cvl stands for Custom Value List
type CVLNode (cvlType : Objects.CVL, cvlValue : Objects.CVLValue) = 
    member this.DataType = (cvlType.DataType, cvlValue.CVLId) |> DataType.parse
    member this.CvlType = cvlType
    member this.CvlValue = cvlValue
    override x.GetHashCode () =
        hash (cvlType, cvlValue)
    override x.Equals(b) =
        match b with
        | :? CVLNode as cvl -> (cvlValue.Id, cvlValue.CVLId) = (cvl.CvlValue.Id, cvl.CvlValue.CVLId)
        | _ -> false
    override x.ToString () = sprintf "(%s)" (cvlValue.ToString())

// this is the entity value that is returned from the type provider
type Entity (entityType, entity : Objects.Entity)  =

    let mutable files : FileCache = Map.empty<string, File>

    // only called with entityType, create a new entity
    new(entityType) = Entity(entityType, Objects.Entity.CreateEntity(entityType))

    // called with entity, make sure we extract entityType
    new(entity : Objects.Entity) = Entity(entity.EntityType, entity)

    member this.PropertyValue fieldTypeId  = 
        match entity.GetField(fieldTypeId) with
        // expected a field, but it is not there
        | null -> failwith (sprintf "Field %s was not set on entity %s:%d" fieldTypeId entityType.Id entity.Id)
        //| null -> failwith (sprintf "Field %s was not set on entity %s:%d" fieldTypeId entityType.Id entity.Id)
        | field -> field.Data

    member this.NewFiles =
        files
        |> Map.toSeq
        // find all new files
        |> Seq.choose 
            (fun (fileTypeId, file) -> 
                match file with 
                | New (fileName, data) -> Some (fileTypeId, fileName, data) 
                | _ -> None
            )

    member this.getFileData fileTypeId =
        match files |> Map.tryFind fileTypeId with
        | None -> None
        // it is a new file
        | Some (New (fileName, data)) -> Some data
        // it is an old file
        | Some (Persisted data) -> Some data

    member this.setFile (fieldTypeId : string) (file : File) = files <- files.Add(fieldTypeId, file)

    member this.setPersistedFileData (fieldTypeId : string) (data : byte array) =
        files <- files.Add(fieldTypeId, Persisted data)
        files
    
    member this.Entity = entity
    
    // default properties
    member this.ChangeSet = entity.ChangeSet

    member this.Completeness =
        if entity.Completeness.HasValue then
            Some entity.Completeness.Value
        else
            None

    member this.CreatedBy = entity.CreatedBy
    
    member this.DateCreated = entity.DateCreated

    member this.EntityType = entityType
    
    member this.FieldSetId = entity.FieldSetId
    
    member this.Id = entity.Id

    member this.LastModified = entity.LastModified

    member this.LoadLevel = entity.LoadLevel

    member this.Locked = entity.Locked

    member this.MainPictureId =
        if entity.MainPictureId.HasValue then
            Some entity.MainPictureId.Value
        else
            None
    
    member this.ModifiedBy = entity.ModifiedBy

    member this.Version = entity.Version
    
   
type EntityTypeFactory (cvlTypes : ProvidedTypeDefinition list, entityType : Objects.EntityType)  =
    // filter all FieldTypes that are mandatory
    let mandatoryFieldTypes =
        entityType.FieldTypes
        // filter out only mandatory fields
        |> Seq.filter (fun fieldType -> fieldType.Mandatory || fieldType.ReadOnly)
        // make sure they're sorted by index
        |> Seq.sortBy (fun fieldType -> fieldType.Index)
        // change their orders so mandatory without default value comes first
        |> Seq.sortBy (fun fieldType -> if fieldType.DefaultValue = null then 1 else 2)
        |> Seq.toList

    // Get the .NET Type for this data type
    let toType = function
        | Boolean -> typeof<bool>
        | CVL id -> cvlTypes |> List.find (fun t -> t.Name = id) :> Type
        | DateTime -> typeof<System.DateTime>
        | Double -> typeof<double>
        | File -> typeof<File>
        | Integer -> typeof<int>
        | LocaleString -> typeof<LocaleString>
        | String | Xml -> typeof<string>
    // Get the Option type for data type
    let toOptionType dataType =
        typedefof<Option<_>>.MakeGenericType([|dataType |> toType|])
    // Shortcut
    let stringToType = DataType.parse >> toType
    
    let fieldTypeToProvidedParameter =
        // a constructor parameter name should remove the leading entityTypeID and make camel case
        let providedParameterNamingConvention fieldTypeID = 
            // the function that creates the identifier
            let namingConvention = 
                // remove entity id from field name
                (fieldNamingConvention entityType.Id) >>
                // change Id suffix to Data if DataType is file
                (fileNameFieldConvention (entityType.FieldTypes |> Seq.find (fun ft -> ft.Id = fieldTypeID))) >>
                // make it camel case
                toCamelCase

            // the expected identifier
            let result = namingConvention fieldTypeID
            // another field will become the same parameter name
            let hasConflictingField = 
                entityType.FieldTypes 
                |> Seq.filter (fun ft -> fieldTypeID <> ft.Id)
                |> Seq.exists (fun ft -> result = (namingConvention ft.Id))
            // return
            if hasConflictingField then
                // there is a conflict, return the original, but to camel case
                fieldTypeID |> toCamelCase
            else
                result

        // map field type to provided parameter
        List.map (fun (fieldType : Objects.FieldType) ->
            let dataType = DataType.parse (fieldType.DataType, fieldType.CVLId)
            // these fields are all mandatory
            if fieldType.DefaultValue = null then
                // so they are required as constructor parameters
                ProvidedParameter((providedParameterNamingConvention fieldType.Id), dataType |> toType)
            else
                // unless there is a default value, then the constructor parameter can be optional
                ProvidedParameter((providedParameterNamingConvention fieldType.Id), (dataType |> toType), optionalValue = None )
            )

    let mandatoryProvidedParameters =
        fieldTypeToProvidedParameter mandatoryFieldTypes

    // create an invoke expression for generated constructors
    let constructorExpression entityTypeID (fieldTypes : Objects.FieldType list) =
        fun (args : Expr list) ->
            // create the entity
            let emptyConstructorExpr =
                <@
                    // get the entity type
                    let entityType = 
                        match inRiverService.getEntityTypeById(entityTypeID) with
                        | Some result -> result
                        | None -> failwith (sprintf "Was expecting entity type %s, but couldn't find it in inRiver service" entityTypeID)

                    // create a new instance of the entity
                    Entity(Objects.Entity.CreateEntity(entityType))
                    @>
            let _constructorExpression =
                args
                |> List.zip fieldTypes
                |> List.fold (fun entityExpr (fieldType, argExpr) ->
                    let fieldTypeId = fieldType.Id
                    match DataType.parse (fieldType.DataType, fieldType.CVLId) with
                    | String | Xml ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : string)
                            if entity.Entity.GetField(fieldTypeId).FieldType.DefaultValue = null then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <- value
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value
                                    // NOTE we cannot trust the default constructor to set a default value
                                    // for XML because the Remoting API seems broken in this aspect. This
                                    // means if you have a mandatory XML value, you will not be able to
                                    // create entities with the default value.
                                    // We fix this here by setting the default value explicitly.
                                    entity.Entity.GetField(fieldTypeId).Data <- entity.Entity.GetField(fieldTypeId).FieldType.DefaultValue
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value
                            entity
                            @>
                    | Integer ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : int)
                            if entity.Entity.GetField(fieldTypeId).FieldType.DefaultValue = null then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <- value
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value set in empty constructor
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value
                            entity
                            @>
                    | Boolean ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : bool)
                            if entity.Entity.GetField(fieldTypeId).FieldType.DefaultValue = null then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <- value
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value set in empty constructor
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value
                            entity
                            @>
                    | Double ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : double)
                            if entity.Entity.GetField(fieldTypeId).FieldType.DefaultValue = null then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <- value
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value set in empty constructor
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value
                            entity
                            @>
                    | DateTime ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : DateTime)
                            if entity.Entity.GetField(fieldTypeId).FieldType.DefaultValue = null then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <- value
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value set in empty constructor
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value
                            entity
                            @>
                    | LocaleString ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : LocaleString)
                            // BUG This cannot work, must convert LocaleString -> Objects.LocaleString
                            let toObjects (input : LocaleString) =
                                // get languages
                                let languages = 
                                    new System.Collections.Generic.List<System.Globalization.CultureInfo> (
                                        input
                                        |> Map.toSeq
                                        |> Seq.map (fun (lang, value) -> System.Globalization.CultureInfo.GetCultureInfo(lang)))
                                // create result object
                                let result = Objects.LocaleString(languages)
                                // feed it with values
                                input
                                |> Map.toSeq
                                |> Seq.iter (fun (lang, value) -> result.[System.Globalization.CultureInfo.GetCultureInfo(lang)] <- value)
                                // return
                                result

                            if entity.Entity.GetField(fieldTypeId).FieldType.DefaultValue = null then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <- value |> toObjects
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value set in empty constructor
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value |> toObjects
                            entity
                            @>
                    | CVL id ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let field = entity.Entity.GetField(fieldTypeId)
                            let value = (%% argExpr : CVLNode)

                            if field.FieldType.DefaultValue = null then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <- value.CvlValue.Key
                                entity
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value set in empty constructor
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value.CvlValue.Key
                                entity
                            @>
                    | File -> 
                        <@ 
                            let entity = (% entityExpr : Entity)
                            let file = (%% argExpr : File)
                            
                            if (file :> obj) = null then
                                failwith (sprintf "Unable to create %s with %s as null value" entity.EntityType.Id fieldTypeId)
                            else
                                entity.setFile fieldTypeId file
                            entity
                            @>
                    ) emptyConstructorExpr
            <@@ %_constructorExpression @@>
    
    let createExpression entityTypeID =
        fun (args : Expr list) ->
        <@@
            let entity = (%% args.[0] : Objects.Entity)
            // is entity of the correct type?
            match entity.EntityType with
            | null -> failwith (sprintf "Unable to create strong type %s. EntityType was not set on entity." entityTypeID)
            | entityType when entityType.Id <> entityTypeID -> failwith (sprintf "Unable to create strong type %s. Entity source was %s." entityTypeID entity.EntityType.Id)
            | _ -> Entity(entity)                
            @@>

    // will save the entity to inRiver
    // Returns Result of the saved entity.
    // * Ok<TEntity>
    // * Error<Exception>
    let saveExpression =
        fun (args : Expr list) ->
        <@@
            let entity = (%% args.[0] : Entity)
            try
                // must save files first
                entity.NewFiles
                |> Seq.map (fun (fieldTypeId, fileName, data) -> fieldTypeId, inRiverService.createFile fileName data)
                // update the entity
                |> Seq.iter (fun (fieldTypeId, fileId) -> entity.Entity.GetField(fieldTypeId).Data <- fileId)

                // save to inRiver -> wrap result entity in TEntity instance
                Ok (Entity(inRiverService.save(entity.Entity)))
            with
                | ex -> Error ex
            @@>

    let stringValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to string
            (entity.PropertyValue fieldTypeID) :?> string
            @>
            
    let localeStringValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // get the field value
            let localeString = (entity.PropertyValue fieldTypeID) :?> Objects.LocaleString
            // convert to immutable map
            match localeString with
            | null -> Map.empty
            | ls -> ls.Languages
                    |> Seq.map (fun lang -> (lang.Name, localeString.[lang]))
                    |> Map.ofSeq
            @>

    let integerValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to int
            (entity.PropertyValue fieldTypeID) :?> int
            @>

    let dateTimeValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to DateTime
            (entity.PropertyValue fieldTypeID) :?> System.DateTime
            @>

    let doubleValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to double
            (entity.PropertyValue fieldTypeID) :?> double
            @>

    let booleanValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to bool
            (entity.PropertyValue fieldTypeID) :?> bool
            @>
        
    let cvlValueExpression cvlId fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the cvl key
            let cvlValueKey = (entity.PropertyValue fieldTypeID) :?> string
            // get the cvl value
            let cvlValue = match inRiverService.getCvlValueByKey cvlId cvlValueKey with
                           | Some cvlValue -> cvlValue
                           | None -> failwith (sprintf "Was unable to find CVLValue with key %s in service" cvlValueKey)

            // get the cvl type
            let cvlType = match inRiverService.getCvlTypeById cvlId with
                          | Some cvlType -> cvlType
                          | None -> failwith (sprintf "Was expecting CVL with id %s in service" cvlId)
            // create the value
            CVLNode(cvlType, cvlValue)
            @>

    let fileValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)

            match entity.getFileData fieldTypeID with
            | Some data -> data                
            | None ->
                // get the file id
                let fileId = entity.Entity.GetField(fieldTypeID).Data :?> int
                // get the file data
                let data = inRiverService.getFile fileId
                // store the value in internal cache and return
                ignore <| entity.setPersistedFileData fieldTypeID data
                // return data
                data
            @>

    let objValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to string
            (entity.PropertyValue fieldTypeID)
            @>

    // try to create a property name that does not conflict with anything else on the entity
    // Example: A Product entity with the field ProductCreatedBy would conflict with the Entity.CreatedBy property
    // Example: A Product entity with the field ProductAuthor and the field Author would conflict with each other
    let createPropertyName fieldTypeID =
        let namingConvention =
            // remove the entity type id from field name
            (fieldNamingConvention entityType.Id) >>
            // remove Id suffix if field type is File
            (fileNameFieldConvention (entityType.FieldTypes |> Seq.find (fun ft -> ft.Id = fieldTypeID)))
        
        let result = namingConvention fieldTypeID

        // there is a property already on the Entity type matching this name
        let hasFixedProperty = typeof<Entity>.GetProperty(result) <> null
        // another field will become the same property
        let hasConflictingField = 
            entityType.FieldTypes 
            |> Seq.filter (fun ft -> fieldTypeID <> ft.Id)
            |> Seq.exists (fun ft -> result = (namingConvention ft.Id))
        
        if hasFixedProperty || hasConflictingField then
            // there is a conflict, return the original property name
            fieldTypeID
        else
            // there isn't a conflict, return the convention property
            result

    let fieldToProperty (fieldType : Objects.FieldType) propertyName =
        let fieldTypeID = fieldType.Id
        let dataType = DataType.parse (fieldType.DataType, fieldType.CVLId)

        // match on data types and if fieldType is mandatory or not
        match dataType, fieldType.Mandatory with
        | String, true -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((stringValueExpression fieldTypeID) >> uncheckedExpression))
        | String, false -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((stringValueExpression fieldTypeID) >> optionPropertyExpression))
        
        // LocaleString properties will be the same independently if it's mandatory or not
        | LocaleString, _ -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((localeStringValueExpression fieldTypeID) >> uncheckedExpression))

        | DateTime, true -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((dateTimeValueExpression fieldTypeID) >> uncheckedExpression))
        | DateTime, false -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((dateTimeValueExpression fieldTypeID) >> optionPropertyExpression))
        
        | Integer, true -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((integerValueExpression fieldTypeID) >> uncheckedExpression))
        | Integer, false -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((integerValueExpression fieldTypeID) >> optionPropertyExpression))
        
        | Boolean, true -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((booleanValueExpression fieldTypeID) >> uncheckedExpression))
        | Boolean, false -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((booleanValueExpression fieldTypeID) >> optionPropertyExpression))
        
        | Double, true -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((doubleValueExpression fieldTypeID) >> uncheckedExpression))
        | Double, false -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((doubleValueExpression fieldTypeID) >> optionPropertyExpression))
        
        | CVL id, true -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((cvlValueExpression id fieldTypeID) >> uncheckedExpression))
        | CVL id, false -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((cvlValueExpression id fieldTypeID) >> optionPropertyExpression))
        
        | Xml, true -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((stringValueExpression fieldTypeID) >> uncheckedExpression))
        | Xml, false -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((stringValueExpression fieldTypeID) >> optionPropertyExpression))
        
        // File properties are same independently on mandatory parameter, right now Files should always be mandatory
        | File, _ -> ProvidedProperty(propertyName, typeof<byte[]>, [], GetterCode = ((fileValueExpression fieldTypeID) >> uncheckedExpression))

    member this.createProvidedTypeDefinition assembly ns =
        // create the type
        let typeDefinition = ProvidedTypeDefinition(assembly, ns, entityType.Id, Some typeof<Entity>)
        typeDefinition.HideObjectMethods <- true;
        
        // create a constructor
        let ctor = ProvidedConstructor(mandatoryProvidedParameters, InvokeCode = constructorExpression entityType.Id mandatoryFieldTypes)
        typeDefinition.AddMember ctor

        // creation method
        let createMethod = ProvidedMethod("Create", [ProvidedParameter("entity", typeof<Objects.Entity>)], typeDefinition)
        createMethod.IsStaticMethod <- true
        createMethod.InvokeCode <- (createExpression entityType.Id)
        typeDefinition.AddMember createMethod

        // save method
        let saveMethodReturnType = typedefof<Result<_,_>>.MakeGenericType([|(typeDefinition :> Type); typeof<Exception>|])
        let saveMethod = ProvidedMethod("Save", [ProvidedParameter("entity", typeDefinition)], saveMethodReturnType)
        saveMethod.IsStaticMethod <- true
        saveMethod.InvokeCode <- saveExpression
        typeDefinition.AddMember saveMethod

        // add fields as properties
        typeDefinition.AddMembers (
            entityType.FieldTypes 
            |> Seq.map (fun fieldType -> fieldToProperty fieldType (createPropertyName fieldType.Id))
            |> Seq.toList
            )

        // find display properties
        [ "DisplayName",  entityType.FieldTypes |> Seq.tryFind (fun (fieldType : Objects.FieldType) -> fieldType.IsDisplayName)
        ; "DisplayDescription", entityType.FieldTypes |> Seq.tryFind (fun (fieldType : Objects.FieldType) -> fieldType.IsDisplayDescription) ]
        // filter out DisplayName or DisplayDescription if found
        |> List.filter (fun (_, fieldType) -> fieldType.IsSome)
        // map to ProvidedParameter type
        |> List.map (fun (propertyName, fieldType) -> fieldToProperty fieldType.Value propertyName)
        // append to type definition
        |> typeDefinition.AddMembers

        typeDefinition

type CvlTypeFactory(cvlType : Objects.CVL) =
    
    // this is a property expression, when calling a static property FashionMaterial.cotton there
    // should be a new instance of CVLNode created with that cvl value
    let cvlValueExpression cvlId cvlValueId =
        fun args ->
        <@@
            // get the type
            let cvlType = 
                match inRiverService.getCvlTypeById(cvlId) with
                | Some cvlType -> cvlType
                | None -> failwith (sprintf "CVL type with ID %s was not found in inRiver" cvlId)

            // get the value
            let cvlValue = 
                match inRiverService.getCvlValueById(cvlValueId) with
                | Some cvlValue -> cvlValue
                | None -> failwith (sprintf "CVL value with ID %d was not found in inRiver" cvlValueId)
                
            CVLNode(cvlType, cvlValue)
            @@>

    let cvlValuePropertyExpression = function
        | String -> fun (args : Expr list) -> <@@ (%% args.[0] : CVLNode).CvlValue.Value :?> string @@>
        | LocaleString -> 
            fun (args : Expr list) ->
            <@@
                // get the cvl value
                let localeString = (%% args.[0] : CVLNode).CvlValue.Value :?> Objects.LocaleString
                // convert to immutable map
                match localeString with
                | null -> Map.empty
                | ls -> ls.Languages
                        |> Seq.map (fun lang -> (lang.Name, localeString.[lang]))
                        |> Map.ofSeq
                @@>
        | _ -> failwith "Only String and LocalString are supported data types for CVLs"

    let cvlValueToProperty typeDefinition (cvlValue : Objects.CVLValue) =
        let prop = ProvidedProperty(cvlValue.Key, typeDefinition, [], GetterCode = (cvlValueExpression cvlType.Id cvlValue.Id))
        prop.IsStatic <- true
        prop

    // Get the .NET Type for this data type
    let toType = function
        | "LocaleString" -> typeof<LocaleString>
        | "String" -> typeof<string>
        | _ -> failwith "CVLs can only be of String and LocalString types"
        
    member this.createProvidedTypeDefinition assembly ns =
        // create the type
        let typeDefinition = ProvidedTypeDefinition(assembly, ns, cvlType.Id, Some typeof<CVLNode>)
        typeDefinition.HideObjectMethods <- true;

        // create values as static properties
        let cvlValues =
            inRiverService.getCvlValues(cvlType.Id)
                |> Seq.map (cvlValueToProperty typeDefinition)
                |> Seq.toList
        typeDefinition.AddMembers cvlValues

        // create a value property with the cvl value
        let valueProperty = ProvidedProperty("value", (cvlType.DataType |> toType), [], GetterCode = (cvlValuePropertyExpression ((cvlType.DataType, "") |> DataType.parse)))
        typeDefinition.AddMembers [valueProperty]

        typeDefinition


[<TypeProvider>]
type InRiverProvider(config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()

    let ns = "inQuiry.Model"
    let assembly = Assembly.GetExecutingAssembly()

    let cvlTypes =
        inRiverService.getCvlTypes()
            |> Seq.map (fun cvl -> CvlTypeFactory(cvl).createProvidedTypeDefinition assembly ns)
            |> Seq.toList
    
    // get the entity types from InRiver Model Service
    let entityTypes = 
        inRiverService.getEntityTypes() 
            |> Seq.map (fun et -> EntityTypeFactory(cvlTypes, et).createProvidedTypeDefinition assembly ns)
            |> Seq.toList

    do
        this.AddNamespace(ns, entityTypes @ cvlTypes)

    
[<assembly:TypeProviderAssembly>] 
do()
