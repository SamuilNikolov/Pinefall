param([string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",[ValidateRange(1024,65000)][int]$Port=18918,[int]$TimeoutSeconds=120)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'Unreal\Pinefall\Pinefall.uproject'
$editor=Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
$run=Join-Path $repo ('artifacts\unreal-wildlife\'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$report=Join-Path $run 'wildlife.json'
$children=@()
try {
    foreach($role in @('host','client')) {
        $log=Join-Path $run ($role+'.log')
        $travel=if($role -eq 'host'){'/Game/Pinefall/Maps/CedarCrossing?listen?play'}else{"127.0.0.1:$Port"}
        $arguments=@(('"'+$project+'"'),$travel,'-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFWildlifeProbe',('-abslog="'+$log+'"'),'-SCCProvider=None')
        if($role -eq 'host'){$arguments+=@("-port=$Port",('-PFFaunaReport="'+$report+'"'))}
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
        if((Get-Date) -gt $deadline){throw "Wildlife test timed out. Inspect $run"};Start-Sleep -Milliseconds 500
    }
    $data=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if($data.failed.Count -gt 0 -or $data.passed.Count -lt 12){throw "Wildlife checks failed: $($data.failed -join ', ')"}
    $clientLog=Join-Path $run 'client.log'
    foreach($species in @('Deer','Wolf','Fox')) {
        if(!(Select-String -LiteralPath $clientLog -Pattern "PF_FAUNA_SPAWN species=$species .*mesh=$species" -Quiet)){throw "Remote $species presentation was not created."}
    }
    foreach($role in @('host','client')) {
        if(Select-String -LiteralPath (Join-Path $run ($role+'.log')) -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet){throw "Runtime errors in $role log."}
    }
    Write-Output "Wildlife passed $($data.passed.Count) assertions and three remote skeletal spawns. Evidence: $run"
} finally {
    foreach($child in $children){if(!$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}}
}
