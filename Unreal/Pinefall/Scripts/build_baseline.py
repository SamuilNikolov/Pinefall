"""Explicit editor-only generator for Cedar Crossing; never runs in the shipped game.

Run through Setup-Unreal.ps1. Regeneration is intentional and replaces the generated map.
"""
from pathlib import Path
import json,unreal

PROJECT=Path(__file__).resolve().parents[1]
REPO=PROJECT.parents[1]
ART=PROJECT/'SourceArt'
TOOLS=unreal.AssetToolsHelpers.get_asset_tools()
LIB=unreal.EditorAssetLibrary
MEL=unreal.MaterialEditingLibrary
if 'PFRebuildBaseline' not in unreal.SystemLibrary.get_command_line():
    raise RuntimeError('This generator requires the explicit -PFRebuildBaseline switch.')
REUSE='PFReuseImports' in unreal.SystemLibrary.get_command_line()

def imported(source,folder,name=None):
    task=unreal.AssetImportTask();task.filename=str(source);task.destination_path=folder
    if name:task.destination_name=name
    task.automated=True;task.replace_existing=True;task.save=True
    TOOLS.import_asset_tasks([task])
    objects=task.get_objects()
    if not objects:raise RuntimeError('No import result: '+str(source))
    return objects

def texture(source,name=None,normal=False,linear=False):
    name=name or Path(source).stem
    objects=imported(source,'/Game/Pinefall/Art/Textures',name)
    tex=next(o for o in objects if isinstance(o,unreal.Texture2D))
    tex.set_editor_property('max_texture_size',2048)
    if normal:
        tex.set_editor_property('compression_settings',unreal.TextureCompressionSettings.TC_NORMALMAP)
        tex.set_editor_property('flip_green_channel',True)
    tex.set_editor_property('srgb',not(normal or linear));LIB.save_loaded_asset(tex)
    return tex

def material(name):
    path='/Game/Pinefall/Materials/'+name
    result=unreal.load_asset(path) if LIB.does_asset_exist(path) else TOOLS.create_asset(name,'/Game/Pinefall/Materials',unreal.Material,unreal.MaterialFactoryNew())
    MEL.delete_all_material_expressions(result)
    return result

def node(mat,kind,**properties):
    result=MEL.create_material_expression(mat,kind)
    for key,value in properties.items():result.set_editor_property(key,value)
    return result
def link(a,output,b,input_name):
    # Unreal exposes unnamed unary pins as an empty string, despite the UI's Input label.
    pin='' if input_name=='Input' else input_name
    if not MEL.connect_material_expressions(a,output,b,pin):raise RuntimeError('Invalid material connection: '+input_name)
def surface(a,output,mat,prop):
    if not MEL.connect_material_property(a,output,prop):raise RuntimeError('Invalid material output: '+str(prop))
def sample(mat,tex,normal=False,linear=False,uv=None):
    sampler=unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL if normal else unreal.MaterialSamplerType.SAMPLERTYPE_LINEAR_COLOR if linear else unreal.MaterialSamplerType.SAMPLERTYPE_COLOR
    result=node(mat,unreal.MaterialExpressionTextureSample,texture=tex,sampler_type=sampler)
    if uv:link(uv,'',result,'UVs')
    return result

def stochastic(mat,tex,uv,normal=False,linear=False):
    sampler=unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL if normal else unreal.MaterialSamplerType.SAMPLERTYPE_LINEAR_COLOR if linear else unreal.MaterialSamplerType.SAMPLERTYPE_COLOR
    obj=node(mat,unreal.MaterialExpressionTextureObject,texture=tex,sampler_type=sampler)
    inputs=[]
    for name in ['Tex','UV','NormalMap']:
        entry=unreal.CustomInput();entry.set_editor_property('input_name',name);inputs.append(entry)
    result=node(mat,unreal.MaterialExpressionCustom,
        code=(PROJECT/'Scripts/ground_stochastic.hlsl').read_text(encoding='utf-8'),
        output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,
        inputs=inputs)
    flag=node(mat,unreal.MaterialExpressionConstant,r=1 if normal else 0)
    link(obj,'',result,'Tex');link(uv,'',result,'UV');link(flag,'',result,'NormalMap')
    return result

