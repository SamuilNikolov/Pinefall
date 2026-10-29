"""Build the reserve separately. Cedar remains an intact regression arena."""
from pathlib import Path
import json,math,unreal
PROJECT=Path(__file__).resolve().parents[1];REPO=PROJECT.parents[1]
LIB=unreal.EditorAssetLibrary;MEL=unreal.MaterialEditingLibrary;TOOLS=unreal.AssetToolsHelpers.get_asset_tools()
for folder in ['Art/Reserve','Materials/Reserve']:LIB.make_directory('/Game/Pinefall/'+folder)
def node(m,kind,**values):
 n=MEL.create_material_expression(m,kind)
 for k,v in values.items():n.set_editor_property(k,v)
 return n
def wire(a,p,b,q):
 if not MEL.connect_material_expressions(a,p,b,q):raise RuntimeError('Material '+q)
def output(n,p,m,q):
 if not MEL.connect_material_property(n,p,q):raise RuntimeError('Output '+str(q))
def material(name,folder='/Game/Pinefall/Materials/Reserve'):
 path=folder+'/'+name;m=unreal.load_asset(path) if LIB.does_asset_exist(path) else TOOLS.create_asset(name,folder,unreal.Material,unreal.MaterialFactoryNew());MEL.delete_all_material_expressions(m);return m
def texture(path,name):
 target='/Game/Pinefall/Art/Textures/'+name;t=unreal.load_asset(target)
 if not t:
  task=unreal.AssetImportTask();task.filename=str(path);task.destination_path='/Game/Pinefall/Art/Textures';task.destination_name=name;task.automated=True;task.save=True;TOOLS.import_asset_tasks([task]);t=unreal.load_asset(target)
 if not t:raise RuntimeError('Texture '+str(path))
 t.set_editor_property('srgb',True);t.set_editor_property('max_texture_size',2048);LIB.save_loaded_asset(t);return t
palette={'Paint':(.14,.21,.12),'Metal':(.18,.21,.22),'Rubber':(.025,.029,.032),'Glass':(.09,.18,.21),'Wood':(.30,.24,.15),'Roof':(.12,.16,.16),'Stone':(.38,.36,.32),'Cloth':(.29,.35,.26),'Red':(.65,.035,.025),'White':(.82,.84,.78),'Brass':(.55,.34,.10)}
props={}
for name,color in palette.items():
 m=material('M_Reserve_'+name);base=node(m,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(*color,1))
 if name in ['Wood','Metal','Stone']:
  image={'Wood':'WeatheredWood','Metal':'WeatheredMetal','Stone':'Rock'}[name]
  sample=node(m,unreal.MaterialExpressionTextureSample,texture=texture(REPO/'Assets/Art/Terrain'/(image+'.jpg'),'T_Reserve_'+image))
  uv=node(m,unreal.MaterialExpressionTextureCoordinate,u_tiling=3,v_tiling=3);wire(uv,'',sample,'UVs')
  tint=node(m,unreal.MaterialExpressionMultiply);wire(sample,'RGB',tint,'A');wire(base,'',tint,'B');base=tint
 output(base,'',m,unreal.MaterialProperty.MP_BASE_COLOR)
 output(node(m,unreal.MaterialExpressionConstant,r=.3 if name=='Glass' else .56 if name in ['Metal','Brass'] else .82),'',m,unreal.MaterialProperty.MP_ROUGHNESS)
 output(node(m,unreal.MaterialExpressionConstant,r=.65 if name in ['Metal','Brass'] else 0),'',m,unreal.MaterialProperty.MP_METALLIC)
 MEL.set_material_usage(m,unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES);MEL.recompile_material(m);LIB.save_loaded_asset(m);props[name]=m
