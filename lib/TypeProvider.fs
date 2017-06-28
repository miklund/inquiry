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
    | NewFile of string * byte array
    /// An already persisted file is just data
    | PersistedFile of byte array

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
    | MultiValueCVL of string
    | DateTime 
    | Double 
    | File 
    | Integer 
    | LocaleString 
    | String 
    | Xml
    | Guid

    /// Parsing a Objects.FieldType into a data type. Mostly using the DataType
    /// property to determine the type, but also CvlID and DefaultValue to
    /// figure out if a string should be a Guid
    static member parse (fieldType : Objects.FieldType) =
        match fieldType with
        | ft when ft.DataType = "Boolean" -> Boolean
        | ft when ft.DataType = "CVL" && ft.Multivalue -> MultiValueCVL (ft.CVLId)
        | ft when ft.DataType = "CVL"-> CVL (ft.CVLId)
        | ft when ft.DataType = "DateTime" -> DateTime
        | ft when ft.DataType = "Double" -> Double
        | ft when ft.DataType = "File" -> File
        | ft when ft.DataType = "Integer" -> Integer
        | ft when ft.DataType = "LocaleString" -> LocaleString
        | ft when ft.DataType = "String" && ft.DefaultValue = "guid" -> Guid
        | ft when ft.DataType = "String" -> String
        | ft when ft.DataType = "Xml" -> Xml
        | _ -> failwith "Failed to determine the data type from the field type."

/// CVL stands for Custom Value List and it is a kind of advanced Enum, quite
/// similiar to the Category concept in EPiServer CMS. This type is the base
/// type for the provided CVL types.
type CVLNode (cvlType : Objects.CVL, cvlValue : Objects.CVLValue) = 

    /// The `Objects.CVL` this provided type was generated from
    member this.CvlType = cvlType

    /// The `Objects.CVLValue` this provided type was generated from
    member this.CvlValue = cvlValue

    /// A CVL can be either of type `String` or `LocaleString`.
    member this.DataType = match cvlType.DataType with | "String" -> String | "LocaleString" -> LocaleString | _ -> failwith "CVL can only have String or LocaleString as datatype"
    
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
    
    /// A window into the internal file representation. Please do
    /// not access this directly. Instead use the File property on
    /// entity you want to modify.
    member this.Files = files

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
                | NewFile (fileName, data) -> Some (fieldTypeId, fileName, data) 
                | _ -> None
            )
      
    /// Because code quotations doesn't like discriminated unions we have
    /// this helper method to get the file data for a field.
    member this.getFileData fileTypeId =
        match files |> Map.tryFind fileTypeId with
        | None -> None
        // it is a new file
        | Some (NewFile (fileName, data)) -> Some data
        // it is an old file
        | Some (PersistedFile data) -> Some data

    /// Will mutate the file Map and update it with a new value. For internal
    /// use this is fine, but not for updating the file properties externally.
    /// Then we need to return a new value of the entitiy
    member this.setFile (fieldTypeId : string) (file : File) = files <- files.Add(fieldTypeId, file)

    /// A wrapper to this.setFile because code quotations can't handle
    /// discriminated unions.
    member this.setPersistedFileData (fieldTypeId : string) (data : byte array) =
        files <- files.Add(fieldTypeId, PersistedFile data)
        files

    member this.clone () =
        let cloneEntity = Objects.Entity.CreateEntity(entity.EntityType)
        
        // copy attributes
        cloneEntity.ChangeSet <- this.Entity.ChangeSet
        cloneEntity.Completeness <- this.Entity.Completeness
        cloneEntity.CreatedBy <- this.Entity.CreatedBy
        cloneEntity.DateCreated <- this.Entity.DateCreated
        cloneEntity.DisplayName <- this.Entity.DisplayName
        cloneEntity.DisplayDescription <- this.Entity.DisplayDescription
        cloneEntity.FieldSetId <- this.Entity.FieldSetId
        cloneEntity.Id <- this.Entity.Id
        cloneEntity.LastModified <- this.Entity.LastModified
        cloneEntity.LoadLevel <- this.Entity.LoadLevel
        cloneEntity.Locked <- this.Entity.Locked
        cloneEntity.MainPictureId <- this.Entity.MainPictureId
        cloneEntity.ModifiedBy <- this.Entity.ModifiedBy
        cloneEntity.Version <- this.Entity.Version

        // copy fields
        this.Entity.Fields
        |> Seq.iteri (fun index field -> cloneEntity.Fields.[index] <- field.Clone() :?> Objects.Field)
    
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
    
