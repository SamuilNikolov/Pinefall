"""Original quiet confirmation cues; recorded gunfire remains in the weapon table."""
from pathlib import Path
import math,random,struct,wave,unreal

project=Path(__file__).resolve().parents[1]
source=project/'SourceArt/Audio/Feedback'
source.mkdir(parents=True,exist_ok=True)
tools=unreal.AssetToolsHelpers.get_asset_tools()
reimport='PFReimportAudio' in unreal.SystemLibrary.get_command_line()
for name,duration,tone in [('Hit',.075,520),('ArmorHit',.11,880),('Elimination',.22,660),('Hurt',.12,125)]:
    rng=random.Random(name);samples=[];smooth=0;rate=24000
    for index in range(round(duration*rate)):
        t=index/rate;u=t/duration
        smooth=.68*smooth+.32*rng.uniform(-1,1)
        envelope=min(1,t/.003)*math.exp(-u*6)*(1-u)
        frequency=tone*(1-.18*u)
        signal=.19*smooth+.11*math.sin(2*math.pi*frequency*t)
        if name=='Elimination':signal+=.08*math.sin(2*math.pi*(tone*1.5)*t)*min(1,t/.04)
        samples.append(round(max(-1,min(1,signal*envelope))*32767))
    path=source/(name+'.wav')
    with wave.open(str(path),'wb') as out:
        out.setnchannels(1);out.setsampwidth(2);out.setframerate(rate);out.writeframes(struct.pack('<'+'h'*len(samples),*samples))
    if not reimport and unreal.EditorAssetLibrary.does_asset_exist('/Game/Pinefall/Audio/'+name):continue
    task=unreal.AssetImportTask();task.filename=str(path);task.destination_path='/Game/Pinefall/Audio';task.destination_name=name
    task.automated=True;task.replace_existing=True;task.save=True;tools.import_asset_tasks([task])
    if not any(isinstance(asset,unreal.SoundWave) for asset in task.get_objects()):raise RuntimeError('Sound import failed: '+name)
unreal.log('PF_COMBAT_AUDIO_OK')
repo=project.parents[1]
for name,file in [('Canid','canid0'),('Elk','elk0'),('Goat','goat0')]+[('GrassStep'+str(i),'grass'+str(i)) for i in range(5)]:
    if not reimport and unreal.EditorAssetLibrary.does_asset_exist('/Game/Pinefall/Audio/'+name):continue
    task=unreal.AssetImportTask();task.filename=str(repo/'Assets/Resources/Audio07'/(file+'.wav'))
    task.destination_path='/Game/Pinefall/Audio';task.destination_name=name;task.automated=True;task.replace_existing=True;task.save=True
    tools.import_asset_tasks([task])
    if not any(isinstance(asset,unreal.SoundWave) for asset in task.get_objects()):raise RuntimeError('Animal call import failed: '+name)
unreal.log('PF_WILDLIFE_AUDIO_OK')