def pbr(name,color,normal=None,arm=None,alpha=None):
    mat=material(name)
    c=sample(mat,color);surface(c,'RGB',mat,unreal.MaterialProperty.MP_BASE_COLOR)
    if normal:surface(sample(mat,normal,True),'RGB',mat,unreal.MaterialProperty.MP_NORMAL)
    if arm:
        a=sample(mat,arm,linear=True);surface(a,'R',mat,unreal.MaterialProperty.MP_AMBIENT_OCCLUSION);surface(a,'G',mat,unreal.MaterialProperty.MP_ROUGHNESS)
    else:surface(node(mat,unreal.MaterialExpressionConstant,r=.85),'',mat,unreal.MaterialProperty.MP_ROUGHNESS)
    if alpha:
        mat.set_editor_property('blend_mode',unreal.BlendMode.BLEND_MASKED);mat.set_editor_property('two_sided',True)
        mat.set_editor_property('shading_model',unreal.MaterialShadingModel.MSM_TWO_SIDED_FOLIAGE)
        surface(sample(mat,alpha,linear=True),'R',mat,unreal.MaterialProperty.MP_OPACITY_MASK)
        tint=node(mat,unreal.MaterialExpressionMultiply,const_b=.28);link(c,'RGB',tint,'A');surface(tint,'',mat,unreal.MaterialProperty.MP_SUBSURFACE_COLOR)
    MEL.recompile_material(mat);LIB.save_loaded_asset(mat);return mat

for folder in ['Art','Art/Textures','Art/Weapons','Art/Foliage','Materials','Audio','Data','Maps']:LIB.make_directory('/Game/Pinefall/'+folder)
meshes={}
for name in ['SM_Rifle','SM_Pistol','SM_PineA','SM_PineB','SM_PineC','SM_Grass','SM_Fern']:
    folder='/Game/Pinefall/Art/'+('Weapons' if name in ['SM_Rifle','SM_Pistol'] else 'Foliage')
    if REUSE and not ('PFRefreshTrees' in unreal.SystemLibrary.get_command_line() and name.startswith('SM_Pine')) and LIB.does_asset_exist(folder+'/'+name):mesh=unreal.load_asset(folder+'/'+name)
    else:mesh=next(o for o in imported(ART/'Prepared'/name/(name+'.gltf'),folder,name) if isinstance(o,unreal.StaticMesh))
    meshes[name]=mesh
    expected=folder+'/'+name
    if mesh.get_path_name().split('.')[0]!=expected:
        if LIB.does_asset_exist(expected):raise RuntimeError('Unexpected duplicate model: '+expected)
        LIB.rename_asset(mesh.get_path_name(),expected)
    unreal.PFWorldBuilder.configure_mesh(mesh,name.startswith('SM_Pine'))
    LIB.save_loaded_asset(mesh)
    unreal.log('PF_IMPORTED '+name+' '+mesh.get_path_name())

tree=ART/'pine_sapling_small/textures'
leaf=pbr('M_PineNeedles',texture(tree/'pine_sapling_small_twig_diff_1k.jpg'),texture(tree/'pine_sapling_small_twig_nor_gl_1k.jpg',normal=True),texture(tree/'pine_sapling_small_twig_arm_1k.jpg',linear=True),texture(tree/'pine_sapling_small_twig_alpha_1k.png',linear=True))
bark=pbr('M_PineBark',texture(tree/'pine_sapling_small_bark_diff_1k.jpg'),texture(tree/'pine_sapling_small_bark_nor_gl_1k.jpg',normal=True),texture(tree/'pine_sapling_small_bark_arm_1k.jpg',linear=True))
for name in ['SM_PineA','SM_PineB','SM_PineC']:
    meshes[name].set_material(0,bark);meshes[name].set_material(1,leaf);LIB.save_loaded_asset(meshes[name])
for mat in [leaf,bark]:
    MEL.set_material_usage(mat,unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES)
    MEL.set_material_usage(mat,unreal.MaterialUsage.MATUSAGE_NANITE)
    LIB.save_loaded_asset(mat)
