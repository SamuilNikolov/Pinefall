param([string]$EngineRoot="$env:ProgramFiles\Epic Games\UE_5.6",[string]$View='Forest',[int]$Width=1600,[int]$Height=900,[int]$Port=18983,[int]$TimeoutSeconds=600,[switch]$Capture,[ValidateRange(0,2)][int]$Peers=2,[switch]$TraceCPU,[switch]$CompareLighting,[string]$RenderCommands="")
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$run=Join-Path $repo ('artifacts/unreal-render/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+$View)
New-Item -ItemType Directory -Path $run -Force|Out-Null
$project=Join-Path $repo 'Unreal/Pinefall/Pinefall.uproject'
$game=Join-Path $EngineRoot 'Engine/Binaries/Win64/UnrealEditor.exe'
$headless=Join-Path $EngineRoot 'Engine/Binaries/Win64/UnrealEditor-Cmd.exe'
$capturePath=Join-Path $run 'view.png'
$report=Join-Path $run 'render.json';$hostLog=Join-Path $run 'host.log';$children=@()
function Wait-Marker([string]$Path,[string]$Marker,[datetime]$Deadline) {
    while((Get-Date) -lt $Deadline) {
        if((Test-Path -LiteralPath $Path) -and (Select-String -LiteralPath $Path -Pattern $Marker -Quiet)){return}
        foreach($child in $children){if($child.HasExited){throw "Render test process exited. Inspect $run"}}
        Start-Sleep -Milliseconds 500
    };throw "Render readiness timeout. Inspect $run"
}
try {
    $argsHost=@(('"'+$project+'"'),'/Game/Pinefall/Maps/WildfrontReserve?listen?play?Name=Ranger','-game','-windowed',"-ResX=$Width","-ResY=$Height",'-ForceRes','-nosound','-unattended','-PFSmoke',"-PFCombatView=$View","-PFRenderPeers=$Peers",('-PFRenderReport="'+$report+'"'),('-abslog="'+$hostLog+'"'),"-port=$Port",'-ExecCmds="r.VSync 0,t.MaxFPS 0,t.IdleWhenNotForeground 0"')
    if($RenderCommands){$argsHost=$argsHost|ForEach-Object {if($_ -like '-ExecCmds=*'){$_ -replace '"$',(","+$RenderCommands+'"')}else{$_}}}
    if($CompareLighting){$argsHost+='-PFRenderCompareLighting'}
    if($TraceCPU){$Capture=$true;$argsHost+=@('-trace=cpu,frame,bookmark','-statnamedevents',('-tracefile="'+(Join-Path $run 'cpu.utrace')+'"'),'-PFQuitAfterCapture')}
    if($Capture){$argsHost+=('-PFCapture="'+$capturePath+'"')}
    $children+=Start-Process -FilePath $game -ArgumentList $argsHost -WindowStyle Normal -PassThru
    $deadline=(Get-Date).AddSeconds($TimeoutSeconds)
    Wait-Marker $hostLog 'GameNetDriver.*listening on port' $deadline
    for($i=0;$i -lt $Peers;$i++) {
        $log=Join-Path $run ("client-$i.log")
        $argsClient=@(('"'+$project+'"'),("127.0.0.1:$Port"),'-game','-nullrhi','-nosound','-unattended','-PFSmoke',('-abslog="'+$log+'"'),'-ExecCmds="t.IdleWhenNotForeground 0,t.MaxFPS 30"')
        $children+=Start-Process -FilePath $headless -ArgumentList $argsClient -WindowStyle Hidden -PassThru
    }
    $finalReport=if($CompareLighting){Join-Path $run 'render-balanced.json'}else{$report}
    while(!(Test-Path -LiteralPath $finalReport)) {
        if((Get-Date) -gt $deadline){throw "Rendered frame report timeout. Inspect $run"}
        foreach($child in $children){if($child.HasExited){throw "Render process exited. Inspect $run"}};Start-Sleep -Milliseconds 500
    }
    if($Capture){Wait-Marker $hostLog 'PF_CAPTURE_SAVED' $deadline}
    if($TraceCPU -and !$children[0].WaitForExit(15000)){throw "Trace did not close cleanly: $run"}
    $data=Get-Content -LiteralPath $finalReport -Raw|ConvertFrom-Json
    if(!$data.rendered -or $data.minimum_players -lt ($Peers+1) -or $data.frames -lt 100 -or $data.width -ne $Width -or $data.height -ne $Height){throw "Invalid graphics/peer coverage. Inspect $report"}
    @{scope="Graphics-enabled listen host with $Peers real headless remote clients capped at 30 FPS on the same machine; static biome view, AI and HUD active. One-machine static-scene evidence; no whole-match, public-WAN or different-hardware claim";view=$View;remote_peers=$Peers;render_commands=$RenderCommands;lighting_comparison=[bool]$CompareLighting;report=(Split-Path -Leaf $finalReport)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $run 'scope.json') -Encoding utf8
    Write-Output ("Rendered host: {0:N1} FPS average, p95 {1:N1} ms, p99 {2:N1} ms. Evidence: {3}" -f (1000/$data.frame_avg_ms),$data.frame_p95_ms,$data.frame_p99_ms,$run)
    if($CompareLighting){
        $before=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
        if($before.'sg.GlobalIlluminationQuality' -ne 2 -or $before.'sg.ReflectionQuality' -ne 2 -or $data.'sg.GlobalIlluminationQuality' -ne 1 -or $data.'sg.ReflectionQuality' -ne 1){throw "Lighting comparison did not exercise High then Balanced: $run"}
        foreach($key in @('r.Nanite','landscape.RenderNanite','r.ScreenPercentage','foliage.DensityScale','r.ViewDistanceScale','width','height','wildlife_population','minimum_players')){if($before.$key -ne $data.$key){throw "Lighting comparison changed $key : $run"}}
        Write-Output ("Same-process High: {0:N1} FPS; Balanced: {1:N1} FPS" -f (1000/$before.frame_avg_ms),(1000/$data.frame_avg_ms))
    }
} finally {foreach($child in $children){if(!$child.HasExited){Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue}}}
