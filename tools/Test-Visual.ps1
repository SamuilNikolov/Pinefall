[CmdletBinding()]
param([string]$PlayerPath = 'Builds/Windows/Pinefall.exe', [string]$OutputPath = 'artifacts/visual08', [int]$Width = 1600, [int]$Height = 900)
. (Join-Path $PSScriptRoot 'ProbeCommon.ps1')
$playerExe = Resolve-PlayerPath $PlayerPath
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not [IO.Path]::IsPathRooted($OutputPath)) { $OutputPath = Join-Path $repoRoot $OutputPath }
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
$captureLog = Join-Path $OutputPath 'capture.log'
$launchArguments = "--capture `"$OutputPath`" --fieldcraft-capture 1 --silence 1 -screen-fullscreen 0 -screen-width $Width -screen-height $Height -logFile `"$captureLog`""
Write-Host "Opening native visual test. Captures: $OutputPath"
$captureProcess = Start-Process -FilePath $playerExe -ArgumentList $launchArguments -WindowStyle Normal -PassThru
try {
    if (-not $captureProcess.WaitForExit(180000)) { throw 'Native visual test exceeded its deadline.' }
    if ($captureProcess.ExitCode -ne 0) { throw "Visual test exited with $($captureProcess.ExitCode). Inspect $captureLog" }
    if (-not (Select-String -LiteralPath $captureLog -SimpleMatch 'FIELDCRAFT_VISUAL_OK' -Quiet)) { throw 'Visual sequence did not complete.' }
    Write-Host 'FIELDCRAFT_NATIVE_OK'
}
finally { if (-not $captureProcess.HasExited) { Stop-Process -Id $captureProcess.Id }; $captureProcess.Dispose() }
