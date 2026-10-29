"""Assemble the stylized CC0 ranger and author planted combat crouch poses."""
from pathlib import Path
import bpy,math,json
from mathutils import Vector,Matrix,Quaternion
PROJECT=Path(__file__).resolve().parents[1];OUT=PROJECT/'SourceArt/Prepared/TrailRanger';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(PROJECT/'Saved/StylizedRangerSource.glb'))
rig=max((o for o in bpy.data.objects if o.type=='ARMATURE'),key=lambda o:len(o.children))
meshes=[o for o in bpy.data.objects if o.type=='MESH' and 'pauldron' not in o.name.lower()]
for obj in list(bpy.data.objects):
 if obj.type=='MESH' and obj not in meshes:bpy.data.objects.remove(obj,do_unlink=True)
for obj in meshes:
 world=obj.matrix_world.copy();obj.parent=rig;obj.matrix_world=world
 for m in obj.modifiers:
  if m.type=='ARMATURE':m.object=rig
 for group in obj.vertex_groups:group.name=group.name.lower()
for other in list(bpy.data.objects):
 if other.type=='ARMATURE' and other!=rig:bpy.data.objects.remove(other,do_unlink=True)
for bone in rig.data.bones:bone.name=bone.name.lower()
for action in bpy.data.actions:
 for curve in action.fcurves:curve.data_path=curve.data_path.replace('"Head"','"head"')
source={track.name:track.strips[0].action for track in rig.animation_data.nla_tracks if track.strips}
rig.animation_data.use_nla=False
aliases={'idle':'Idle_Loop','walk':'Walk_Loop','run':'Sprint_Loop','walk_back':'Walk_Loop','strafe_left':'Jog_Fwd_Loop','strafe_right':'Jog_Fwd_Loop','jump':'Jump_Start','fall':'Jump_Loop','land':'Jump_Land','swim':'Swim_Fwd_Loop','swim_idle':'Swim_Idle_Loop'}
actions={}
for name,original in aliases.items():
 if original not in source:raise RuntimeError('Animation missing '+original+' available '+str(list(source)))
 action=source[original].copy();action.name=name;actions[name]=action
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();body=bpy.context.object;body.name='TrailRanger';body.data.name='TrailRanger'
for mat in body.data.materials:
 if mat:mat.name=mat.name.replace('.','_')
def upper_pose(name,pistol=False,reload=False):
    original=actions['idle'];rig.animation_data.action=original
    bpy.context.scene.frame_set(int(original.frame_range[0]));bpy.context.view_layer.update()
    grip_z=rig.pose.bones['spine_03'].matrix.translation.z-.10
    targets=[];constraints=[]
    for side,position,pole in [('r',(-.16,-.40,grip_z),(-.60,-.06,grip_z-.23)),('l',((-.13,-.42,grip_z-.02) if pistol else (-.12,-.68,grip_z-.04)),(.60,-.08,grip_z-.20))]:
        target=bpy.data.objects.new('Grip_'+side,None);bpy.context.collection.objects.link(target);target.location=position
        elbow=bpy.data.objects.new('Elbow_'+side,None);bpy.context.collection.objects.link(elbow);elbow.location=pole
        ik=rig.pose.bones['lowerarm_'+side].constraints.new('IK');ik.target=target;ik.pole_target=elbow;ik.chain_count=2;ik.use_stretch=False
        targets.extend([target,elbow]);constraints.append((rig.pose.bones['lowerarm_'+side],ik))
        orient=bpy.data.objects.new('Wrist_'+side,None);bpy.context.collection.objects.link(orient)
        sign=1 if side=='l' else -1
        orient.rotation_mode='QUATERNION';orient.rotation_quaternion=Quaternion((0,-1,0), -math.pi*.5 if side=='r' else 0) @ Matrix(((0,0,sign),(0,-1,0),(sign,0,0))).to_quaternion()
        wrist=rig.pose.bones['hand_'+side];copy=wrist.constraints.new('COPY_ROTATION');copy.target=orient;copy.owner_space='WORLD';copy.target_space='WORLD'
        constraints.append((wrist,copy));targets.append(orient)
    start,end=map(int,original.frame_range);bpy.context.scene.frame_set(start)
    for bone,c in constraints:
        if c.type!='IK':continue
        best=(float('-inf'),0)
        for angle in [0,math.pi*.5,math.pi,-math.pi*.5]:
            c.pole_angle=angle;bpy.context.view_layer.update()
            root=bone.parent.head;score=(bone.head-root).dot((c.pole_target.location-root).normalized())
            if score>best[0]:best=(score,angle)
        c.pole_angle=best[1]
    baked=[]
    if reload:end=start+60
    for frame in range(start,end+1):
        bpy.context.scene.frame_set(frame)
        if reload:
            u=(frame-start)/max(1,end-start);dip=math.sin(u*math.pi)**2
            targets[3].location=Vector((.015,-.35,1.17) if pistol else (.10,-.68,grip_z-.04))+Vector((-.10,.10,-.22))*dip
        bpy.context.view_layer.update()
        baked.append((frame,{b.name:rig.convert_space(pose_bone=b,matrix=b.matrix.copy(),from_space='POSE',to_space='LOCAL') for b in rig.pose.bones}))
    for bone,c in constraints:bone.constraints.remove(c)
    for obj in targets:bpy.data.objects.remove(obj,do_unlink=True)
    action=bpy.data.actions.new(name);rig.animation_data.action=action
    for frame,poses in baked:
        for bone_name,matrix in poses.items():
            bone=rig.pose.bones[bone_name];bone.matrix_basis=matrix;bone.rotation_mode='QUATERNION'
            bone.keyframe_insert('location',frame=frame);bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('scale',frame=frame)
    return action
