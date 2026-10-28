"""Additive Cedar art/collision pass. Regeneration stays separate from runtime."""
from pathlib import Path
import json,math,struct,unreal
PROJECT=Path(__file__).resolve().parents[1]
LIB=unreal.EditorAssetLibrary;MEL=unreal.MaterialEditingLibrary;TOOLS=unreal.AssetToolsHelpers.get_asset_tools()
def imported(path,folder,name):
    task=unreal.AssetImportTask();task.filename=str(path);task.destination_path=folder;task.destination_name=name
    task.automated=True;task.replace_existing=True;task.save=True;TOOLS.import_asset_tasks([task])
    return task.get_objects()
def texture(path,name,normal=False,linear=False):
    target='/Game/Pinefall/Art/Textures/'+name
    t=unreal.load_asset(target) if LIB.does_asset_exist(target) else next(x for x in imported(path,'/Game/Pinefall/Art/Textures',name) if isinstance(x,unreal.Texture2D))
    t.set_editor_property('max_texture_size',2048);t.set_editor_property('srgb',not(normal or linear))
    if normal:t.set_editor_property('compression_settings',unreal.TextureCompressionSettings.TC_NORMALMAP);t.set_editor_property('flip_green_channel',True)
    LIB.save_loaded_asset(t);return t
def material(name):
    path='/Game/Pinefall/Materials/'+name
    m=unreal.load_asset(path) if LIB.does_asset_exist(path) else TOOLS.create_asset(name,'/Game/Pinefall/Materials',unreal.Material,unreal.MaterialFactoryNew())
    MEL.delete_all_material_expressions(m);return m
def node(m,kind,**values):
    n=MEL.create_material_expression(m,kind)
    for k,v in values.items():n.set_editor_property(k,v)
    return n
def link(a,p,b,q):
    if not MEL.connect_material_expressions(a,p,b,q):raise RuntimeError('Invalid material pin '+q)
def output(a,p,m,q):
    if not MEL.connect_material_property(a,p,q):raise RuntimeError('Invalid output '+str(q))
def stochastic(m,t,uv,normal=False,linear=False):
    sampler=unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL if normal else unreal.MaterialSamplerType.SAMPLERTYPE_LINEAR_COLOR if linear else unreal.MaterialSamplerType.SAMPLERTYPE_COLOR
    obj=node(m,unreal.MaterialExpressionTextureObject,texture=t,sampler_type=sampler)
    inputs=[]
    for name in ['Tex','UV','NormalMap']:
        p=unreal.CustomInput();p.set_editor_property('input_name',name);inputs.append(p)
    result=node(m,unreal.MaterialExpressionCustom,code=(PROJECT/'Scripts/ground_stochastic.hlsl').read_text(),output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,inputs=inputs)
    link(obj,'',result,'Tex');link(uv,'',result,'UV');link(node(m,unreal.MaterialExpressionConstant,r=1 if normal else 0),'',result,'NormalMap');return result
