[CmdletBinding()]
param([string]$PlayerPath = 'Builds/Windows/Pinefall.exe', [ValidateRange(1024,65535)][int]$Port = 18100, [string]$OutputPath = '')
. (Join-Path $PSScriptRoot 'ProbeCommon.ps1')
$script:playerExe = Resolve-PlayerPath $PlayerPath
$script:resultFolder = New-ResultFolder 'fieldcraft' $OutputPath
Write-Host "Results: $script:resultFolder"
try {
    $hostProbe = Start-GameProbe 'fieldcraft-host' '--mode host --battle 0 --smoke fieldcraft --team 0' $Port
    Wait-ProbeLog $hostProbe 'fieldcraft-host' 'PINEFALL_CONNECTED 0'
    $ally = Start-GameProbe 'fieldcraft-ally' '--mode client --address 127.0.0.1 --battle 0 --smoke fieldcraft --team 0' $Port
    $enemy = Start-GameProbe 'fieldcraft-enemy' '--mode client --address 127.0.0.1 --battle 0 --smoke fieldcraft --team 1' $Port
    Wait-ProbeLog $hostProbe 'fieldcraft-host' 'FIELDCRAFT_LATE_READY' 180
    $late = Start-GameProbe 'fieldcraft-late' '--mode client --address 127.0.0.1 --smoke fieldcraft --field-role late --team 1' $Port
    Wait-ProbeGroup @($hostProbe,$ally,$enemy,$late) 180
    $total = 0
    foreach ($name in @('fieldcraft-host','fieldcraft-ally','fieldcraft-enemy','fieldcraft-late')) { $total += Assert-GameReport $name }
    Write-Host "FIELDCRAFT_OK : $total assertions. $script:resultFolder"
}
finally { Stop-OwnProbes }
