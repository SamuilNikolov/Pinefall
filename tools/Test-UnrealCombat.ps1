param(
    [string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",
    [ValidateRange(1024,65000)][int]$Port=18928,
    [ValidateRange(0,250)][int]$PacketLagMs=0,
    [int]$TimeoutSeconds=150
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'Unreal\Pinefall\Pinefall.uproject'
$editor=Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
$run=Join-Path $repo ('artifacts\unreal-combat\'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$report=Join-Path $run 'combat.json'
$children=@()
try {
    foreach($role in @('host','client')) {
        $log=Join-Path $run ($role+'.log')
        $travel=if($role -eq 'host'){'/Game/Pinefall/Maps/CedarCrossing?listen?play?Name=Host'}else{"127.0.0.1:$Port`?Name=Client"}
        $arguments=@(('"'+$project+'"'),$travel,'-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFCombatProbe',('-abslog="'+$log+'"'),'-SCCProvider=None')
        if($PacketLagMs -gt 0){$arguments+="-PktLag=$PacketLagMs"}
        if($role -eq 'host'){$arguments+=@("-port=$Port",('-PFCombatReport="'+$report+'"'))}
        $children+=Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
        if($role -eq 'host') {
            $deadline=(Get-Date).AddSeconds(180)
            while(!(Test-Path -LiteralPath $log) -or !(Select-String -LiteralPath $log -SimpleMatch 'PF_JOIN players=1' -Quiet)) {
                if((Get-Date) -gt $deadline -or $children[0].HasExited){throw 'Host failed to start.'};Start-Sleep -Milliseconds 500
            }
        }
    }
    $deadline=(Get-Date).AddSeconds($TimeoutSeconds)
    while(!(Test-Path -LiteralPath $report)) {
        foreach($child in $children){if($child.HasExited){throw "Process $($child.Id) exited before its report."}}
        if((Get-Date) -gt $deadline){throw "Player combat test timed out. Inspect $run"};Start-Sleep -Milliseconds 500
    }
    $data=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if($data.failed.Count -gt 0 -or $data.passed.Count -lt 16){throw "Player combat failed: $($data.failed -join ', ')"}
    foreach($role in @('host','client')) {
        $log=Join-Path $run ($role+'.log')
        if(Select-String -LiteralPath $log -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet){throw "Runtime errors in $role log."}
    }
    $clientLog=Join-Path $run 'client.log'
    foreach($pattern in @('PF_CONFIRMED damage=80','PF_CONFIRMED damage=112 .*kill=1 head=1')) {
        if(!(Select-String -LiteralPath $clientLog -Pattern $pattern -Quiet)){throw "Missing client confirmation: $pattern"}
    }
    @{packet_lag_ms_each_sender=$PacketLagMs;passed=$data.passed.Count;failed=0;scope='Actual standing/crouched/moving player targets, scoped FP/TP, current cover and head damage; no general latency/loss, WAN or rendered FPS claim'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'summary.json') -Encoding utf8
    Write-Output "Player combat passed $($data.passed.Count) checks; per-sender artificial lag $PacketLagMs ms. Evidence: $run"
} finally {
    foreach($child in $children){if(!$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}}
}
