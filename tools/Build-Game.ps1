[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EDITOR,
    [string]$OutputPath = 'Builds/Windows/Pinefall.exe',
    [switch]$RegenerateWorld
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $UnityPath) {
    $UnityPath = 'C:\Program Files\Unity\Hub\Editor\2022.3.13f1\Editor\Unity.exe'
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw 'Set UNITY_EDITOR or pass -UnityPath to the Unity 2022.3.13f1 executable.'
}
if (-not [IO.Path]::IsPathRooted($OutputPath)) { $OutputPath = Join-Path $repoRoot $OutputPath }
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
if ([IO.Path]::GetExtension($OutputPath) -ne '.exe') { throw 'OutputPath must name the Windows .exe player.' }
$logRoot = Join-Path $repoRoot 'artifacts/build'
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $OutputPath -Parent) | Out-Null
$logPath = Join-Path $logRoot (Get-Date -Format 'yyyyMMdd-HHmmss-ffff')
$logPath += '.log'
$entryPoint = if ($RegenerateWorld) { 'BuildReserve.Build' } else { 'BuildReserve.BuildCurrent' }
$graphics = if ($RegenerateWorld) { '' } else { '-nographics' }
$arguments = "-batchmode $graphics -quit -projectPath `"$repoRoot`" -executeMethod $entryPoint -pinefallBuildPath `"$OutputPath`" -logFile `"$logPath`""
Write-Host "Building with $entryPoint. Log: $logPath"
$buildProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$buildProcess.WaitForExit()
if ($buildProcess.ExitCode -ne 0) { throw "Unity exited with $($buildProcess.ExitCode). Inspect $logPath" }
if (-not (Test-Path -LiteralPath $OutputPath -PathType Leaf)) { throw "No player produced. Inspect $logPath" }
if (-not (Select-String -LiteralPath $logPath -SimpleMatch 'PINEFALL_BUILD Succeeded' -Quiet)) {
    throw "Build success marker missing. Inspect $logPath"
}
Write-Host "BUILD_OK $OutputPath"
