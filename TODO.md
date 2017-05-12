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
let product = Product("SKU123")
product.Save() |> ignore
```

* Mandatory properties should not be Option<> type
* Mandatory properties with default value should not be required in constructor (optional?)
* SaveOrUpdate method on entity class
* Readme.md with example of how inQuiry simplify things
* Implement data type CVL lists
* Implement data type LocaleString
* Implement data type XML
* Implement data type File