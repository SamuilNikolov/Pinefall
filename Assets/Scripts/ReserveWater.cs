using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class ReserveWater:MonoBehaviour
    {
        public struct Lake{public Vector2 center,radius;public float level,depth;public string name;public Lake(string n,float x,float z,float rx,float rz,float y,float d){name=n;center=new Vector2(x,z);radius=new Vector2(rx,rz);level=y;depth=d;}}
        public static readonly Lake[] Lakes={new Lake("Mirror Mere",-200,350,260,190,43,10),new Lake("Reedwater Reservoir",1050,-2300,460,320,37,7),new Lake("Alpine Abyss",2300,1700,220,180,108,12),new Lake("Amberwood Anchorage",1500,750,330,220,62,9),new Lake("Silverwater Sound",-1850,1200,290,180,59,8)};
        public static float RiverX(float z){float t=Mathf.InverseLerp(350,-2300,z);return Mathf.Lerp(-200,1050,t)+Mathf.Sin(t*Mathf.PI*2)*180;}
        public static float RiverWidth(float z){return Mathf.Lerp(28,42,Mathf.InverseLerp(350,-2300,z));}
        public static float RiverLevel(float z){return Mathf.Lerp(43,37,Mathf.InverseLerp(350,-2300,z));}
        static float Ellipse(Lake l,float x,float z){return new Vector2((x-l.center.x)/l.radius.x,(z-l.center.y)/l.radius.y).magnitude;}
        public static bool Surface(float x,float z,out float level)
        {foreach(var l in Lakes)if(Ellipse(l,x,z)<1){level=l.level;return true;}if(z<350&&z>-2300&&Mathf.Abs(x-RiverX(z))<RiverWidth(z)){level=RiverLevel(z);return true;}level=-100;return false;}
        public static bool HasSurface(float x,float z){float level;return Surface(x,z,out level);}
        public static float Level(float x,float z){float level;Surface(x,z,out level);return level;}
        public static float Depth(float x,float z){float level;return Surface(x,z,out level)?Mathf.Max(0,level-ReserveWorld.Height(x,z)):0;}
        public static float Carve(float x,float z,float height)
        {
            foreach(var l in Lakes){float q=Ellipse(l,x,z),bank=Mathf.Max(0,q-1)*Mathf.Min(l.radius.x,l.radius.y);if(bank>=105)continue;float bed=q<1?l.level-l.depth*(1-q*q):l.level+bank*.055f;float blend=1-Mathf.SmoothStep(0,1,bank/105);height=Mathf.Lerp(height,bed,blend);}
            if(z<350&&z>-2300){float width=RiverWidth(z),d=Mathf.Abs(x-RiverX(z)),bank=Mathf.Max(0,d-width);if(bank<100){float bed=d<width?RiverLevel(z)-6*(1-Mathf.Pow(d/width,2)):RiverLevel(z)+bank*.05f;float carved=Mathf.Lerp(height,bed,1-Mathf.SmoothStep(0,1,bank/100));bool inLake=false;foreach(var lake in Lakes)if(Ellipse(lake,x,z)<1){inLake=true;break;}height=inLake?Mathf.Min(height,carved):carved;}}
            return height;
        }
        public static Vector3 LootPosition(float x,float z,float offset=.22f){float level;return new Vector3(x,Mathf.Max(ReserveWorld.Height(x,z),Surface(x,z,out level)?level:float.NegativeInfinity)+offset,z);}
        public static Vector3 BoatPoint(Vector2 shore)
        {
            Vector3 best=Vector3.zero;float distance=float.MaxValue;
            for(int r=20;r<=420;r+=15)for(int a=0;a<32;a++){float angle=a/32f*Mathf.PI*2,x=shore.x+Mathf.Cos(angle)*r,z=shore.y+Mathf.Sin(angle)*r;if(Depth(x,z)<2)continue;float d=(new Vector2(x,z)-shore).sqrMagnitude;if(d<distance){best=new Vector3(x,Level(x,z)+.23f,z);distance=d;}}
            return distance<float.MaxValue?best:new Vector3(Lakes[0].center.x,Lakes[0].level+.23f,Lakes[0].center.y);
        }
        Material material;readonly List<Mesh> meshes=new List<Mesh>();
        void Awake()
        {
            material=new Material(Resources.Load<Shader>("ReserveWater"));
            foreach(var l in Lakes){var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();for(int ring=0;ring<=16;ring++)for(int a=0;a<=96;a++){float angle=a/96f*Mathf.PI*2;var p=new Vector3(l.center.x+Mathf.Cos(angle)*l.radius.x*ring/16,l.level,l.center.y+Mathf.Sin(angle)*l.radius.y*ring/16);v.Add(p);uv.Add(new Vector2(p.x/10,p.z/10));if(ring<16&&a<96){int n=ring*97+a;t.AddRange(new[]{n,n+1,n+97,n+1,n+98,n+97});}}Create(l.name,v,uv,t);}
            var points=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();for(int i=0;i<=240;i++){float z=Mathf.Lerp(350,-2300,i/240f);for(int side=0;side<2;side++){float x=RiverX(z)+(side==0?-1:1)*RiverWidth(z);points.Add(new Vector3(x,RiverLevel(z),z));uvs.Add(new Vector2(x/10,z/10));}if(i<240){int n=i*2;tris.AddRange(new[]{n,n+1,n+2,n+1,n+3,n+2});}}Create("Reedwater River",points,uvs,tris);
        }
        void Create(string name,List<Vector3> points,List<Vector2> uv,List<int> triangles){var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);var go=new GameObject(name);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
