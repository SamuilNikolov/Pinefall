#!/usr/bin/env python3
"""Bake selected static glTF nodes into one importable mesh, preserving material slots.

No neural art generation, texture repainting or third-party Python dependency.
The authored source and provenance remain alongside the derived import sources.
"""
from pathlib import Path
import copy,json,math,shutil,struct

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Unreal/Pinefall/SourceArt/Prepared'
IDENTITY=[[1.,0,0,0],[0,1.,0,0],[0,0,1.,0],[0,0,0,1.]]
def multiply(a,b):return [[sum(a[r][k]*b[k][c] for k in range(4)) for c in range(4)] for r in range(4)]
def transform(m,v,point=True):return [sum(m[r][c]*v[c] for c in range(3))+(m[r][3] if point else 0) for r in range(3)]
def normalized(v):
 length=math.sqrt(sum(x*x for x in v));return [x/max(1e-9,length) for x in v]
def node_matrix(node):
 if 'matrix' in node:return [[node['matrix'][c*4+r] for c in range(4)] for r in range(4)]
 x,y,z,w=node.get('rotation',[0,0,0,1]);scale=node.get('scale',[1,1,1]);p=node.get('translation',[0,0,0])
 r=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w),p[0]],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w),p[1]],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y),p[2]],[0,0,0,1]]
 for row in range(3):
  for col in range(3):r[row][col]*=scale[col]
 return r