meshes={}
for source in sorted((PROJECT/'SourceArt/Prepared/Reserve').glob('*.glb')):
 name=source.stem;path='/Game/Pinefall/Art/Reserve/'+name;mesh=unreal.load_asset(path) if LIB.does_asset_exist(path) else None
 if not mesh:
  task=unreal.AssetImportTask();task.filename=str(source);task.destination_path='/Game/Pinefall/Art/Reserve';task.destination_name=name;task.automated=True;task.save=True;TOOLS.import_asset_tasks([task]);mesh=next((x for x in task.get_objects() if isinstance(x,unreal.StaticMesh)),None)
  if not mesh:
   mesh=next((unreal.load_asset(x) for x in LIB.list_assets(path,recursive=True,include_folder=False) if isinstance(unreal.load_asset(x),unreal.StaticMesh)),None)
  if mesh and mesh.get_path_name().split('.')[0]!=path:
   if not LIB.rename_asset(mesh.get_path_name(),path):raise RuntimeError('Mesh canonical path '+name)
 if not mesh:raise RuntimeError('Mesh import '+name)
 for i,slot in enumerate(mesh.get_editor_property('static_materials')):
  for base,m in props.items():
   if str(slot.material_slot_name).startswith(base):mesh.set_material(i,m);break
 unreal.PFWorldBuilder.configure_collision(mesh,True);LIB.save_loaded_asset(mesh);meshes[name]=mesh
m=material('M_ReserveTerrain','/Game/Pinefall/Materials')
p=node(m,unreal.MaterialExpressionWorldPosition);xy=node(m,unreal.MaterialExpressionComponentMask,r=True,g=True,b=False);wire(p,'',xy,'')
uv=node(m,unreal.MaterialExpressionDivide,const_b=420);wire(xy,'',uv,'A')
def sample(t):
 obj=node(m,unreal.MaterialExpressionTextureObject,texture=t);inputs=[]
 for name in ['Tex','UV','NormalMap']:
  x=unreal.CustomInput();x.set_editor_property('input_name',name);inputs.append(x)
 result=node(m,unreal.MaterialExpressionCustom,code=(PROJECT/'Scripts/ground_stochastic.hlsl').read_text(),output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,inputs=inputs)
 wire(obj,'',result,'Tex');wire(uv,'',result,'UV');wire(node(m,unreal.MaterialExpressionConstant,r=0),'',result,'NormalMap');return result
grounds={}
for name,file in [('Forest','ForestFloor'),('Meadow','MeadowDetail'),('Sand','Sand'),('Snow','Snow'),('Rock','Rock')]:grounds[name]=sample(texture(REPO/'Assets/Art/Terrain'/(file+'.jpg'),'T_Reserve_'+file))
inputs=[]
for name in ['P','N','Forest','Meadow','Sand','Snow','Rock']:
 x=unreal.CustomInput();x.set_editor_property('input_name',name);inputs.append(x)
