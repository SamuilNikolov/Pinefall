using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveWorld
    {
        readonly List<Object> fineAssets=new List<Object>();
        static float[] padHeights;
        public static float Height(float x,float z)
        {
            if(Instance&&Instance.sampledHeights!=null)return Instance.SampleCachedHeight(x,z);
            if(padHeights==null){padHeights=new float[ExpeditionLayout.Regions.Length];for(int i=0;i<ExpeditionLayout.Regions.Length;i++){var p=ExpeditionLayout.Regions[i].center;padHeights[i]=NaturalHeight(p.x,p.y);}}
            float h=NaturalHeight(x,z),distance=9999;Vector2 point=new Vector2(x,z),projection=point,along=Vector2.right;
            for(int i=0;i<ExpeditionLayout.Roads.GetLength(0);i++)
            {int a=ExpeditionLayout.Roads[i,0],b=ExpeditionLayout.Roads[i,1];float t;float d=ExpeditionLayout.SegmentDistance(point,ExpeditionLayout.Regions[a].center,ExpeditionLayout.Regions[b].center,out t);if(d<distance){distance=d;projection=Vector2.Lerp(ExpeditionLayout.Regions[a].center,ExpeditionLayout.Regions[b].center,t);along=(ExpeditionLayout.Regions[b].center-ExpeditionLayout.Regions[a].center).normalized;}}
            if(distance<14){float roadH=(NaturalHeight(projection.x-along.x*10,projection.y-along.y*10)+NaturalHeight(projection.x,projection.y)*2+NaturalHeight(projection.x+along.x*10,projection.y+along.y*10))*.25f;roadH=Mathf.Clamp(roadH,h-1.25f,h+1.25f);h=Mathf.Lerp(h,roadH,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,14,distance)));}
            for(int i=0;i<ExpeditionLayout.Regions.Length;i++){float d=Vector2.Distance(point,ExpeditionLayout.Regions[i].center);h=Mathf.Lerp(h,padHeights[i],1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(ExpeditionLayout.Regions[i].minor?62:100,Mathf.Max(ExpeditionLayout.Regions[i].minor?140:170,(ExpeditionLayout.Regions[i].minor?62:100)+Mathf.Abs(h-padHeights[i])*3),d)));}
            return ReserveWater.Carve(x,z,h);
        }
        void CreateUnderstory()
        {
            Shader.SetGlobalTexture("_RoadCoverage",Resources.Load<Texture2D>("RoadCoverage"));
            var reeds=Reeds();fineAssets.Add(reeds);Mesh[] meshes={Sprays(false),Sprays(true),Flowers(),rockMesh};foreach(var mesh in meshes)if(mesh!=rockMesh)fineAssets.Add(mesh);
            Color[,] colors={{new Color(.29f,.40f,.18f),new Color(.36f,.47f,.20f),new Color(.76f,.65f,.75f),new Color(.43f,.46f,.37f)},
                {new Color(.45f,.39f,.21f),new Color(.63f,.51f,.27f),new Color(.85f,.68f,.24f),new Color(.55f,.42f,.29f)},
                {new Color(.37f,.43f,.37f),new Color(.65f,.72f,.66f),new Color(.80f,.88f,.94f),new Color(.72f,.79f,.80f)},
                {new Color(.27f,.35f,.23f),new Color(.46f,.47f,.28f),new Color(.73f,.61f,.86f),new Color(.47f,.49f,.48f)},
                {new Color(.16f,.36f,.17f),new Color(.24f,.44f,.16f),new Color(.94f,.42f,.27f),new Color(.29f,.37f,.26f)},
                {new Color(.31f,.43f,.23f),new Color(.27f,.40f,.20f),new Color(.83f,.80f,.46f),new Color(.33f,.40f,.36f)}};
            var materials=new Material[6,4];for(int b=0;b<6;b++)for(int k=0;k<4;k++){var m=new Material(Shader.Find("Pinefall/Nature")){color=colors[b,k],enableInstancing=true};m.SetFloat("_Wind",k==3?0:.035f);materials[b,k]=m;fineAssets.Add(m);}
            var groups=new Dictionary<int,List<Matrix4x4>>();var rng=new System.Random(Seed+731);
            // Patch noise leaves navigable gaps and makes dense cover feel like a habitat.
            for(int i=0;i<850000;i++)
            {
                float x=(float)(rng.NextDouble()-.5)*(Size-65),z=(float)(rng.NextDouble()-.5)*(Size-65);if(Clearing(x,z)||ReserveWater.HasSurface(x,z))continue;
                int biome=ReserveBiomes.At(x,z);float patch=Mathf.PerlinNoise(x*.027f+81,z*.027f+16);if(patch<.38f)continue;
                int kind=rng.Next(10)<3?3:rng.Next(10)<3?2:rng.Next(2);
                if(biome==2&&kind!=3&&rng.NextDouble()<.94)continue;if(biome==1&&kind!=3&&rng.NextDouble()<.70)continue;
                float scale=kind==3?.15f+(float)rng.NextDouble()*.6f:kind==2?.35f+(float)rng.NextDouble()*.4f:.5f+(float)rng.NextDouble()*.9f;
                if(biome==4&&kind<2)scale*=1.55f;
                int key=Cell(x,z)+(biome*4+kind)*4096;if(!groups.ContainsKey(key))groups[key]=new List<Matrix4x4>();
                groups[key].Add(Matrix4x4.TRS(OnGround(x,z,-.03f),Quaternion.Euler(0,rng.Next(360),0),new Vector3(scale,scale*(.75f+(float)rng.NextDouble()*.5f),scale)));
            }
            // Dense reed beds follow the wetland shore rather than filling the navigable lake.
            var marsh=ReserveWater.Lakes[1];for(int i=0;i<18000;i++){float angle=(float)rng.NextDouble()*Mathf.PI*2,ring=.96f+(float)rng.NextDouble()*.18f;float x=marsh.center.x+Mathf.Cos(angle)*marsh.radius.x*ring,z=marsh.center.y+Mathf.Sin(angle)*marsh.radius.y*ring;if(Clearing(x,z)||ReserveWater.Depth(x,z)>.65f)continue;int key=Cell(x,z)+20*4096;if(!groups.ContainsKey(key))groups[key]=new List<Matrix4x4>();float scale=.85f+(float)rng.NextDouble()*.65f;groups[key].Add(Matrix4x4.TRS(OnGround(x,z),Quaternion.Euler(0,rng.Next(360),0),Vector3.one*scale));}
            var fern=Resources.Load<GameObject>("RealFern");Material fernMaterial=null;if(fern){var renderer=fern.GetComponentInChildren<MeshRenderer>();fernMaterial=new Material(Shader.Find("Pinefall/Nature")){color=new Color(.73f,.90f,.69f),enableInstancing=true,mainTexture=renderer?renderer.sharedMaterial.mainTexture:null};fernMaterial.SetFloat("_Wind",.02f);fineAssets.Add(fernMaterial);}
            var realGrass=Resources.Load<GameObject>("RealGrass");
            var grassFilters=realGrass?realGrass.GetComponentsInChildren<MeshFilter>():new MeshFilter[0];
            var grassMaterials=new Material[grassFilters.Length];
            for(int i=0;i<grassFilters.Length;i++)
            {
                var renderer=grassFilters[i].GetComponent<Renderer>();if(!renderer)continue;
                var material=new Material(Shader.Find("Pinefall/Foliage")){mainTexture=renderer.sharedMaterial.mainTexture,color=Color.white,enableInstancing=true};
                material.SetTexture("_AlphaTex",Resources.Load<Texture2D>("GrassOpacity"));material.SetFloat("_Wind",.04f);fineAssets.Add(material);grassMaterials[i]=material;
            }
            foreach(var entry in groups)
            {
                int type=entry.Key/4096,b=type/4,k=type%4;
                if(k==0&&(b==0||b==4)&&realGrass)
                {
                    for(int i=0;i<grassFilters.Length;i++)
                    {
                        var filter=grassFilters[i];var mat=grassMaterials[i];if(!mat)continue;
                        var matrices=new List<Matrix4x4>(entry.Value.Count);foreach(var matrix in entry.Value)matrices.Add(matrix*filter.transform.localToWorldMatrix);AddBatch(filter.sharedMesh,mat,matrices,110);
                    }
                }
                else if(k==1&&(b==0||b==4||b==5)&&fern)
                {
                    foreach(var filter in fern.GetComponentsInChildren<MeshFilter>()){var renderer=filter.GetComponent<Renderer>();if(!renderer||renderer.sharedMaterials.Length==0)continue;var offset=filter.transform.localToWorldMatrix;var matrices=new List<Matrix4x4>(entry.Value.Count);foreach(var matrix in entry.Value)matrices.Add(matrix*offset);AddBatch(filter.sharedMesh,fernMaterial,matrices,145);}
                }
                else AddBatch(b==5&&k==0?reeds:meshes[k],materials[b,k],entry.Value,k==3?260:k==2?120:b==5?230:180);
            }
        }
        static Mesh Sprays(bool fern)
        {
            var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
            for(int branch=0;branch<(fern?9:13);branch++)
            {
                float a=branch*2.39996f;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=new Vector3(-dir.z,0,dir.x);
                int segments=fern?5:3;float length=fern?.9f:.65f;
                for(int j=0;j<segments;j++)
                {
                    float u=j/(float)segments,u2=(j+1f)/segments;Vector3 p=dir*u*length+Vector3.up*(fern?Mathf.Sin(u*2.3f)*.7f:u*.95f);
                    Vector3 q=dir*u2*length+Vector3.up*(fern?Mathf.Sin(u2*2.3f)*.7f:u2*.95f);float width=(fern?.22f:.28f)*(1-u2*.8f);int n=v.Count;
                    v.Add(p-side*width);v.Add(p+Vector3.up*.045f);v.Add(p+side*width);v.Add(q);t.AddRange(new[]{n,n+1,n+3,n+1,n+2,n+3});
                    for(int k=0;k<4;k++)colors.Add(Color.white*(.7f+(branch%4)*.1f));
                }
            }return MeshOf(fern?"Fern fronds":"Leafy scrub",v,t,colors);
        }
        static Mesh Reeds()
        {
            var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
            for(int stalk=0;stalk<9;stalk++){float angle=stalk*2.39996f,height=1.1f+(stalk%4)*.18f;Vector3 root=new Vector3(Mathf.Cos(angle)*.44f,0,Mathf.Sin(angle)*.44f),top=root+Vector3.up*height+new Vector3(.08f,0,.04f);
                for(int face=0;face<2;face++){Vector3 side=face==0?Vector3.right:Vector3.forward;int n=v.Count;v.AddRange(new[]{root-side*.015f,root+side*.015f,top+side*.012f,top-side*.012f});t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});for(int k=0;k<4;k++)colors.Add(new Color(.64f,.73f,.42f));n=v.Count;v.AddRange(new[]{top-side*.045f,top+side*.045f,top+side*.04f+Vector3.up*.21f,top-side*.04f+Vector3.up*.21f});t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});for(int k=0;k<4;k++)colors.Add(new Color(.43f,.30f,.16f));}
                for(int leaf=0;leaf<3;leaf++){float y=.32f+leaf*.21f;Vector3 side=new Vector3(Mathf.Cos(angle+leaf*2),0,Mathf.Sin(angle+leaf*2));int n=v.Count;v.Add(root+Vector3.up*y);v.Add(root+Vector3.up*y+new Vector3(-side.z,0,side.x)*.06f);v.Add(root+Vector3.up*(y+.28f)+side*.5f);t.AddRange(new[]{n,n+1,n+2});colors.AddRange(new[]{new Color(.53f,.70f,.32f),new Color(.53f,.70f,.32f),new Color(.77f,.81f,.42f)});}}
            return MeshOf("Marsh reeds",v,t,colors);
        }
        static Mesh Flowers()
        {
            var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
            for(int plant=0;plant<4;plant++)
            {Vector3 baseP=new Vector3(Mathf.Sin(plant*2.4f)*.35f,0,Mathf.Cos(plant*2.4f)*.35f),top=baseP+Vector3.up*(.5f+plant*.09f);int n=v.Count;v.Add(baseP-Vector3.right*.013f);v.Add(baseP+Vector3.right*.013f);v.Add(top);t.AddRange(new[]{n,n+1,n+2});colors.AddRange(new[]{new Color(.26f,.46f,.20f),new Color(.26f,.46f,.20f),Color.white});
                for(int petal=0;petal<6;petal++){float a=petal/6f*Mathf.PI*2;Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=new Vector3(-d.z,0,d.x);n=v.Count;v.Add(top);v.Add(top+d*.14f-side*.065f+Vector3.up*.035f);v.Add(top+d*.19f);v.Add(top+d*.14f+side*.065f+Vector3.up*.035f);t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});for(int i=0;i<4;i++)colors.Add(Color.white);}}
            return MeshOf("Wildflower cluster",v,t,colors);
        }
    }
}
