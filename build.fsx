#r "packages/FAKE/tools/FakeLib.dll"

open Fake

let version = "0.1.0"
let srcDir = "."
let outDir = "../build"

let revision = getBuildParamOrDefault "buildrevision" "0"
let buildVersion = sprintf "%s.%s" version revision

type DeployType = ProGet | Octopus

type HostInfo = {
    name : string
    buildProject : string
}

let hosts = 
    [ 
        { name = "ProjectA"
          buildProject = "ProjectA"
        }
        { name = "ProjectB"
          buildProject = "Paynova.Billing.Vouchers.Api.Client"
        }
    ]

let getHost name = hosts |> List.tryFind (fun h -> h.name = name)

let targetHostParam = getBuildParamOrDefault "targetHost" ""

let buildTargetHost =
    match getHost targetHostParam with
    | None -> failwithf "Could not find matching host to build. TargetHostParam: '%s'" targetHostParam
    | Some(hostInfo) -> hostInfo 

printfn "BuildVersion: %s" buildVersion

Target "Restore" (fun _ -> 
    Paket.Restore id
)

Target "InitOutDir" (fun _ -> 
    outDir |> directoryInfo |> ensureDirExists
    CleanDir outDir
)

Target "Build" (fun _ -> 
    MSBuildRelease "" "Rebuild" [ sprintf "%s/%s/%s.csproj" srcDir buildTargetHost.buildProject buildTargetHost.buildProject ]
    |> Log "Project build Output: "
)

Target "ProGetPack" (fun _ ->
    Paket.Pack (fun p -> 
        { p with
            Version = buildVersion
            OutputPath = outDir
            TemplateFile = sprintf "%s/%s/paket.template" srcDir buildTargetHost.buildProject
        })
)
Target "Default" DoNothing
Target "CI" DoNothing

"Restore"
==> "InitOutDir"
==> "Build"
==> "ProGetPack"
==> "Default"

RunTargetOrDefault "Default"
