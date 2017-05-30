#r "./packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing

let outputDir = "./output/"
let outputBuildDir = outputDir + "build/"
let outputTestDir = outputDir + "test/"
let outputPackDir = outputDir + "pack/"

let libDir = "./lib/"
let testDir = "./test/"
let refDir = "./References/"
let typeProvidersStarterPackDir = "./paket-files/fsprojects/FSharp.TypeProviders.StarterPack/src/"

let xUnitToolPath = "./packages/xunit.runner.console/tools/xunit.console.exe"

Target "Clean" (fun _ ->
    !! outputDir
    ++ outputBuildDir
    ++ outputTestDir
    ++ outputPackDir
    |> CleanDirs
)

Target "BuildLib" (fun _ ->
    !! (libDir + "lib.fsproj")
    |> MSBuildRelease outputBuildDir "Build"
    |> Log "AppBuild-Output"
)

Target "BuildTest" (fun _ ->
    !! (testDir + "test.fsproj")
    |> MSBuildRelease outputTestDir "Build"
    |> Log "AppBuild-Output"
)

Target "RunTests" (fun _ ->
    !! (outputTestDir @@ "inQuiry.Test.dll")
      |> xUnit (fun p -> { p with ToolPath = xUnitToolPath})
)

Target "Package" (fun _ ->
    Paket.Pack (fun p ->
        { p with 
            ToolPath = ".paket/paket.exe"
            OutputPath = outputPackDir
            TemplateFile = "./paket.template"
            })
)

"Clean"
   ==> "BuildLib"
    ==> "Package"
   ==> "BuildTest"
    ==> "RunTests"

RunTargetOrDefault "RunTests"