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
    
    // default properties
    member this.CreatedBy
        with get () = entity.CreatedBy
        and set (value) = entity.CreatedBy <- value
   
type EntityTypeFactory (entityType : Objects.EntityType)  =

    let constructorExpression entityTypeID =
        fun args ->
        <@@
            // get the entity type, this should be cached
            let entityType = inRiverService().GetEntityTypeById(entityTypeID)
            // create a new instance of the entity
            Entity(Objects.Entity.CreateEntity(entityType))
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
            // convert the value to string
            (entity.PropertyValue fieldTypeID) :?> Objects.LocaleString
            @>

    let integerValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to string
            (entity.PropertyValue fieldTypeID) :?> int
            @>

    let dateTimeValueExpression fieldTypeID =
        fun (args : Expr list) ->
        <@
            // get the entity
            let entity = (%%(args.[0]) : Entity)
            // convert the value to string
            (entity.PropertyValue fieldTypeID) :?> System.DateTime
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
        | _ -> ProvidedProperty(fieldTypeID, typeof<Option<obj>>, [], GetterCode = ((objValueExpression fieldTypeID) >> optionPropertyExpression))

    member this.createProvidedTypeDefinition assembly ns =
        // create the type
        let typeDefinition = ProvidedTypeDefinition(assembly, ns, entityType.Id, Some typeof<Entity>)
        typeDefinition.HideObjectMethods <- true;

        // create a constructor
        let ctor = ProvidedConstructor([], InvokeCode = constructorExpression entityType.Id)
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
