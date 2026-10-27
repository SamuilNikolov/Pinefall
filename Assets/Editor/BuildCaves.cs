using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Pinefall;

public static partial class BuildReserve
{
    static readonly int[,] CaveTetra={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};
    static readonly int[,] CaveEdges={{0,1},{0,2},{0,3},{1,2},{1,3},{2,3}};
    static float CaveDensity(float y,float ground,Vector3 profile)
    {return Mathf.Min(ground-y,-Mathf.Min(profile.z,Mathf.Min(y-profile.x,profile.y-y)));}
    static void CreateCaves(Terrain terrain)
    {
        var data=terrain.terrainData;int resolution=data.holesResolution;float cell=ReserveWorld.Size/resolution;
        var holes=new bool[resolution,resolution];for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)holes[z,x]=true;
        var stone=new Material(Shader.Find("Pinefall/CaveRock"));stone.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/Rock.jpg");stone.color=new Color(.67f,.70f,.69f);Save(stone,"Assets/Generated/CaveRock.mat");
        var surface=new Material(terrain.materialTemplate);surface.SetFloat("_CavePatch",1);Save(surface,"Assets/Generated/CaveSurface.mat");
        foreach(var cave in CaveLayout.Sites)
        {
            cave.Sample();var root=new GameObject(cave.name+" underground");root.isStatic=true;
            var bounds=new Bounds(cave.World(0,0),Vector3.zero);
            for(int i=0;i<=16;i++)bounds.Encapsulate(cave.World(0,(i/16f-.5f)*cave.length));
            for(int i=0;i<3;i++)bounds.Encapsulate(cave.Room(i));bounds.Expand(36);
            int x0=Mathf.FloorToInt((bounds.min.x+ReserveWorld.Size*.5f)/cell),x1=Mathf.CeilToInt((bounds.max.x+ReserveWorld.Size*.5f)/cell);
            int z0=Mathf.FloorToInt((bounds.min.z+ReserveWorld.Size*.5f)/cell),z1=Mathf.CeilToInt((bounds.max.z+ReserveWorld.Size*.5f)/cell);
            for(int z=z0;z<z1;z++)for(int x=x0;x<x1;x++)holes[z,x]=false;
            int divisions=Mathf.Max(1,Mathf.CeilToInt(cell/2));float step=cell/divisions;int nx=(x1-x0)*divisions,nz=(z1-z0)*divisions;
            float ox=x0*cell-ReserveWorld.Size*.5f,oz=z0*cell-ReserveWorld.Size*.5f;
            var height=new float[nx+1,nz+1];var profile=new Vector3[nx+1,nz+1];float minY=10000,maxY=-10000;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                float wx=ox+x*step,wz=oz+z*step;float ground=data.GetInterpolatedHeight(wx/ReserveWorld.Size+.5f,wz/ReserveWorld.Size+.5f);
                cave.Profile(wx,wz,out float floor,out float roof,out float edge);height[x,z]=ground;profile[x,z]=new Vector3(floor,roof,edge);
                minY=Mathf.Min(minY,floor-5);maxY=Mathf.Max(maxY,ground+3);
            }
            minY=Mathf.Floor(minY/2)*2;int ny=Mathf.CeilToInt((maxY-minY)/2);
            var vertices=new List<Vector3>();var inside=new List<int>();var exterior=new List<int>();var lookup=new Dictionary<Vector3Int,int>();
            var p=new Vector3[8];var d=new float[8];var values=new float[nx+1,nz+1,ny+1];
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)for(int y=0;y<=ny;y++)values[x,z,y]=CaveDensity(minY+y*2,height[x,z],profile[x,z]);
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)for(int y=0;y<ny;y++)
            {
                int positive=0;
                for(int c=0;c<8;c++)
                {
                    int dx=c==1||c==2||c==5||c==6?1:0,dz=c==2||c==3||c==6||c==7?1:0,dy=c>=4?1:0;
                    p[c]=new Vector3(ox+(x+dx)*step,minY+(y+dy)*2,oz+(z+dz)*step);d[c]=values[x+dx,z+dz,y+dy];if(d[c]>0)positive++;
                }
                if(positive==0||positive==8)continue;
                for(int t=0;t<6;t++)CavePolygon(p,d,t,vertices,inside,exterior,lookup,data);
            }
            var mesh=new Mesh{name=cave.name+" connected cavern",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.subMeshCount=2;mesh.SetTriangles(inside,0);mesh.SetTriangles(exterior,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/Generated/"+cave.name.Replace(" ","")+"Cavern.asset";Save(mesh,path);mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=new[]{stone,surface};root.AddComponent<MeshCollider>().sharedMesh=mesh;
            for(int room=0;room<3;room++)CreateCaveCrystals(root.transform,cave,room);
            Debug.Log("CAVE_MESH "+cave.name+" "+vertices.Count+" vertices "+inside.Count/3+" interior triangles");
        }
        data.SetHoles(0,0,holes);EditorUtility.SetDirty(data);
    }
    static void CavePolygon(Vector3[] p,float[] d,int tetra,List<Vector3> vertices,List<int> inside,List<int> exterior,Dictionary<Vector3Int,int> lookup,TerrainData terrain)
    {
        var points=new List<Vector3>(4);Vector3 solid=Vector3.zero,air=Vector3.zero;int solids=0,airs=0;
        for(int v=0;v<4;v++){int i=CaveTetra[tetra,v];if(d[i]>0){solid+=p[i];solids++;}else{air+=p[i];airs++;}}
        if(solids==0||airs==0)return;Vector3 normal=(air/airs-solid/solids).normalized;
        for(int e=0;e<6;e++){int a=CaveTetra[tetra,CaveEdges[e,0]],b=CaveTetra[tetra,CaveEdges[e,1]];if((d[a]>0)==(d[b]>0))continue;points.Add(Vector3.Lerp(p[a],p[b],d[a]/(d[a]-d[b])));}
        Vector3 center=Vector3.zero;foreach(var v in points)center+=v;center/=points.Count;Vector3 tangent=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.right).normalized,bitangent=Vector3.Cross(normal,tangent);
        points.Sort((a,b)=>Mathf.Atan2(Vector3.Dot(a-center,bitangent),Vector3.Dot(a-center,tangent)).CompareTo(Mathf.Atan2(Vector3.Dot(b-center,bitangent),Vector3.Dot(b-center,tangent))));
        for(int i=1;i<points.Count-1;i++)
        {
            var a=points[0];var b=points[i];var c=points[i+1];if(Vector3.Cross(b-a,c-a).sqrMagnitude<.00001f)continue;
            bool top=true;foreach(var v in new[]{a,b,c})top&=v.y>terrain.GetInterpolatedHeight(v.x/ReserveWorld.Size+.5f,v.z/ReserveWorld.Size+.5f)-.7f;
            var triangles=top?exterior:inside;foreach(var v in new[]{a,b,c}){var key=Vector3Int.RoundToInt(v*1000);if(!lookup.TryGetValue(key,out int index)){index=vertices.Count;vertices.Add(v);lookup.Add(key,index);}triangles.Add(index);}
        }
    }
    static void CreateCaveCrystals(Transform root,CaveSite cave,int room)
    {
        Color color=room==1?new Color(.46f,.25f,.85f):room==2?new Color(.16f,.72f,.66f):new Color(.18f,.49f,.91f);
        var material=new Material(Shader.Find("Pinefall/Crystal")){color=color*.65f};material.SetFloat("_Glossiness",.72f);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.48f);Save(material,"Assets/Generated/"+cave.name.Replace(" ","")+"Crystal"+room+".mat");
        var rng=new System.Random(431+room);var center=cave.Room(room);
        for(int cluster=0;cluster<5;cluster++)
        {
            float angle=cluster*2.39996f;var at=center+Quaternion.Euler(0,cave.yaw,0)*new Vector3(Mathf.Cos(angle)*5.2f,0,Mathf.Sin(angle)*5.7f);at.y=cave.Ground(at)+.03f;
            var points=new List<Vector3>();var indices=new List<int>();
            for(int crystal=0;crystal<5;crystal++)
            {
                float a=crystal*2.39996f;var foot=new Vector3(Mathf.Cos(a)*.38f,0,Mathf.Sin(a)*.38f);float h=.5f+(float)rng.NextDouble()*1.2f,r=.12f+(float)rng.NextDouble()*.16f;var lower=new Vector3[6];var upper=new Vector3[6];
                for(int side=0;side<6;side++){float theta=side*Mathf.PI/3;lower[side]=foot+new Vector3(Mathf.Cos(theta)*r*1.1f,0,Mathf.Sin(theta)*r*1.1f);upper[side]=foot+new Vector3(Mathf.Cos(theta)*r+.08f,h*.73f,Mathf.Sin(theta)*r);}
                var tip=foot+new Vector3(.15f,h,.04f);
                for(int side=0;side<6;side++){int next=(side+1)%6;CrystalFace(points,indices,lower[side],upper[side],upper[next]);CrystalFace(points,indices,lower[side],upper[next],lower[next]);CrystalFace(points,indices,upper[side],tip,upper[next]);}

            }
            var mesh=new Mesh{name="Crystal outcrop"};mesh.SetVertices(points);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();Save(mesh,"Assets/Generated/"+cave.name.Replace(" ","")+"Crystal"+room+"-"+cluster+".asset");
            var go=new GameObject("Luminous crystal outcrop");go.isStatic=true;go.transform.SetParent(root);go.transform.position=at;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        var lightObject=new GameObject("Crystal chamber ambience");lightObject.transform.SetParent(root);lightObject.transform.position=center+Vector3.up*2.8f;var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=18;light.intensity=1.55f;light.color=color;light.shadows=LightShadows.None;
    }
    static void CrystalFace(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c)
    {int n=vertices.Count;vertices.AddRange(new[]{a,b,c});indices.AddRange(new[]{n,n+1,n+2});}
    public static void FinalizeLivingPresentation()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Reserve.unity");
        foreach(var cave in CaveLayout.Sites)
        {
            var root=GameObject.Find(cave.name+" underground");if(!root)continue;var old=new List<GameObject>();
            foreach(Transform child in root.transform)if(child.name=="Luminous crystal outcrop"||child.name=="Crystal chamber ambience")old.Add(child.gameObject);
            foreach(var child in old)Object.DestroyImmediate(child);for(int room=0;room<3;room++)CreateCaveCrystals(root.transform,cave,room);
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        RefreshAtlasAndHandbook();BuildCurrent();
    }

}
