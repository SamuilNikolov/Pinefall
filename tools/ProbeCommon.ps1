# Shared Windows process/report helpers. Only processes created here can be terminated here.
$ErrorActionPreference = 'Stop'
$script:probeProcesses = [Collections.Generic.List[System.Diagnostics.Process]]::new()

function Resolve-PlayerPath([string]$Path) {
    $repoRoot = Split-Path $PSScriptRoot -Parent
    if (-not [IO.Path]::IsPathRooted($Path)) { $Path = Join-Path $repoRoot $Path }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Player missing: $Path. Build first or pass -PlayerPath." }
    return (Resolve-Path -LiteralPath $Path).Path
}

function New-ResultFolder([string]$Category, [string]$Path) {
    $repoRoot = Split-Path $PSScriptRoot -Parent
    if (-not $Path) { $Path = Join-Path $repoRoot "artifacts/$Category" }
    elseif (-not [IO.Path]::IsPathRooted($Path)) { $Path = Join-Path $repoRoot $Path }
    # Never reuse a prior report directory as evidence for a new run.
    $unique = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $folder = Join-Path $Path $unique
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    return [IO.Path]::GetFullPath($folder)
}

function Start-GameProbe([string]$Name, [string]$Arguments, [int]$Port, [switch]$Rendered) {
    $report = Join-Path $script:resultFolder "$Name.json"
    $log = Join-Path $script:resultFolder "$Name.log"
    $mode = if ($Rendered) { '' } else { '-batchmode -nographics' }
    $argsText = "$mode --silence 1 --name $Name --port $Port --report `"$report`" -logFile `"$log`" $Arguments"
    $windowStyle = if ($Rendered) { 'Normal' } else { 'Hidden' }
    $process = Start-Process -FilePath $script:playerExe -ArgumentList $argsText -WindowStyle $windowStyle -PassThru
    $script:probeProcesses.Add($process)
    return $process
}

function Wait-ProbeLog([System.Diagnostics.Process]$Process, [string]$Name, [string]$Marker, [int]$Timeout = 60) {
    $log = Join-Path $script:resultFolder "$Name.log"
    $deadline = (Get-Date).AddSeconds($Timeout)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -SimpleMatch $Marker -Quiet)) { return }
        if ($Process.HasExited) { throw "$Name exited before '$Marker'. Inspect $log" }
        Start-Sleep -Milliseconds 500
    }
    throw "$Name timed out waiting for '$Marker'. Inspect $log"
}

function Wait-ProbeGroup([System.Diagnostics.Process[]]$Processes, [int]$Timeout = 240) {
    $deadline = (Get-Date).AddSeconds($Timeout)
    while ((Get-Date) -lt $deadline) {
        $remaining = @($Processes | Where-Object { -not $_.HasExited })
        if ($remaining.Count -eq 0) {
            foreach ($process in $Processes) {
                if ($process.ExitCode -ne 0) { throw "Probe PID $($process.Id) exited with $($process.ExitCode). Inspect $script:resultFolder" }
            }
            return
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Probe group timed out. Inspect $script:resultFolder"
}

function Assert-GameReport([string]$Name) {
    $path = Join-Path $script:resultFolder "$Name.json"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing report: $path" }
    $data = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if ($null -eq $data.passed -or $null -eq $data.failed -or $null -eq $data.errors) { throw "Malformed assertion report: $path" }
    if (@($data.failed).Count -or @($data.errors).Count -or @($data.passed).Count -eq 0) {
        throw "Failed report: $path (failed=$(@($data.failed).Count), errors=$(@($data.errors).Count))"
    }
    Write-Host "$Name : $(@($data.passed).Count) assertions passed"
    return @($data.passed).Count
}

function Stop-OwnProbes {
    foreach ($process in $script:probeProcesses) {
        if (-not $process.HasExited) { Stop-Process -Id $process.Id -ErrorAction SilentlyContinue }
        $process.Dispose()
    }
}
