/// The type provider module has all the functionality needed in order to build
/// strong types from the type information that comes from inRiver Model Service.
/// This module depends on inRiver.Remoting.dll for the Entity domain model and
/// connection to the inRiver Server.
/// The module also depends on FSharp.TypeProviders.StarterPack that is pulled in
/// by source from Github with Paket. (http://github.com/fsprojects/FSharp.TypeProviders.StarterPack)
module inQuiry.TypeProvider

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open inQuiry
open inRiver.Remoting

//
// SUPPORT TYPES
//

/// The `Objects.LocaleString` is a mutable dictionary that's hard to work with
/// in F#, so we replace that with our own implementation of an immutable Map.
type LocaleString = Map<string, string>

// convert LocaleString -> Objects.LocaleString
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

/// The File type in inRiver is an `int` referencing a file in UtilityService.
/// In inQuiry we replace the int with the `byte array` data, lazy loaded for
/// performance. When we create the file, however, we also need the a string
/// for the fileName.
///
/// NOTE: Discriminated unions doesn't work very well within quotation
/// expressions, causing some weird code around the handling of this type.
type File = 
    /// A new file is a fileName and file data
    | New of string * byte array
    /// An already persisted file is just data
    | Persisted of byte array

/// The File cache is a temporary storage for files with the entity. When we
/// create an entity with File as constructor argument we want to delay the
/// call to `UtilityService` until the consumer calls `save` on the entity.
/// Then we want the File to be persisted and the Entity updated with the
/// FileId.
type FileCache = Map<string, File>


/// On the `Objects.Entity` type, the DataType of the entity is represented
/// with a string. In the type provider we use a discriminated union to make
/// matching on the data type less painful.
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

    /// Parsing a string * string into a data type. First part of the tuple is
    /// the `Objects.EntityType.DataType` string and the second part is the
    /// `Objects.EntityType.CVLId`, as CVLs are references to other types.
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

/// CVL stands for Custom Value List and it is a kind of advanced Enum, quite
/// similiar to the Category concept in EPiServer CMS. This type is the base
/// type for the provided CVL types.
type CVLNode (cvlType : Objects.CVL, cvlValue : Objects.CVLValue) = 

    /// The `Objects.CVL` this provided type was generated from
    member this.CvlType = cvlType

    /// The `Objects.CVLValue` this provided type was generated from
    member this.CvlValue = cvlValue

    /// A CVL can be either of type `String` or `LocaleString`.
    member this.DataType = (cvlType.DataType, cvlValue.CVLId) |> DataType.parse
    
    override x.GetHashCode () =
        hash (cvlType, cvlValue)
    
    override x.Equals(b) =
        match b with
        | :? CVLNode as cvl -> (cvlValue.Id, cvlValue.CVLId) = (cvl.CvlValue.Id, cvl.CvlValue.CVLId)
        | _ -> false
    
    override x.ToString () = sprintf "(%s)" (cvlValue.ToString())


