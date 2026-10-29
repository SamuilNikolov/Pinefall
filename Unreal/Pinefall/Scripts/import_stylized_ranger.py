"""Import the dressed CC0 ranger and validate its actual animation skeleton."""
from pathlib import Path
import json,unreal
PROJECT=Path(__file__).resolve().parents[1]
LIB=unreal.EditorAssetLibrary;TOOLS=unreal.AssetToolsHelpers.get_asset_tools()
folder='/Game/Pinefall/Art/Characters/TrailRanger'
# Interchange reimports only the selected mesh and retains stale sibling clips.
# Explicit refresh rebuilds this generated set, which has no Blueprint consumers.
if 'PFRefreshRangerAnimations' in unreal.SystemLibrary.get_command_line() and LIB.does_directory_exist(folder):
    if not LIB.delete_directory(folder):raise RuntimeError('Could not replace generated stylized ranger assets')
LIB.make_directory(folder)
task=unreal.AssetImportTask();task.filename=str(PROJECT/'SourceArt/Prepared/TrailRanger/TrailRanger.glb');task.destination_path=folder
pipeline=unreal.InterchangeGenericAssetsPipeline()
pipeline.set_editor_property('reimport_strategy',unreal.ReimportStrategyFlags.APPLY_PIPELINE_PROPERTIES)
common_meshes=pipeline.get_editor_property('common_meshes_properties')
common_meshes.set_editor_property('recompute_normals',False)
common_meshes.set_editor_property('recompute_tangents',True)
pipeline.animation_pipeline.set_editor_property('import_animations',True);pipeline.animation_pipeline.set_editor_property('import_bone_tracks',True)
stack=unreal.InterchangePipelineStackOverride();stack.add_pipeline(pipeline);task.options=stack
task.automated=True;task.replace_existing=True;task.save=True;TOOLS.import_asset_tasks([task])
assets=[unreal.load_asset(p) for p in LIB.list_assets(folder,recursive=True,include_folder=False)]
bodies=[x for x in assets if isinstance(x,unreal.SkeletalMesh)]
clips=[x for x in assets if isinstance(x,unreal.AnimSequence)]
if len(bodies)!=1 or len(clips)<17:raise RuntimeError('Ranger animated import incomplete: '+str([(x.get_name(),x.get_class().get_name()) for x in assets]))
body=bodies[0];skeleton=body.skeleton
if any(c.get_editor_property('skeleton')!=skeleton or c.get_play_length()<=0 for c in clips):raise RuntimeError('Ranger skeleton mismatch')
if not body.physics_asset:raise RuntimeError('Ranger needs skeletal collision')
unreal.PFRangerAssetTools.fit_head_collision(body)
LIB.save_loaded_asset(body.physics_asset)
if body.get_path_name().split('.')[0]!=folder+'/TrailRanger':
    if not LIB.rename_asset(body.get_path_name(),folder+'/TrailRanger'):raise RuntimeError('Ranger mesh naming failed')
for a in assets:LIB.save_loaded_asset(a)
(PROJECT/'Saved/TrailRangerImport.json').write_text(json.dumps({'mesh':body.get_path_name(),'physics':body.physics_asset.get_path_name(),'clips':{c.get_name():c.get_path_name() for c in clips}},indent=2),encoding='utf-8')
unreal.log('PF_RANGER_IMPORT_OK clips='+str(len(clips)))
