param(
    [string]$EngineRoot = "$env:ProgramFiles\Epic Games\UE_5.6",
    [switch]$RebuildEnvironment
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$projectRoot = Join-Path $repo 'Unreal\Pinefall'
$project = Join-Path $projectRoot 'Pinefall.uproject'
$editor = Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
if (!(Test-Path -LiteralPath $editor)) { throw "Install Unreal 5.6.1 with templates and Starter Content, or pass -EngineRoot." }
$content = Join-Path $projectRoot 'Content'
# These are installed Epic assets, subject to Epic's terms; they remain ignored in Git.
$copies = @(
    @('Templates\TemplateResources\High\Characters\Content', 'Characters'),
    @('Templates\TemplateResources\High\Input\Content', 'Input')
)
foreach ($pair in $copies) {
    $source = Join-Path $EngineRoot $pair[0]
    if (!(Test-Path -LiteralPath $source)) { throw "Missing installed template content: $source" }
    $destination = Join-Path $content $pair[1]
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -Path "$source\*" -Destination $destination -Recurse -Force
}
foreach ($folder in @('Props','Materials','Textures','Particles','Audio','Shapes')) {
    $source = Join-Path $EngineRoot "Samples\StarterContent\Content\StarterContent\$folder"
    if (!(Test-Path -LiteralPath $source)) { throw "Missing Starter Content: $source" }
    $destination = Join-Path $content "StarterContent\$folder"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -Path "$source\*" -Destination $destination -Recurse -Force
}
& (Join-Path $PSScriptRoot 'Build-Unreal.ps1') -EngineRoot $EngineRoot
$map = Join-Path $content 'Pinefall\Maps\CedarCrossing.umap'
if ($RebuildEnvironment -or !(Test-Path -LiteralPath $map)) {
    $logFolder = Join-Path $repo 'artifacts'
    New-Item -ItemType Directory -Path $logFolder -Force | Out-Null
    $log = Join-Path $logFolder ('unreal-assets-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
    $script = Join-Path $projectRoot 'Scripts\build_baseline.py'
    & $editor $project -run=pythonscript "-script=$script" -PFRebuildBaseline -unattended -nullrhi -SCCProvider=None "-abslog=$log"
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $map) -or !(Select-String -LiteralPath $log -SimpleMatch 'PF_BASELINE_ASSETS_OK' -Quiet)) {
        throw "Environment generation did not pass. Inspect $log"
    }
}
Write-Host 'Unreal foundation ready. Run tools\Run-Unreal.ps1 or open Unreal\Pinefall\Pinefall.uproject.'