grass=ART/'Prepared/SM_Grass/textures'
grass_mat=pbr('M_LowGrass',texture(grass/'grass_medium_01_diff_1k.jpg'),texture(grass/'grass_medium_01_nor_gl_1k.jpg',normal=True),texture(grass/'grass_medium_01_arm_1k.jpg',linear=True),texture(ART/'Terrain/GrassAlpha.jpg',linear=True))
meshes['SM_Grass'].set_material(0,grass_mat);LIB.save_loaded_asset(meshes['SM_Grass'])
MEL.set_material_usage(grass_mat,unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES);LIB.save_loaded_asset(grass_mat)
for slot in meshes['SM_Fern'].get_editor_property('static_materials'):
    base=slot.material_interface.get_base_material()
    MEL.set_material_usage(base,unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES);LIB.save_loaded_asset(base)

# Native Landscape uses world-space metre-scale scans and slope-aware stone.
terrain=material('M_Terrain')
position=node(terrain,unreal.MaterialExpressionWorldPosition)
mask=node(terrain,unreal.MaterialExpressionComponentMask,r=True,g=True,b=False,a=False);link(position,'',mask,'Input')
uv=node(terrain,unreal.MaterialExpressionDivide,const_b=320);link(mask,'',uv,'A')
meadow_texture=texture(ART/'Terrain/MeadowDetail.jpg')
detail=stochastic(terrain,meadow_texture,uv)
# Broad 86 m color patches complement stochastic close detail.
macro_uv=node(terrain,unreal.MaterialExpressionMultiply,const_b=.037);link(uv,'',macro_uv,'A')
macro=sample(terrain,meadow_texture,uv=macro_uv)
palette=node(terrain,unreal.MaterialExpressionLinearInterpolate)
green=node(terrain,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(.40,.66,.34,1))
dry=node(terrain,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(.85,.91,.64,1))
link(green,'',palette,'A');link(dry,'',palette,'B');link(macro,'R',palette,'Alpha')
natural=node(terrain,unreal.MaterialExpressionMultiply);link(detail,'',natural,'A');link(palette,'',natural,'B')
far_color=node(terrain,unreal.MaterialExpressionMultiply);link(macro,'RGB',far_color,'A');link(palette,'',far_color,'B')
depth=node(terrain,unreal.MaterialExpressionPixelDepth)
fade_start=node(terrain,unreal.MaterialExpressionSubtract,const_b=1800);link(depth,'',fade_start,'A')
fade_range=node(terrain,unreal.MaterialExpressionDivide,const_b=7000);link(fade_start,'',fade_range,'A')
distance_fade=node(terrain,unreal.MaterialExpressionClamp,min_default=0,max_default=1);link(fade_range,'',distance_fade,'Input')
ground=node(terrain,unreal.MaterialExpressionLinearInterpolate);link(natural,'',ground,'A');link(far_color,'',ground,'B');link(distance_fade,'',ground,'Alpha')
rock=stochastic(terrain,texture(ART/'Terrain/Rock.jpg'),uv)
normal=node(terrain,unreal.MaterialExpressionVertexNormalWS)
up=node(terrain,unreal.MaterialExpressionComponentMask,r=False,g=False,b=True,a=False);link(normal,'',up,'Input')
slope=node(terrain,unreal.MaterialExpressionOneMinus);link(up,'',slope,'Input')
weight=node(terrain,unreal.MaterialExpressionMultiply,const_b=3);link(slope,'',weight,'A')
clamp=node(terrain,unreal.MaterialExpressionClamp,min_default=0,max_default=1);link(weight,'',clamp,'Input')
mix=node(terrain,unreal.MaterialExpressionLinearInterpolate);link(ground,'',mix,'A');link(rock,'',mix,'B');link(clamp,'',mix,'Alpha');surface(mix,'',terrain,unreal.MaterialProperty.MP_BASE_COLOR)
meadow_normal=stochastic(terrain,texture(ART/'Terrain/MeadowDetailNormal.jpg',normal=True),uv,normal=True)
rock_normal=stochastic(terrain,texture(ART/'Terrain/RockNormal.jpg',normal=True),uv,normal=True)
nmix=node(terrain,unreal.MaterialExpressionLinearInterpolate);link(meadow_normal,'',nmix,'A');link(rock_normal,'',nmix,'B');link(clamp,'',nmix,'Alpha')
flat=node(terrain,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(0,0,1,1))
distance_normal=node(terrain,unreal.MaterialExpressionLinearInterpolate);link(nmix,'',distance_normal,'A');link(flat,'',distance_normal,'B');link(distance_fade,'',distance_normal,'Alpha');surface(distance_normal,'',terrain,unreal.MaterialProperty.MP_NORMAL)
arm=stochastic(terrain,texture(ART/'Terrain/MeadowDetailArm.jpg',linear=True),uv,linear=True)
rough=node(terrain,unreal.MaterialExpressionComponentMask,r=False,g=True);link(arm,'',rough,'Input');surface(rough,'',terrain,unreal.MaterialProperty.MP_ROUGHNESS)
ao=node(terrain,unreal.MaterialExpressionComponentMask,r=True,g=False);link(arm,'',ao,'Input');surface(ao,'',terrain,unreal.MaterialProperty.MP_AMBIENT_OCCLUSION)
MEL.recompile_material(terrain);LIB.save_loaded_asset(terrain)

