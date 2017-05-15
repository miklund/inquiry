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

v0.1

The goal with this version is to provide the functionality to do the following.

```fsharp
let product = Product(number = "ANewProductNumber")
ignore <| Product.SaveOrUpdate(product)
```

* Mandatory properties should not be Option<> type
* Mandatory properties with default value should not be required in constructor (optional?)
* SaveOrUpdate method on entity class
* Readme.md with example of how inQuiry simplify things
* Implement data type CVL lists
* Implement data type LocaleString
* Implement data type XML
* Implement data type File