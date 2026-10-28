"""Import the credited animated animal sources; default is a one-species pipeline check."""
from pathlib import Path
import json,base64,struct,unreal
PROJECT=Path(__file__).resolve().parents[1]
REPO=PROJECT.parents[1]
LIB=unreal.EditorAssetLibrary
TOOLS=unreal.AssetToolsHelpers.get_asset_tools()
names=['Deer','Stag','Fox','Wolf','Bull','Horse','Alpaca'] if 'PFImportAllWildlife' in unreal.SystemLibrary.get_command_line() else ['Deer']
report={}
for name in names:
    folder='/Game/Pinefall/Art/Fauna/'+name;LIB.make_directory(folder)
    source=REPO/'Assets/Art/Animals'/(name+'.gltf')
    document=json.loads(source.read_text(encoding='utf-8'))
    parents={child:index for index,node in enumerate(document['nodes']) for child in node.get('children',[])}
    for skin in document.get('skins',[]):
        roots=[joint for joint in skin['joints'] if parents.get(joint) not in skin['joints']]
        if len(roots)==1:skin.setdefault('skeleton',roots[0])
        else:
            # These sources have separate leg/body roots under one identity armature.
            # UE 5.6 synthesizes a mesh root but leaves animation tracks on the old
            # roots. Include the shared armature in the skin so both resolve alike.
            common=parents.get(roots[0])
            if common is None or any(parents.get(j)!=common for j in roots):
                raise RuntimeError('Unsupported joint hierarchy: '+name)
            armature=document['nodes'][common]
            if any(k in armature for k in ['matrix','translation','rotation','scale']):
                raise RuntimeError('Armature must be identity: '+name)
            old=document['accessors'][skin['inverseBindMatrices']]
            view=document['bufferViews'][old['bufferView']]
            buffer=document['buffers'][view.get('buffer',0)]
            raw=bytearray(base64.b64decode(buffer['uri'].split(',',1)[1]))
            offset=len(raw);raw.extend(b'\0'*((-offset)%4));offset=len(raw)
            begin=view.get('byteOffset',0)+old.get('byteOffset',0)
            raw.extend(raw[begin:begin+old['count']*64])
            raw.extend(struct.pack('<16f',1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1))
            document['bufferViews'].append({'buffer':view.get('buffer',0),'byteOffset':offset,'byteLength':(old['count']+1)*64})
            document['accessors'].append({'bufferView':len(document['bufferViews'])-1,'componentType':5126,'count':old['count']+1,'type':'MAT4'})
            skin['inverseBindMatrices']=len(document['accessors'])-1
            skin['joints'].append(common);skin['skeleton']=common
            buffer['uri']='data:application/octet-stream;base64,'+base64.b64encode(raw).decode('ascii');buffer['byteLength']=len(raw)
    prepared=PROJECT/'Saved/PreparedWildlife';prepared.mkdir(parents=True,exist_ok=True)
    derived=prepared/(name+'.gltf');derived.write_text(json.dumps(document),encoding='utf-8')
    task=unreal.AssetImportTask();task.filename=str(derived);task.destination_path=folder
    pipeline=unreal.InterchangeGenericAssetsPipeline()
    pipeline.get_editor_property('animation_pipeline').set_editor_property('import_animations',True)
    pipeline.get_editor_property('animation_pipeline').set_editor_property('import_bone_tracks',True)
    stack=unreal.InterchangePipelineStackOverride();stack.add_pipeline(pipeline);task.options=stack
    task.automated=True;task.replace_existing=True;task.save=True;TOOLS.import_asset_tasks([task])
    # Reimport reports only the mesh in get_objects; animation packages persist.
    objects=[unreal.load_asset(path) for path in LIB.list_assets(folder,recursive=True,include_folder=False)]
    bodies=[x for x in objects if isinstance(x,unreal.SkeletalMesh)]
    animations=[x for x in objects if isinstance(x,unreal.AnimSequence)]
    if not bodies or len(animations)<len(document['animations']):raise RuntimeError('Animated import incomplete for '+name+': '+str([(x.get_name(),x.get_class().get_name()) for x in objects]))
    skeleton=bodies[0].get_editor_property('skeleton')
    if any(x.get_editor_property('skeleton')!=skeleton or x.get_play_length()<=0 for x in animations):raise RuntimeError('Invalid animation skeleton/length: '+name)
    physics=bodies[0].get_editor_property('physics_asset')
    report[name]={'body':bodies[0].get_path_name(),'animations':{x.get_name():x.get_path_name() for x in animations},'physicsAsset':physics.get_path_name() if physics else None}
    for asset in objects:LIB.save_loaded_asset(asset)
    unreal.log('PF_WILDLIFE_IMPORTED '+name+' animations='+str(len(animations)))
path=PROJECT/'Saved/WildlifeImport.json';path.write_text(json.dumps(report,indent=2),encoding='utf-8')
unreal.log('PF_WILDLIFE_IMPORT_OK '+str(path))