sounds={}
for name in ['Rifle','Pistol','Step','Reload','Birds']:
    path='/Game/Pinefall/Audio/'+name
    sounds[name]=unreal.load_asset(path) if REUSE and LIB.does_asset_exist(path) else next(o for o in imported(ART/'Audio'/(name+'.wav'),'/Game/Pinefall/Audio',name) if isinstance(o,unreal.SoundWave))
    LIB.save_loaded_asset(sounds[name])
balance=json.loads((REPO/'Design/Legacy/balance.json').read_text(encoding='utf-8'))
data=[]
for weapon in balance['weapons']:
    rid=weapon['name'].replace(' ','').replace('-','')
    row={'Name':rid,'DisplayName':weapon['name'],'Category':weapon['category'],'bAutomatic':weapon['mode']=='Automatic','Magazine':weapon['magazine'],'Damage':weapon['playerDamage'],'Interval':weapon['interval'],'ReloadSeconds':weapon['reload'],'RangeCm':weapon['range']*100,'AimFOV':weapon['zoom']}
    if rid in ['Ridgeline','ServiceNine']:
        row['Model']=meshes['SM_Rifle' if rid=='Ridgeline' else 'SM_Pistol'].get_path_name();row['ShotSound']=sounds['Rifle' if rid=='Ridgeline' else 'Pistol'].get_path_name()
    data.append(row)
table_path='/Game/Pinefall/Data/DT_Weapons'
if LIB.does_asset_exist(table_path):table=unreal.load_asset(table_path)
else:
    factory=unreal.DataTableFactory();factory.struct=unreal.load_object(None,'/Script/Pinefall.PFWeaponDefinition');table=TOOLS.create_asset('DT_Weapons','/Game/Pinefall/Data',unreal.DataTable,factory)
if not unreal.DataTableFunctionLibrary.fill_data_table_from_json_string(table,json.dumps(data)):raise RuntimeError('Weapon data import failed')
LIB.save_loaded_asset(table)
unreal.log('PF_WEAPON_TABLE '+unreal.DataTableFunctionLibrary.export_data_table_to_json_string(table))

level=unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
LIB.make_directory('/Game/Pinefall/Art/World')
for source,name,complex_collision in [('/Engine/BasicShapes/Cylinder','SM_TrunkCollision',False),('/Game/StarterContent/Props/SM_Rock','SM_Limestone',True)]:
    path='/Game/Pinefall/Art/World/'+name
    mesh=unreal.load_asset(path) if LIB.does_asset_exist(path) else LIB.duplicate_asset(source,path)
    if not mesh:raise RuntimeError('Missing collision mesh '+name)
    unreal.PFWorldBuilder.configure_collision(mesh,complex_collision);LIB.save_loaded_asset(mesh)
# Build an untitled world and write the named map only after successful generation.
# This also avoids interactive checkout prompts in commandlet save_current_level.
world=unreal.EditorLoadingAndSavingUtils.new_blank_map(False)
if not world:raise RuntimeError('Could not create range world')
actors=unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
unreal.PFWorldBuilder.build_landscape()
unreal.PFWorldBuilder.populate_foliage([meshes['SM_PineA'],meshes['SM_PineB'],meshes['SM_PineC']],meshes['SM_Grass'],meshes['SM_Fern'])

