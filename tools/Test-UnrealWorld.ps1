param([string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",[int]$TimeoutSeconds=120)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$run=Join-Path $repo ('artifacts\unreal-world\'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$log=Join-Path $run 'world.log';$report=Join-Path $run 'world.json'
$project=Join-Path $repo 'Unreal\Pinefall\Pinefall.uproject'
$editor=Join-Path $EngineRoot 'Engine\Binaries\Win64\UnrealEditor-Cmd.exe'
$child=$null
try {
    $arguments=@(('"'+$project+'"'),'/Game/Pinefall/Maps/CedarCrossing?play?Name=WorldTest','-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFWorldProbe',('-PFWorldReport="'+$report+'"'),('-abslog="'+$log+'"'),'-SCCProvider=None')
    $child=Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $deadline=(Get-Date).AddSeconds($TimeoutSeconds)
    while(!(Test-Path -LiteralPath $report)) {
        if($child.HasExited){throw 'World probe exited without a report'}
        if((Get-Date) -gt $deadline){throw "World probe timed out. Inspect $run"};Start-Sleep -Milliseconds 500
    }
    $data=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if($data.failed.Count -gt 0 -or $data.passed.Count -lt 15){throw "World physics failed: $($data.failed -join ', ')"}
    if(Select-String -LiteralPath $log -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet){throw 'World probe reported runtime errors'}
    Write-Output "World physics passed $($data.passed.Count) checks. Evidence: $run"
} finally {
    if($child -and !$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}
}
