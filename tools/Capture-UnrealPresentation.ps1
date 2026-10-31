param(
    [string[]]$Views = @('Forest'),
    [int]$Width = 1600,
    [int]$Height = 900,
    [string]$OutputDirectory = 'artifacts/unreal-0.4-visual-dev',
    [switch]$Inventory,
    [switch]$ShowWindow,
    [string]$EngineRoot = "$env:ProgramFiles\Epic Games\UE_5.6"
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'Unreal/Pinefall/Pinefall.uproject'
$editor = Join-Path $EngineRoot 'Engine/Binaries/Win64/UnrealEditor.exe'
$output = [IO.Path]::GetFullPath((Join-Path $repo $OutputDirectory))
New-Item -ItemType Directory -Force -Path $output | Out-Null
foreach ($view in $Views) {
    $stem = "$view-$Width-$Height" + $(if ($Inventory) { '-inventory' } else { '' })
    $png = Join-Path $output "$stem.png"
    $log = Join-Path $output "$stem.log"
    $arguments = @('"' + $project + '"', '/Game/Pinefall/Maps/WildfrontReserve?play?Name=Ranger',
        '-game', '-windowed', "-ResX=$Width", "-ResY=$Height", '-ForceRes', '-nosound', '-unattended',
        "-PFCombatView=$view", '-PFCapture="' + $png + '"', '-PFQuitAfterCapture', '-abslog="' + $log + '"')
    if ($Inventory) { $arguments += '-PFCaptureInventory' }
    $style = if ($ShowWindow) { 'Normal' } else { 'Hidden' }
    $process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle $style -PassThru
    $deadline = (Get-Date).AddSeconds(420)
    try {
        while (!$process.WaitForExit(1000)) {
            if ((Get-Date) -gt $deadline) { throw "Capture timed out: $view" }
        }
        if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $png)) { throw "Capture failed: $view; inspect $log" }
        Write-Output "PF_CAPTURE_OK $png"
    } finally {
        if (!$process.HasExited) { Stop-Process -Id $process.Id -Force }
    }
}
