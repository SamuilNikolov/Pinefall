param([string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",[ValidateRange(1024,65000)][int]$Port=18908,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'Unreal\Pinefall\Pinefall.uproject'
$editor=Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
$run=Join-Path $repo ('artifacts\unreal-battle\'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$report=Join-Path $run 'round.json'
$children=@()
try {
    foreach($role in @('host','ally','enemy')) {
        $log=Join-Path $run ($role+'.log')
        $travel=if($role -eq 'host'){'/Game/Pinefall/Maps/CedarCrossing?listen?br?play?team=2?Name=Host'}else{"127.0.0.1:$Port`?Name=$role"}
        $arguments=@(('"'+$project+'"'),$travel,'-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFRoundProbe',"-PFProbeRole=$role",('-abslog="'+$log+'"'),'-SCCProvider=None')
        if($role -eq 'host'){$arguments+=@("-port=$Port",('-PFRoundReport="'+$report+'"'))}
        $children+=Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
        if($role -eq 'host'){
            $startDeadline=(Get-Date).AddSeconds(60)
            while(!(Test-Path -LiteralPath $log) -or !(Select-String -LiteralPath $log -SimpleMatch 'PF_JOIN players=1' -Quiet)) {
                if((Get-Date) -gt $startDeadline -or $children[0].HasExited){throw 'Host failed to start.'};Start-Sleep -Milliseconds 500
            }
        }
    }
    $deadline=(Get-Date).AddSeconds($TimeoutSeconds)
    while(!(Test-Path -LiteralPath $report)) {
        foreach($child in $children){if($child.HasExited){throw "Process $($child.Id) exited before its report."}}
        if((Get-Date) -gt $deadline){throw "Battle test timed out. Inspect $run"};Start-Sleep -Milliseconds 500
    }
    $data=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if($data.failed.Count -gt 0 -or $data.passed.Count -lt 25){throw "Battle rules failed: $($data.failed -join ', ')"}
    foreach($role in @('host','ally','enemy')) {
        if(Select-String -LiteralPath (Join-Path $run ($role+'.log')) -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet){throw "Runtime errors in $role log."}
    }
    Write-Output "Battle round passed $($data.passed.Count) assertions across three real processes. Evidence: $run"
} finally {
    foreach($child in $children){if(!$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}}
}
