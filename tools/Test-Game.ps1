[CmdletBinding()]
param(
    [string]$PlayerPath = 'Builds/Windows/Pinefall.exe',
    [ValidateSet('All', 'Expedition', 'Living', 'Polish')][string]$Suite = 'All',
    [ValidateRange(1024, 65515)][int]$PortBase = 17930,
    [string]$OutputPath = ''
)
. (Join-Path $PSScriptRoot 'ProbeCommon.ps1')
$script:playerExe = Resolve-PlayerPath $PlayerPath
$script:resultFolder = New-ResultFolder 'tests' $OutputPath
$reports = [Collections.Generic.List[string]]::new()
Write-Host "Results: $script:resultFolder"
try {
    if ($Suite -in @('All', 'Expedition')) {
        $hostProbe = Start-GameProbe 'host' '--mode host --teamsize 2 --team 0 --smoke expedition --faststorm 10 --wait-late 1' $PortBase
        Wait-ProbeLog $hostProbe 'host' 'PINEFALL_CONNECTED 0'
        $clientA = Start-GameProbe 'client-a' '--mode client --address 127.0.0.1 --team 0 --smoke expedition' $PortBase
        $clientB = Start-GameProbe 'client-b' '--mode client --address 127.0.0.1 --team 1 --smoke expedition' $PortBase
        Wait-ProbeLog $hostProbe 'host' 'PINEFALL_ROUND_START 1' 180
        $late = Start-GameProbe 'late-join' '--mode client --address 127.0.0.1 --team 1 --smoke expedition-late' $PortBase
        Wait-ProbeGroup @($hostProbe, $clientA, $clientB, $late) 240
        foreach ($name in @('host', 'client-a', 'client-b', 'late-join')) { $reports.Add($name) }
        foreach ($size in @(1, 4)) {
            $name = "practice-$size"
            $practice = Start-GameProbe $name "--mode host --teamsize $size --bots 1 --smoke expedition-practice" ($PortBase + $size)
            Wait-ProbeGroup @($practice) 100
            $reports.Add($name)
        }
    }
    if ($Suite -in @('All', 'Living')) {
        $port = $PortBase + 10
        $livingHost = Start-GameProbe 'living-host' '--mode host --team 0 --battle 0 --smoke living' $port
        Wait-ProbeLog $livingHost 'living-host' 'PINEFALL_CONNECTED 0'
        $ally = Start-GameProbe 'living-ally' '--mode client --address 127.0.0.1 --team 0 --battle 0 --smoke living' $port
        $enemy = Start-GameProbe 'living-enemy' '--mode client --address 127.0.0.1 --team 1 --battle 0 --smoke living' $port
        Wait-ProbeGroup @($livingHost, $ally, $enemy) 180
        foreach ($name in @('living-host', 'living-ally', 'living-enemy')) { $reports.Add($name) }
    }
    if ($Suite -in @('All', 'Polish')) {
        $polish = Start-GameProbe 'polish' '--mode host --battle 0 --smoke polish' ($PortBase + 19)
        Wait-ProbeGroup @($polish) 180
        $reports.Add('polish')
    }
    $total = 0
    foreach ($name in $reports) { $total += Assert-GameReport $name }
    Write-Host "GAMEPLAY_OK : $total assertions across $($reports.Count) reports. $script:resultFolder"
}
finally { Stop-OwnProbes }
