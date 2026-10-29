"""Combine the credited stylized ranger with the credited universal animation library.
Retarget local rest offsets and preserve embedded textures; no Unity runtime is used.
"""
from pathlib import Path
import json,struct,math
PROJECT=Path(__file__).resolve().parents[1];REPO=PROJECT.parents[1]
def read(path):
 raw=path.read_bytes();n=struct.unpack_from('<I',raw,12)[0];return json.loads(raw[20:20+n]),bytearray(raw[28+n:])
def write(path,j,b):
 j['buffers']=[{'byteLength':len(b)}];a=json.dumps(j,separators=(',',':')).encode();a+=b' '*((-len(a))%4);b+=b'\0'*((-len(b))%4);path.write_bytes(struct.pack('<III',0x46546c67,2,12+8+len(a)+8+len(b))+struct.pack('<II',len(a),0x4e4f534a)+a+struct.pack('<II',len(b),0x004e4942)+b)
def mul(a,b):
 x,y,z,w=a;X,Y,Z,W=b;return [w*X+x*W+y*Z-z*Y,w*Y-x*Z+y*W+z*X,w*Z+x*Y-y*X+z*W,w*W-x*X-y*Y-z*Z]
body,blob=read(REPO/'Assets/Art/Characters/Hunter.glb');anim,ab=read(REPO/'Assets/Art/Characters/Animations.glb')
base=len(blob);blob+=b'\0'*((-len(blob))%4);base=len(blob);blob+=ab
vo=len(body.get('bufferViews',[]));ao=len(body.get('accessors',[]))
for view in anim.get('bufferViews',[]):
 v=dict(view);v['buffer']=0;v['byteOffset']=v.get('byteOffset',0)+base;body.setdefault('bufferViews',[]).append(v)
for accessor in anim.get('accessors',[]):
 a=dict(accessor)
 if 'bufferView' in a:a['bufferView']+=vo
 body.setdefault('accessors',[]).append(a)
main=max(body['skins'],key=lambda s:len(s['joints']));lookup={body['nodes'][i]['name']:i for i in main['joints']};clips=[]
for clip in anim['animations']:
 new={'name':clip['name'],'samplers':[],'channels':[]}
 for channel in clip['channels']:
  target=channel['target'];source_node=anim['nodes'][target['node']];name=source_node.get('name');dest=lookup.get(name)
  if dest is None:continue
  sampler=clip['samplers'][channel['sampler']];path=target['path'];out=sampler['output']+ao
  if path in ['rotation','translation']:
   source_a=anim['accessors'][sampler['output']];view=anim['bufferViews'][source_a['bufferView']];offset=view.get('byteOffset',0)+source_a.get('byteOffset',0);width=4 if path=='rotation' else 3
   if sampler.get('interpolation','LINEAR') not in ['LINEAR','STEP']:raise RuntimeError('Unsupported cubic retarget')
   values=struct.unpack_from('<'+str(source_a['count']*width)+'f',ab,offset);patched=[]
   target_node=body['nodes'][dest]
   for i in range(source_a['count']):
    v=list(values[i*width:(i+1)*width])
    if path=='translation':v=[v[k]+target_node.get('translation',[0,0,0])[k]-source_node.get('translation',[0,0,0])[k] for k in range(3)]
    else:
     q=source_node.get('rotation',[0,0,0,1]);d=mul(target_node.get('rotation',[0,0,0,1]),[-q[0],-q[1],-q[2],q[3]]);v=mul(d,v);length=math.sqrt(sum(x*x for x in v));v=[x/length for x in v]
    patched.extend(v)
   blob+=b'\0'*((-len(blob))%4);start=len(blob);blob+=struct.pack('<'+str(len(patched))+'f',*patched);body['bufferViews'].append({'buffer':0,'byteOffset':start,'byteLength':len(blob)-start});body['accessors'].append({'bufferView':len(body['bufferViews'])-1,'componentType':5126,'count':source_a['count'],'type':'VEC4' if width==4 else 'VEC3'});out=len(body['accessors'])-1
  new['samplers'].append({'input':sampler['input']+ao,'output':out,'interpolation':sampler.get('interpolation','LINEAR')});new['channels'].append({'sampler':len(new['samplers'])-1,'target':{'node':dest,'path':path}})
 clips.append(new)
body['animations']=clips
out=PROJECT/'Saved/StylizedRangerSource.glb';out.parent.mkdir(exist_ok=True);write(out,body,blob);print('PF_STYLIZED_SOURCE_OK',len(clips))