code='float woods=1-smoothstep(-28000,-13000,P.x);float sand=(1-smoothstep(-40000,-25000,P.x))*(1-smoothstep(-36000,-18000,P.y));float snow=smoothstep(30000,55000,P.x)*smoothstep(18000,40000,P.y);float cliff=saturate((1-N.z)*3.5);float3 c=lerp(Meadow,Forest,woods*.72);c=lerp(c,Sand,sand);c=lerp(c,Snow,snow);c=lerp(c,Rock,cliff*(1-snow*.7));float macro=.91+.09*sin(P.x/1700+sin(P.y/3300))+.045*sin(P.y/7000);return c*macro;'
blend=node(m,unreal.MaterialExpressionCustom,code=code,output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,inputs=inputs)
wire(p,'',blend,'P');wire(node(m,unreal.MaterialExpressionVertexNormalWS),'',blend,'N')
for name,s in grounds.items():wire(s,'',blend,name)
output(blend,'',m,unreal.MaterialProperty.MP_BASE_COLOR);output(node(m,unreal.MaterialExpressionConstant,r=.86),'',m,unreal.MaterialProperty.MP_ROUGHNESS)
MEL.recompile_material(m);LIB.save_loaded_asset(m)
road=material('M_ReserveRoad');output(node(road,unreal.MaterialExpressionConstant3Vector,constant=unreal.LinearColor(.09,.095,.085,1)),'',road,unreal.MaterialProperty.MP_BASE_COLOR);output(node(road,unreal.MaterialExpressionConstant,r=.95),'',road,unreal.MaterialProperty.MP_ROUGHNESS);MEL.set_material_usage(road,unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES);MEL.recompile_material(road);LIB.save_loaded_asset(road)
world=unreal.EditorLoadingAndSavingUtils.new_blank_map(False);actors=unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
unreal.PFWorldBuilder.build_reserve_landscape()
unreal.PFWorldBuilder.populate_reserve([unreal.load_asset('/Game/Pinefall/Art/Foliage/SM_Pine'+x) for x in ['A','B','C']],unreal.load_asset('/Game/Pinefall/Art/Foliage/SM_Grass'),unreal.load_asset('/Game/Pinefall/Art/Foliage/SM_Fern'))
def spawn(kind,label,position=(0,0,0),yaw=0):
 a=actors.spawn_actor_from_class(kind,unreal.Vector(*position),unreal.Rotator(pitch=0,yaw=yaw,roll=0));a.set_actor_label(label);return a
def place(mesh,label,position,yaw=0):
 a=spawn(unreal.StaticMeshActor,label,position,yaw);c=a.static_mesh_component;c.set_static_mesh(mesh);c.set_collision_profile_name('BlockAll');return a
layout=json.loads(unreal.PFWorldBuilder.reserve_layout_json());sites=layout['sites'];loot=[];vehicles=[];boats=[];buildings=[]
height=unreal.PFWorldBuilder.reserve_height;reserve=spawn(unreal.PFReserveWorld,'Reserve gameplay and private map feed')
for index,site in enumerate(sites):
 x,y=site['x'],site['y']
 for side in [-1,1]:
  for row in [-2400,-800,800,2400]:
   px,py=x+side*1800,y+row;z=height(px,py);yaw=-90 if side>0 else 90
   place(meshes['SM_RangerCabin'],site['name']+' cabin',(px,py,z),yaw);buildings.append({'x':px,'y':py,'yaw':yaw})
   loot.extend([unreal.Vector(px,py,z+40),unreal.Vector(px,py+160,z+40),unreal.Vector(px,py-160,z+40)])
   if row== -2400:vehicles.append(unreal.Transform(location=unreal.Vector(x+side*700,y-1800,height(x+side*700,y-1800)+110),rotation=unreal.Rotator(pitch=0,yaw=90,roll=0)))
 loot.extend([unreal.Vector(x+400,y+400,height(x+400,y+400)+35),unreal.Vector(x+400,y+540,height(x+400,y+540)+35)])
