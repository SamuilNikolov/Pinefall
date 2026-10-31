param([string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",[int]$TimeoutSeconds=120)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$run=Join-Path $repo ('artifacts/unreal-inventory/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run -Force|Out-Null
$report=Join-Path $run 'inventory.json';$log=Join-Path $run 'inventory.log'
$project=Join-Path $repo 'Unreal/Pinefall/Pinefall.uproject';$editor=Join-Path $EngineRoot 'Engine/Binaries/Win64/UnrealEditor-Cmd.exe'
$child=$null
try {
    $arguments=@(('"'+$project+'"'),'/Game/Pinefall/Maps/CedarCrossing?play?Name=InventoryTest','-game','-nullrhi','-nosound','-unattended','-PFSmoke','-PFInventoryProbe',('-PFInventoryReport="'+$report+'"'),('-abslog="'+$log+'"'),'-SCCProvider=None')
    $child=Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $deadline=(Get-Date).AddSeconds($TimeoutSeconds)
    while(!(Test-Path -LiteralPath $report)) {
        if($child.HasExited){throw "Inventory probe exited without a report. Inspect $run"}
        if((Get-Date) -gt $deadline){throw "Inventory probe timed out. Inspect $run"};Start-Sleep -Milliseconds 500
    }
    $data=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
    if($data.failed.Count -gt 0 -or $data.passed.Count -lt 15){throw "Inventory checks failed: $($data.failed -join '; '). Inspect $run"}
    if(Select-String -LiteralPath $log -Pattern 'Fatal error:|Log.*: Error:|Ensure condition failed' -Quiet){throw "Runtime errors in $log"}
    Write-Output "Inventory passed $($data.passed.Count) UI and transaction checks. Evidence: $run"
} finally {if($child -and !$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}}
