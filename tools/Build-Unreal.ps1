param([string]$EngineRoot = "$env:ProgramFiles\Epic Games\UE_5.6")
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'Unreal\Pinefall\Pinefall.uproject'
$build = Join-Path $EngineRoot 'Engine\Build\BatchFiles\Build.bat'
if (!(Test-Path -LiteralPath $build)) { throw "Unreal 5.6 installation not found: $EngineRoot" }
& $build PinefallEditor Win64 Development "-Project=$project" -WaitMutex -NoHotReloadFromIDE -MaxParallelActions=4
if ($LASTEXITCODE -ne 0) { throw "Unreal compilation failed ($LASTEXITCODE)." }
