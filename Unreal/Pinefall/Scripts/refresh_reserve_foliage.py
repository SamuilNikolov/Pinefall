"""Rebuild only authored reserve vegetation using the current deterministic biome rules.
Does not regenerate architecture, terrain geometry, loot, vehicles or collision profiles.
"""
from pathlib import Path
import json, unreal
PROJECT=Path(__file__).resolve().parents[1]
LIB=unreal.EditorAssetLibrary
unreal.EditorLoadingAndSavingUtils.load_map('/Game/Pinefall/Maps/WildfrontReserve')
actors=unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
prefixes=('Reserve canopy','Reserve trunk collision','Reserve low meadow','Reserve dry short','Reserve scanned understory','Reserve scanned boulders','Reserve weathered field stones','Reserve scanned rock faces')
for actor in actors.get_all_level_actors():
    if actor.get_actor_label().startswith(prefixes):
        actors.destroy_actor(actor)
meshes=[unreal.load_asset('/Game/Pinefall/Art/Scenery/'+name) for name in ['SM_ForestFir','SM_WoodlandTree','SM_JungleTree','SM_DesertTree','SM_ForestFir']]
if any(x is None for x in meshes): raise RuntimeError('Missing biome canopy')
unreal.PFWorldBuilder.populate_reserve(meshes,unreal.load_asset('/Game/Pinefall/Art/Scenery/SM_GrassPatch'),unreal.load_asset('/Game/Pinefall/Art/Scenery/SM_ScrubBush'))
report=[]
for actor in actors.get_all_level_actors():
    if actor.get_actor_label().startswith(prefixes):
        for group in actor.get_components_by_class(unreal.HierarchicalInstancedStaticMeshComponent):
            report.append({'label':actor.get_actor_label(),'mesh':group.get_editor_property('static_mesh').get_name(),'instances':group.get_instance_count(),'collision':str(group.get_collision_enabled()),'end_cull_cm':group.get_editor_property('instance_end_cull_distance')})
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True): raise RuntimeError('Foliage map save failed')
(PROJECT/'Saved/ReserveFoliage.json').write_text(json.dumps(report,indent=2))
unreal.log('PF_RESERVE_FOLIAGE_REFRESH_OK '+str(sum(x['instances'] for x in report)))
