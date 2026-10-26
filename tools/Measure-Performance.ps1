[CmdletBinding()]
param(
    [string]$PlayerPath = 'Builds/Windows/Pinefall.exe',
    [ValidateSet(1, 3)][int]$Players = 3,
    [ValidateRange(640, 7680)][int]$Width = 1600,
    [ValidateRange(480, 4320)][int]$Height = 900,
    [ValidateRange(1024, 65535)][int]$Port = 17950,
    [string]$OutputPath = ''
)
. (Join-Path $PSScriptRoot 'ProbeCommon.ps1')
$script:playerExe = Resolve-PlayerPath $PlayerPath
$script:resultFolder = New-ResultFolder 'performance' $OutputPath
Write-Host "A rendered test window will open. Results: $script:resultFolder"
try {
    $arguments = "--mode host --battle 0 --smoke performance --perf-players $Players --uncapped 1 -screen-fullscreen 0 -screen-width $Width -screen-height $Height"
    $hostProbe = Start-GameProbe 'performance-host' $arguments $Port -Rendered
    Wait-ProbeLog $hostProbe 'performance-host' 'PINEFALL_CONNECTED 0'
    $group = @($hostProbe)
    if ($Players -eq 3) {
        $group += Start-GameProbe 'performance-ally' '--mode client --address 127.0.0.1 --team 0 --battle 0 --smoke benchmark --duration 120' $Port
        $group += Start-GameProbe 'performance-enemy' '--mode client --address 127.0.0.1 --team 1 --battle 0 --smoke benchmark --duration 120' $Port
        Wait-ProbeLog $hostProbe 'performance-host' 'PINEFALL_CONNECTED 1'
        Wait-ProbeLog $hostProbe 'performance-host' 'PINEFALL_CONNECTED 2'
    }
    Wait-ProbeGroup $group 180
    $path = Join-Path $script:resultFolder 'performance-host.json'
    $report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if ($null -eq $report.samples -or @($report.samples).Count -ne 6 -or @($report.errors).Count -or $report.microphoneStarts -ne 0) {
        throw "Invalid performance run. Inspect $path"
    }
    if (-not $report.gpu -or $report.gpu -match '(?i)null|headless') {
        throw "A real graphics device is required for performance evidence. Inspect $path"
    }
    foreach ($sample in $report.samples) {
        if ($sample.frames -lt 1 -or $sample.averageFps -le 0 -or $sample.p95Ms -le 0) { throw "Invalid sample in $path" }
    }
    Write-Host "GPU: $($report.gpu) | CPU: $($report.cpu) | $($report.width)x$($report.height)"
    $report.samples | Select-Object scene, averageFps, medianMs, p95Ms, p99Ms | Format-Table -AutoSize
    Write-Host "PERFORMANCE_OK : $path"
}
finally { Stop-OwnProbes }