scan=PROJECT/'SourceArt/Terrain/Scans'
m=material('M_Terrain')
p=node(m,unreal.MaterialExpressionWorldPosition);xy=node(m,unreal.MaterialExpressionComponentMask,r=True,g=True,b=False);link(p,'',xy,'')
uv=node(m,unreal.MaterialExpressionDivide,const_b=300);link(xy,'',uv,'A')
n=node(m,unreal.MaterialExpressionVertexNormalWS);up=node(m,unreal.MaterialExpressionComponentMask,r=False,g=False,b=True);link(n,'',up,'')
s=node(m,unreal.MaterialExpressionOneMinus);link(up,'',s,'');weight=node(m,unreal.MaterialExpressionMultiply,const_b=4);link(s,'',weight,'A')
slope=node(m,unreal.MaterialExpressionClamp,min_default=0,max_default=1);link(weight,'',slope,'')
maps=[('diffuse','diff',unreal.MaterialProperty.MP_BASE_COLOR,False,False),('nor_gl','nor_gl',unreal.MaterialProperty.MP_NORMAL,True,False),('rough','rough',unreal.MaterialProperty.MP_ROUGHNESS,False,True)]
for forest_kind,rock_kind,prop,normal,linear in maps:
    a=stochastic(m,texture(scan/f'forest_leaves_02_{forest_kind}_2k.jpg','T_Forest_'+forest_kind,normal,linear),uv,normal,linear)
    rock_texture=texture(scan/f'rocky_terrain_{rock_kind}_2k.jpg','T_Ridge_'+rock_kind,normal,linear)
    if normal:b=stochastic(m,rock_texture,uv,True,False)
    else:
        # Steep slopes need projections from all axes; an XY-only sample stretches
        # a photograph into vertical stripes on a cliff.
        inputs=[]
        for name in ['Tex','Position','Normal']:
            i=unreal.CustomInput();i.set_editor_property('input_name',name);inputs.append(i)
        b=node(m,unreal.MaterialExpressionCustom,output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,inputs=inputs,code='float3 w=pow(abs(Normal),6); w/=max(w.x+w.y+w.z,0.0001); float3 p=Position/350; return Texture2DSample(Tex,TexSampler,p.yz).rgb*w.x+Texture2DSample(Tex,TexSampler,p.xz).rgb*w.y+Texture2DSample(Tex,TexSampler,p.xy).rgb*w.z;')
        obj=node(m,unreal.MaterialExpressionTextureObject,texture=rock_texture,sampler_type=unreal.MaterialSamplerType.SAMPLERTYPE_LINEAR_COLOR if linear else unreal.MaterialSamplerType.SAMPLERTYPE_COLOR)
        link(obj,'',b,'Tex');link(p,'',b,'Position');link(n,'',b,'Normal')
    mix=node(m,unreal.MaterialExpressionLinearInterpolate);link(a,'',mix,'A');link(b,'',mix,'B');link(slope,'',mix,'Alpha')
    if normal:
        fade=node(m,unreal.MaterialExpressionDivide,const_b=18000);link(node(m,unreal.MaterialExpressionPixelDepth),'',fade,'A')
        f=node(m,unreal.MaterialExpressionClamp,min_default=0,max_default=.85);link(fade,'',f,'')
        smooth=node(m,unreal.MaterialExpressionLinearInterpolate);link(mix,'',smooth,'A');link(node(m,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(0,0,1,1)),'',smooth,'B');link(f,'',smooth,'Alpha');mix=smooth
    if linear:
        channel=node(m,unreal.MaterialExpressionComponentMask,r=True,g=False,b=False);link(mix,'',channel,'');mix=channel
    output(mix,'',m,prop)
MEL.recompile_material(m);LIB.save_loaded_asset(m)

# A round lake mesh removes the old rectangular water corners. Shore transparency
# uses actual scene depth, while two small moving normal layers provide ripples.
water=material('M_LakeSurface');water.set_editor_property('blend_mode',unreal.BlendMode.BLEND_TRANSLUCENT)
water.set_editor_property('two_sided',True)
water.set_editor_property('translucency_lighting_mode',unreal.TranslucencyLightingMode.TLM_SURFACE)
output(node(water,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(.018,.085,.095,1)),'',water,unreal.MaterialProperty.MP_BASE_COLOR)
output(node(water,unreal.MaterialExpressionConstant,r=.14),'',water,unreal.MaterialProperty.MP_ROUGHNESS)
output(node(water,unreal.MaterialExpressionConstant,r=.65),'',water,unreal.MaterialProperty.MP_SPECULAR)
fade=node(water,unreal.MaterialExpressionDepthFade,fade_distance_default=120,opacity_default=.78);output(fade,'',water,unreal.MaterialProperty.MP_OPACITY)
wave=unreal.load_asset('/Game/StarterContent/Textures/T_Water_N')
samples=[]
for speed_x,speed_y,scale in [(.008,.003,14),(-.004,.006,23)]:
    uv=node(water,unreal.MaterialExpressionTextureCoordinate,u_tiling=scale,v_tiling=scale)
    pan=node(water,unreal.MaterialExpressionPanner,speed_x=speed_x,speed_y=speed_y);link(uv,'',pan,'Coordinate')
    t=node(water,unreal.MaterialExpressionTextureSample,texture=wave,sampler_type=unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL);link(pan,'',t,'UVs');samples.append(t)
