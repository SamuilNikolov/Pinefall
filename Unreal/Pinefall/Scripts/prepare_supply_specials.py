"""Original supply models. Python 3 + NumPy; exports one GLB mesh with PBR material sections."""
from pathlib import Path
import numpy as np,math,json,struct
OUT=Path(__file__).resolve().parents[1]/'SourceArt/Prepared/Supplies';OUT.mkdir(parents=True,exist_ok=True)
colors={'Paint':(.19,.25,.15,1),'Metal':(.20,.23,.25,1),'Rubber':(.025,.03,.035,1),'Glass':(.06,.15,.20,1),'Signal':(.15,.65,.85,1),'Cloth':(.73,.66,.47,1)}
parts=[]
def add(v,n,f,p,mat,R=None):
 R=np.eye(3) if R is None else R
 parts.append((np.array(v)@R.T+np.array(p),np.array(n)@R.T,np.array(f,dtype=np.uint32),mat))
def box(name,p,s,mat='Metal',rotation=(0,0,0)):
 # Rounded cuboid: project a subdivided cube onto a small-radius Minkowski hull.
 v=[];n=[];f=[];s=np.array(s)/2;r=min(s)*.30;core=s-r;steps=6
 for axis in range(3):
  for sign in [-1,1]:
   start=len(v);a=(axis+1)%3;b=(axis+2)%3
   for i in range(steps+1):
    for j in range(steps+1):
     q=np.zeros(3);q[axis]=sign*s[axis];q[a]=(i/steps*2-1)*s[a];q[b]=(j/steps*2-1)*s[b]
     inner=np.clip(q,-core,core);d=q-inner;d/=np.linalg.norm(d);v.append(inner+d*r);n.append(d)
   for i in range(steps):
    for j in range(steps):
     x=start+i*(steps+1)+j;tri=[(x,x+steps+1,x+1),(x+1,x+steps+1,x+steps+2)]
     f.extend(tri if sign>0 else [tuple(reversed(t)) for t in tri])
 cx,sx=math.cos(rotation[0]),math.sin(rotation[0]);cy,sy=math.cos(rotation[1]),math.sin(rotation[1]);cz,sz=math.cos(rotation[2]),math.sin(rotation[2])
 R=np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])@np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]])@np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]])
 add(v,n,f,p,mat,R)
def ellipsoid(name,p,s,mat='Paint'):
 v=[];n=[];f=[];rows=16;cols=32
 for i in range(rows+1):
  a=math.pi*i/rows
  for j in range(cols+1):
   b=2*math.pi*j/cols;d=np.array([math.sin(a)*math.cos(b),math.sin(a)*math.sin(b),math.cos(a)]);v.append(d*s);normal=d/s;normal/=np.linalg.norm(normal);n.append(normal)
 for i in range(rows):
  for j in range(cols):
   x=i*(cols+1)+j;f.extend([(x,x+cols+1,x+1),(x+1,x+cols+1,x+cols+2)])
 add(v,n,f,p,mat)
def rod(name,a,b,r=.035,mat='Metal'):
 a,b=np.array(a),np.array(b);z=b-a;length=np.linalg.norm(z);z/=length;x=np.cross(z,[0,0,1] if abs(z[2])<.9 else [0,1,0]);x/=np.linalg.norm(x);y=np.cross(z,x);R=np.column_stack((x,y,z));v=[];n=[];f=[];cols=24
 for h in [-length/2,length/2]:
  for i in range(cols):
   angle=2*math.pi*i/cols;d=np.array([math.cos(angle),math.sin(angle),0]);v.append(d*r+[0,0,h]);n.append(d)
 for i in range(cols):
  j=(i+1)%cols;f.extend([(i,j,i+cols),(j,j+cols,i+cols)])
 for h,sign in [(-length/2,-1),(length/2,1)]:
  start=len(v);v.append([0,0,h]);n.append([0,0,sign])
  for i in range(cols):
   angle=2*math.pi*i/cols;v.append([r*math.cos(angle),r*math.sin(angle),h]);n.append([0,0,sign])
  for i in range(cols):
   tri=(start,start+1+i,start+1+(i+1)%cols);f.append(tri if sign>0 else tuple(reversed(tri)))
 add(v,n,f,(a+b)/2,mat,R)
