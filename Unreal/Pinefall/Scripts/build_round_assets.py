"""Small additive editor pass: supply/zone materials, inventory thumbnails and an accurate Cedar atlas."""
from pathlib import Path
import math,struct,unreal
PROJECT=Path(__file__).resolve().parents[1]
REPO=PROJECT.parents[1]
LIB=unreal.EditorAssetLibrary
MEL=unreal.MaterialEditingLibrary
TOOLS=unreal.AssetToolsHelpers.get_asset_tools()
for folder in ['Materials','Art','Art/UI']:LIB.make_directory('/Game/Pinefall/'+folder)
def mat(name):
    path='/Game/Pinefall/Materials/'+name
    asset=unreal.load_asset(path) if LIB.does_asset_exist(path) else TOOLS.create_asset(name,'/Game/Pinefall/Materials',unreal.Material,unreal.MaterialFactoryNew())
    MEL.delete_all_material_expressions(asset);return asset
def node(material,kind,**values):
    result=MEL.create_material_expression(material,kind)
    for key,value in values.items():result.set_editor_property(key,value)
    return result
def wire(a,out,b,pin):
    if not MEL.connect_material_expressions(a,out,b,pin):raise RuntimeError('Material input: '+pin)
def output(a,out,m,prop):
    if not MEL.connect_material_property(a,out,prop):raise RuntimeError('Material output: '+str(prop))
material=mat('M_Supply')
tint=node(material,unreal.MaterialExpressionVectorParameter,parameter_name='Tint',default_value=unreal.LinearColor(.2,.3,.25,1))
output(tint,'',material,unreal.MaterialProperty.MP_BASE_COLOR)
output(node(material,unreal.MaterialExpressionConstant,r=.6),'',material,unreal.MaterialProperty.MP_ROUGHNESS)
MEL.recompile_material(material);LIB.save_loaded_asset(material)
rage=mat('M_WildRage')
rage.set_editor_property('blend_mode',unreal.BlendMode.BLEND_TRANSLUCENT)
output(node(rage,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(.5,.08,.015,1)),'',rage,unreal.MaterialProperty.MP_BASE_COLOR)
rim=node(rage,unreal.MaterialExpressionFresnel,exponent=3.0,base_reflect_fraction=.03)
strength=node(rage,unreal.MaterialExpressionMultiply,const_b=.22);wire(rim,'',strength,'A')
output(strength,'',rage,unreal.MaterialProperty.MP_OPACITY)
MEL.recompile_material(rage);LIB.save_loaded_asset(rage)
material=mat('M_StormSurface')
material.set_editor_property('blend_mode',unreal.BlendMode.BLEND_TRANSLUCENT)
material.set_editor_property('shading_model',unreal.MaterialShadingModel.MSM_UNLIT)
material.set_editor_property('two_sided',True)
output(node(material,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(.13,.3,.9,1)),'',material,unreal.MaterialProperty.MP_EMISSIVE_COLOR)
normal=node(material,unreal.MaterialExpressionVertexNormalWS)
z=node(material,unreal.MaterialExpressionComponentMask,r=False,g=False,b=True);wire(normal,'',z,'')
absolute=node(material,unreal.MaterialExpressionAbs);wire(z,'',absolute,'')
side=node(material,unreal.MaterialExpressionOneMinus);wire(absolute,'',side,'')
opacity=node(material,unreal.MaterialExpressionMultiply,const_b=.35);wire(side,'',opacity,'A');output(opacity,'',material,unreal.MaterialProperty.MP_OPACITY)
MEL.recompile_material(material);LIB.save_loaded_asset(material)
def import_texture(path,name,folder):
    task=unreal.AssetImportTask();task.filename=str(path);task.destination_path=folder;task.destination_name=name;task.automated=True;task.replace_existing=True;task.save=True
    TOOLS.import_asset_tasks([task]);objects=task.get_objects()
    asset=next((x for x in objects if isinstance(x,unreal.Texture2D)),None)
    if not asset:raise RuntimeError('Texture import failed: '+str(path))
    asset.set_editor_property('max_texture_size',1024);LIB.save_loaded_asset(asset);return asset