average=node(water,unreal.MaterialExpressionLinearInterpolate,const_alpha=.5);link(samples[0],'RGB',average,'A');link(samples[1],'RGB',average,'B')
gentle=node(water,unreal.MaterialExpressionLinearInterpolate,const_alpha=.32);link(node(water,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(0,0,1,1)),'',gentle,'A');link(average,'',gentle,'B');output(gentle,'',water,unreal.MaterialProperty.MP_NORMAL)
MEL.recompile_material(water);LIB.save_loaded_asset(water)
folder=PROJECT/'SourceArt/Prepared/SM_Lake';folder.mkdir(parents=True,exist_ok=True)
positions=[(0.,0.,0.)]+[(math.cos(i*math.tau/128),0.,math.sin(i*math.tau/128)) for i in range(128)]
normals=[(0.,1.,0.)]*len(positions);uvs=[((x+1)*.5,(z+1)*.5) for x,y,z in positions]
indices=[v for i in range(128) for v in (0,(i+1)%128+1,i+1)]
raw=bytearray();views=[];accessors=[]
for values,kind,count,code,component in [(positions,'VEC3',len(positions),'f',5126),(normals,'VEC3',len(normals),'f',5126),(uvs,'VEC2',len(uvs),'f',5126),(indices,'SCALAR',len(indices),'I',5125)]:
    offset=len(raw);flat=values if kind=='SCALAR' else [v for row in values for v in row];raw.extend(struct.pack('<'+str(len(flat))+code,*flat));views.append({'buffer':0,'byteOffset':offset,'byteLength':len(raw)-offset})
    a={'bufferView':len(views)-1,'componentType':component,'count':count,'type':kind}
    if kind=='VEC3':a.update(min=[min(x[k] for x in values) for k in range(3)],max=[max(x[k] for x in values) for k in range(3)])
    accessors.append(a)
(folder/'SM_Lake.bin').write_bytes(raw)
doc={'asset':{'version':'2.0'},'scene':0,'scenes':[{'nodes':[0]}],'nodes':[{'mesh':0,'name':'SM_Lake'}],'meshes':[{'name':'SM_Lake','primitives':[{'attributes':{'POSITION':0,'NORMAL':1,'TEXCOORD_0':2},'indices':3}]}],'buffers':[{'uri':'SM_Lake.bin','byteLength':len(raw)}],'bufferViews':views,'accessors':accessors}
(folder/'SM_Lake.gltf').write_text(json.dumps(doc),encoding='utf-8')
lake=next(x for x in imported(folder/'SM_Lake.gltf','/Game/Pinefall/Art/World','SM_Lake') if isinstance(x,unreal.StaticMesh));lake.set_material(0,water);LIB.save_loaded_asset(lake)

# Reimport only the corrected assembled pistol, preserving the weapon table path.
pistol=PROJECT/'SourceArt/Prepared/SM_Pistol/SM_Pistol.gltf'
imported(pistol,'/Game/Pinefall/Art/Weapons','SM_Pistol')
unreal.EditorLoadingAndSavingUtils.load_map('/Game/Pinefall/Maps/CedarCrossing')
actors=unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
for actor in actors.get_all_level_actors():
    if isinstance(actor,unreal.PFWaterBody):
        actor.surface.set_static_mesh(lake);actor.surface.set_relative_scale3d(unreal.Vector(42,60,1));actor.surface.set_material(0,water)
    if isinstance(actor,unreal.DirectionalLight):
        light=actor.get_component_by_class(unreal.DirectionalLightComponent);light.set_editor_property('temperature',6500);light.set_editor_property('intensity',35000)
    if isinstance(actor,unreal.SkyLight):actor.get_component_by_class(unreal.SkyLightComponent).set_editor_property('intensity',2.5)
if not unreal.EditorLoadingAndSavingUtils.save_current_level():raise RuntimeError('World presentation save failed')
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True):raise RuntimeError('World asset save failed')
unreal.log('PF_WORLD_UPGRADE_OK')
