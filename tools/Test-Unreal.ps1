param(
    [string]$EngineRoot = "$env:ProgramFiles\Epic Games\UE_5.6",
    [ValidateRange(1024,65000)][int]$Port = 18888,
    [ValidateRange(30,240)][int]$TimeoutSeconds = 90
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'Unreal\Pinefall\Pinefall.uproject'
$editor = Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
$run = Join-Path $repo ('artifacts\unreal-tests\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$hostLog = Join-Path $run 'host.log'
$clientLog = Join-Path $run 'client.log'
$children = @()
function Wait-Marker([string]$Path,[string]$Pattern,[datetime]$Deadline) {
    while ((Get-Date) -lt $Deadline) {
        if ((Test-Path -LiteralPath $Path) -and (Select-String -LiteralPath $Path -Pattern $Pattern -Quiet)) { return }
        foreach ($child in $children) { if ($child.HasExited) { throw "Test process $($child.Id) exited unexpectedly." } }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $Pattern in $Path"
}
try {
    # Local synthetic input only. Never enables or records a microphone.
    $common = @('-game','-nullrhi','-nosound','-unattended','-PFSmoke','-SCCProvider=None')
    $hostArgs = @(('"'+$project+'"'),'/Game/Pinefall/Maps/CedarCrossing?listen?play',"-port=$Port",('-abslog="'+$hostLog+'"')) + $common
    $children += Start-Process -FilePath $editor -ArgumentList $hostArgs -WindowStyle Hidden -PassThru
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    Wait-Marker $hostLog 'PF_JOIN players=1' $deadline
    $clientArgs = @(('"'+$project+'"'),"127.0.0.1:$Port",'-PFSmokeFire',('-abslog="'+$clientLog+'"')) + $common
    $children += Start-Process -FilePath $editor -ArgumentList $clientArgs -WindowStyle Hidden -PassThru
    Wait-Marker $hostLog 'PF_JOIN players=2' $deadline
    Wait-Marker $clientLog 'PF_CONFIRMED damage=80' $deadline
    Wait-Marker $hostLog 'PF_SHOT .*ammo=4' $deadline
    $assertions = @('Host admitted a real second process','Remote weapon loaded a five-round magazine','Server accepted the remote shot and decremented ammo','Remote owner received confirmed 80-point rifle damage')
    if (!(Select-String -LiteralPath $clientLog -Pattern 'PF_WEAPON .*magazine=5' -Quiet)) { throw 'Remote weapon data did not load.' }
    foreach ($log in @($hostLog,$clientLog)) {
        if (Select-String -LiteralPath $log -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet) { throw "Runtime error in $log" }
    }
    @{ passed=$assertions; failed=@(); limitations=@('NullRHI: no graphics/FPS or audible sound claim','Range target damage only; moving-player hit fairness/rewind remains unverified','No WAN, microphone, inventory or battle royale parity test') } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $run 'result.json') -Encoding utf8
    Write-Output "Unreal host/client smoke passed. Evidence: $run"
} catch {
    @{passed=@();failed=@($_.Exception.Message)} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'result.json') -Encoding utf8
    throw
} finally {
    foreach ($child in $children) { if (!$child.HasExited) { Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue } }
}
