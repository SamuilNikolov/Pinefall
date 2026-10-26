using System.Collections.Generic;
using UnityEngine;
namespace Pinefall{
public static class RasterIcons{
static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
public static Texture2D Get(string kind){Texture2D texture;if(cache.TryGetValue(kind,out texture))return texture;const int n=64;var pixels=new Color32[n*n];
System.Action<float,float,float,float> line=(a,b,c,d)=>{Vector2 p=new Vector2(a*n,b*n),q=new Vector2(c*n,d*n),delta=q-p;float length=delta.sqrMagnitude;int left=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(p.x,q.x)-4)),right=Mathf.Min(n-1,Mathf.CeilToInt(Mathf.Max(p.x,q.x)+4)),top=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(p.y,q.y)-4)),bottom=Mathf.Min(n-1,Mathf.CeilToInt(Mathf.Max(p.y,q.y)+4));for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++){var at=new Vector2(x+.5f,y+.5f);float t=Mathf.Clamp01(Vector2.Dot(at-p,delta)/Mathf.Max(.01f,length));byte alpha=(byte)(Mathf.Clamp01(3.1f-Vector2.Distance(at,p+delta*t))*255);int index=(n-1-y)*n+x;if(alpha>pixels[index].a)pixels[index]=new Color32(255,255,255,alpha);}};
System.Action<float,float,float> dot=(a,b,r)=>{float cx=a*n,cy=b*n,radius=r*n*.5f;for(int y=Mathf.Max(0,(int)(cy-radius-1));y<Mathf.Min(n,cy+radius+1);y++)for(int x=Mathf.Max(0,(int)(cx-radius-1));x<Mathf.Min(n,cx+radius+1);x++){byte alpha=(byte)(Mathf.Clamp01(radius+.5f-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(cx,cy)))*255);int index=(n-1-y)*n+x;if(alpha>pixels[index].a)pixels[index]=new Color32(255,255,255,alpha);}};
            if(kind=="tree"||kind=="wood"){line(.12f,.68f,.65f,.15f);line(.28f,.85f,.82f,.31f);line(.12f,.68f,.28f,.85f);line(.65f,.15f,.82f,.31f);line(.30f,.65f,.69f,.26f);line(.38f,.74f,.77f,.35f);}
            else if(kind=="stone"){line(.1f,.77f,.21f,.36f);line(.21f,.36f,.56f,.17f);line(.56f,.17f,.89f,.49f);line(.89f,.49f,.84f,.84f);line(.84f,.84f,.1f,.77f);line(.21f,.36f,.53f,.57f);line(.53f,.57f,.56f,.17f);line(.53f,.57f,.84f,.84f);}
            else if(kind=="metal"||kind=="repair"){line(.18f,.14f,.82f,.14f);line(.18f,.30f,.82f,.30f);line(.18f,.70f,.82f,.70f);line(.18f,.86f,.82f,.86f);line(.4f,.3f,.4f,.7f);line(.6f,.3f,.6f,.7f);line(.18f,.14f,.18f,.3f);line(.82f,.14f,.82f,.3f);line(.18f,.7f,.18f,.86f);line(.82f,.7f,.82f,.86f);}
            else if(kind=="player"){dot(.5f,.24f,.30f);line(.27f,.49f,.73f,.49f);line(.27f,.49f,.16f,.86f);line(.73f,.49f,.84f,.86f);line(.16f,.86f,.84f,.86f);}
            else if(kind=="storm"){line(.60f,.08f,.27f,.52f);line(.27f,.52f,.56f,.52f);line(.56f,.52f,.39f,.91f);line(.39f,.91f,.82f,.36f);line(.82f,.36f,.52f,.36f);line(.52f,.36f,.60f,.08f);}
            else if(kind=="ammo"){line(.21f,.79f,.21f,.28f);line(.21f,.28f,.31f,.11f);line(.31f,.11f,.41f,.28f);line(.41f,.28f,.41f,.79f);line(.21f,.79f,.41f,.79f);line(.60f,.79f,.60f,.28f);line(.60f,.28f,.70f,.11f);line(.70f,.11f,.80f,.28f);line(.80f,.28f,.80f,.79f);line(.60f,.79f,.80f,.79f);}
            else if(kind=="food"){dot(.60f,.35f,.58f);line(.46f,.54f,.18f,.83f);line(.12f,.72f,.26f,.88f);}
            else if(kind=="health"){line(.15f,.5f,.85f,.5f);line(.5f,.15f,.5f,.85f);}
            else if(kind=="shield"){line(.15f,.16f,.85f,.16f);line(.15f,.16f,.22f,.64f);line(.85f,.16f,.78f,.64f);line(.22f,.64f,.5f,.90f);line(.78f,.64f,.5f,.90f);}
            else if(kind=="paw"){dot(.5f,.68f,.39f);dot(.18f,.36f,.21f);dot(.39f,.22f,.21f);dot(.64f,.23f,.21f);dot(.83f,.40f,.21f);}
            else if(kind=="house"){line(.1f,.43f,.5f,.1f);line(.5f,.1f,.9f,.43f);line(.2f,.36f,.2f,.89f);line(.8f,.36f,.8f,.89f);line(.2f,.89f,.8f,.89f);line(.45f,.89f,.45f,.62f);line(.60f,.89f,.60f,.62f);}
            else if(kind=="boat"){line(.1f,.6f,.27f,.85f);line(.27f,.85f,.73f,.85f);line(.73f,.85f,.9f,.6f);line(.1f,.6f,.9f,.6f);line(.5f,.6f,.5f,.1f);line(.5f,.1f,.8f,.49f);line(.5f,.49f,.8f,.49f);}
            else if(kind=="bike"){dot(.21f,.72f,.30f);dot(.79f,.72f,.30f);line(.21f,.72f,.43f,.32f);line(.43f,.32f,.79f,.72f);line(.36f,.42f,.65f,.42f);line(.65f,.42f,.72f,.19f);}
            else if(kind=="car"){line(.1f,.70f,.9f,.70f);line(.1f,.70f,.16f,.40f);line(.16f,.40f,.84f,.40f);line(.84f,.40f,.9f,.70f);line(.25f,.40f,.35f,.16f);line(.35f,.16f,.65f,.16f);line(.65f,.16f,.75f,.40f);dot(.24f,.80f,.20f);dot(.76f,.80f,.20f);}
            else if(kind=="crate"){line(.18f,.20f,.82f,.20f);line(.82f,.20f,.82f,.85f);line(.82f,.85f,.18f,.85f);line(.18f,.85f,.18f,.20f);line(.5f,.2f,.5f,.85f);line(.18f,.44f,.82f,.44f);}
            else if(kind=="radio"){line(.3f,.88f,.3f,.35f);line(.3f,.35f,.7f,.35f);line(.7f,.35f,.7f,.88f);line(.3f,.88f,.7f,.88f);line(.4f,.35f,.4f,.08f);line(.4f,.08f,.85f,.08f);dot(.5f,.64f,.14f);}
            else if(kind=="sprint"){dot(.6f,.13f,.20f);line(.55f,.30f,.4f,.58f);line(.4f,.58f,.13f,.83f);line(.4f,.58f,.73f,.64f);line(.73f,.64f,.83f,.90f);line(.55f,.30f,.80f,.45f);line(.5f,.33f,.23f,.35f);}
            else {for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;line(.5f+Mathf.Cos(a)*.36f,.5f+Mathf.Sin(a)*.36f,.5f+Mathf.Cos(b)*.36f,.5f+Mathf.Sin(b)*.36f);}}
texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="HUD icon "+kind,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};texture.SetPixels32(pixels);texture.Apply(false,true);cache.Add(kind,texture);return texture;
}}}
