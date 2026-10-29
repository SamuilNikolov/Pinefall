param(
    [string]$EngineRoot = "$env:ProgramFiles\Epic Games\UE_5.6",
    [switch]$RebuildEnvironment,
    [switch]$RebuildRoundAssets,
    [switch]$RebuildReserve,
    [switch]$RebuildCharacterMaterials
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
$passes = @()
if ($RebuildEnvironment -or !(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Materials\M_LakeSurface.uasset'))) {$passes += 'upgrade_world.py'}
if (!(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Art\Characters\TrailRanger\TrailRanger.uasset'))) {$passes += 'import_stylized_ranger.py'}
if ($RebuildEnvironment -or $RebuildRoundAssets -or !(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Materials\M_WildRage.uasset'))) {$passes += 'build_round_assets.py'}
if (!(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Audio\GrassStep4.uasset'))) {$passes += 'build_combat_audio.py'}
if (!(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Art\Fauna\Alpaca\Walk.uasset'))) {$passes += 'import_wildlife.py'}
if ($RebuildCharacterMaterials -or $passes -contains 'import_stylized_ranger.py' -or !(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Materials\Characters\M_Ranger_MI_Ranger.uasset'))) {$passes += 'fix_stylized_materials.py'}
$needsReserve=$RebuildReserve -or !(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Maps\WildfrontReserve.umap'))
if($needsReserve){$passes+='build_reserve.py'}
if($needsReserve -or !(Test-Path -LiteralPath (Join-Path $content 'Pinefall\Art\T_ReserveAtlas.uasset'))){$passes+='import_reserve_atlas.py'}
foreach ($pass in $passes) {
    $logFolder = Join-Path $repo 'artifacts'
    New-Item -ItemType Directory -Path $logFolder -Force | Out-Null
    $log = Join-Path $logFolder ('unreal-' + [IO.Path]::GetFileNameWithoutExtension($pass) + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
    $script = Join-Path $projectRoot ('Scripts\' + $pass)
    & $editor $project -run=pythonscript "-script=$script" -PFImportAllWildlife -unattended -nullrhi -SCCProvider=None "-abslog=$log"
    if ($LASTEXITCODE -ne 0 -or (Select-String -LiteralPath $log -Pattern 'Log.*: Error:|Ensure condition failed' -Quiet)) {throw "Asset generation did not pass. Inspect $log"}
    if($pass -eq 'build_reserve.py'){
        & python -X utf8 (Join-Path $projectRoot 'Scripts\bake_reserve_atlas.py')
        if($LASTEXITCODE -ne 0){throw 'Atlas authoring requires Python with NumPy and Pillow. Normal play/build uses committed assets.'}
    }
}
Write-Host 'Unreal round ready. Run tools\Run-Unreal.ps1 or open Unreal\Pinefall\Pinefall.uproject.'
