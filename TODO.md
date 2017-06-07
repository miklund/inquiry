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

* Query API
* Opt-in logging for the code generation
* Add XML comments for all the fields

v0.2

The purpose of v0.2 is to retrieve a single entity, change it and save it back to inRiver.

```fsharp
let product = Product.GetByNumber("SKU123")
let newProduct = product |> set (fun p -> p.Number = Some "SKU 456")
newProduct.save() |> ignore
```

* If the string field has default value 'guid', present the Property as Guid type instead
* Implement Read-Only properties, shall not have a set property, but can be optional in constructor
* Implement multivalue CVL fields
* Xml field should be represented by XDocument and not string, string is the DTO data type
* Implement GET-functions

* Deal with relations between entities, handle links
* Implement FieldSets
* Implement Categories
* Implement Unique
* Implement Hidden
* Implement Exclude from Default View
* Create fsi files to protect internal members of the library

BUGS

* BUG: CVL type Users is not generating all the CVL values
* BUG: Can't handle a CVL with the same name as Entity

DOCUMENTATION

* Documentation of configuration switches
* Write example on
  - How to work with the XML field, query and update it
  - How to filter out specific fieldsets
  - How to filter out categories
  - How to read and write properties
  - How to write an import extension importing data from XML to inRiver

  META

* Create a logo for the nuget package
* Add a reference to System.Configuration in nuget package
