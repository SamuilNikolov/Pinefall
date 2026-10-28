"""Run with portable Blender: assemble CC0 body/clothing and original weapon poses.

No generator code from the character source is bundled. Only its CC0 outputs are
inputs. The resulting dressed mesh and animations are ordinary game source assets.
"""
from pathlib import Path
import bpy,math,json
from mathutils import Vector,Matrix,Quaternion
PROJECT=Path(__file__).resolve().parents[1]
SOURCE=PROJECT/'SourceArt/Characters/FieldRanger'
OUT=PROJECT/'SourceArt/Prepared/FieldRanger';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE/'base_body_anim.glb'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
actions={track.name:track.strips[0].action for track in rig.animation_data.nla_tracks if track.strips}
rig.animation_data.use_nla=False;rig.animation_data.action=None
for name in ['clothing_shirt','clothing_jeans','clothing_boots','hair_ponytail01']:
    before=set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE/(name+'.glb')))
    additions=set(bpy.data.objects)-before
    for obj in additions:
        if obj.type!='MESH':continue
        world=obj.matrix_world.copy();obj.parent=None;obj.matrix_world=world
        for modifier in obj.modifiers:
            if modifier.type=='ARMATURE':modifier.object=rig
    for obj in additions:
        if obj.type=='ARMATURE':bpy.data.objects.remove(obj,do_unlink=True)
rig.data.pose_position='REST'
meshes=[o for o in bpy.data.objects if o.type=='MESH' and o.name!='Icosphere']
for obj in meshes:
    bpy.ops.object.select_all(action='DESELECT')
    if obj.data.shape_keys:
        if obj.data.shape_keys.animation_data:obj.data.shape_keys.animation_data_clear()
        for key in obj.data.shape_keys.key_blocks:key.value=0
        for key in obj.data.shape_keys.key_blocks:
            if key.name=='gender_female':key.value=.7
        # Bake the same morphology into body, clothes and hair before stripping
        # customization targets from the lightweight playable mesh.
        bpy.context.view_layer.objects.active=obj;obj.select_set(True)
        bpy.ops.object.convert(target='MESH');obj.select_set(False)
    if not any(m.type=='ARMATURE' for m in obj.modifiers):obj.modifiers.new('Skin','ARMATURE').object=rig
    for mod in obj.modifiers:
        if mod.type=='ARMATURE':mod.object=rig
    if obj.name=='Body':
        # Long sleeves, jeans and boots cover these triangles. Keep neck/head and
        # hands, removing hidden body surfaces rather than accepting cloth leaks.
        keep={g.index for g in obj.vertex_groups if g.name in ['neck_01','head'] or any(v in g.name for v in ['hand_','index_','middle_','ring_','pinky_','thumb_'])}
        visible={v.index for v in obj.data.vertices if sum(g.weight for g in v.groups if g.group in keep)>.35}
        import bmesh
        bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm,geom=[f for f in bm.faces if all(v.index not in visible for v in f.verts)],context='FACES')
        bm.to_mesh(obj.data);bm.free()
    for mat in obj.data.materials:
        if not mat or not mat.use_nodes:continue
        bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if bsdf:bsdf.inputs['Roughness'].default_value=.8
        if bsdf and 'shirt' in obj.name.lower():
            # Keep scanned cloth detail while tinting it toward outdoor olive.
            socket=bsdf.inputs['Base Color'];links=list(socket.links)
            if links:
                mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.30,.42,.24,1)
                old=links[0].from_socket;mat.node_tree.links.remove(links[0]);mat.node_tree.links.new(old,mix.inputs[1]);mat.node_tree.links.new(mix.outputs[0],socket)
rig.data.pose_position='POSE'
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join()
body=bpy.context.object;body.name='FieldRanger'
import bmesh
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bm.to_mesh(body.data);bm.free()
if body.data.has_custom_normals:bpy.ops.mesh.customdata_custom_splitnormals_clear()
for polygon in body.data.polygons:polygon.use_smooth=True
body.data.update()
for obj in list(bpy.data.objects):
    if obj not in [body,rig]:bpy.data.objects.remove(obj,do_unlink=True)

