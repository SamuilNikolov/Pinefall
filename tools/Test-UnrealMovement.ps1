param(
    [string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",
    [ValidateRange(1024,65000)][int]$Port=18948,
    [ValidateRange(0,250)][int]$PacketLagMs=0,
    [int]$TimeoutSeconds=150,
    [switch]$Rendered
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'Unreal\Pinefall\Pinefall.uproject'
$editor=Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
$run=Join-Path $repo ('artifacts\unreal-movement\'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$report=Join-Path $run 'movement.json'
$children=@()
try {
    foreach($role in @('host','client')) {
        $log=Join-Path $run ($role+'.log')
        $travel=if($role -eq 'host'){'/Game/Pinefall/Maps/CedarCrossing?listen?play?Name=Host'}else{"127.0.0.1:$Port`?Name=Client"}
        $arguments=@(('"'+$project+'"'),$travel,'-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFMovementProbe',('-abslog="'+$log+'"'),'-SCCProvider=None')
        if($PacketLagMs -gt 0){$arguments+="-PktLag=$PacketLagMs"}
        if($role -eq 'host'){$arguments+=@("-port=$Port",('-PFMovementReport="'+$report+'"'))}
        $binary=$editor
        if($Rendered -and $role -eq 'client'){
            $binary=$editor.Replace('UnrealEditor-Cmd.exe','UnrealEditor.exe')
            $arguments=@($arguments | Where-Object {$_ -ne '-nullrhi'})
            $arguments+=@('-windowed','-ResX=1600','-ResY=900','-PFMovementVisual',('-PFMovementCapture="'+(Join-Path $run 'slide.png')+'"'))
        }
        $children+=Start-Process -FilePath $binary -ArgumentList $arguments -WindowStyle Hidden -PassThru
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
        if((Get-Date) -gt $deadline){throw "Movement test timed out. Inspect $run"};Start-Sleep -Milliseconds 500
    }
    $data=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if($data.failed.Count -gt 0 -or $data.passed.Count -lt 24){throw "Movement failed: $($data.failed -join ', ')"}
    foreach($role in @('host','client')) {
        $log=Join-Path $run ($role+'.log')
        if(Select-String -LiteralPath $log -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet){throw "Runtime errors in $role log."}
    }
    if($Rendered -and !(Test-Path -LiteralPath (Join-Path $run 'slide.png'))){throw 'No rendered slide capture was produced.'}
    $clientLog=Join-Path $run 'client.log'
    if(!(Select-String -LiteralPath $clientLog -SimpleMatch 'PF_MOVEMENT_CLIENT_ON_FIXTURE' -Quiet)){throw 'Client did not move on the shared elevated fixture.'}
    if(!(Select-String -LiteralPath $clientLog -SimpleMatch 'PF_MOVEMENT_PEER_SLIDE' -Quiet)){throw 'Joining client did not observe replicated host slide.'}
    @{packet_lag_ms_each_sender=$PacketLagMs;passed=$data.passed.Count;failed=0;scope='Real owner slide/hop/ceiling/menu/teleport inputs on identical flat collision fixtures; no public WAN or rendered FPS claim'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'summary.json') -Encoding utf8
    Write-Output "Movement passed $($data.passed.Count) checks; per-sender artificial lag $PacketLagMs ms. Evidence: $run"
} finally {
    foreach($child in $children){if(!$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}}
}
