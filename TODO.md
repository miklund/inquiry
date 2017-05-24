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
let newProduct = { product | Number = "SKU 456"}
newProduct.Save() |> ignore
```

* Deal with relations between entities, handle links
* Add XML comments for all the fields
* Able to change values by immutable type syntax
* Implement FieldSets
* Implement Categories
* Implement Read-Only properties
* Implement MultiValue
* Implement Unique
* Implement Hidden
* Implement Exclude from Default View
* Create fsi files to protect internal members of the library

v0.1

The goal with this version is to provide the functionality to do the following.

```fsharp
let product = Product(number = "ANewProductNumber")
ignore <| Product.Save(product)
```

* Setup a nuget package
* Setup inQuiry.wiki with example code
* Setup an example project with the following examples
  - How to create and add a new product to inriver by FSX
  - How to write an import extension importing data from XML to inRiver
* Implement data type XML
* Implement data type File
