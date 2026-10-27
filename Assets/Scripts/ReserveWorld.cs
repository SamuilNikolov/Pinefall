using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinefall
{
    public sealed partial class ReserveWorld : MonoBehaviour
    {
        public const float Size = 6144f;
        public const int Seed = 7319;
        public const float Edge=Size*.48f,LayoutScale=Size/1536f;
        public static ReserveWorld Instance;
        public Terrain ground;
        public Material pine, bark, stone, grass,palm,cactus;
        public Vector3 spawn = new Vector3(0, 0, -160);
        public readonly List<Vector3> trees = new List<Vector3>();
        readonly List<Batch> batches = new List<Batch>();
        Material autumn;Mesh broadleafMesh,crownMesh, trunkMesh, rockMesh,palmMesh,cactusMesh;
        public Texture2D map;
        class Batch { public Mesh mesh; public Material mat; public Matrix4x4[] matrices; public Vector3 center; public float distance; public Bounds bounds; }

        static float NaturalHeight(float x, float z)
        {
            float wx=x,wz=z;x/=Size/2304f;z/=Size/2304f;
            float n = Mathf.PerlinNoise((x+2701)*.0028f,(z+1911)*.0028f);
            float fine = Mathf.PerlinNoise((x+471)*.012f,(z+881)*.012f);
            float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(495, 1152, new Vector2(x,z).magnitude));
            float hill = 23 + n*31 + fine*4 + edge*(30+55*Mathf.PerlinNoise(x*.003f+8,z*.003f+3));
            float meadow = Mathf.Exp(-Mathf.Pow(x/130,2)) * Mathf.Exp(-Mathf.Pow((z+65)/260,2));
            hill = Mathf.Lerp(hill, 35 + Mathf.Sin(z*.007f)*3 + fine*2, meadow*.88f);
            float mesa=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.68f,Mathf.PerlinNoise(wx*.003f+71,wz*.003f+12)));float desert=31+n*17+Mathf.Sin(x*.032f+z*.019f)*4+mesa*47;
            float ice=52+n*15+fine*3;
            float ridge=1-Mathf.Abs(Mathf.PerlinNoise(wx*.0026f+12,wz*.0026f+42)*2-1);float rockies=60+Mathf.Pow(ridge,3)*150+fine*18;
            rockies+=110*Mathf.Exp(-((wx-1850)*(wx-1850)+(wz-1900)*(wz-1900))/145000f);
            float jungle=31+n*45+fine*8;
            float a,b,c,d,e,f;ReserveBiomes.Weights(wx,wz,out a,out b,out c,out d,out e,out f);hill=hill*a+desert*b+ice*c+rockies*d+jungle*e+(32+n*8+fine)*f;float rough=(Mathf.PerlinNoise(wx*.035f+6,wz*.035f+19)-.5f)*2.8f;hill+=rough*(.25f+d*.9f+e*.5f);
            float camp = 1-Mathf.SmoothStep(0, 1, Mathf.InverseLerp(14,48,Vector2.Distance(new Vector2(x,z),new Vector2(0,-160))));
            return Mathf.Lerp(hill, 36, camp)+CaveLayout.ExteriorLift(wx,wz);
        }
        public static float UnmodifiedHeight(float x,float z){return NaturalHeight(x,z);}
        public static Vector3 OnGround(float x, float z, float offset=0) { return new Vector3(x, Height(x,z)+offset,z); }
        public static float PathX(float z) { return Mathf.Sin((z+160)*.011f)*13; }
        public static bool Clearing(float x, float z)
        {
            return ExpeditionLayout.IsClearing(x,z)||Vector2.Distance(new Vector2(x,z),new Vector2(0,-160))<34 || Mathf.Abs(x-PathX(z))<7 && z>-190 && z<370;
        }
        void Awake()
        {
            Instance=this;HarvestResources.Clear();CacheTerrainHeights();
            spawn=OnGround(0,-160, .2f);
            CreateFoliage();CreateUnderstory();PrepareLowGrass();gameObject.AddComponent<ReserveWater>();gameObject.AddComponent<AmbientBirds>();
            map=Resources.Load<Texture2D>("DetailedAtlas");if(!map)map=MakeMap();
        }
        void CreateFoliage()
        {
            broadleafMesh=MakeBroadleaf();crownMesh=MakePine(); trunkMesh=MakeTrunk(); rockMesh=MakeRock();palmMesh=MakePalm();cactusMesh=MakeCactus();
            autumn=new Material(pine);autumn.color=new Color(.72f,.39f,.12f);fineAssets.Add(autumn);var rng=new System.Random(Seed);
            var groups=new Dictionary<int,List<Matrix4x4>>();
            var rocks=new Dictionary<int,List<Matrix4x4>>();var treeNodes=new Dictionary<int,List<HarvestNode>>();var rockNodes=new Dictionary<int,List<HarvestNode>>();
            for(int i=0;i<88000;i++)
            {
                float x=(float)(rng.NextDouble()-.5)*(Size-56), z=(float)(rng.NextDouble()-.5)*(Size-56);
                float forest=Mathf.PerlinNoise(x*.009f+40,z*.009f+76);
                int biome=ReserveBiomes.At(x,z);
                float meadow=Mathf.Exp(-Mathf.Pow(x/130,2))*Mathf.Exp(-Mathf.Pow((z+55)/300,2));
                if(Clearing(x,z)||ReserveWater.HasSurface(x,z) || forest<.38f || rng.NextDouble()<meadow*.9f)continue;
                if(biome==1&&rng.NextDouble()<.92 || biome==2&&rng.NextDouble()<.96 || biome==3&&Height(x,z)>119)continue;
                float h=Height(x,z); float s= .7f+(float)rng.NextDouble()*.8f;
                if(biome==1)s*=.4f;
                var pos=new Vector3(x,h-.08f,z); trees.Add(pos);
                int key=Cell(x,z)+(ReserveBiomes.Autumn(x,z)?6:biome)*4096; if(!groups.ContainsKey(key))groups[key]=new List<Matrix4x4>();
                groups[key].Add(Matrix4x4.TRS(pos,Quaternion.Euler(0,(float)rng.NextDouble()*360,0),new Vector3(s,s*(.9f+(float)rng.NextDouble()*.3f),s)));
                var go=new GameObject("Pine trunk"); go.transform.SetParent(transform); go.transform.position=pos;
                var col=go.AddComponent<CapsuleCollider>(); col.radius=.32f*s; col.height=10*s; col.center=new Vector3(0,5*s,0);if(!treeNodes.ContainsKey(key))treeNodes[key]=new List<HarvestNode>();treeNodes[key].Add(HarvestResources.Register(col,0,150));
            }
            for(int i=0;i<5600;i++)
            {
                float x=(float)(rng.NextDouble()-.5)*(Size-80),z=(float)(rng.NextDouble()-.5)*(Size-80);
                if(Clearing(x,z)) continue;
                int key=Cell(x,z)+(i%7==0?4096:0); if(!rocks.ContainsKey(key))rocks[key]=new List<Matrix4x4>();
                float s=.6f+(float)rng.NextDouble()*2.8f;
                var p=OnGround(x,z,-.2f); rocks[key].Add(Matrix4x4.TRS(p,Quaternion.Euler((float)rng.NextDouble()*20,(float)rng.NextDouble()*360,0),new Vector3(s,s*.65f,s*.8f)));
                var g=new GameObject("Boulder");g.transform.SetParent(transform);g.transform.position=p;
                var c=g.AddComponent<SphereCollider>();c.radius=s*.75f;c.center=Vector3.up*s*.3f;if(!rockNodes.ContainsKey(key))rockNodes[key]=new List<HarvestNode>();rockNodes[key].Add(HarvestResources.Register(c,i%7==0?2:1,i%7==0?210:135));
            }
            foreach(var pair in groups)
            {
                int biome=pair.Key/4096;
                if(biome==1)AddBatch(cactusMesh,cactus,pair.Value,650,treeNodes[pair.Key]);
                else {if((biome==0||biome==3||biome==5)&&Resources.Load<Material>("LivingFir"))AddLivingFirs(pair.Value,treeNodes[pair.Key]);else if(biome!=2)AddBatch(biome==6?broadleafMesh:biome==4?palmMesh:crownMesh,biome==6?autumn:biome==4?palm:pine,pair.Value,850,treeNodes[pair.Key]);AddBatch(trunkMesh,bark,pair.Value,620,treeNodes[pair.Key]);}
            }
            foreach(var pair in rocks) AddBatch(rockMesh,pair.Key>=4096?BuildGeometry.Surface(2):stone,pair.Value,650,rockNodes[pair.Key]);
        }
        int Cell(float x,float z) { return Mathf.FloorToInt((x+Size/2)/128)+Mathf.FloorToInt((z+Size/2)/128)*64; }
        void AddBatch(Mesh m,Material mat,List<Matrix4x4> list,float distance,List<HarvestNode> nodes=null)
        {
            if(!mat.enableInstancing)mat.enableInstancing=true;
            for(int start=0;start<list.Count;start+=1023)
            {
                int count=Mathf.Min(1023,list.Count-start);var a=list.GetRange(start,count).ToArray();HarvestResources.Bind(nodes,start,count,a);
                Vector3 c=Vector3.zero;foreach(var tr in a)c+=(Vector3)tr.GetColumn(3);c/=count;
                var bounds=new Bounds(c,Vector3.zero);foreach(var matrix in a)bounds.Encapsulate((Vector3)matrix.GetColumn(3));bounds.Expand(36);batches.Add(new Batch{mesh=m,mat=mat,matrices=a,center=c,distance=distance,bounds=bounds});
            }
        }
        void LateUpdate()
        {
            var cam=Camera.main;if(!cam||!SystemInfo.supportsInstancing)return;
            DrawFoliage(cam,null);
        }
        public void DrawAtlasFoliage(Camera camera)
        {DrawLivingFirs(camera,camera,true);foreach(var batch in batches)if(batch.distance>=500)Graphics.DrawMeshInstanced(batch.mesh,0,batch.mat,batch.matrices,batch.matrices.Length,null,ShadowCastingMode.On,true,0,camera,LightProbeUsage.Off);}
        readonly Plane[] drawPlanes=new Plane[6];
        public void DrawFoliage(Camera cam,Camera renderCamera)
        {
            if(!SystemInfo.supportsInstancing)return;
            float q=(PlayerPrefs.GetInt("quality",1)==0?.65f:1)*(.5f+RuntimePerformance.Detail*.5f);GeometryUtility.CalculateFrustumPlanes(cam,drawPlanes);float shadowRange=QualitySettings.shadowDistance+128;Vector3 cameraPosition=cam.transform.position;
            DrawLivingFirs(cam,renderCamera);DrawLowGrass(cam,renderCamera);
            foreach(var b in batches)
            {
                float distanceSquared=(b.center-cameraPosition).sqrMagnitude,range=b.distance*q+95;if(distanceSquared>range*range)continue;if((b.distance<=260||distanceSquared>shadowRange*shadowRange)&&!GeometryUtility.TestPlanesAABB(drawPlanes,b.bounds))continue;
                Graphics.DrawMeshInstanced(b.mesh,0,b.mat,b.matrices,b.matrices.Length,null,b.distance<=260||distanceSquared>shadowRange*shadowRange?ShadowCastingMode.Off:ShadowCastingMode.On,true,0,renderCamera,LightProbeUsage.Off);
            }
        }
        public Texture2D MakeMap()
        {
            const int n=2048;var texture=new Texture2D(n,n,TextureFormat.RGB24,true){filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,anisoLevel=4};var pixels=new Color32[n*n];int res=ground.terrainData.heightmapResolution;var heights=ground.terrainData.GetHeights(0,0,res,res);float sy=ground.terrainData.size.y;
            Color[] palette={new Color(.36f,.50f,.32f),new Color(.66f,.55f,.37f),new Color(.73f,.82f,.85f),new Color(.43f,.47f,.47f),new Color(.16f,.34f,.24f),new Color(.28f,.45f,.36f)};
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float wx=(x/(n-1f)-.5f)*Size,wz=(y/(n-1f)-.5f)*Size;int hx=Mathf.Clamp(Mathf.RoundToInt(x/(n-1f)*(res-1)),1,res-2),hz=Mathf.Clamp(Mathf.RoundToInt(y/(n-1f)*(res-1)),1,res-2);float h=heights[hz,hx]*sy;
                Vector3 normal=new Vector3((heights[hz,hx-1]-heights[hz,hx+1])*sy,Size/(res-1f)*2,(heights[hz-1,hx]-heights[hz+1,hx])*sy).normalized;float relief=.82f+Mathf.Max(0,Vector3.Dot(normal,new Vector3(-.5f,.75f,.45f).normalized))*.24f;
                float a,b,c,d,e,f;ReserveBiomes.Weights(wx,wz,out a,out b,out c,out d,out e,out f);float forest=Mathf.PerlinNoise(wx*.009f+40,wz*.009f+76);Color color=(palette[0]*a+palette[1]*b+palette[2]*c+palette[3]*d+palette[4]*e+palette[5]*f)*relief;color*=Mathf.Lerp(1,Mathf.Lerp(1.06f,.72f,Mathf.InverseLerp(.35f,.73f,forest)),a+e+f);color=Color.Lerp(color,new Color(.79f,.83f,.82f),d*Mathf.InverseLerp(110,157,h));
                float contour=h%10;if(contour<.28f)color*=.82f;float level;if(ReserveWater.Surface(wx,wz,out level))color=Color.Lerp(new Color(.35f,.58f,.58f),new Color(.10f,.29f,.41f),Mathf.InverseLerp(0,9,level-h));pixels[y*n+x]=color;
            }
            for(int pass=0;pass<2;pass++)for(int road=0;road<ExpeditionLayout.Roads.GetLength(0);road++)
            {
                Vector2 a=ExpeditionLayout.Regions[ExpeditionLayout.Roads[road,0]].center,b=ExpeditionLayout.Regions[ExpeditionLayout.Roads[road,1]].center;Vector2 pa=(a/Size+Vector2.one*.5f)*(n-1),pb=(b/Size+Vector2.one*.5f)*(n-1);int steps=Mathf.CeilToInt(Vector2.Distance(pa,pb)*2);
                for(int j=0;j<=steps;j++){var p=Vector2.Lerp(pa,pb,j/(float)Mathf.Max(1,steps));int px=Mathf.RoundToInt(p.x),py=Mathf.RoundToInt(p.y);if(pass==0){for(int oy=-2;oy<=2;oy++)for(int ox=-2;ox<=2;ox++){int x=px+ox,y=py+oy;if(x>=0&&x<n&&y>=0&&y<n)pixels[x+y*n]=new Color(.18f,.22f,.20f);}}else for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++){int x=px+ox,y=py+oy;if(x>=0&&x<n&&y>=0&&y<n)pixels[x+y*n]=new Color(.82f,.75f,.55f);}}
            }
            texture.SetPixels32(pixels);texture.Apply(true);return texture;
        }
        public static Mesh MakePine()
        {
            var vs=new List<Vector3>();var ts=new List<int>();var colors=new List<Color>();var uv=new List<Vector2>();
            var rng=new System.Random(35);
            for(int tier=0;tier<11;tier++)
            {
                float y=1.4f+tier*.82f,r=3.5f*(1-tier/12f);
                int branches=9;
                for(int j=0;j<branches;j++)
                {
                    float a=j/(float)branches*Mathf.PI*2+tier*.77f+(float)rng.NextDouble()*.25f;
                    var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-radial.z,0,radial.x);
                    float length=r*(.8f+(float)rng.NextDouble()*.35f);
                    for(int spray=0;spray<2;spray++)
                    {
                        var start=radial*length*(spray==0?.05f:.40f)+Vector3.up*(y+(float)rng.NextDouble()*.24f);
                        var end=radial*length+Vector3.up*(y+.55f+(float)rng.NextDouble()*.55f);
                        var across=spray==0?side:Vector3.Lerp(side,Vector3.up,.65f).normalized;
                        float width=length*(spray==0?.46f:.38f)+.2f;int k=vs.Count;
                        vs.Add(start-across*width);vs.Add(start+across*width);vs.Add(end-across*width*.18f);vs.Add(end+across*width*.18f);
                        uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)});
                        ts.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});
                        float tint=.74f+(float)rng.NextDouble()*.40f;for(int c=0;c<4;c++)colors.Add(new Color(tint,tint,tint,1));
                    }
                }
            }
            var mesh=MeshOf("Pine boughs",vs,ts,colors);mesh.SetUVs(0,uv);return mesh;
        }
        public static Mesh MakeTrunk()
        {
            var vs=new List<Vector3>();var ts=new List<int>();
            for(int i=0;i<9;i++)
            {
                float a=i/8f*Mathf.PI*2;vs.Add(new Vector3(Mathf.Cos(a)*.32f,0,Mathf.Sin(a)*.32f));vs.Add(new Vector3(Mathf.Cos(a)*.13f,10,Mathf.Sin(a)*.13f));
                if(i<8){int k=i*2;ts.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
            }
            return MeshOf("Pine trunk",vs,ts,null);
        }
        public static Mesh MakePalm()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<18;i++)
            {
                float a=i/18f*Mathf.PI*2;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=new Vector3(-dir.z,0,dir.x);float length=3.5f+(i%3)*.55f;
                for(int k=0;k<6;k++)
                {
                    float u=k/6f,w=(1-u)*.65f;Vector3 p=dir*(length*u)+Vector3.up*(9.2f+Mathf.Sin(u*Mathf.PI)*1.1f-u*1.7f);
                    float u2=(k+1)/6f;Vector3 p2=dir*(length*u2)+Vector3.up*(9.2f+Mathf.Sin(u2*Mathf.PI)*1.1f-u2*1.7f);int n=v.Count;
                    v.Add(p-side*w);v.Add(p+side*w);v.Add(p2-side*w*.7f);v.Add(p2+side*w*.7f);t.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
                }
            }
            return MeshOf("Rainforest fronds",v,t,null);
        }
        public static Mesh MakeCactus()
        {
            var v=new List<Vector3>();var t=new List<int>();
            var paths=new[]{new[]{Vector3.zero,new Vector3(0,8,0)},new[]{new Vector3(0,3,0),new Vector3(2,3.5f,0),new Vector3(2,6.5f,0)},new[]{new Vector3(0,4.5f,0),new Vector3(-1.6f,5,0),new Vector3(-1.6f,7.5f,0)}};
            foreach(var path in paths)
            {
                int start=v.Count;for(int j=0;j<path.Length;j++)for(int k=0;k<9;k++){float a=k/8f*Mathf.PI*2;v.Add(path[j]+new Vector3(Mathf.Cos(a)*.38f,0,Mathf.Sin(a)*.38f));}
                for(int j=0;j<path.Length-1;j++)for(int k=0;k<8;k++){int n=start+j*9+k;t.AddRange(new[]{n,n+9,n+1,n+1,n+9,n+10});}
            }
            return MeshOf("Dune cactus",v,t,null);
        }
        public static Mesh MakeRock()
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere); var m=Instantiate(g.GetComponent<MeshFilter>().sharedMesh);DestroyImmediate(g);
            var v=m.vertices;
            for(int i=0;i<v.Length;i++){float n=Mathf.PerlinNoise(v[i].x*9+1,v[i].z*9+7);v[i]*=1.5f+n*.7f;v[i].y=Mathf.Max(-.15f,v[i].y);}
            m.vertices=v;m.RecalculateNormals();m.RecalculateBounds();m.name="Weathered stone";return m;
        }
        static Mesh MeshOf(string name,List<Vector3> v,List<int> t,List<Color> c)
        {
            var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);if(c!=null)m.SetColors(c);
            var uv=new Vector2[v.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(v[i].x*.3f,v[i].y*.2f);m.uv=uv;m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        void OnDestroy()
        {
            foreach(var asset in fineAssets)if(asset)Destroy(asset);
            if(crownMesh)Destroy(crownMesh);if(trunkMesh)Destroy(trunkMesh);if(rockMesh)Destroy(rockMesh);if(map)Destroy(map);
            if(palmMesh)Destroy(palmMesh);if(cactusMesh)Destroy(cactusMesh);
            if(Instance==this)Instance=null;
        }
    }
}
