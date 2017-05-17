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

* Able to change values by immutable type syntax
* Implement FieldSets
* Implement Categories
* Implement Read-Only properties
* Implement Display Name
* Implement Display Description
* Implement MultiValue
* Implement Unique
* Implement Hidden
* Implement Exclude from Default View

v0.1

The goal with this version is to provide the functionality to do the following.

```fsharp
let product = Product(number = "ANewProductNumber")
ignore <| Product.Save(product)
```

* Setup a nuget package
* Setup inQuiry.wiki with example code
* Implement data type CVL lists
* Implement data type LocaleString
* Implement data type XML
* Implement data type File
* Implement dynamic DisplayDescription
* Implement dynamic DisplayName