def prepare(source,name,selected,length=None,height=None,ground=False,offsets=None):
 source=ROOT/source;j=json.loads(source.read_text(encoding='utf-8'));buffers=[(source.parent/b['uri']).read_bytes() for b in j['buffers']]
 def accessor(index):
  a=j['accessors'][index];v=j['bufferViews'][a['bufferView']];count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
  code={5120:'b',5121:'B',5122:'h',5123:'H',5125:'I',5126:'f'}[a['componentType']];fmt='<'+code*count;stride=v.get('byteStride',struct.calcsize(fmt));offset=v.get('byteOffset',0)+a.get('byteOffset',0)
  return [list(struct.unpack_from(fmt,buffers[v['buffer']],offset+i*stride)) for i in range(a['count'])]
 worlds={}
 def visit(index,parent):
  node=j['nodes'][index];worlds[index]=multiply(parent,node_matrix(node))
  for child in node.get('children',[]):visit(child,worlds[index])
 children={x for n in j['nodes'] for x in n.get('children',[])}
 for index in range(len(j['nodes'])):
  if index not in children:visit(index,IDENTITY)
 parts=[]
 for index,node in enumerate(j['nodes']):
  if 'mesh' not in node or node.get('name') not in selected:continue
  m=copy.deepcopy(worlds[index])
  if offsets and node.get('name') in offsets:
   for axis,value in enumerate(offsets[node['name']]):m[axis][3]+=value
  for primitive in j['meshes'][node['mesh']]['primitives']:
   if primitive.get('mode',4)!=4:continue
   attributes={key:accessor(value) for key,value in primitive['attributes'].items() if key in ['POSITION','NORMAL','TEXCOORD_0','TANGENT']}
   attributes['POSITION']=[transform(m,p) for p in attributes['POSITION']]
   if 'NORMAL' in attributes:attributes['NORMAL']=[normalized(transform(m,n,False)) for n in attributes['NORMAL']]
   if 'TANGENT' in attributes:attributes['TANGENT']=[normalized(transform(m,n[:3],False))+[n[3]] for n in attributes['TANGENT']]
   indices=accessor(primitive['indices']) if 'indices' in primitive else [[i] for i in range(len(attributes['POSITION']))]
   parts.append((attributes,indices,primitive.get('material',0)))
 if not parts:raise ValueError('No selected geometry in '+str(source))
 points=[v for attributes,_,_ in parts for v in attributes['POSITION']];low=[min(v[i] for v in points) for i in range(3)];high=[max(v[i] for v in points) for i in range(3)]
 scale=height/(high[1]-low[1]) if height else length/max(high[i]-low[i] for i in range(3)) if length else 1
 center=[(low[i]+high[i])*.5 for i in range(3)];center[1]=low[1] if ground else center[1]
 if ground and name.startswith('SM_Pine'):
  base=[p for p in points if p[1]<low[1]+(high[1]-low[1])*.08]
  # Plant the actual trunk, not the center of an asymmetric canopy, at the origin.
  for axis in [0,2]:center[axis]=(min(p[axis] for p in base)+max(p[axis] for p in base))*.5
 output=OUT/name;output.mkdir(parents=True,exist_ok=True);binary=bytearray();views=[];accessors=[];primitives=[]
 def write(values,kind,indices=False):
  while len(binary)%4:binary.append(0)
  offset=len(binary);code='I' if indices else 'f';flat=[n for row in values for n in row];binary.extend(struct.pack('<'+str(len(flat))+code,*flat));view=len(views)
  views.append(dict(buffer=0,byteOffset=offset,byteLength=len(binary)-offset,target=34963 if indices else 34962))
  entry=dict(bufferView=view,componentType=5125 if indices else 5126,count=len(values),type=kind)
  if kind=='VEC3':entry.update(min=[min(row[c] for row in values) for c in range(3)],max=[max(row[c] for row in values) for c in range(3)])
  accessors.append(entry);return len(accessors)-1
 for attributes,indices,material in parts:
  attributes['POSITION']=[[(v[i]-center[i])*scale for i in range(3)] for v in attributes['POSITION']]
  refs={key:write(values,'VEC'+str(len(values[0]))) for key,values in attributes.items()}
  primitives.append(dict(attributes=refs,indices=write(indices,'SCALAR',True),material=material))
 result={key:copy.deepcopy(j[key]) for key in ['asset','materials','textures','images','samplers','extensionsUsed','extensionsRequired'] if key in j}
 result.update(scene=0,scenes=[dict(nodes=[0])],nodes=[dict(name=name,mesh=0)],meshes=[dict(name=name,primitives=primitives)],buffers=[dict(uri=name+'.bin',byteLength=len(binary))],bufferViews=views,accessors=accessors)
 for image in result.get('images',[]):
  src=source.parent/image['uri'];dst=output/'textures'/src.name;dst.parent.mkdir(exist_ok=True);shutil.copy2(src,dst);image['uri']='textures/'+src.name
 (output/(name+'.bin')).write_bytes(binary);(output/(name+'.gltf')).write_text(json.dumps(result,separators=(',',':')),encoding='utf-8')
 print(name,'vertices',sum(len(a['POSITION']) for a,_,_ in parts),'triangles',sum(len(i)//3 for _,i,_ in parts),'sourceScale',round(scale,5))

if __name__=='__main__':
 prepare('Assets/Art/RealModels/bolt_action_rifle_7_62/bolt_action_rifle_7_62.gltf','SM_Rifle',{'bolt_action_rifle_7_62','bolt_action_rifle_7_62_scope','bolt_action_rifle_7_62_wrap','bolt_action_rifle_7_62_bolt_a','bolt_action_rifle_7_62_trigger'},length=1.1)
 pistol=json.loads((ROOT/'Assets/Art/RealModels/service_pistol/service_pistol.gltf').read_text(encoding='utf-8'))
 # The scan includes an alternate pistol and two detached display magazines.
 # Assemble the loaded magazine in its grip; omit the empty display copy.
 names={'service_pistol_pistol_a','service_pistol_slide_a','service_pistol_magazine_loaded','service_pistol_hammer_a','service_pistol_trigger_a'}
 prepare('Assets/Art/RealModels/service_pistol/service_pistol.gltf','SM_Pistol',names,length=.3,offsets={'service_pistol_magazine_loaded':(.1,0,0)})
 for letter in 'abc':prepare('Unreal/Pinefall/SourceArt/pine_sapling_small/pine_sapling_small_1k.gltf','SM_Pine'+letter.upper(),{'pine_sapling_small_'+letter},ground=True)
 prepare('Assets/Art/RealModels/grass_medium_01/grass_medium_01.gltf','SM_Grass',{'grass_medium_01_tiny_a_LOD0'},height=.24,ground=True)
 prepare('Assets/Art/RealModels/fern_02/fern_02.gltf','SM_Fern',{'fern_02_a'},height=.65,ground=True)