def spawn(kind,label,position=(0,0,0),rotation=(0,0,0)):
    actor=actors.spawn_actor_from_class(kind,unreal.Vector(*position),unreal.Rotator(pitch=rotation[0],yaw=rotation[1],roll=rotation[2]));actor.set_actor_label(label);return actor
spawn(unreal.SkyAtmosphere,'Physical sky')
sun=spawn(unreal.DirectionalLight,'Afternoon sun',rotation=(-24,-38,0)).get_component_by_class(unreal.DirectionalLightComponent)
sun.set_mobility(unreal.ComponentMobility.MOVABLE);sun.set_editor_property('intensity',50000);sun.set_editor_property('atmosphere_sun_light',True);sun.set_editor_property('use_temperature',True);sun.set_editor_property('temperature',5400)
sky=spawn(unreal.SkyLight,'Dynamic sky fill').get_component_by_class(unreal.SkyLightComponent)
sky.set_mobility(unreal.ComponentMobility.MOVABLE);sky.set_editor_property('real_time_capture',True);sky.set_editor_property('intensity',1)
fog=spawn(unreal.ExponentialHeightFog,'Distance atmosphere').get_component_by_class(unreal.ExponentialHeightFogComponent)
fog.set_editor_property('fog_density',.012);fog.set_editor_property('fog_height_falloff',.25);fog.set_editor_property('enable_volumetric_fog',True)
post=spawn(unreal.PostProcessVolume,'Gameplay exposure and lens')
post.set_editor_property('unbound',True)
settings=post.get_editor_property('settings')
for name,value in [('auto_exposure_min_brightness',9),('auto_exposure_max_brightness',15),('auto_exposure_bias',.25),('bloom_intensity',.25),('vignette_intensity',.2),('motion_blur_amount',0)]:
    settings.set_editor_property('override_'+name,True);settings.set_editor_property(name,value)
post.set_editor_property('settings',settings)
water=spawn(unreal.PFWaterBody,'Cedar Lake - swimming water',(9000,9500,-1120))

# Large shoreline stones and range architecture are authored cover, not repeated huts.
import random
rng=random.Random(7319)
rock_mesh=unreal.load_asset('/Game/Pinefall/Art/World/SM_Limestone')
for index in range(100):
    x,y=rng.uniform(-24000,24000),rng.uniform(-24000,24000)
    if abs(y)<2000 and -11000<x<5000:continue
    z=unreal.PFWorldBuilder.height_at(x,y)
    if z< -200:continue
    scale=rng.uniform(1.5,4.5);box=rock_mesh.get_bounding_box()
    base=z-box.min.z*scale*.8-(box.max.z-box.min.z)*scale*.8*.3
    actor=spawn(unreal.StaticMeshActor,'Limestone outcrop %03d'%index,(x,y,base),(0,rng.uniform(0,360),0));actor.static_mesh_component.set_static_mesh(rock_mesh);actor.set_actor_scale3d(unreal.Vector(scale,scale*.85,scale*.8))
    actor.static_mesh_component.set_collision_profile_name('BlockAll');actor.static_mesh_component.set_collision_enabled(unreal.CollisionEnabled.QUERY_AND_PHYSICS)
if not unreal.EditorLoadingAndSavingUtils.save_map(world,'/Game/Pinefall/Maps/CedarCrossing'):
    raise RuntimeError('Generated world could not be saved')
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True):
    raise RuntimeError('Generated packages could not be saved')
report={'map':'/Game/Pinefall/Maps/CedarCrossing','landscapeSpanMetres':504,'weaponDataRows':len(data),'enabledWeapons':['Ridgeline','ServiceNine'],'meshes':{name:asset.get_path_name() for name,asset in meshes.items()},'limits':['Range loadout has unlimited reserve','Full BR/rules, vehicles, animals, inventory/building, voice and advanced animation are later milestones','Water is currently an environment surface; swimming/boats need a gameplay-water milestone']}
destination=PROJECT/'Saved/BaselineBuild.json';destination.parent.mkdir(exist_ok=True);destination.write_text(json.dumps(report,indent=2),encoding='utf-8')
unreal.log('PF_BASELINE_ASSETS_OK '+str(destination))