def upper_pose(name,pistol=False,reload=False):
    original=actions['idle'];rig.animation_data.action=original
    targets=[];constraints=[]
    for side,position,pole in [('r',(-.14,-.37,1.19),(-.55,-.15,1.10)),('l',((.015,-.35,1.17) if pistol else (.08,-.49,1.15)),(.48,-.14,1.04))]:
        target=bpy.data.objects.new('Grip_'+side,None);bpy.context.collection.objects.link(target);target.location=position
        elbow=bpy.data.objects.new('Elbow_'+side,None);bpy.context.collection.objects.link(elbow);elbow.location=pole
        ik=rig.pose.bones['lowerarm_'+side].constraints.new('IK');ik.target=target;ik.pole_target=elbow;ik.chain_count=2;ik.use_stretch=False
        targets.extend([target,elbow]);constraints.append((rig.pose.bones['lowerarm_'+side],ik))
        orient=bpy.data.objects.new('Wrist_'+side,None);bpy.context.collection.objects.link(orient)
        sign=1 if side=='l' else -1
        orient.rotation_mode='QUATERNION';orient.rotation_quaternion=Matrix(((0,0,sign),(0,-1,0),(sign,0,0))).to_quaternion()
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
        for bone in rig.pose.bones:
            if not any(part in bone.name for part in ['index_','middle_','ring_','pinky_','thumb_']):continue
            rest=rig.data.bones[bone.name];direction=(rest.tail_local-rest.head_local).normalized()
            axis=direction.cross(Vector((0,-1,0)))
            if axis.length<.001:continue
            local_axis=rest.matrix_local.to_3x3().inverted()@axis.normalized()
            amount=30 if 'index_' in bone.name else 25 if 'thumb_' in bone.name else 65 if '_02_' in bone.name else 45
            bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion(local_axis,math.radians(amount))
        if reload:
            u=(frame-start)/max(1,end-start);dip=math.sin(u*math.pi)**2
            targets[3].location=Vector((.015,-.35,1.17) if pistol else (.08,-.60,1.21))+Vector((-.10,.10,-.22))*dip
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
        target=bpy.data.objects.new('Foot_'+side,None);bpy.context.collection.objects.link(target)
        pole=bpy.data.objects.new('Knee_'+side,None);bpy.context.collection.objects.link(pole);pole.location=(.18 if side=='l' else -.18,-.55,.55)
        bone=rig.pose.bones['calf_'+side];c=bone.constraints.new('IK');c.target=target;c.pole_target=pole;c.chain_count=2;c.use_stretch=False
        targets.extend([target,pole]);constraints.append((bone,c))
    pelvis=bpy.data.objects.new('CrouchPelvis',None);bpy.context.collection.objects.link(pelvis)
    bone=rig.pose.bones['pelvis'];c=bone.constraints.new('COPY_LOCATION');c.target=pelvis;c.owner_space='WORLD';c.target_space='WORLD';constraints.append((bone,c));targets.append(pelvis)
    frames=[];start,end=map(int,source.frame_range)
    for frame in range(start,end+1):
        for _,c in constraints:c.mute=True
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        targets[0].location=rig.pose.bones['foot_l'].matrix.translation
        targets[2].location=rig.pose.bones['foot_r'].matrix.translation
        pelvis.location=rig.pose.bones['pelvis'].matrix.translation+Vector((0,0,-.45))
        for _,c in constraints:c.mute=False
        bpy.context.view_layer.update()
        frames.append((frame,{b.name:rig.convert_space(pose_bone=b,matrix=b.matrix.copy(),from_space='POSE',to_space='LOCAL') for b in rig.pose.bones}))
    for bone,c in constraints:bone.constraints.remove(c)
    for obj in targets:bpy.data.objects.remove(obj,do_unlink=True)
    action=bpy.data.actions.new(name);rig.animation_data.action=action
    for frame,poses in frames:
        for name,matrix in poses.items():
            bone=rig.pose.bones[name];bone.matrix_basis=matrix;bone.rotation_mode='QUATERNION'
            bone.keyframe_insert('location',frame=frame);bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('scale',frame=frame)
    return action
actions['CrouchIdle']=crouch_pose('CrouchIdle',actions['idle'])
actions['CrouchWalk']=crouch_pose('CrouchWalk',actions['walk'])
# Export only this rig's curated clips; importer-created morph/auxiliary actions
# must not leak into the character animation set.
rig.animation_data.action=None
for track in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(track)
for name,action in actions.items():
    track=rig.animation_data.nla_tracks.new();track.name=name;track.strips.new(name,int(action.frame_range[0]),action);track.mute=True
rig.animation_data.use_nla=True
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=str(OUT/'FieldRanger.glb'),export_format='GLB',use_selection=True,export_morph=False,export_animations=True,export_animation_mode='NLA_TRACKS',export_skins=True)
(OUT/'preparation.json').write_text(json.dumps({'source':'MakeHuman CC0 outputs via kblood/CharacterCreator','mesh_vertices':len(body.data.vertices),'clips':list(actions),'notes':'Original IK weapon holds/reload motions, hidden covered body removed, morphology baked; close hand grip still requires rendered review.'},indent=2),encoding='utf-8')
print('PF_RANGER_PREPARED',len(body.data.vertices),len(actions))
