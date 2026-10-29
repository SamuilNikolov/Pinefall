"""Explicit PBR ranger materials; do not rely on incomplete glTF shader translation."""
from pathlib import Path
import json,unreal
PROJECT=Path(__file__).resolve().parents[1]
LIB=unreal.EditorAssetLibrary;MEL=unreal.MaterialEditingLibrary;TOOLS=unreal.AssetToolsHelpers.get_asset_tools()
source=PROJECT/'SourceArt/Prepared/TrailRanger/Textures'
folder='/Game/Pinefall/Materials/Characters';LIB.make_directory(folder)
def node(m,kind,**values):
 n=MEL.create_material_expression(m,kind)
 for k,v in values.items():n.set_editor_property(k,v)
 return n
def wire(a,p,b,q):
 if not MEL.connect_material_expressions(a,p,b,q):raise RuntimeError(q)
def output(n,p,m,q):
 if not MEL.connect_material_property(n,p,q):raise RuntimeError(str(q))
mesh=unreal.load_asset('/Game/Pinefall/Art/Characters/TrailRanger/TrailRanger')
rows=json.loads((source/'materials.json').read_text());report=[];slots=mesh.get_editor_property('materials')
for index,row in enumerate(rows):
 name='M_Ranger_'+row['name'];path=folder+'/'+name
 m=unreal.load_asset(path) if LIB.does_asset_exist(path) else TOOLS.create_asset(name,folder,unreal.Material,unreal.MaterialFactoryNew())
 # Existing shaders may be rooted by the character CDO during commandlet startup.
 # Keep authored graphs and update cornea additively; deleting rooted expressions
 # crashes the editor. Fresh materials below are built normally.
 if MEL.get_material_property_input_node(m,unreal.MaterialProperty.MP_BASE_COLOR):
  if row['name']=='Cornea' and m.get_editor_property('blend_mode')!=unreal.BlendMode.BLEND_TRANSLUCENT:
   m.modify();m.set_editor_property('blend_mode',unreal.BlendMode.BLEND_TRANSLUCENT)
   output(node(m,unreal.MaterialExpressionConstant,r=.08),'',m,unreal.MaterialProperty.MP_OPACITY)
   MEL.recompile_material(m);LIB.save_loaded_asset(m,False)
  entry=slots[index];entry.set_editor_property('material_interface',m);slots[index]=entry
  report.append({'slot':row['name'],'material':path,'textured':bool(row.get('color_file'))});continue
 m.set_editor_property('two_sided',True)
 color=row['color']
 if row['name']=='Cloth_shirt':color=[.20,.29,.12,1]
 if row['name']=='Cloth_jeans':color=[.09,.16,.24,1]
 factor=node(m,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(*color[:3],1))
 base=factor;alpha=None
 if row.get('color_file'):
  name='T_Ranger_'+row['name']+'_Color';target='/Game/Pinefall/Art/Textures/'+name
  texture=unreal.load_asset(target)
  if not texture:
   task=unreal.AssetImportTask();task.filename=str(source/row['color_file']);task.destination_path='/Game/Pinefall/Art/Textures';task.destination_name=name;task.automated=True;task.save=True;TOOLS.import_asset_tasks([task]);texture=unreal.load_asset(target)
  texture.set_editor_property('srgb',True);texture.set_editor_property('max_texture_size',2048);LIB.save_loaded_asset(texture)
  sample=node(m,unreal.MaterialExpressionTextureSample,texture=texture,sampler_type=unreal.MaterialSamplerType.SAMPLERTYPE_COLOR)
  base=node(m,unreal.MaterialExpressionMultiply);wire(sample,'RGB',base,'A');wire(factor,'',base,'B');alpha=sample
 output(base,'',m,unreal.MaterialProperty.MP_BASE_COLOR)
 # Imported flat region/pore maps produced visibly broken lighting. Preserve
 # smooth mesh normals until a calibrated tangent-space normal is authored.
 output(node(m,unreal.MaterialExpressionConstant,r=.82),'',m,unreal.MaterialProperty.MP_ROUGHNESS)
 output(node(m,unreal.MaterialExpressionConstant,r=.28),'',m,unreal.MaterialProperty.MP_SPECULAR)
 if row['name']=='Cornea':
  m.set_editor_property('blend_mode',unreal.BlendMode.BLEND_TRANSLUCENT);output(node(m,unreal.MaterialExpressionConstant,r=.08),'',m,unreal.MaterialProperty.MP_OPACITY)
 if row['alpha']=='MASK' and alpha:
  m.set_editor_property('blend_mode',unreal.BlendMode.BLEND_MASKED);output(alpha,'A',m,unreal.MaterialProperty.MP_OPACITY_MASK)
 MEL.set_material_usage(m,unreal.MaterialUsage.MATUSAGE_SKELETAL_MESH);MEL.recompile_material(m);LIB.save_loaded_asset(m);entry=slots[index];entry.set_editor_property('material_interface',m);slots[index]=entry
 report.append({'slot':row['name'],'material':path,'textured':bool(row.get('color_file'))})
mesh.modify();mesh.set_editor_property('materials',slots);LIB.save_loaded_asset(mesh,False)
for index,row in enumerate(rows):
 actual=mesh.get_editor_property('materials')[index].material_interface
 if not actual or actual.get_name()!='M_Ranger_'+row['name']:raise RuntimeError('Material assignment did not persist: '+row['name'])
(PROJECT/'Saved/CharacterMaterialRepair.json').write_text(json.dumps(report,indent=2))
unreal.log('PF_CHARACTER_MATERIALS_OK slots='+str(len(rows)))
