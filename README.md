# inQuiry - inRiver Type Provider

inQuiry is an F# type provider for [inRiver PIM](https://www.inriver.com/). The
purpose is to provide a strongly typed alternative to the Remoting API that
requires less coding, ease of use and security. This is done by generating types
in compile time that will supply most of the boilerplate coding needed for the
Remoting API.

Create a new product with Remoting API (example code from inRiver Wiki)

```csharp
// Get product entity type
EntityType productEntityType = RemoteManager.ModelService.GetEntityType("Product");
// Create entity object
Entity product = Entity.CreateEntity(productEntityType);
// Set product number, it is a mandatory field
Field productNumber = product.Fields.Find(f => f.FieldType.Id == "ProductNumber");
productNumber.Data = "ANewProductNumber";
// Add the new product to PIM
Entity createdProduct = RemoteManager.DataService.AddEntity(product);
```

The same thing with inQuiry.

```fsharp
// Create a new product and save it
Product(number = "ANewProductNumber") |> Product.save
```

The strong type `Product` is generated from the entity information in the
inRiver Model API. Why do we prefer using inQuiry?

* Less code
* Easier to read code
* No magic strings
* Error handling when the model changes is built in
* When the model changes you get compile time errors in stead of runtime errors

Please take a look at more examples in the Wiki to understand how to use inQuiry.

## Installation & Getting Started

Open Package-Manager in Visual Studio and install the inQuiry package in your F# project.

```
Install-Package inQuiry
```

Or configure Paket to install inQuiry. Put the following in your `paket.dependencies`
file and run `.paket\paket.exe install`.

```
nuget inQuiry
```

inQuiry is using the `inRiver.Remoting.dll` that comes with the inRiver Server
installation. Make sure that you reference this DLL from the same project where you
use inQuiry.

## Version of inRiver that are supported

inQuiry supports the following versions of inRiver Remoting API. These versions are
verified each time a new minor version is tagged. inRiver version support will not
be verified for each an every commit.

| inRiver Version | 
| --------------- |
| 6.3.0 SP2       |

## Can I use inQuiry from C#?

You can use inQuiry from an F# project or an FSX script file. Use an F# project
if you want to create a plugin to inRiver using inQuiry, or use an FSX script
file if you just want to run some code calling the inRiver API.

C# does not support type providers and such inQuiry does not support C#. If you
are a C# developer you should not be intimidated by F# as the code you'll write
to integrate with inRiver will be mostly imperative anyway so C#/F# solutions
would be quite similiar to each other.

If you want to use inQuiry together with C# you can use F#/inQuiry to provide a
data layer and expose the functionality to C# through a facade. This is
quite simple and you should look in the examples section for it. However you can
not return a strongly generated type from inQuiry to your C# code. The interface
between C# and F# must always be entities from inRiver Remoting API.

## Contributing

Not able to handle contributions to this project at this time. This is a 1.0 feature.
