module inQuiry.TypeProvider

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open inQuiry
open inRiver.Remoting

// turn a value expression to an option value expression
let optionPropertyExpression<'a when 'a : equality> (valueExpression : Expr<'a>) =
    <@@
        let value = (%valueExpression : 'a)
        if value = Unchecked.defaultof<'a> then
            None
        else
            Some(value)
        @@>

// this is the entity value that is returned from the type provider
type Entity (entityType, entity : Objects.Entity)  =

    // only called with entityType, create a new entity
    new(entityType) = Entity(entityType, Objects.Entity.CreateEntity(entityType))

    // called with entity, make sure we extract entityType
    new(entity : Objects.Entity) = Entity(entity.EntityType, entity)

    member this.PropertyValue fieldTypeId  = 
        match entity.GetField(fieldTypeId) with
        // expected a field, but it is not there
        | null -> failwith (sprintf "Field %s was not set on entity %s:%d" fieldTypeId entityType.Id entity.Id)
        | field -> field.Data

    member this.EntityType = entityType

    member this.Entity = entity
    
    // default properties
    member this.CreatedBy
        with get () = entity.CreatedBy
        and set (value) = entity.CreatedBy <- value
   
type EntityTypeFactory (entityType : Objects.EntityType)  =

    // map inRiver DataType to .NET types
    let mapDataType = function
    | "String" -> typeof<string>
    | "LocaleString" -> typeof<Objects.LocaleString>
    | "DateTime" -> typeof<System.DateTime>
    | "Integer" -> typeof<int>
    | "Boolean" -> typeof<bool>
    | _ -> typeof<obj>
    
    // filter all FieldTypes that are mandatory
    let mandatoryFieldTypes =
        entityType.FieldTypes
        // filter out only mandatory fields
        |> Seq.filter (fun fieldType -> fieldType.Mandatory)
        // make sure they're sorted by index
        |> Seq.sortBy (fun fieldType -> fieldType.Index)
        |> Seq.toList

    let fieldTypeToProvidedParameter =
        // map field type to provided parameter
        List.map (fun (fieldType : Objects.FieldType) -> ProvidedParameter(fieldType.Id, mapDataType(fieldType.DataType)))

    let mandatoryProvidedParameters =
        fieldTypeToProvidedParameter mandatoryFieldTypes

    // empty constructor for entities
    let constructorExpression0 entityTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity type, this should be cached
            let entityType = inRiverService().GetEntityTypeById(entityTypeID)
            // create a new instance of the entity
            Entity(Objects.Entity.CreateEntity(entityType))
            @@>

    // constructors with 1 arguments
    let constructorExpression1 entityTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity type, this should be cached
            let entityType = inRiverService().GetEntityTypeById(entityTypeID)
            // create a new instance of the entity
            let entity = Entity(Objects.Entity.CreateEntity(entityType))
            
            // this pattern is incomplete because we know there will be three arguments always
            let [ft0] =
                entityType.FieldTypes
                |> Seq.filter (fun fieldType -> fieldType.Mandatory)
                |> Seq.sortBy (fun fieldType -> fieldType.Index)
                |> Seq.toList
            
            match ft0.DataType with
            | "String" -> entity.Entity.GetField(ft0.Id).Data <- (%%args.[0] : string)
            | dt -> failwith (sprintf "constructor1 cannot handle data type %s yet" dt)

            entity
            @@>

    // constructors with 2 arguments
    let constructorExpression2 entityTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity type, this should be cached
            let entityType = inRiverService().GetEntityTypeById(entityTypeID)
            // create a new instance of the entity
            let entity = Entity(Objects.Entity.CreateEntity(entityType))
            
            // this pattern is incomplete because we know there will be three arguments always
            let [ft0; ft1] =
                entityType.FieldTypes
                |> Seq.filter (fun fieldType -> fieldType.Mandatory)
                |> Seq.sortBy (fun fieldType -> fieldType.Index)
                |> Seq.toList
            
            match ft0.DataType with
            | "String" -> entity.Entity.GetField(ft0.Id).Data <- (%%args.[0] : string)
            | dt -> failwith (sprintf "constructor1 cannot handle data type %s yet" dt)

            match ft1.DataType with
            | "CVL" -> entity.Entity.GetField(ft1.Id).Data <- (%%args.[1] : obj)
            | dt -> failwith (sprintf "constructor1 cannot handle data type %s yet" dt)

            entity
            @@>

    // constructor with 3 arguments
    let constructorExpression3 entityTypeID =
        fun (args : Expr list) ->
        <@@
            // get the entity type, this should be cached
            let entityType = inRiverService().GetEntityTypeById(entityTypeID)
            // create a new instance of the entity
            let entity = Entity(Objects.Entity.CreateEntity(entityType))
            
            // this pattern is incomplete because we know there will be three arguments always
            let [ft0; ft1; ft2] =
                entityType.FieldTypes
                |> Seq.filter (fun fieldType -> fieldType.Mandatory)
                |> Seq.sortBy (fun fieldType -> fieldType.Index)
                |> Seq.toList
            
            match ft0.DataType with
            | "String" -> entity.Entity.GetField(ft0.Id).Data <- (%%args.[0] : string)
            | dt -> failwith (sprintf "constructor3 cannot handle data type %s yet" dt)

            match ft1.DataType with
            | "CVL" -> entity.Entity.GetField(ft1.Id).Data <- (%%args.[1] : obj)
            | dt -> failwith (sprintf "constructor3 cannot handle data type %s yet" dt)

            match ft2.DataType with
            | "Boolean" -> entity.Entity.GetField(ft2.Id).Data <- (%%args.[2] : bool)
            | dt -> failwith (sprintf "constructor3 cannot handle data type %s yet" dt)

            entity
            @@>
    
    let createExpression =
        fun (args : Expr list) ->
        <@@
            Entity(%%(args.[0]) : Objects.Entity)
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
            // convert the value to LocaleString
            (entity.PropertyValue fieldTypeID) :?> Objects.LocaleString
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

    let booleanValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%% args.[0] : Entity)
            // convert the value to bool
            (entity.PropertyValue fieldTypeID) :?> bool
            @>

    let objValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to string
            (entity.PropertyValue fieldTypeID)
            @>

    let fieldToProperty (fieldType : Objects.FieldType) =
        let fieldTypeID = fieldType.Id
        // match field DataType to a .NET type
        match fieldType.DataType with
        | "String" -> ProvidedProperty(fieldTypeID, typeof<Option<string>>, [], GetterCode = ((stringValueExpression fieldTypeID) >> optionPropertyExpression))
        | "LocaleString" -> ProvidedProperty(fieldTypeID, typeof<Option<Objects.LocaleString>>, [], GetterCode = ((localeStringValueExpression fieldTypeID) >> optionPropertyExpression))
        | "DateTime" -> ProvidedProperty(fieldTypeID, typeof<Option<System.DateTime>>, [], GetterCode = ((dateTimeValueExpression fieldTypeID) >> optionPropertyExpression))
        | "Integer" -> ProvidedProperty(fieldTypeID, typeof<Option<int>>, [], GetterCode = ((integerValueExpression fieldTypeID) >> optionPropertyExpression))
        | "Boolean" -> ProvidedProperty(fieldTypeID, typeof<Option<bool>>, [], GetterCode = ((booleanValueExpression fieldTypeID) >> optionPropertyExpression))
        | _ -> ProvidedProperty(fieldTypeID, typeof<Option<obj>>, [], GetterCode = ((objValueExpression fieldTypeID) >> optionPropertyExpression))

    member this.createProvidedTypeDefinition assembly ns =
        // create the type
        let typeDefinition = ProvidedTypeDefinition(assembly, ns, entityType.Id, Some typeof<Entity>)
        typeDefinition.HideObjectMethods <- true;

        // create a constructor
        let ctor = 
            match mandatoryProvidedParameters.Length with
            | 0 -> ProvidedConstructor([], InvokeCode = constructorExpression0 entityType.Id)
            | 1 -> ProvidedConstructor(mandatoryProvidedParameters, InvokeCode = constructorExpression1 entityType.Id)
            | 2 -> ProvidedConstructor(mandatoryProvidedParameters, InvokeCode = constructorExpression2 entityType.Id)
            | 3 -> ProvidedConstructor(mandatoryProvidedParameters, InvokeCode = constructorExpression3 entityType.Id)
            // default to an empty constructor, this should probably fail instead
            | _ -> ProvidedConstructor([], InvokeCode = constructorExpression0 entityType.Id)
        typeDefinition.AddMember ctor

        // creation method
        let createMethod = ProvidedMethod("Create", [ProvidedParameter("entity", typeof<Objects.Entity>)], typeDefinition)
        createMethod.IsStaticMethod <- true;
        createMethod.InvokeCode <- createExpression
        typeDefinition.AddMember createMethod;

        // add fields as properties
        typeDefinition.AddMembers (entityType.FieldTypes |> Seq.map fieldToProperty |> Seq.toList)
        typeDefinition

[<TypeProvider>]
type InRiverProvider(config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()

    let ns = "inQuiry.Model"
    let assembly = Assembly.GetExecutingAssembly()
    // TODO: This is bad, better to use the empty constructor
    let service = inRiverService("http://localhost:8080", "pimuser1", "pimuser1");
    
    // get the entity types from InRiver Model Service
    let entityTypes = 
        service.GetEntityTypes() 
            |> Seq.map (fun et -> EntityTypeFactory(et).createProvidedTypeDefinition assembly ns)
            |> Seq.toList

    do
        this.AddNamespace(ns, entityTypes)

    
[<assembly:TypeProviderAssembly>] 
do()
