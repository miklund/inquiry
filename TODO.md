v1.0

Requirements for taking the step to 1.0

* Running inQuiry in at least 10 different production scenarios
* Have a working contributing open source project including rules of conduct, and having recieved and merged at least 5 pull requests
* Support for multiversion inRiver
* Support for .NET Core


v0.5

Setup a CI pipeline. Use travic-ci in order to build each new commit to VCS.

* Setup TravisCI
* Get the project to build with Mono
* Get the project to build with .NET Core
* Setup an Azure VM with inRiver for testing
* Make sure that tests run on Mono


v0.4

Create support to connect to two or more inRiver servers at the same time. When initializing
the type provider, the connection details must be specified.

```fsharp
type inRiver1 = inQuiry<"https://pim.dev:8080", "pimuser1", "pimuser1">
let product1 = inRiver1.Product("SKU123")

type inRiver2 = inQuiry<"https://pim.test:8080", "pimuser1", "pimuser1">
let product2 = inRiver2.Product("SKU123")
```

v0.3

The purpose of v0.3 is to query the inRiver API for several entities.

v0.2

The purpose of v0.2 is to retrieve a single entity, change it and save it back to inRiver.

```fsharp
let product = Product.GetByNumber("SKU123")
let newProduct = { product with Number = "SKU 456"}
newProduct.save() |> ignore
```

* Deal with relations between entities, handle links
* Add XML comments for all the fields
* Able to change values by immutable type syntax
* Implement FieldSets
* Implement Categories
* Implement Read-Only properties, shall not have a set property, but can be optional i constructor
* Implement MultiValue
* Implement Unique
* Implement Hidden
* Implement Exclude from Default View
* If the string field has default value 'guid', present the Property as Guid type instead
* Create fsi files to protect internal members of the library
* Implement multivalue CVL fields
* Write example on
  - How to work with the XML field, query and update it
  - How to filter out specific fieldsets
  - How to filter out categories
  - How to read and write properties
  - How to write an import extension importing data from XML to inRiver
* Make the Resource integrated with its data
* Xml field should be represented by XDocument and not string, string is the DTO data type
