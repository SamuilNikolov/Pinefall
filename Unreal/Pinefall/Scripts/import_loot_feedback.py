"""Original inexpensive rarity cues and a matching repair icon. No dynamic lights/ticks."""
from pathlib import Path
import struct,unreal
P=Path(__file__).resolve().parents[1];L=unreal.EditorAssetLibrary;E=unreal.MaterialEditingLibrary;A=unreal.AssetToolsHelpers.get_asset_tools()
def node(m,kind,**props):
 n=E.create_material_expression(m,kind)
 for k,v in props.items():n.set_editor_property(k,v)
 return n
def link(a,p,b,q):
 if not E.connect_material_expressions(a,p,b,q):raise RuntimeError('Input '+q)
def output(a,p,m,q):
 if not E.connect_material_property(a,p,q):raise RuntimeError('Output '+str(q))
for name in ['M_LootHalo','M_LootBeacon']:
 folder='/Game/Pinefall/Materials';path=folder+'/'+name
 m=unreal.load_asset(path) if L.does_asset_exist(path) else A.create_asset(name,folder,unreal.Material,unreal.MaterialFactoryNew())
 E.delete_all_material_expressions(m);m.set_editor_property('two_sided',True);m.set_editor_property('blend_mode',unreal.BlendMode.BLEND_TRANSLUCENT);m.set_editor_property('shading_model',unreal.MaterialShadingModel.MSM_UNLIT)
 tint=node(m,unreal.MaterialExpressionVectorParameter,parameter_name='Tint',default_value=unreal.LinearColor(.15,.5,1,1))
 strength=node(m,unreal.MaterialExpressionScalarParameter,parameter_name='Strength',default_value=.75)
 bright=node(m,unreal.MaterialExpressionMultiply);link(tint,'',bright,'A');link(strength,'',bright,'B')
 eye=node(m,unreal.MaterialExpressionMax,const_b=.0001);link(node(m,unreal.MaterialExpressionEyeAdaptation),'',eye,'A')
 emission=node(m,unreal.MaterialExpressionDivide);link(bright,'',emission,'A');link(eye,'',emission,'B');output(emission,'',m,unreal.MaterialProperty.MP_EMISSIVE_COLOR)
 code='float d=length(UV-.5)*2; float core=pow(saturate(1-d),2); float rim=1-smoothstep(.012,.04,abs(d-.75)); return (core*.22+rim*.12)*S;' if name=='M_LootHalo' else 'return (1-smoothstep(.45,.98,UV.y))*.42*S;'
 opacity=node(m,unreal.MaterialExpressionCustom,code=code,output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT1)
 inputs=[]
 for label in ['UV','S']:
  i=unreal.CustomInput();i.set_editor_property('input_name',label);inputs.append(i)
 opacity.set_editor_property('inputs',inputs);link(node(m,unreal.MaterialExpressionTextureCoordinate),'',opacity,'UV');link(strength,'',opacity,'S');output(opacity,'',m,unreal.MaterialProperty.MP_OPACITY)
 E.recompile_material(m);L.save_loaded_asset(m,False)
def polygon(x,y,points):
 inside=False;j=len(points)-1
 for i,(ax,ay) in enumerate(points):
  bx,by=points[j]
  if (ay>y)!=(by>y) and x<(bx-ax)*(y-ay)/(by-ay)+ax:inside=not inside
  j=i
 return inside
pixels=bytearray()
for y in range(128):
 for x in range(128):
  head=(x-85)**2+(y-38)**2<23**2 and not ((x-96)**2+(y-24)**2<18**2)
  handle=polygon(x,y,[(19,100),(75,43),(89,57),(33,114)]) and not ((x-29)**2+(y-103)**2<5**2)
  pixels.extend((242,245,247,255 if head or handle else 0))
path=P/'Saved/Generated/T_RepairIcon.tga';path.parent.mkdir(parents=True,exist_ok=True)
path.write_bytes(struct.pack('<BBBHHBHHHHBB',0,0,2,0,0,0,0,0,128,128,32,0x28)+pixels)
t=unreal.AssetImportTask();t.filename=str(path);t.destination_path='/Game/Pinefall/Art/UI';t.destination_name='T_RepairIcon';t.automated=True;t.replace_existing=True;t.save=True;A.import_asset_tasks([t])
unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True);unreal.log('PF_LOOT_FEEDBACK_ASSETS_OK')
