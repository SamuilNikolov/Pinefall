"""Import original meshes and existing recorded supply calls; no world regeneration."""
from pathlib import Path
import unreal
project=Path(__file__).resolve().parents[1];repo=project.parents[1];lib=unreal.EditorAssetLibrary;tools=unreal.AssetToolsHelpers.get_asset_tools()
folder='/Game/Pinefall/Art/Supplies';lib.make_directory(folder)
for source in sorted((project/'SourceArt/Prepared/Supplies').glob('*.glb')):
 path=folder+'/'+source.stem
 if lib.does_asset_exist(path):continue
 task=unreal.AssetImportTask();task.filename=str(source);task.destination_path=folder;task.destination_name=source.stem;task.automated=True;task.save=True;tools.import_asset_tasks([task])
 mesh=next((a for a in task.get_objects() if isinstance(a,unreal.StaticMesh)),None)
 if not mesh:
  mesh=next((unreal.load_asset(a) for a in lib.list_assets(path,True,False) if isinstance(unreal.load_asset(a),unreal.StaticMesh)),None)
 if not mesh:raise RuntimeError('Mesh import failed '+source.stem)
 if mesh.get_path_name().split('.')[0]!=path:
  if not lib.rename_asset(mesh.get_path_name(),path):raise RuntimeError('Canonical mesh rename '+source.stem)
 # Explicit simple materials preserve authored colors across commandlet imports.
 palette={'Paint':(.19,.25,.15),'Metal':(.20,.23,.25),'Rubber':(.025,.03,.035),'Glass':(.06,.15,.20),'Signal':(.15,.65,.85),'Cloth':(.73,.66,.47)}
 for i,slot in enumerate(mesh.get_editor_property('static_materials')):
  name=next((n for n in palette if str(slot.material_slot_name).startswith(n)),'Metal');mp=folder+'/M_Supply_'+name
  m=unreal.load_asset(mp)
  if not m:
   m=tools.create_asset('M_Supply_'+name,folder,unreal.Material,unreal.MaterialFactoryNew());node=unreal.MaterialEditingLibrary.create_material_expression(m,unreal.MaterialExpressionConstant3Vector);node.set_editor_property('constant',unreal.LinearColor(*palette[name],1));unreal.MaterialEditingLibrary.connect_material_property(node,'',unreal.MaterialProperty.MP_BASE_COLOR)
   for prop,value in [(unreal.MaterialProperty.MP_ROUGHNESS,.42 if name=='Metal' else .75),(unreal.MaterialProperty.MP_METALLIC,.7 if name=='Metal' else .15)]:
    c=unreal.MaterialEditingLibrary.create_material_expression(m,unreal.MaterialExpressionConstant);c.set_editor_property('r',value);unreal.MaterialEditingLibrary.connect_material_property(c,'',prop)
   if name=='Cloth':m.set_editor_property('two_sided',True)
   unreal.MaterialEditingLibrary.recompile_material(m);lib.save_loaded_asset(m)
  mesh.set_material(i,m)
 unreal.PFWorldBuilder.configure_mesh(mesh,False);lib.save_loaded_asset(mesh,False)
for name,file in [('SupplyInbound','inbound'),('SupplyLanded','landed'),('SupplyClaimed','claimed')]:
 if lib.does_asset_exist('/Game/Pinefall/Audio/'+name):continue
 task=unreal.AssetImportTask();task.filename=str(repo/'Assets/Resources/Announcer'/(file+'.wav'));task.destination_path='/Game/Pinefall/Audio';task.destination_name=name;task.automated=True;task.save=True;tools.import_asset_tasks([task])
 if not any(isinstance(a,unreal.SoundWave) for a in task.get_objects()):raise RuntimeError('Narration import '+name)
task=unreal.AssetImportTask();task.filename=str(project/'SourceArt/Audio/Supplies/Propeller.wav');task.destination_path='/Game/Pinefall/Audio';task.destination_name='SupplyPropeller';task.automated=True;task.save=True
if not lib.does_asset_exist('/Game/Pinefall/Audio/SupplyPropeller'):tools.import_asset_tasks([task])
sound=unreal.load_asset('/Game/Pinefall/Audio/SupplyPropeller')
if not isinstance(sound,unreal.SoundWave):raise RuntimeError('Aircraft audio import')
sound.set_editor_property('looping',True);lib.save_loaded_asset(sound)
unreal.log('PF_SUPPLY_SPECIAL_ASSETS_OK')