/// Base type for the generated entities from the ModelService. It contains
/// all the properties that are native to the `Objects.Entity`, and the rest
/// of the properties are generated from `Objects.EntityType.FieldTypes`.
type Entity (entityType, entity : Objects.Entity)  =

    // This is an ugly solution to keep track on what files have been saved
    // and what files are already persisted.
    let mutable files : FileCache = Map.empty<string, File>
    
    // Only called with entityType, create a new entity
    new(entityType) = Entity(entityType, Objects.Entity.CreateEntity(entityType))

    // Called with entity, make sure we extract entityType
    new(entity : Objects.Entity) = Entity(entity.EntityType, entity)

    /// The `Object.Entity` that this instance is based on.
    member this.Entity = entity

    /// The `Object.EntityType` that this type is generated from.
    member this.EntityType = entityType
    
    /// Get all new files stored in this entity with the return type
    /// string * string * string. The first value of the tuple is the
    /// fileTypeId this file belongs to. The second is the filename and
    /// the last part of the tuple is the byte data.
    member this.NewFiles =
        files
        |> Map.toSeq
        // find all new files
        |> Seq.choose 
            (fun (fieldTypeId, file) -> 
                match file with 
                | New (fileName, data) -> Some (fieldTypeId, fileName, data) 
                | _ -> None
            )
      
    /// Because code quotations doesn't like discriminated unions we have
    /// this helper method to get the file data for a field.
    member this.getFileData fileTypeId =
        match files |> Map.tryFind fileTypeId with
        | None -> None
        // it is a new file
        | Some (New (fileName, data)) -> Some data
        // it is an old file
        | Some (Persisted data) -> Some data

    /// Will mutate the file Map and update it with a new value. For internal
    /// use this is fine, but not for updating the file properties externally.
    /// Then we need to return a new value of the entitiy
    member this.setFile (fieldTypeId : string) (file : File) = files <- files.Add(fieldTypeId, file)

    /// A wrapper to this.setFile because code quotations can't handle
    /// discriminated unions.
    member this.setPersistedFileData (fieldTypeId : string) (data : byte array) =
        files <- files.Add(fieldTypeId, Persisted data)
        files

    member this.clone () =
        let cloneEntity = Objects.Entity.CreateEntity(entity.EntityType)
        
        // copy fields
        this.Entity.Fields
        |> Seq.iter (fun field -> cloneEntity.GetField(field.FieldType.Id).Data <- (field.Clone() :?> Objects.Field).Data)
    
        // copy links
        this.Entity.Links
        |> Seq.iter (fun link -> cloneEntity.Links.Add(Objects.Link(Id = link.Id, Inactive = link.Inactive, Index = link.Index, LinkEntity = link.LinkEntity, LinkType = link.LinkType, Source = link.Source, Target = link.Target)))

        // create result instance
        let result = Entity(cloneEntity)

        // copy files
        files |> Map.toSeq |> Seq.iter (fun (fileTypeId, file) -> result.setFile fileTypeId file)

        // return 
        result

    //
    // Here comes default properties that are just references to the respective
    // values in the Entity.
    //

    member this.ChangeSet = entity.ChangeSet

    member this.Completeness =
        if entity.Completeness.HasValue then
            Some entity.Completeness.Value
        else
            None

    member this.CreatedBy = entity.CreatedBy
    
    member this.DateCreated = entity.DateCreated
        
    member this.FieldSetId = entity.FieldSetId
    
    member this.Id = entity.Id

    member this.LastModified = entity.LastModified

    member this.LoadLevel = entity.LoadLevel

    member this.Locked = entity.Locked

    // TODO: This should probably be a file
    member this.MainPictureId =
        if entity.MainPictureId.HasValue then
            Some entity.MainPictureId.Value
        else
            None
    
    member this.ModifiedBy = entity.ModifiedBy

    member this.Version = entity.Version
    

let set<'a when 'a :> Entity> (update : 'a -> unit) (entity : 'a) =
    // Here I create a new instance of Entity and upcast it to 'a, ex. Product
    // This is not possible in normal code, you cannot upcast to a sub class.
    // But in this case the upcast will happen in runtime, when Product doesn't
    // exist and all instances of 'a, actually is an Entity instance.
    // Welcome to the fabulous world of meta programming with type providers!
    let clone = entity.clone() :?> 'a
    // mutate the state
    update(clone)
    // return
    clone


//
// NAMING CONVENTIONS
//


/// If the field name starts with the entityID, ex. ProductNumber, remove the entityID part, ex. Number
let fieldNamingConvention entityID fieldName =
    // try get configuration value if this naming convention is active
    let foundConfigValue, value = bool.TryParse(Configuration.ConfigurationManager.AppSettings.["inQuiry:fieldNamingConvention"])
    // apply fieldNamingConvention if configuration not found or unparsable, otherwise use configuration value
    if (not foundConfigValue) || value then
        // remove entityID if it appears at the beginning of the fieldName
        System.Text.RegularExpressions.Regex.Replace(fieldName, "^" + entityID, System.String.Empty)
    else
        fieldName

/// make first letter lower case
/// > camelCase "ProductName"
/// -> "productName"
let toCamelCase = function
| s when s = null -> null
| s when s = String.Empty -> s
| s when s.Length = 1 -> s.ToLower()
| s -> System.Char.ToLower(s.[0]).ToString() + s.Substring(1)


