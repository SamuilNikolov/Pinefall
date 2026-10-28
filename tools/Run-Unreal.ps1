param(
    [string]$EngineRoot = "$env:ProgramFiles\Epic Games\UE_5.6",
    [ValidateSet('Menu','Practice','Host','Solo','Duo','Squad','Join')][string]$Mode = 'Menu',
    [string]$Address = '127.0.0.1:7777',
    [ValidateRange(1,65535)][int]$Port = 7777
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$projectRoot = Join-Path $repo 'Unreal\Pinefall'
$editor = Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor.exe'
$project = Join-Path $projectRoot 'Pinefall.uproject'
if (!(Test-Path -LiteralPath $editor) -or !(Test-Path -LiteralPath (Join-Path $projectRoot 'Content\Pinefall\Maps\CedarCrossing.umap'))) {
    throw 'Run tools\Setup-Unreal.ps1 first. The native project and generated scene must both exist.'
}
$travel = '/Game/Pinefall/Maps/CedarCrossing'
if ($Mode -eq 'Practice') { $travel += '?play' }
if ($Mode -eq 'Host') { $travel += '?listen?play' }
if ($Mode -in @('Solo','Duo','Squad')) {
    $teamSize = if ($Mode -eq 'Duo') {2} elseif ($Mode -eq 'Squad') {4} else {1}
    $travel += "?listen?br?play?team=$teamSize"
}
if ($Mode -eq 'Join') {
    if ($Address -notmatch '^(localhost|(?:\d{1,3}\.){3}\d{1,3})(:\d{1,5})?$') { throw 'Use an IPv4 address with an optional port.' }
    $travel = $Address
}
# This is an explicitly requested interactive game window. Keep the editor out of the way.
Start-Process -FilePath $editor -ArgumentList @(('"'+$project+'"'),$travel,'-game','-windowed','-ResX=1600','-ResY=900',"-port=$Port") -WorkingDirectory $projectRoot