/// An immutable set function that will help you work with the entities
/// in an immutable manner. Use it instead of accessing the properties
/// directly
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
    if fieldType |> DataType.parse = File then
        // replace the Id suffix with Data
        // it would be better to just remove the suffix, but what if the name
        // of the property is `Id`, then the result would be empty string,
        // which would not suffice as a legal property name.
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


// We need to parse string as guid, but quotation expressions can't
// deal with ref parameters. That is why we make a wrapper like this.
type System.Guid with
    static member tryParse s =
        match System.Guid.TryParse(s) with
        | success, guid -> (success, guid)


// wrapper making .NET String.join more functional to use
let concat delimiter (items : string seq) =
    System.String.Join(delimiter, items)

type Objects.FieldType with
    /// This is required in constructor if marked as mandatory and do not have
    /// a default value.
    member this.RequiredConstructorParameter =
        // mandatory without default value -> Required
        (this.Mandatory, this.DefaultValue = null) = (true, true)
    
    member this.OptionalConstructorParameter = not this.RequiredConstructorParameter
     
// the equivilalent of Option.get for Result
type Result<'T,'TError> with
    static member get (result : Result<'T, 'TError>) : 'T =
        match result with
        | Ok value -> value
        | Error e -> failwith (sprintf "%A" e)

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
        | MultiValueCVL id -> 
            typedefof<List<_>>.MakeGenericType([|cvlTypes |> List.find (fun t -> t.Name = id) :> Type|])
        | DateTime -> typeof<System.DateTime>
        | Double -> typeof<double>
        | File -> typeof<File>
        | Integer -> typeof<int>
        | LocaleString -> typeof<LocaleString>
        | String -> typeof<string>
        | Xml -> typeof<System.Xml.Linq.XDocument>
        | Guid -> typeof<Guid>

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
        |> Seq.toList
        // filter out only mandatory fields
        |> List.filter (fun fieldType -> fieldType.Mandatory || fieldType.ReadOnly)
        // group by required parameters and not required
        |> List.partition (fun fieldType -> fieldType.Mandatory && fieldType.DefaultValue = null)
        // sort each group by index
        |> fun (required, optional) ->
            // sort the required fields by index
            (required |> List.sortBy (fun fieldType -> fieldType.Index)) @ 
            // sort the optional fields by index
            (optional |> List.sortBy (fun fieldType -> fieldType.Index))
            // concatenate
        
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
            let dataType = DataType.parse fieldType

            // is mandatory, has default value, is read-only
            match fieldType.Mandatory, fieldType.DefaultValue <> null, fieldType.ReadOnly with
            // mandatory but no default value
            | true, false, _ -> ProvidedParameter((providedParameterNamingConvention fieldType.Id), dataType |> toType)
            // mandatory and has default value
            | true, true, _ -> ProvidedParameter((providedParameterNamingConvention fieldType.Id), (dataType |> toType), optionalValue = None )
            // not mandatory, but read-only with no default value
            | false, _, true -> ProvidedParameter((providedParameterNamingConvention fieldType.Id), (dataType |> toType), optionalValue = None )
            // not expected, some new case
            | false, _, false -> failwith "Cannot create constructor, there should only be mandatory or read-only parameters"
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
                    match DataType.parse fieldType with
                    | String ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : string)
                            if entity.Entity.GetField(fieldTypeId).FieldType.RequiredConstructorParameter then
                                // this is a required constructor parameter
                                if value <> null then
                                    entity.Entity.GetField(fieldTypeId).Data <- value
                                else
                                    failwith (sprintf "Failed to create entity, %s.%s is required." entity.EntityType.Id fieldTypeId)
                            else
                                // this is an optional constructor parameter
                                if (value :> obj) = null then
                                    // use the default value
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- value
                            entity
                            @>
                    | Guid ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : Guid)
                            // NOTE This does not follow the pattern of other constructor setters, because
                            // it is different. DefaultValue will never be null, it will always be "guid".
                            // This means there will always be a generated guid in the Data field, but ..
                            // perhaps the user will send a guid they want instead. Then we'll overwrite
                            // the generated guid.
                            if value = System.Guid.Empty then
                                // default generated guid is fine
                                ()
                            else
                                // overwrite the generated guid with this guid
                                entity.Entity.GetField(fieldTypeId).Data <- value.ToString()
                            entity
                            @>
                    | Integer ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : int)
                            if entity.Entity.GetField(fieldTypeId).FieldType.RequiredConstructorParameter then
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
                            if entity.Entity.GetField(fieldTypeId).FieldType.RequiredConstructorParameter then
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
                            if entity.Entity.GetField(fieldTypeId).FieldType.RequiredConstructorParameter then
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
                            if entity.Entity.GetField(fieldTypeId).FieldType.RequiredConstructorParameter then
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

                            if entity.Entity.GetField(fieldTypeId).FieldType.RequiredConstructorParameter then
                                // this is a required constructor parameter
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

                            if field.FieldType.RequiredConstructorParameter then
                                // this is a required constructor parameter
                                if (value :> obj) <> null then
                                    entity.Entity.GetField(fieldTypeId).Data <- value.CvlValue.Key
                                    entity
                                else
                                    failwith (sprintf "Failed to create entity, %s.%s is required." entity.EntityType.Id fieldTypeId)
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
                    | MultiValueCVL id ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let field = entity.Entity.GetField(fieldTypeId)
                            let values = (%% argExpr : CVLNode list)

                            if field.FieldType.RequiredConstructorParameter then
                                // this is a mandatory constructor parameter
                                entity.Entity.GetField(fieldTypeId).Data <-
                                    values
                                    // get the cvl keys
                                    |> List.map (fun cvlNode -> cvlNode.CvlValue.Key)
                                    // concat with semicolon delimiters
                                    |> concat ";"
                                entity
                            else
                                // this is an optional constructor parameter
                                if (values :> obj) = null then
                                    // use the default value set in empty constructor
                                    ()
                                else
                                    // overwrite default value with provided value
                                    entity.Entity.GetField(fieldTypeId).Data <- 
                                        values
                                        // get the cvl keys
                                        |> List.map (fun cvlNode -> cvlNode.CvlValue.Key)
                                        // concat with semicolon delimiters
                                        |> concat ";"
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
                    | Xml ->
                        <@
                            let entity = (% entityExpr : Entity)
                            let value = (%% argExpr : System.Xml.Linq.XDocument)
                            if entity.Entity.GetField(fieldTypeId).FieldType.RequiredConstructorParameter then
                                // this is a required constructor parameter
                                if value <> null then
                                    entity.Entity.GetField(fieldTypeId).Data <- value.ToString()
                                else
                                    failwith (sprintf "Failed to create entity, %s.%s is required." entity.EntityType.Id fieldTypeId)
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
                                    entity.Entity.GetField(fieldTypeId).Data <- value.ToString()
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
                // the following files are about to be orphaned
                let deleteOrphanedFiles = System.Configuration.ConfigurationManager.AppSettings.["inQuiry:deleteOrphanedFiles"]
                let orphanedFileIds =
                    // client has to activly configure this library to delete orphaned files
                    if "true".Equals(deleteOrphanedFiles, StringComparison.InvariantCultureIgnoreCase) then
                        entity.NewFiles
                        // only field types that are unique or we can't know that its orphaned
                        |> Seq.filter (fun (fieldTypeId, _, _) -> entity.Entity.GetField(fieldTypeId).FieldType.Unique)
                        // only where the field already has a value
                        |> Seq.filter (fun (fieldTypeId, _, _) -> entity.Entity.GetField(fieldTypeId).Data <> null)
                        // get the file Ids
                        |> Seq.map (fun (fieldTypeId, _, _) -> entity.Entity.GetField(fieldTypeId).Data :?> int)
                        |> Seq.toList
                    else
                        []

                // must save files first
                entity.NewFiles
                |> Seq.map (fun (fieldTypeId, fileName, data) -> fieldTypeId, inRiverService.createFile fileName data)
                // update the entity with fileId
                // NOTE This might mean that the previous fileId is overwritten
                // by a new fileId, and in that case the previous file might be
                // orphaned. We can't really know in this stage if the file is
                // used anywhere else, so we leave it as it is. The consumer
                // of this API will have to deal with orphaned files.
                |> Seq.iter (fun (fieldTypeId, fileId) -> entity.Entity.GetField(fieldTypeId).Data <- fileId)

                // delete orphanded files
                orphanedFileIds |> List.map inRiverService.deleteFile |> ignore

                // new files are now persisted files
                entity.NewFiles
                |> Seq.iter (fun (fieldTypeId, fileName, data) ->  ignore <| entity.setPersistedFileData fieldTypeId data)

                // save to inRiver -> wrap result entity in TEntity instance
                Ok (Entity(inRiverService.save(entity.Entity)))

            with
                | ex -> Error ex
            @@>

    // get an entity by id
    let getMethodExpression entityTypeID =
        fun (args : Expr list) ->
        <@@
            let entityId = (%% args.[0] : int)
            
            try
                match inRiverService.getEntity entityId with
                // found an entity, but it has the wrong type
                // we could just return None, but I think the consumer screwed up
                // and would like to know about it.
                | Some entity when entity.EntityType.Id <> entityTypeID ->
                    Error (System.Exception(sprintf "Tried to get entity %d of entity type %s, but it was %s" entityId entityTypeID entity.EntityType.Id))
                // found entity
                | Some entity -> Ok (Entity(entity))
                // didn't find entity
                | None -> Error (System.Exception(sprintf "Tried to get entity %d of entity type %s, but it was not found" entityId entityTypeID))
            with
                | ex -> Error ex
            @@>

    let getUniqueMethodExpression (fieldType : Objects.FieldType) =
        let fieldTypeID = fieldType.Id
        let entityTypeID = fieldType.EntityTypeId
        let _getUniqueMethodExpression toStringConverterExpression =
            fun (args : Expr list) ->
            <@@
                let value = (% toStringConverterExpression(args.[0]))

                try
                    match inRiverService.getEntityByUniqueValue fieldTypeID value with
                    // found entity, should only be able to find entities of the correct entity type
                    | Some entity -> Ok (Entity(entity))
                    // didn't find entity
                    | None -> Error (System.Exception(sprintf "Tried to get %s by %s = %s, but it was not found" entityTypeID fieldTypeID value))
                with
                    | ex -> Error ex
                @@>
            
        // Some code is really fun to write
        match fieldType |> DataType.parse with
        | String -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : string) @>)
        | Boolean -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : bool).ToString() @>)
        | CVL id -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : CVLNode).CvlValue.Key @>)
        | MultiValueCVL id -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : CVLNode list) |> List.map (fun cvlNode -> cvlNode.CvlValue.Key) |> concat ";" @>)
        | DateTime -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : DateTime).ToString() @>)
        | Double -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : double).ToString() @>)
        | File -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : int).ToString() @>)
        | Integer -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : int).ToString() @>)
        | LocaleString -> _getUniqueMethodExpression (fun expr -> <@ ((%% expr : LocaleString) |> toObjects).ToString() @>)
        | Xml -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : System.Xml.Linq.XDocument).ToString() @>)
        | Guid -> _getUniqueMethodExpression (fun expr -> <@ (%% expr : Guid).ToString() @>)
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

    let getGuidValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to Guid
            match Guid.tryParse(entity |> getFieldData fieldTypeID :?> string) with
            | true, guid -> Some guid
            | false, _ -> None
            @@>

    let setGuidValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            match (%% args.[1] : Guid option) with
            // if some value set it
            | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value.ToString()
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null  
            @@>
            
    let getLocaleStringValueExpression fieldTypeID =
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

    let getIntegerValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to int
            match (entity |> getFieldData fieldTypeID) with
            | null -> None
            | o -> Some (o :?> int)
            @@>

    let setIntegerValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            match (%% args.[1] : int option) with
            // if some value set it
            | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null 
            @@>

    // Expr list -> Expr<DateTime option>
    let getDateTimeValueExpression fieldTypeID =
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

    let getDoubleValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to double
            match (entity |> getFieldData fieldTypeID) with
            | null -> None
            | o -> Some (o :?> double)
            @@>

    let setDoubleValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            match (%% args.[1] : double option) with
            // if some value set it
            | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null 
            @@>

    let getBooleanValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to bool
            match (entity |> getFieldData fieldTypeID) with
            | null -> None
            | o -> Some (o :?> bool)
            @@>
        
    let setBooleanValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            match (%% args.[1] : bool option) with
            // if some value set it
            | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null 
            @@>

    let getCvlValueExpression cvlId fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the cvl key
            let cvlValueKey = (entity |> getFieldData fieldTypeID) :?> string
            if cvlValueKey = null then
                None
            else
                // get the cvl value
                let cvlValue = match inRiverService.getCvlValueByKey cvlId cvlValueKey with
                               | Some cvlValue -> cvlValue
                               | None -> failwith (sprintf "Was unable to find CVLValue with key %s in service" cvlValueKey)

                // get the cvl type
                let cvlType = match inRiverService.getCvlTypeById cvlId with
                              | Some cvlType -> cvlType
                              | None -> failwith (sprintf "Was expecting CVL with id %s in service" cvlId)

                // create the value
                Some (CVLNode(cvlType, cvlValue))
            @@>

    let setCvlValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            //get the entity
            let entity = ( %% args.[0] : Entity)
            // get the value
            match (%% args.[1] : CVLNode option) with
            // if some value set it
            | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value.CvlValue.Key
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null 
            @@>

    let getMultiValueCvlExpression cvlId fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the cvl keys
            let cvlValueKeys = (entity |> getFieldData fieldTypeID) :?> string
            if cvlValueKeys = null then
                []
            else
                // get the cvl type
                let cvlType = match inRiverService.getCvlTypeById cvlId with
                              | Some cvlType -> cvlType
                              | None -> failwith (sprintf "Was expecting CVL with id %s in service" cvlId)
                
                // read the individual keys from the string
                cvlValueKeys.Split([|';'|])
                |> Array.toList
                // find CVL values for each key
                |> List.choose (fun cvlValueKey -> inRiverService.getCvlValueByKey cvlId cvlValueKey)
                // create CVL nodes
                |> List.map (fun cvlValue -> CVLNode(cvlType, cvlValue))                        
            @@>

    let setMultiValueCvlExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = ( %% args.[0] : Entity)
            // get the values
            let cvlValues = 
                (%% args.[1] : CVLNode list)
                |> List.map (fun cvlNode -> cvlNode.CvlValue.Key)
                |> concat ";"

            match cvlValues with
            // if no value, but field is mandatory = fail
            | "" | null when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | "" | null -> entity.Entity.GetField(fieldTypeID).Data <- null
            // if some value set it
            | _ -> entity.Entity.GetField(fieldTypeID).Data <- cvlValues
            @@>

    let getFileValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)

            match entity.Files |> Map.tryFind fieldTypeID with
            | Some file -> Some file
            | None ->
                if entity.Entity.GetField(fieldTypeID).Data = null then
                    // the fileId field is not set => no file
                    None
                else
                    // get the file id
                    let fileId = entity.Entity.GetField(fieldTypeID).Data :?> int
                    // get the file data
                    let data = inRiverService.getFile fileId

                    if data = null then
                        // no file with that fileId was found (uh-oh!) => None
                        None
                    else
                        // store the value in internal cache and return
                        ignore <| entity.setPersistedFileData fieldTypeID data
                        // return data
                        Some (PersistedFile data)
            @>

    let setFileValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            match (%% args.[1] : File option) with
            | Some file -> 
                if (file :> obj) = null then
                    failwith (sprintf "Unable to create %s with %s as null value" entity.EntityType.Id fieldTypeID)
                else
                    // store file in entity cache, it will be saved later by save function
                    entity.setFile fieldTypeID file
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null                 
            @@>
            
    let getXmlValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            let xmlString = (entity |> getFieldData fieldTypeID) :?> string
            if System.String.IsNullOrEmpty(xmlString) then
                None
            else
                // this will potentially crash if the XML data is bad
                // better to let the caller know there is a problem than
                // ignoring it and returning None
                Some (System.Xml.Linq.XDocument.Parse(xmlString))
            @@>

    let setXmlValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // get the value
            match (%% args.[1] : System.Xml.Linq.XDocument option) with
            // if some value set it
            | Some value -> entity.Entity.GetField(fieldTypeID).Data <- value.ToString()
            // if no value, but field is mandatory = fail
            | None when entity.Entity.GetField(fieldTypeID).FieldType.Mandatory -> failwith  (sprintf "Cannot set %s.%s to None, because it is marked as Mandatory" entity.EntityType.Id fieldTypeID)
            // if no value, set to null (this is also for primitive types)
            | None -> entity.Entity.GetField(fieldTypeID).Data <- null   
            @@>


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
        let dataType = DataType.parse fieldType

        // create property of different data types
        let property =
            match dataType with
            | String -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((getStringValueExpression fieldTypeID) >> optionPropertyExpression))
            | LocaleString -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = ((getLocaleStringValueExpression fieldTypeID) >> uncheckedExpression))
            | DateTime -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (getDateTimeValueExpression fieldTypeID))
            | Integer -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (getIntegerValueExpression fieldTypeID))
            | Boolean -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (getBooleanValueExpression fieldTypeID))
            | Double -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (getDoubleValueExpression fieldTypeID))
            | CVL id -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (getCvlValueExpression id fieldTypeID))
            | MultiValueCVL id -> ProvidedProperty(propertyName, (toType dataType), [], GetterCode = (getMultiValueCvlExpression id fieldTypeID))
            | Xml -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (getXmlValueExpression fieldTypeID))
            | File -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = ((getFileValueExpression fieldTypeID) >> uncheckedExpression))
            | Guid -> ProvidedProperty(propertyName, (toOptionType dataType), [], GetterCode = (getGuidValueExpression fieldTypeID))
        
        if fieldType.ReadOnly then
            // return property as it is and do not add setter
            property
        else
            property.SetterCode <- 
                match dataType with
                | String -> setStringValueExpression fieldTypeID
                | LocaleString -> setLocaleStringValueExpression fieldTypeID
                | DateTime -> setDateTimeValueExpression fieldTypeID
                | Integer -> setIntegerValueExpression fieldTypeID
                | Boolean -> setBooleanValueExpression fieldTypeID
                | Double -> setDoubleValueExpression fieldTypeID
                | CVL id -> setCvlValueExpression fieldTypeID
                | MultiValueCVL id -> setMultiValueCvlExpression fieldTypeID
                | Xml -> setXmlValueExpression fieldTypeID
                | File -> setFileValueExpression fieldTypeID
                | Guid -> setGuidValueExpression fieldTypeID

            // return
            property

    member this.createProvidedTypeDefinition assembly ns =
        // create the type
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

        // get method
        let getMethodReturnType = typedefof<Result<_,_>>.MakeGenericType([|typeDefinition :> Type; typeof<Exception>|])
        let getMethod = ProvidedMethod("get", [ProvidedParameter("id", typeof<int>)], getMethodReturnType)
        getMethod.IsStaticMethod <- true
        getMethod.InvokeCode <- getMethodExpression entityType.Id
        typeDefinition.AddMember getMethod

        // get unique methods
        let getUniqueMethodReturnType = typedefof<Result<_,_>>.MakeGenericType([|typeDefinition :> Type; typeof<Exception>|])
        entityType.FieldTypes 
        |> Seq.toList
        |> List.filter (fun ft -> ft.Unique)
        // OH MY GOD! Please refactor this mess
        |> List.map (fun ft -> ProvidedMethod("getBy" + (fieldNamingConvention entityType.Id ft.Id), [ProvidedParameter((fieldNamingConvention entityType.Id ft.Id) |> toCamelCase, match DataType.parse ft with | File -> typeof<int> | dt -> dt |> toType)], getUniqueMethodReturnType, IsStaticMethod = true, InvokeCode = getUniqueMethodExpression ft))
        |> List.iter typeDefinition.AddMember

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
        let dataType = match cvlType.DataType with | "String" -> String | "LocaleString" -> LocaleString | _ -> failwith "CVL can only have String or LocaleString as datatype"
        let valueProperty = ProvidedProperty("value", (cvlType.DataType |> toType), [], GetterCode = (cvlValuePropertyExpression dataType))
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