[CmdletBinding()]
param([string]$PlayerPath='Builds/Windows/Pinefall.exe',[int]$Port=18660,[string]$OutputPath='')
. (Join-Path $PSScriptRoot 'ProbeCommon.ps1')
$script:playerExe=Resolve-PlayerPath $PlayerPath
$script:resultFolder=New-ResultFolder frontier $OutputPath
Write-Host "Results: $script:resultFolder"
try {
    $probe=Start-GameProbe frontier '--mode host --battle 0 --smoke frontier' $Port
    Wait-ProbeGroup @($probe) 180
    $count=Assert-GameReport frontier
    Write-Host "FRONTIER_OK : $count assertions. $script:resultFolder"
} finally { Stop-OwnProbes }
