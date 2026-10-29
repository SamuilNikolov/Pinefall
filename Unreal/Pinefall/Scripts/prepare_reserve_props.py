"""Original reserve architecture, vehicle and supply meshes. Run in Blender 4.3."""
from pathlib import Path
import bpy,math,json
from mathutils import Vector
PROJECT=Path(__file__).resolve().parents[1];OUT=PROJECT/'SourceArt/Prepared/Reserve';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
colors={'Paint':(.14,.21,.12,1),'Metal':(.18,.21,.22,1),'Rubber':(.025,.029,.032,1),'Glass':(.09,.18,.21,1),'Wood':(.30,.24,.15,1),'Roof':(.12,.16,.16,1),'Stone':(.38,.36,.32,1),'Cloth':(.29,.35,.26,1),'Red':(.65,.035,.025,1),'White':(.82,.84,.78,1),'Brass':(.55,.34,.10,1)}
materials={}
for name,color in colors.items():
 m=bpy.data.materials.new(name);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=color;b.inputs['Roughness'].default_value=.32 if name in ['Metal','Glass'] else .78;b.inputs['Metallic'].default_value=.7 if name in ['Metal','Brass'] else 0;materials[name]=m
objects=[]
def box(name,loc,size,mat='Metal',bevel=.025,rotation=(0,0,0)):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rotation);o=bpy.context.object;o.name=name;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Soft machined edges','BEVEL');mod.width=bevel;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(materials[mat]);objects.append(o);return o
def cylinder(name,loc,radius,depth,mat='Metal',rotation=(0,0,0),vertices=24):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=rotation);o=bpy.context.object;o.name=name;o.data.materials.append(materials[mat]);objects.append(o)
 for face in o.data.polygons:face.use_smooth=len(face.vertices)==4
 return o
def hull(name,verts,faces,mat):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);mesh.materials.append(materials[mat]);objects.append(o);return o
def save(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;o.data.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True,export_animations=False)
 bpy.data.objects.remove(o,do_unlink=True);objects.clear()
# A compact 4x4 with a real cab, bed, bumper, grille, lamps, tyres and roof rack.
box('Chassis',(0,0,.47),(1.72,4.0,.24),'Metal',.06)
box('Body sills',(0,0,.77),(1.88,3.9,.44),'Paint',.08)
box('Hood',(0,1.28,1.09),(1.78,1.28,.40),'Paint',.07)
box('Cab roof',(0,.05,1.91),(1.80,1.7,.12),'Paint',.05)
for x in [-.84,.84]:
 for y in [-.75,.76]:box('Window pillar',(x,y,1.52),(.09,.10,.77),'Paint',.02)
 box('Cab door',(x,.05,1.05),(.10,1.42,.38),'Paint',.025)
 box('Side window',(x,.05,1.55),(.035,1.32,.56),'Glass',.01)
 box('Wing mirror',(x*1.18,.65,1.48),(.13,.23,.14),'Metal',.025)
box('Windshield',(0,.79,1.55),(1.56,.035,.59),'Glass',.01)
box('Rear window',(0,-.78,1.55),(1.55,.035,.59),'Glass',.01)
box('Truck bed',(0,-1.37,.99),(1.58,1.0,.07),'Metal')
for x in [-.86,.86]:box('Bed rail',(x,-1.35,1.13),(.12,1.15,.42),'Paint',.025)
box('Tailgate',(0,-1.94,1.13),(1.75,.10,.42),'Paint')
for y in [-2.03,2.03]:box('Bumper',(0,y,.64),(1.96,.15,.20),'Metal',.04)
box('Grille',(0,1.96,1.02),(1.13,.035,.28),'Rubber',.01)
for x in [-.42,-.21,0,.21,.42]:box('Grille ribs',(x,1.99,1.02),(.025,.025,.23),'Metal',.005)
for x in [-.71,.71]:
 box('Headlamp',(x,1.95,1.09),(.25,.04,.21),'White')
 box('Tail lamp',(x,-2,1.13),(.19,.04,.18),'Red')
 for y in [-1.32,1.25]:
  cylinder('Tyre',(x*1.34,y,.46),.46,.30,'Rubber',(0,math.pi/2,0),32)
  cylinder('Wheel rim',(x*1.55,y,.46),.27,.035,'Metal',(0,math.pi/2,0),24)
  cylinder('Wheel hub',(x*1.59,y,.46),.10,.05,'Metal',(0,math.pi/2,0),16)