def save(name):
 data=bytearray();views=[];accessors=[];primitives=[];names=list(colors)
 def buffer(a,kind,target):
  while len(data)%4:data.append(0)
  offset=len(data);data.extend(a.tobytes());views.append({'buffer':0,'byteOffset':offset,'byteLength':a.nbytes,'target':target})
  item={'bufferView':len(views)-1,'componentType':5125 if kind=='SCALAR' else 5126,'count':len(a),'type':kind}
  if kind=='VEC3':item.update(min=a.min(axis=0).tolist(),max=a.max(axis=0).tolist())
  accessors.append(item);return len(accessors)-1
 # Blender-style Z-up to GLTF Y-up, preserving the existing native import convention.
 C=np.array([[1,0,0],[0,0,1],[0,-1,0]])
 for mat in names:
  selected=[p for p in parts if p[3]==mat]
  if not selected:continue
  vv=[];nn=[];ff=[];offset=0
  for v,n,f,_ in selected:vv.append(v@C.T);nn.append(n@C.T);ff.append(f+offset);offset+=len(v)
  pos=buffer(np.concatenate(vv).astype('<f4'),'VEC3',34962);norm=buffer(np.concatenate(nn).astype('<f4'),'VEC3',34962);idx=buffer(np.concatenate(ff).reshape(-1).astype('<u4'),'SCALAR',34963)
  primitives.append({'attributes':{'POSITION':pos,'NORMAL':norm},'indices':idx,'material':names.index(mat)})
 doc={'asset':{'version':'2.0','generator':'Pinefall original supply geometry'},'buffers':[{'byteLength':len(data)}],'bufferViews':views,'accessors':accessors,'materials':[{'name':m,'pbrMetallicRoughness':{'baseColorFactor':colors[m],'metallicFactor':.7 if m=='Metal' else .15,'roughnessFactor':.42 if m=='Metal' else .75}} for m in names],'meshes':[{'name':name,'primitives':primitives}],'nodes':[{'mesh':0,'name':name}],'scenes':[{'nodes':[0]}],'scene':0}
 js=json.dumps(doc,separators=(',',':')).encode();js+=b' '*((-len(js))%4);data+=b'\0'*((-len(data))%4);total=12+8+len(js)+8+len(data)
 (OUT/(name+'.glb')).write_bytes(struct.pack('<III',0x46546c67,2,total)+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(data),0x004e4942)+data);parts.clear();print(name)
# Cargo plane: high wings, rounded fuselage, cockpit, twin turboprop nacelles, tail.
ellipsoid('Fuselage',(0,0,0),(1.0,6,.95));ellipsoid('Cockpit',(0,4.2,.42),(.82,1.4,.58),'Glass')
box('High wing',(0,.7,.7),(15,1.6,.18),'Paint');box('Tailplane',(0,-4.6,.65),(5,1.1,.14),'Paint');box('Fin',(0,-4.5,1.4),(.16,1.5,2),'Paint')
for x in [-3.5,3.5]:
 ellipsoid('Engine',(x,1.2,.42),(.48,1.5,.48));rod('Propeller hub',(x,2.4,.4),(x,2.8,.4),.17)
 box('Propeller blade',(x,2.85,.4),(.17,.08,3),'Rubber');box('Propeller blade',(x,2.85,.4),(3,.08,.17),'Rubber')
for y in [-3,-2,-1,0,1,2]:
 for x in [-.98,.98]:box('Cargo window',(x,y,.3),(.035,.42,.36),'Glass')
save('SM_CargoPlane')
# Trail motorcycle, wheels are animated native components rather than baked mesh.
box('Engine',(0,0,.54),(.48,.6,.5));ellipsoid('Fuel tank',(0,.35,.95),(.30,.45,.24));box('Seat',(0,-.32,1),(.42,.8,.14),'Rubber')
for x in [-.22,.22]:
 rod('Frame',(x,-.85,.4),(x,.55,.9));rod('Frame',(x,.55,.9),(x,.2,.38));rod('Swingarm',(x,.2,.38),(x,-.95,.38));rod('Fork',(x,.8,1.15),(x,.95,.38),.055)
 rod('Exhaust',(x,-.3,.5),(x,-.95,.75),.065)
rod('Handlebar',(-.48,.7,1.25),(.48,.7,1.25),.035)
for x in [-.4,.4]:rod('Grip',(x-.09,.7,1.25),(x+.09,.7,1.25),.05,'Rubber')
box('Headlamp',(0,.9,1.1),(.3,.15,.25),'Signal');box('Front fender',(0,.95,.82),(.4,.65,.06),'Paint');box('Rear fender',(0,-.9,.82),(.4,.6,.06),'Paint');box('Instrument',(0,.58,1.25),(.22,.22,.06),'Glass')
save('SM_TrailBike')
box('Receiver',(0,0,.1),(.32,.5,.18),'Rubber');box('Screen',(0,.07,.2),(.24,.25,.025),'Signal')
for x in [-.07,.07]:ellipsoid('Button',(x,-.17,.2),(.025,.025,.012),'Metal')
rod('Antenna',(.12,.18,.15),(.12,.18,.65),.012)
save('SM_SurveyReceiver')

# Open fabric dome and eight suspension lines; authored in meters at canopy origin.
v=[];n=[];f=[];rows=12;cols=48
for i in range(rows+1):
 a=math.pi*.5*i/rows
 for j in range(cols+1):
  b=2*math.pi*j/cols;d=np.array([math.sin(a)*math.cos(b),math.sin(a)*math.sin(b),math.cos(a)]);v.append(d*[2.5,2.5,.8]);normal=d/[2.5,2.5,.8];normal/=np.linalg.norm(normal);n.append(normal)
for i in range(rows):
 for j in range(cols):
  x=i*(cols+1)+j;f.extend([(x,x+cols+1,x+1),(x+1,x+cols+1,x+cols+2)])
add(v,n,f,[0,0,0],'Cloth')
for i in range(8):
 a=i*2*math.pi/8;x,y=math.cos(a),math.sin(a);rod('Suspension',(x*2.5,y*2.5,0),(x*.5,y*.5,-3.5),.012,'Cloth')
save('SM_SupplyCanopy')