for index in range(20):
 x,y=-32000+(index//5)*150,-20400+(index%5)*180;spawn(unreal.PlayerStart,'Reserve arrival %02d'%index,(x,y,height(x,y)+110))
road_count=unreal.PFWorldBuilder.build_reserve_roads();cube=unreal.load_asset('/Engine/BasicShapes/Cube')
water=spawn(unreal.PFWaterBody,'Willow Lake - navigable water',(30000,30000,-1300))
water.get_editor_property('water_bounds').set_box_extent(unreal.Vector(22000,17000,1000));water.surface.set_static_mesh(unreal.load_asset('/Game/Pinefall/Art/World/SM_Lake'));water.surface.set_relative_scale3d(unreal.Vector(220,170,1));water.surface.set_material(0,unreal.load_asset('/Game/Pinefall/Materials/M_LakeSurface'))
for angle in [math.pi/2,math.pi,-math.pi/4]:
 r=1.0
 while r>.60:
  x,y=30000+math.cos(angle)*22000*r,30000+math.sin(angle)*17000*r
  if height(x,y)<-450:break
  r-=.01
 boats.append(unreal.Transform(location=unreal.Vector(x,y,-265),rotation=unreal.Rotator(pitch=0,yaw=math.degrees(angle)+180,roll=0)))
 sx,sy=30000+math.cos(angle)*22000*1.10,30000+math.sin(angle)*17000*1.10
 length=math.hypot(sx-x,sy-y);dock=place(cube,'Lake launch pier',((sx+x)*.5,(sy+y)*.5,-185),math.degrees(math.atan2(sy-y,sx-x)))
 dock.set_actor_scale3d(unreal.Vector(length/100,2.4,.18));dock.static_mesh_component.set_material(0,props['Wood'])
reserve.set_editor_property('loot_points',loot);reserve.set_editor_property('vehicle_starts',vehicles);reserve.set_editor_property('boat_starts',boats)
spawn(unreal.SkyAtmosphere,'Reserve physical sky')
sun=spawn(unreal.DirectionalLight,'Reserve sunlight').get_component_by_class(unreal.DirectionalLightComponent);sun.set_world_rotation(unreal.Rotator(pitch=-38,yaw=-35,roll=0),False,True);sun.set_mobility(unreal.ComponentMobility.MOVABLE);sun.set_editor_property('intensity',35000);sun.set_editor_property('atmosphere_sun_light',True)
sky=spawn(unreal.SkyLight,'Reserve sky fill').get_component_by_class(unreal.SkyLightComponent);sky.set_mobility(unreal.ComponentMobility.MOVABLE);sky.set_editor_property('real_time_capture',True);sky.set_editor_property('intensity',2.5)
fog=spawn(unreal.ExponentialHeightFog,'Reserve distance haze').get_component_by_class(unreal.ExponentialHeightFogComponent);fog.set_editor_property('fog_density',.008);fog.set_editor_property('fog_height_falloff',.18)
post=spawn(unreal.PostProcessVolume,'Reserve exposure');post.set_editor_property('unbound',True);settings=post.get_editor_property('settings')
for name,value in [('auto_exposure_min_brightness',9),('auto_exposure_max_brightness',14),('auto_exposure_bias',.35),('bloom_intensity',.18),('motion_blur_amount',0)]:settings.set_editor_property('override_'+name,True);settings.set_editor_property(name,value)
post.set_editor_property('settings',settings)
if not unreal.EditorLoadingAndSavingUtils.save_map(world,'/Game/Pinefall/Maps/WildfrontReserve'):raise RuntimeError('Reserve map save failed')
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True,True):raise RuntimeError('Reserve asset save failed')
layout.update({'span_m':2016,'buildings':buildings,'loot_points':len(loot),'trucks':len(vehicles),'boats':len(boats),'road_segments':road_count,'height_size':505})
layout['mesh_bounds']={name:{'min':[mesh.get_bounding_box().min.x,mesh.get_bounding_box().min.y,mesh.get_bounding_box().min.z],'max':[mesh.get_bounding_box().max.x,mesh.get_bounding_box().max.y,mesh.get_bounding_box().max.z]} for name,mesh in meshes.items()}
layout['trees']=[]
for actor in actors.get_all_level_actors():
 if actor.get_actor_label().startswith('Reserve scanned pines'):
  for group in actor.get_components_by_class(unreal.HierarchicalInstancedStaticMeshComponent):
   for i in range(group.get_instance_count()):
    t=group.get_instance_transform(i,True);v=t.translation;layout['trees'].append([v.x,v.y])
layout['heights']=[height(-100800+x*400,-100800+y*400) for y in range(505) for x in range(505)]
(PROJECT/'Saved/ReserveBuild.json').write_text(json.dumps(layout),encoding='utf-8')
unreal.log('PF_RESERVE_BUILD_OK sites='+str(len(sites))+' buildings='+str(len(buildings)))
