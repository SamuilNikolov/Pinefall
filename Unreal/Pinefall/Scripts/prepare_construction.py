"""Original tool and open storm shell geometry; Blender, metres, MIT."""
from pathlib import Path
import bpy, math
PROJECT=Path(__file__).resolve().parents[1]
OUT=PROJECT/'SourceArt/Prepared/Construction';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
materials={}
for name,color in [('Wood',(.26,.12,.055,1)),('Metal',(.22,.25,.27,1)),('Storm',(.2,.1,.7,1))]:
 m=bpy.data.materials.new(name);m.diffuse_color=color;materials[name]=m

def box(name,location,scale,material,bevel=.02):
 bpy.ops.mesh.primitive_cube_add(size=1,location=location);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.data.materials.append(materials[material]);mod=o.modifiers.new('Soft forged edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 for p in o.data.polygons:p.use_smooth=True
 return o
box('Hickory shaft',(.26,0,0),(.95,.045,.045),'Wood',.012)
box('Grip',(-.10,0,0),(.22,.054,.054),'Wood',.014)
head=box('Forged head',(.68,0,.035),(.095,.5,.085),'Metal',.018)
box('Head collar',(.65,0,0),(.13,.095,.12),'Metal',.012)
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=head;bpy.ops.object.join();head.name='SM_FieldPickaxe'
bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.export_scene.gltf(filepath=str(OUT/'SM_FieldPickaxe.glb'),export_format='GLB',use_selection=True,export_apply=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
vertices=[];faces=[]
for i in range(257):
 a=i/256*math.tau
 vertices.extend([(math.cos(a),math.sin(a),0),(math.cos(a),math.sin(a),1)])
 if i<256:faces.append((i*2,i*2+2,i*2+3,i*2+1))
mesh=bpy.data.meshes.new('Open shell');mesh.from_pydata(vertices,[],faces);mesh.update()
o=bpy.data.objects.new('SM_StormWall',mesh);bpy.context.collection.objects.link(o)
uv=mesh.uv_layers.new(name='Boundary')
for poly in mesh.polygons:
 for li in poly.loop_indices:
  v=mesh.loops[li].vertex_index;uv.data[li].uv=(v//2/256,v%2)
 poly.use_smooth=True
o.select_set(True);bpy.context.view_layer.objects.active=o
bpy.ops.export_scene.gltf(filepath=str(OUT/'SM_StormWall.glb'),export_format='GLB',use_selection=True,export_apply=True)
print('PF_CONSTRUCTION_GEOMETRY_OK')

for name,kind in [('SM_WoodBundle','Wood'),('SM_StoneStack','Stone'),('SM_MetalBundle','Metal')]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 materials.clear()
 for label,color in [('Wood',(.26,.12,.055,1)),('Metal',(.22,.25,.27,1)),('Stone',(.34,.29,.25,1))]:
  m=bpy.data.materials.new(label);m.diffuse_color=color;materials[label]=m
 for i in range(3):
  if kind=='Wood':box('Bundled split timber',(0,(i-1)*.09,.055+(i%2)*.065),(.48,.075,.075),kind,.018)
  elif kind=='Stone':box('Cut masonry',(0,(i-1)*.11,.05+(i%2)*.08),(.28,.10,.09),kind,.012)
  else:
   box('Salvage web',(0,(i-1)*.10,.065),(.40,.025,.11),kind,.004)
   for z in [.015,.115]:box('Flange',(0,(i-1)*.10,z),(.40,.085,.02),kind,.004)
 bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=bpy.context.selected_objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
