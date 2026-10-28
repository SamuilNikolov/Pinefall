"""Read-only source/scene inspection. No generation or gameplay overrides."""
from pathlib import Path
import json,unreal
project=Path(__file__).resolve().parents[1]
unreal.EditorLoadingAndSavingUtils.load_map('/Game/Pinefall/Maps/CedarCrossing')
actors=unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
report={'meshes':{},'world':[]}
for name in ['SM_Pistol','SM_Rifle']:
    mesh=unreal.load_asset('/Game/Pinefall/Art/Weapons/'+name)
    if not mesh:continue
    box=mesh.get_bounding_box();report['meshes'][name]={'min':str(box.min),'max':str(box.max)}
for actor in actors.get_all_level_actors():
    label=actor.get_actor_label()
    if not any(key in label for key in ['Tree trunk','Limestone outcrop','Lake']):continue
    components=actor.get_components_by_class(unreal.PrimitiveComponent)
    report['world'].append({'label':label,'components':[{'name':c.get_name(),'enabled':str(c.get_collision_enabled()),'profile':str(c.get_collision_profile_name()),'mesh':c.static_mesh.get_path_name() if isinstance(c,unreal.StaticMeshComponent) and c.static_mesh else None} for c in components]})
(project/'Saved/SceneAudit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
unreal.log('PF_SCENE_AUDIT_OK')