for name,pistol,reload in [('RifleHold',False,False),('PistolHold',True,False),('RifleReload',False,True),('PistolReload',True,True)]:
    actions[name]=upper_pose(name,pistol,reload)

def crouch_pose(name,source):
 rig.animation_data.action=source;targets=[];constraints=[]
 for side in ['l','r']:
  target=bpy.data.objects.new('Ankle_'+side,None);bpy.context.collection.objects.link(target)
  pole=bpy.data.objects.new('Knee_'+side,None);bpy.context.collection.objects.link(pole);pole.location=(.24 if side=='l' else -.24,-.80,.5)
  calf=rig.pose.bones['calf_'+side];c=calf.constraints.new('IK');c.target=target;c.pole_target=pole;c.chain_count=2;c.use_stretch=False
  targets.extend([target,pole]);constraints.append((calf,c))
  foot=rig.pose.bones['foot_'+side];rotation=foot.constraints.new('COPY_ROTATION');rotation.target=target;rotation.owner_space='WORLD';rotation.target_space='WORLD';constraints.append((foot,rotation))
 pelvis=bpy.data.objects.new('Crouch pelvis',None);bpy.context.collection.objects.link(pelvis);bone=rig.pose.bones['pelvis'];c=bone.constraints.new('COPY_LOCATION');c.target=pelvis;c.owner_space='WORLD';c.target_space='WORLD';constraints.append((bone,c));targets.append(pelvis)
 frames=[];start,end=map(int,source.frame_range)
 for frame in range(start,end+1):
  for _,c in constraints:c.mute=True
  bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
  for side,index in [('l',0),('r',2)]:
   foot=rig.pose.bones['foot_'+side];targets[index].matrix_world=rig.matrix_world@foot.matrix
  pelvis.location=(rig.matrix_world@rig.pose.bones['pelvis'].matrix).translation+Vector((0,.10,-.35))
  for _,c in constraints:c.mute=False
  if frame==start:
   for calf,c in constraints:
    if c.type!='IK':continue
    best=(-1e9,0)
    for angle in [0,math.pi*.5,math.pi,-math.pi*.5]:
     c.pole_angle=angle;bpy.context.view_layer.update();root=calf.parent.head;score=(calf.head-root).dot((c.pole_target.location-root).normalized())
     if score>best[0]:best=(score,angle)
    c.pole_angle=best[1]
  bpy.context.view_layer.update();frames.append((frame,{b.name:rig.convert_space(pose_bone=b,matrix=b.matrix.copy(),from_space='POSE',to_space='LOCAL') for b in rig.pose.bones}))
 for bone,c in constraints:bone.constraints.remove(c)
 for obj in targets:bpy.data.objects.remove(obj,do_unlink=True)
 action=bpy.data.actions.new(name);rig.animation_data.action=action
 for frame,poses in frames:
  for n,matrix in poses.items():
   bone=rig.pose.bones[n];bone.matrix_basis=matrix;bone.rotation_mode='QUATERNION';bone.keyframe_insert('location',frame=frame);bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('scale',frame=frame)
 return action
actions['CrouchIdle']=crouch_pose('CrouchIdle',actions['idle'])
actions['CrouchWalk']=crouch_pose('CrouchWalk',actions['walk'])
rig.animation_data.action=None
for track in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(track)
for name,action in actions.items():
 track=rig.animation_data.nla_tracks.new();track.name=name;track.strips.new(name,int(action.frame_range[0]),action);track.mute=True
rig.animation_data.use_nla=True
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=str(OUT/'TrailRanger.glb'),export_format='GLB',use_selection=True,export_morph=False,export_animations=True,export_animation_mode='NLA_TRACKS',export_skins=True)
(OUT/'preparation.json').write_text(json.dumps({'source':'Credited Quaternius CC0 ranger and universal animation library','clips':list(actions),'vertices':len(body.data.vertices),'crouch':'35 cm lowered/back pelvis, forward knee poles, planted ankle rotations; native capsule 72 cm half-height'},indent=2))
print('PF_STYLIZED_RANGER_PREPARED',len(actions))

# Preserve explicit material inputs for the Unreal material repair pass.
import struct,re
raw=(OUT/'TrailRanger.glb').read_bytes();size=struct.unpack_from('<I',raw,12)[0]
doc=json.loads(raw[20:20+size]);blob=raw[28+size:];folder=OUT/'Textures';folder.mkdir(exist_ok=True);rows=[]
for mat in doc['materials']:
 name=re.sub('[^A-Za-z0-9_]','_',mat['name']);pbr=mat.get('pbrMetallicRoughness',{})
 row={'name':name,'color':pbr.get('baseColorFactor',[1,1,1,1]),'alpha':mat.get('alphaMode','OPAQUE')}
 if 'baseColorTexture' in pbr:
  image=doc['images'][doc['textures'][pbr['baseColorTexture']['index']]['source']];view=doc['bufferViews'][image['bufferView']]
  filename=name+('.png' if image['mimeType']=='image/png' else '.jpg');start=view.get('byteOffset',0)
  (folder/filename).write_bytes(blob[start:start+view['byteLength']]);row['color_file']=filename
 rows.append(row)
(folder/'materials.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
