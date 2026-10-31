"""Build Nanite terrain and update ground/grass lighting in the saved reserve.

Run via UnrealEditor-Cmd -run=pythonscript -script=... -nullrhi.
Does not regenerate geography, placement, collision or the atlas.
"""
from pathlib import Path
import unreal

project = Path(__file__).resolve().parents[1]
level = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
if not level.load_level('/Game/Pinefall/Maps/WildfrontReserve'):
    raise RuntimeError('Reserve could not be loaded')
actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
grass = []
for actor in actors.get_all_level_actors():
    if isinstance(actor, unreal.Landscape) and actor.get_actor_label().startswith('Wildfront Reserve'):
        actor.modify()
        actor.set_editor_property('enable_nanite', True)
        actor.set_editor_property('nanite_skirt_enabled', True)
        actor.set_editor_property('nanite_skirt_depth', 50.0)
    if actor.get_actor_label() == 'Reserve exposure':
        settings = actor.get_editor_property('settings')
        settings.set_editor_property('override_auto_exposure_bias', True)
        settings.set_editor_property('auto_exposure_bias', -0.35)
        actor.modify()
        actor.set_editor_property('settings', settings)
    if actor.get_actor_label() not in [
        'Reserve low meadow and woodland grass', 'Reserve dry short grass'
    ]:
        continue
    for component in actor.get_components_by_class(unreal.HierarchicalInstancedStaticMeshComponent):
        component.modify()
        component.set_editor_property('affect_dynamic_indirect_lighting', False)
        component.set_editor_property('affect_distance_field_lighting', False)
        grass.append(component.get_instance_count())
if len(grass) != 2 or min(grass) < 1000:
    raise RuntimeError('Expected both populated grass groups: ' + str(grass))
material = unreal.load_asset('/Game/Pinefall/Materials/M_ReserveTerrain')
# The custom expression is owned by the material; ObjectIterator avoids any
# dependency on private material expression collection properties.
updated = 0
for obj in unreal.ObjectIterator(unreal.MaterialExpressionCustom):
    if obj.get_outer() == material and 'PFStochasticGround' in obj.get_editor_property('code'):
        obj.modify()
        obj.set_editor_property('code', (project / 'Scripts/reserve_ground_pbr.hlsl').read_text(encoding='utf-8-sig'))
        updated += 1
if updated < 1:
    raise RuntimeError('No terrain expression found')
unreal.MaterialEditingLibrary.recompile_material(material)
unreal.EditorAssetLibrary.save_loaded_asset(material, False)
if not unreal.PFWorldBuilder.build_reserve_nanite():
    raise RuntimeError('Landscape Nanite build did not complete')
if not level.save_current_level():
    raise RuntimeError('Reserve could not be saved')
unreal.log('PF_RESERVE_RENDERING_OK grass_instances=' + str(grass))
