"""Bake a 2K terrain/road/building/forest atlas from the saved reserve report."""
from pathlib import Path
import json,math,numpy as np
from PIL import Image,ImageDraw
PROJECT=Path(__file__).resolve().parents[1];data=json.loads((PROJECT/'Saved/ReserveBuild.json').read_text())
size=2048;half=100800;raw=np.array(data['heights'],dtype=np.float32).reshape(505,505).T[::-1,:]
h=np.asarray(Image.fromarray(raw).resize((size,size),Image.Resampling.BICUBIC))
y=np.linspace(-half,half,size)[None,:];x=np.linspace(half,-half,size)[:,None]
colors=np.zeros((size,size,3),dtype=np.float32);colors[:]=[109,143,82]
def smooth(a,b,v):
 t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)
def mix(base,tint,w):return base*(1-w[:,:,None])+np.array(tint)[None,None,:]*w[:,:,None]
woods=np.broadcast_to(1-smooth(-28000,-13000,x),(size,size));colors=mix(colors,[66,108,73],woods*.72)
sand=(1-smooth(-40000,-25000,x))*(1-smooth(-36000,-18000,y));snow=smooth(30000,55000,x)*smooth(18000,40000,y)
colors=mix(colors,[186,162,113],sand);colors=mix(colors,[207,218,216],snow)
gv,gu=np.gradient(h);shade=np.clip(1+(gu-gv)*.018,.57,1.32);colors*=shade[:,:,None]
contours=np.abs((h+50)%1000-50)<18;colors[contours]*=.91
lake=((x-30000)/22000)**2+((y-30000)/17000)**2<1;wet=lake&(h< -300)
colors[wet]=[44,111,133];colors[wet&(h< -850)]=[31,91,119]
im=Image.fromarray(np.uint8(np.clip(colors,0,255)),'RGB');d=ImageDraw.Draw(im)
def pos(x,y):return ((.5+y/(half*2))*(size-1),(.5-x/(half*2))*(size-1))
for index,(tx,ty) in enumerate(data.get('trees',[])):
 u,v=pos(tx,ty);r=2.2+(index%3)*.45
 d.ellipse((u-r+1,v-r+1,u+r+2,v+r+2),fill=(49,71,50));d.ellipse((u-r,v-r,u+r,v+r),fill=[(57,92,59),(64,103,65),(73,111,68)][index%3])
for ai,bi in data['roads']:
 a,b=data['sites'][ai],data['sites'][bi];points=[pos(a['x'],a['y']),pos(b['x'],b['y'])];d.line(points,fill=(49,54,51),width=11);d.line(points,fill=(129,128,110),width=7);d.line(points,fill=(176,166,134),width=1)
for building in data['buildings']:
 u,v=pos(building['x'],building['y']);d.rectangle((u-6,v-5,u+7,v+6),fill=(43,48,42));d.rectangle((u-5,v-4,u+5,v+4),fill=(179,156,119));d.line((u-5,v,u+5,v),fill=(93,88,76),width=1)
path=PROJECT/'SourceArt/Prepared/Reserve/ReserveAtlas.png';im.save(path);print('PF_RESERVE_ATLAS_BAKED',path)