for x in [-.68,.68]:box('Roof rack rail',(x,.02,2.02),(.05,1.5,.08),'Metal',.01)
for y in [-.6,-.2,.2,.6]:box('Roof rack crossbar',(0,y,2.03),(1.4,.045,.07),'Metal',.01)
save('SM_TrailTruck')
# Shallow-draft open river boat, tapered bow and raised gunwale, with benches.
ring=[(-.78,-1.8),(.78,-1.8),(.94,.65),(.62,1.7),(0,2.22),(-.62,1.7),(-.94,.65)]
verts=[(x*.75,y*.93,.12) for x,y in ring]+[(x,y,.72) for x,y in ring]
faces=[tuple(range(6,-1,-1))]+[(i,(i+1)%7,(i+1)%7+7,i+7) for i in range(7)]
hull('Tapered alloy hull',verts,faces,'Paint')
box('Deck',(0,-.1,.30),(1.35,3.2,.09),'Wood')
for y in [-.9,.35,1.15]:box('Bench seat',(0,y,.70),(1.5,.32,.10),'Wood')
for x in [-.78,.78]:box('Gunwale',(x,-.55,.75),(.09,2.6,.10),'Metal')
box('Outboard',(0,-1.96,.62),(.37,.46,.55),'Metal',.09);box('Outboard leg',(0,-2.10,.17),(.11,.16,.62),'Metal')
cylinder('Propeller hub',(0,-2.17,-.1),.11,.13,'Metal',(math.pi/2,0,0));box('Propeller',(0,-2.23,-.1),(.5,.05,.1),'Metal')
save('SM_RiverBoat')
# Enterable timber cabin. Door and windows are actual openings in the shell.
box('Foundation',(0,0,.12),(7.8,9.2,.24),'Stone',.035)
box('Floor',(0,0,.29),(7.4,8.8,.10),'Wood',.015)
# Front at +Y: centre door and separate window openings.
for x in [-3.65,3.65]:
 box('Side lower wall',(x,0,.85),(.18,8.6,1.1),'Wood')
 box('Side upper wall',(x,0,2.68),(.18,8.6,.75),'Wood')
 for y in [-3.8,-1.6,1.6,3.8]:box('Window mullion',(x,y,1.85),(.18,.8,.95),'Wood')
box('Rear wall',(0,-4.3,1.69),(7.3,.18,2.8),'Wood')
for x in [-2.25,2.25]:box('Front wall wings',(x,4.3,1.69),(2.8,.18,2.8),'Wood')
box('Door header',(0,4.3,2.86),(1.7,.22,.45),'Wood')
for x in [-.87,.87]:box('Door jamb',(x,4.4,1.42),(.12,.17,2.35),'White',.01)
box('Porch deck',(0,4.98,.22),(4.8,1.45,.16),'Wood')
for x in [-3.8,3.8]:
 for y in [-4.45,4.45]:box('Corner post',(x,y,1.70),(.24,.24,3.0),'Wood')
# Pitched roof and gables leave a walkable attic silhouette, no sealed box.
for side in [-1,1]:box('Pitched roof',(side*1.95,0,3.55),(4.5,9.6,.16),'Roof',.015,(0,side*math.radians(24),0))
box('Chimney',(-2.1,-2.4,3.9),(.6,.65,1.5),'Stone',.025)
save('SM_RangerCabin')
# Loot props with silhouettes that read from several metres.
box('Medical bag',(0,0,.15),(.45,.25,.27),'Cloth',.065)
for x in [-.15,.15]:box('Bag strap',(x,0,.16),(.035,.27,.29),'Rubber',.008)
box('Patch',(0,.133,.17),(.15,.015,.15),'White',.005);box('Cross vertical',(0,.145,.17),(.035,.015,.11),'Red',.003);box('Cross horizontal',(0,.145,.17),(.11,.015,.035),'Red',.003)
save('SM_MedicalBag')
for x in [-.12,0,.12]:
 cylinder('Food tin',(x,0,.095),.065,.19,'Paint');cylinder('Lid',(x,0,.195),.069,.015,'Metal')
save('SM_RationTins')
box('Ammo carton',(0,0,.07),(.30,.19,.14),'Wood',.012)
for x in [-.09,-.03,.03,.09]:
 cylinder('Cartridge',(x,.02,.19),.021,.17,'Brass',vertices=12);cylinder('Bullet tip',(x,.02,.29),.017,.045,'Metal',vertices=12)
save('SM_AmmoBox')
box('Vest front',(0,0,.16),(.40,.12,.34),'Cloth',.055)
for x in [-.13,.13]:
 box('Pouch',(x,.08,.12),(.16,.07,.17),'Cloth',.022)
 box('Shoulder strap',(x,0,.39),(.08,.10,.15),'Rubber',.02)
save('SM_FieldVest')
box('Tool case',(0,0,.12),(.42,.25,.23),'Paint',.045)
for x in [-.15,.15]:box('Case latch',(x,.13,.12),(.05,.025,.07),'Metal',.01)
box('Case handle',(0,0,.28),(.20,.06,.05),'Rubber',.015)
save('SM_ServiceCase')
(OUT/'provenance.json').write_text(json.dumps({'author':'Original Pinefall procedural mesh preparation','generator':'Scripts/prepare_reserve_props.py, Blender 4.3','commercial_assets_copied':False,'meshes':['SM_TrailTruck','SM_RiverBoat','SM_RangerCabin','SM_MedicalBag','SM_RationTins','SM_AmmoBox','SM_FieldVest','SM_ServiceCase']},indent=2))
print('PF_RESERVE_PROPS_PREPARED')
