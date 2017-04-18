#r "./packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing

let buildDir = "./build/"
let libDir = "./lib/"
let testDir = "./test/"
let refDir = "./References/"
let typeProvidersStarterPackDir = "./paket-files/fsprojects/FSharp.TypeProviders.StarterPack/src/"

let xUnitToolPath = "./packages/xunit.runner.console/tools/xunit.console.exe"

Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "inQuiry.dll" (fun _ ->
    !! (libDir + "lib.fsproj")
    |> MSBuildRelease buildDir "Build"
    |> Log "AppBuild-Output"
)

Target "inQuiry.Test.dll" (fun _ ->
    !! (testDir + "test.fsproj")
    |> MSBuildRelease buildDir "Build"
    |> Log "AppBuild-Output"
)

Target "RunTests" (fun _ ->
    !! (buildDir @@ "inQuiry.Test.dll")
      |> xUnit (fun p -> { p with ToolPath = xUnitToolPath})
)

"Clean"
   ==> "inQuiry.dll"
   ==> "inQuiry.Test.dll"
   ==> "RunTests"

RunTargetOrDefault "RunTests"