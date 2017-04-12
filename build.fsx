#r "./packages/FAKE/tools/FakeLib.dll"
open Fake

let buildDir = "./build/"
let libDir = "./lib/"
let testDir = "./test/"
let refDir = "./References/"
let typeProvidersStarterPackDir = "./paket-files/fsprojects/FSharp.TypeProviders.StarterPack/src/"

Target "Clean" (fun _ ->
    CleanDir buildDir
)

// Target "inQuiry.dll" (fun _ ->
//     [ libDir + "AssemblyInfo.fs"
//     ; typeProvidersStarterPackDir + "AssemblyReader.fs"
//     ; typeProvidersStarterPackDir + "AssemblyReaderReflection.fs"
//     ; typeProvidersStarterPackDir +   "ProvidedTypes.fsi"
//     ; typeProvidersStarterPackDir +  "ProvidedTypes.fs"
//     ; typeProvidersStarterPackDir +   "ProvidedTypesContext.fs"
//     ; libDir + "Remoting.fs"
//     ; libDir + "InRiverTypeProvider.fs"
//     ] |> FscHelper.compile [
//          FscHelper.FscParam.Fra
//          FscHelper.Out (buildDir + "inQuiry.dll")
//          FscHelper.References [refDir + "inRiver.Remoting.dll"; "System.Configuration.dll"]
//          FscHelper.Target FscHelper.TargetType.Library
//     ] |> function 0 -> () | c -> failwithf "F# compiler returned code: %i" c
// )

Target "inQuiry.dll" (fun _ ->
    !! (libDir + "lib.fsproj")
    |> MSBuildRelease buildDir "Build"
    |> Log "AppBuild-Output"
)

Target "inQuiry.Test.exe" (fun _ ->
    !! (testDir + "test.fsproj")
    |> MSBuildRelease buildDir "Build"
    |> Log "AppBuild-Output"
)

Target "RunTests" (fun _ ->
    FuchuHelper.Fuchu [buildDir + "inQuiry.Test.exe"]
)

"Clean"
   ==> "inQuiry.dll"
   ==> "inQuiry.Test.exe"
   ==> "RunTests"

RunTargetOrDefault "RunTests"