/// If the field is of type File and has a "Id" suffix, remove the suffix.
let fileNameFieldConvention (fieldType : Objects.FieldType) (input : string) =
    // if field type is  File
    if (fieldType.DataType, fieldType.CVLId) |> DataType.parse = File then
        // replace the Id suffix with Data
        System.Text.RegularExpressions.Regex.Replace(input, "Id$", "Data")
    else
        // otherwise return as it is
        input


//
// UTILS
//

/// Get data of a field. If the field is null (not set) this function throws
/// an exception. This can happen when the entity model is changed without
/// recompiling the integration code.
let getFieldData fieldTypeId (entity : Entity)  = 
    match entity.Entity.GetField(fieldTypeId) with
    // expecting a field, but it is not there
    | null -> failwith (sprintf "Field %s was not set on entity %s:%d" fieldTypeId entity.EntityType.Id entity.Id)
    | field -> field.Data


/// Turn a value expression to an option value expression
let optionPropertyExpression<'a when 'a : equality> (valueExpression : Expr<'a>) =
    <@@
        let value = (%valueExpression : 'a)
        if value = Unchecked.defaultof<'a> then
            None
        else
            Some(value)
        @@>

/// Make an typed expression into an untyped expression
let uncheckedExpression (valueExpression : Expr<'a>) =
    <@@ (% valueExpression : 'a ) @@>


//
// TYPE FACTORIES
// - EntityTypeFactory for creating Product, Item, Resource and so on
// - CvlTypeFactory for creating CVL ProductStatus, Gender, Industry and so on
//   


/// Create one type out of one EntityType. Need to have list of generated CVL
/// types in order to create properties of those types.
type EntityTypeFactory (cvlTypes : ProvidedTypeDefinition list, entityType : Objects.EntityType)  =

    //
    // DATATYPE
    //

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

    //
    // FIELDS
    //

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
                                // store file in entity cache
                                entity.setFile fieldTypeId file
                            entity
                            @>
                    ) emptyConstructorExpr
            <@@ %_constructorExpression @@>
    
    /// Creation method for entity, static method `create`
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


    //
    // Property expressions
    //

    let getStringValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to string
            (entity |> getFieldData fieldTypeID) :?> string
            @>

    let setStringValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            match (%% args.[1] : string option) with
            // if some value set it
            | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null            
            @@>
            
    let localeStringValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // get the field value
            let localeString = (entity |> getFieldData fieldTypeID) :?> Objects.LocaleString
            // convert to immutable map
            match localeString with
            | null -> Map.empty
            | ls -> ls.Languages
                    |> Seq.map (fun lang -> (lang.Name, localeString.[lang]))
                    |> Map.ofSeq
            @>

    let setLocaleStringValueExpression fieldTypeID =
        fun (args : Expr list) ->
            <@@
                // get the entity
                let entity = (%% args.[0] : Entity)
                // get the value
                let value = (%% args.[1] : LocaleString)
                // set the value
                entity.Entity.GetField(fieldTypeID).Data <- value |> toObjects
                @@>

    let integerValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to int
            (entity |> getFieldData fieldTypeID) :?> int
            @>

    // Expr list -> Expr<DateTime option>
    let dateTimeValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to DateTime
            match (entity |> getFieldData fieldTypeID) with
            | null -> None
            | o -> Some (o:?> System.DateTime)
            @@>

    let setDateTimeValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
                let entity = (%% args.[0] : Entity)
                // get the value
                match (%% args.[1] : DateTime option) with
                // if some value set it
                | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value
                // if no value, but field is mandatory = fail
                | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
                // if no value, set to null (this is also for primitive types)
                | None -> entity.Entity.GetField(fieldTypeID).Data <- null 
            @@>

    let doubleValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to double
            (entity |> getFieldData fieldTypeID) :?> double
            @>

    let booleanValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to bool
            (entity |> getFieldData fieldTypeID) :?> bool
            @>
        
    let cvlValueExpression cvlId fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the cvl key
            let cvlValueKey = (entity |> getFieldData fieldTypeID) :?> string
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
        match dataType with
        | String -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((getStringValueExpression fieldTypeID) >> optionPropertyExpression), SetterCode = (setStringValueExpression fieldTypeID))
        | LocaleString -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((localeStringValueExpression fieldTypeID) >> uncheckedExpression), SetterCode = (setLocaleStringValueExpression fieldTypeID))
        | DateTime -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (dateTimeValueExpression fieldTypeID), SetterCode = (setDateTimeValueExpression fieldTypeID))
        | Integer -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((integerValueExpression fieldTypeID) >> optionPropertyExpression))
        | Boolean -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((booleanValueExpression fieldTypeID) >> optionPropertyExpression))
        | Double -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((doubleValueExpression fieldTypeID) >> optionPropertyExpression))
        | CVL id -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((cvlValueExpression id fieldTypeID) >> optionPropertyExpression))
        | Xml -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((getStringValueExpression fieldTypeID) >> optionPropertyExpression))
        | File -> ProvidedProperty(propertyName, typeof<byte[]>, [], GetterCode = ((fileValueExpression fieldTypeID) >> uncheckedExpression))

    member this.createProvidedTypeDefinition assembly ns =
        // create the type
        //let typeDefinition = ProvidedTypeDefinition(assembly, ns, entityType.Id, Some typeof<Entity>)
        let typeDefinition = ProvidedTypeDefinition(entityType.Id, Some typeof<Entity>)
        typeDefinition.HideObjectMethods <- true;
        
        let ctor = ProvidedConstructor(mandatoryProvidedParameters, InvokeCode = constructorExpression entityType.Id mandatoryFieldTypes)
        typeDefinition.AddMember ctor

        // creation method
        let createMethod = ProvidedMethod("create", [ProvidedParameter("entity", typeof<Objects.Entity>)], typeDefinition)
        createMethod.IsStaticMethod <- true
        createMethod.InvokeCode <- (createExpression entityType.Id)
        typeDefinition.AddMember createMethod

        // save method
        let saveMethodReturnType = typedefof<Result<_,_>>.MakeGenericType([|(typeDefinition :> Type); typeof<Exception>|])
        let saveMethod = ProvidedMethod("save", [ProvidedParameter("entity", typeDefinition)], saveMethodReturnType)
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

