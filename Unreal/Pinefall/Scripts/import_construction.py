"""Owned construction previews and a clearly visible open storm boundary."""
from pathlib import Path
import unreal
P=Path(__file__).resolve().parents[1];L=unreal.EditorAssetLibrary;E=unreal.MaterialEditingLibrary;A=unreal.AssetToolsHelpers.get_asset_tools()
for folder in ['/Game/Pinefall/Art/Construction','/Game/Pinefall/Materials']:L.make_directory(folder)
def node(m,kind,**props):
 n=E.create_material_expression(m,kind)
 for k,v in props.items():n.set_editor_property(k,v)
 return n
def link(a,p,b,q):
 if not E.connect_material_expressions(a,p,b,q):raise RuntimeError('Input '+q)
def output(a,p,m,q):
 if not E.connect_material_property(a,p,q):raise RuntimeError('Output '+str(q))
def material(name):
 m=unreal.load_asset('/Game/Pinefall/Materials/'+name)
 if not m:m=A.create_asset(name,'/Game/Pinefall/Materials',unreal.Material,unreal.MaterialFactoryNew())
 E.delete_all_material_expressions(m);m.set_editor_property('two_sided',True);m.set_editor_property('blend_mode',unreal.BlendMode.BLEND_TRANSLUCENT);m.set_editor_property('shading_model',unreal.MaterialShadingModel.MSM_UNLIT)
 return m
for name,color in [('M_BuildPreview',(.08,.5,1.5,1)),('M_BuildInvalid',(1.5,.07,.04,1)),('M_StormSurface',(.15,.05,.6,1)),('M_BuildEditRemoved',(.07,.09,.13,1))]:
 m=material(name);c=node(m,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(*color))
 eye=node(m,unreal.MaterialExpressionEyeAdaptation);expose=node(m,unreal.MaterialExpressionMax,const_b=.0001);link(eye,'',expose,'A')
 emission=node(m,unreal.MaterialExpressionDivide);link(c,'',emission,'A');link(expose,'',emission,'B');output(emission,'',m,unreal.MaterialProperty.MP_EMISSIVE_COLOR)
 if name=='M_StormSurface':
  fn=node(m,unreal.MaterialExpressionCustom,code='float waves=.5+.5*sin(P.z*.004-T*2.1+sin(P.x*.0008+P.y*.001)*2); float streak=pow(saturate(sin(P.x*.0017+P.y*.0012+T*.6)),12); return saturate(.28+waves*.17+streak*.4);',output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT1)
  inputs=[]
  for label in ['P','T']:
   i=unreal.CustomInput();i.set_editor_property('input_name',label);inputs.append(i)
  fn.set_editor_property('inputs',inputs);link(node(m,unreal.MaterialExpressionWorldPosition),'',fn,'P');link(node(m,unreal.MaterialExpressionTime),'',fn,'T');output(fn,'',m,unreal.MaterialProperty.MP_OPACITY)
 else:output(node(m,unreal.MaterialExpressionConstant,r=.33),'',m,unreal.MaterialProperty.MP_OPACITY)
 E.set_material_usage(m,unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES);E.recompile_material(m);L.save_loaded_asset(m,False)
for name in ['SM_FieldPickaxe','SM_StormWall','SM_WoodBundle','SM_StoneStack','SM_MetalBundle']:
 t=unreal.AssetImportTask();t.filename=str(P/'SourceArt/Prepared/Construction'/(name+'.glb'));t.destination_path='/Game/Pinefall/Art/Construction';t.destination_name=name;t.automated=True;t.replace_existing=True;t.save=True;A.import_asset_tasks([t])
 objects=[o for o in t.get_objects() if isinstance(o,unreal.StaticMesh)]
 if not objects:
  objects=[unreal.load_asset(k) for k in L.list_assets('/Game/Pinefall/Art/Construction/'+name,True) if isinstance(unreal.load_asset(k),unreal.StaticMesh)]
 mesh=next((x for x in objects if x.get_name()==name),None)
 if not mesh:raise RuntimeError('Mesh '+name)
 target='/Game/Pinefall/Art/Construction/'+name
 if mesh.get_path_name().split('.')[0]!=target:L.rename_asset(mesh.get_path_name(),target);mesh=unreal.load_asset(target)
 unreal.PFWorldBuilder.configure_mesh(mesh,False)
 if name!='SM_StormWall':
  for i,s in enumerate(mesh.get_editor_property('static_materials')):
   label=str(s.material_slot_name);part='Wood' if 'Wood' in label else 'Stone' if 'Stone' in label else 'Metal';mesh.set_material(i,unreal.load_asset('/Game/Pinefall/Materials/Reserve/M_Reserve_'+part))
 else:mesh.set_material(0,unreal.load_asset('/Game/Pinefall/Materials/M_StormSurface'))
 L.save_loaded_asset(mesh,False)
unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True)
unreal.log('PF_CONSTRUCTION_ASSETS_OK')
