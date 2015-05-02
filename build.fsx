// include Fake lib
#r "packages/FAKE/tools/FakeLib.dll"
open Fake

RestorePackages()

// Properties
let buildDir = "./build/"
let testDir  = "./test/"
let deployDir = "./deploy/"

let version = "1.1"

// Targets
Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "BuildApp" (fun _ ->
    !! "Chamook.Tracker/*.fsproj"
      |> MSBuildRelease buildDir "Build"
      |> Log "AppBuild-Output: "
)

Target "Zip" (fun _ ->
    !! (buildDir + "/**/*.*")
        -- "*.zip"
        |> Zip buildDir (deployDir + "Chamook.Tracker." + version + ".zip")
)

Target "Default" (fun _ ->
    trace "Hej from Copenhagen :)"
)

// Dependencies
"Clean"
  ==> "BuildApp"
  ==> "Zip"
  ==> "Default"

// start build
RunTargetOrDefault "Default"