/// Generate strong types for the CVLs in ModelService.
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
        //let typeDefinition = ProvidedTypeDefinition(assembly, ns, cvlType.Id, Some typeof<CVLNode>)
        let typeDefinition = ProvidedTypeDefinition(cvlType.Id, Some typeof<CVLNode>)
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
type inRiverProvider(config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()

    let ns = "inQuiry.Model"
    let asm = Assembly.GetExecutingAssembly()

    let inRiverProvider = ProvidedTypeDefinition(asm, ns, "inRiverProvider", Some(typeof<obj>))
    
    let parameters = 
        [ ProvidedStaticParameter("host", typeof<string>)
        ; ProvidedStaticParameter("userName", typeof<string>)
        ; ProvidedStaticParameter("password", typeof<string>)
        ]
    
    do inRiverProvider.DefineStaticParameters(parameters, fun typeName args ->

        // set connection details
        inRiverService.connection <- {
                host = args.[0] :?> string
                userName = args.[1] :?> string
                password = args.[2] :?> string
            }

        // internal provider
        let provider = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, HideObjectMethods = true)
        
        let cvlTypes =
            inRiverService.getCvlTypes()
                |> Seq.map (fun cvl -> CvlTypeFactory(cvl).createProvidedTypeDefinition asm ns)
                |> Seq.toList
    
        // get the entity types from InRiver Model Service
        let entityTypes = 
            inRiverService.getEntityTypes() 
                |> Seq.map (fun et -> EntityTypeFactory(cvlTypes, et).createProvidedTypeDefinition asm ns)
                |> Seq.toList

        provider.AddMembers(entityTypes @ cvlTypes)

        // return
        provider
    )

    do this.AddNamespace(ns, [inRiverProvider])
    
    
[<assembly:TypeProviderAssembly>] 
do()