for index,name in [(0,'T_RifleIcon'),(3,'T_PistolIcon')]:import_texture(REPO/'Assets/Resources/WeaponIcons'/(str(index)+'.png'),name,'/Game/Pinefall/Art/UI')
# Original compact pictograms. TGA keeps this editor tool dependency-free.
generated=PROJECT/'Saved/Generated';generated.mkdir(parents=True,exist_ok=True)
def rectangle(x,y,a,b,c,d):return a<=x<=c and b<=y<=d
def polygon(x,y,points):
    inside=False;j=len(points)-1
    for i,(ax,ay) in enumerate(points):
        bx,by=points[j]
        if (ay>y)!=(by>y) and x<(bx-ax)*(y-ay)/(by-ay)+ax:inside=not inside
        j=i
    return inside
def icon(name,shape):
    pixels=bytearray()
    for y in range(128):
        for x in range(128):pixels.extend((235,242,233,255 if shape(x,y) else 0))
    path=generated/(name+'.tga');path.write_bytes(struct.pack('<BBBHHBHHHHBB',0,0,2,0,0,0,0,0,128,128,32,0x28)+pixels);import_texture(path,name,'/Game/Pinefall/Art/UI')
icon('T_Ammo',lambda x,y:(rectangle(x,y,30,42,51,104) or polygon(x,y,[(30,42),(40,20),(51,42)]) or rectangle(x,y,73,42,94,104) or polygon(x,y,[(73,42),(84,20),(94,42)])) and not rectangle(x,y,25,88,100,93))
icon('T_Food',lambda x,y:rectangle(x,y,37,30,91,103) and not rectangle(x,y,34,43,94,47) and not rectangle(x,y,34,85,94,89))
icon('T_Medkit',lambda x,y:(rectangle(x,y,26,43,102,103) or (rectangle(x,y,48,26,80,47) and not rectangle(x,y,54,32,74,42))) and not (rectangle(x,y,59,57,69,91) or rectangle(x,y,47,69,81,79)))
icon('T_Plate',lambda x,y:polygon(x,y,[(42,22),(86,22),(103,47),(96,105),(32,105),(25,47)]))
icon('T_Vest',lambda x,y:polygon(x,y,[(38,23),(51,23),(56,39),(72,39),(77,23),(90,23),(105,52),(92,64),(92,106),(36,106),(36,64),(23,52)]) and not rectangle(x,y,61,49,67,107))
# Orthographic map uses the same editor height function as the actual landscape,
# sampled once during generation, never by a runtime UI update.
size=512;pixels=bytearray()
for y in range(size):
    x_world=(.5-y/(size-1))*48000
    for x in range(size):
        y_world=(x/(size-1)-.5)*48000
        height=unreal.PFWorldBuilder.height_at(x_world,y_world)
        dx=unreal.PFWorldBuilder.height_at(x_world+100,y_world)-height
        dy=unreal.PFWorldBuilder.height_at(x_world,y_world+100)-height
        lake=((x_world-9000)/4200)**2+((y_world-9500)/6000)**2
        if lake<1.03 and height< -120:color=(34,79,101)
        else:
            slope=min(1,math.hypot(dx,dy)/120);shade=max(.55,min(1.3,1+(dx-dy)*.004))
            color=tuple(int(min(255,max(0,(a*(1-slope)+b*slope)*shade))) for a,b in zip((78,112,84),(140,139,122)))
            if height>400 and (height%700)<32:color=tuple(int(v*.75) for v in color)
        pixels.extend((color[2],color[1],color[0],255))
folder=PROJECT/'Saved/Generated';folder.mkdir(parents=True,exist_ok=True)
path=folder/'CedarAtlas.tga';header=struct.pack('<BBBHHBHHHHBB',0,0,2,0,0,0,0,0,size,size,32,0x28);path.write_bytes(header+pixels)
import_texture(path,'T_CedarAtlas','/Game/Pinefall/Art')
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True):raise RuntimeError('Could not save round assets')
unreal.log('PF_ROUND_ASSETS_OK')
