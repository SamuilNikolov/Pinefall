"""Natural species palettes with an original generated coat texture in skin rest space.
Keeps all source skeletons, hit primitives, motion clips and material slots intact.
"""
from pathlib import Path
import json,unreal
P=Path(__file__).resolve().parents[1];R=P.parents[1];L=unreal.EditorAssetLibrary;E=unreal.MaterialEditingLibrary;A=unreal.AssetToolsHelpers.get_asset_tools()
folder='/Game/Pinefall/Art/FaunaCoats';L.make_directory(folder)
t=unreal.AssetImportTask();t.filename=str(P/'SourceArt/FaunaCoats/NeutralFur-v1.png');t.destination_path=folder;t.destination_name='T_NeutralFur';t.automated=True;t.replace_existing=True;t.save=True;A.import_asset_tasks([t]);texture=unreal.load_asset(folder+'/T_NeutralFur')
if not texture:raise RuntimeError('Coat image import failed')
texture.set_editor_property('max_texture_size',1024);L.save_loaded_asset(texture,False)
def node(m,kind,**props):
 n=E.create_material_expression(m,kind)
 for k,v in props.items():n.set_editor_property(k,v)
 return n
def link(a,p,b,q):
 if not E.connect_material_expressions(a,p,b,q):raise RuntimeError('Input '+q)
def output(a,p,m,q):
 if not E.connect_material_property(a,p,q):raise RuntimeError('Output '+str(q))
report=[]
for name in ['Deer','Stag','Fox','Wolf','Bull','Horse','Alpaca']:
 d=json.loads((R/'Assets/Art/Animals'/(name+'.gltf')).read_text());specs={m['name'].replace(".","_"):m for m in d['materials']}
 bodies=[unreal.load_asset(k) for k in L.list_assets('/Game/Pinefall/Art/Fauna/'+name,True) if isinstance(unreal.load_asset(k),unreal.SkeletalMesh)]
 if len(bodies)!=1:raise RuntimeError('Expected one rigged body '+name)
 body=bodies[0];slots=body.get_editor_property('materials')
 for i,slot in enumerate(slots):
  label=str(slot.material_slot_name);spec=specs.get(label)
  if not spec:
   spec=next((v for k,v in specs.items() if label.startswith(k+'.') or label==k+'_Material'),None)
  if not spec:raise RuntimeError('Unknown coat slot '+name+' '+label)
  assetname='M_'+name+'_Coat_'+str(i);m=unreal.load_asset(folder+'/'+assetname)
  if not m:m=A.create_asset(assetname,folder,unreal.Material,unreal.MaterialFactoryNew())
  E.delete_all_material_expressions(m);color=spec.get('pbrMetallicRoughness',{}).get('baseColorFactor',[.3,.2,.1,1]);lower=label.lower();eye='eye' in lower
  if 'white' in lower and eye:color=[.27,.28,.24,1]
  base=node(m,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(*color))
  eye=eye or (name=='Stag' and lower=='material_011')
  coated=(('main' in lower or lower in ['grey','black','hair']) or (name=='Stag' and lower=='material')) and not eye
  if coated:
   detail=node(m,unreal.MaterialExpressionCustom,code='float3 w=pow(abs(N),4);w/=max(.0001,w.x+w.y+w.z);float3 c=Texture2DSample(Fur,FurSampler,P.yz*.04).rgb*w.x+Texture2DSample(Fur,FurSampler,P.xz*.04).rgb*w.y+Texture2DSample(Fur,FurSampler,P.xy*.04).rgb*w.z;return .78+dot(c,float3(.299,.587,.114))*.85;',output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT1)
   inputs=[]
   for label2 in ['P','N','Fur']:
    item=unreal.CustomInput();item.set_editor_property('input_name',label2);inputs.append(item)
   detail.set_editor_property('inputs',inputs)
   for label2,kind in [('P',unreal.MaterialExpressionPreSkinnedPosition),('N',unreal.MaterialExpressionPreSkinnedNormal)]:
    vi=node(m,unreal.MaterialExpressionVertexInterpolator);link(node(m,kind),'',vi,'');link(vi,'',detail,label2)
   link(node(m,unreal.MaterialExpressionTextureObject,texture=texture),'',detail,'Fur')
   multiply=node(m,unreal.MaterialExpressionMultiply);link(base,'',multiply,'A');link(detail,'',multiply,'B');base=multiply
  output(base,'',m,unreal.MaterialProperty.MP_BASE_COLOR)
  output(node(m,unreal.MaterialExpressionConstant,r=.14 if eye else .9 if coated else .62),'',m,unreal.MaterialProperty.MP_ROUGHNESS)
  output(node(m,unreal.MaterialExpressionConstant,r=.35 if eye else .2),'',m,unreal.MaterialProperty.MP_SPECULAR)
  E.set_material_usage(m,unreal.MaterialUsage.MATUSAGE_SKELETAL_MESH);E.recompile_material(m);L.save_loaded_asset(m,False);slot.material_interface=m
 body.set_editor_property('materials',slots);L.save_loaded_asset(body,False);report.append({'species':name,'body':body.get_path_name(),'slots':len(slots),'skeleton_unchanged':True})
(P/'Saved/FaunaCoats.json').write_text(json.dumps(report,indent=2));unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True);unreal.log('PF_FAUNA_COATS_OK species='+str(len(report)))
