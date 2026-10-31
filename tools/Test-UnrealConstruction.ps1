param([string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",[int]$TimeoutSeconds=150,[int]$PacketLagMs=80,[int]$Port=18995)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$run=Join-Path $repo ('artifacts/unreal-construction/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$project=Join-Path $repo 'Unreal/Pinefall/Pinefall.uproject'
$editor=Join-Path $EngineRoot 'Engine/Binaries/Win64/UnrealEditor-Cmd.exe'
$report=Join-Path $run 'construction.json';$hostLog=Join-Path $run 'host.log';$clientLog=Join-Path $run 'client.log';$lateLog=Join-Path $run 'late.log'
$children=@()
function Wait-Marker([string]$Path,[string]$Marker,[datetime]$Deadline) {
    while((Get-Date) -lt $Deadline) {
        if((Test-Path -LiteralPath $Path) -and (Select-String -LiteralPath $Path -Pattern $Marker -Quiet)){return}
        foreach($child in $children){if($child.HasExited){throw "Construction test process exited. Inspect $run"}}
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $Marker. Inspect $run"
}
try {
    $argsHost=@(('"'+$project+'"'),'/Game/Pinefall/Maps/CedarCrossing?listen?Name=BuildHost','-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFConstructionProbe',("-port=$Port"),('-PFConstructionReport="'+$report+'"'),('-abslog="'+$hostLog+'"'),'-SCCProvider=None',("-PktLag=$PacketLagMs"))
    $children+=Start-Process -FilePath $editor -ArgumentList $argsHost -WindowStyle Hidden -PassThru
    $deadline=(Get-Date).AddSeconds($TimeoutSeconds)
    Wait-Marker $hostLog 'GameNetDriver.*listening on port' $deadline
    $argsClient=@(('"'+$project+'"'),("127.0.0.1:$Port"),'-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFConstructionProbe',('-abslog="'+$clientLog+'"'),'-SCCProvider=None',("-PktLag=$PacketLagMs"))
    $children+=Start-Process -FilePath $editor -ArgumentList $argsClient -WindowStyle Hidden -PassThru
    Wait-Marker $hostLog 'PF_BUILD_WAIT_LATEJOIN' $deadline
    $argsLate=@(('"'+$project+'"'),("127.0.0.1:$Port"),'-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFConstructionProbe',('-abslog="'+$lateLog+'"'),'-SCCProvider=None')
    $children+=Start-Process -FilePath $editor -ArgumentList $argsLate -WindowStyle Hidden -PassThru
    while(!(Test-Path -LiteralPath $report)){if((Get-Date) -gt $deadline){throw "Construction report timeout. Inspect $run"};Start-Sleep -Milliseconds 500}
    $data=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
    if($data.failed.Count -gt 0 -or $data.passed.Count -lt 40){throw "Construction checks failed: $($data.failed -join '; '). Inspect $run"}
    Wait-Marker $clientLog 'PF_BUILD_CLIENT_OBSERVED phase=9' $deadline
    Wait-Marker $lateLog 'PF_BUILD_CLIENT_OBSERVED phase=10' $deadline
    foreach($log in @($hostLog,$clientLog,$lateLog)){if(Select-String -LiteralPath $log -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet){throw "Runtime errors in $log"}}
    Write-Output "Construction passed $($data.passed.Count) checks with three real processes and $PacketLagMs ms packet lag. Evidence: $run"
} finally {
    foreach($child in $children){if(!$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}}
}
