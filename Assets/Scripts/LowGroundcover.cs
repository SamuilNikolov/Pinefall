using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinefall
{
    public sealed partial class ReserveWorld
    {
        sealed class GrassCell { public Vector2Int key;public Matrix4x4[] matrices=new Matrix4x4[640];public int count,biome;public Bounds bounds; }
        readonly List<GrassCell> groundGrass=new List<GrassCell>(64);
        readonly Stack<GrassCell> grassPool=new Stack<GrassCell>();
        readonly Vector2Int[] grassOffsets=new Vector2Int[49];
        Mesh[] lowGrassMeshes;Matrix4x4 lowGrassOffset;Material[] lowGrassMaterials,lowGrassDistantMaterials;
        void PrepareLowGrass()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
            var source=Resources.Load<GameObject>("RealGroundGrass");if(!source)return;var filter=source.GetComponentInChildren<MeshFilter>();if(!filter)return;
            lowGrassOffset=filter.transform.localToWorldMatrix;lowGrassMeshes=new Mesh[3];
            for(int level=0;level<3;level++)
            {
                int blades=level==0?8:level==1?4:2;var combined=new CombineInstance[blades];
                for(int blade=0;blade<blades;blade++){float angle=blade*2.39996f,radius=blade==0?0:.18f+blade*.07f;combined[blade]=new CombineInstance{mesh=filter.sharedMesh,transform=Matrix4x4.TRS(new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius),Quaternion.Euler(0,blade*137,0),Vector3.one)*lowGrassOffset};}
                var mesh=new Mesh{name="Scanned short grass LOD "+level};mesh.CombineMeshes(combined,true,true);lowGrassMeshes[level]=mesh;fineAssets.Add(mesh);
            }
            var renderer=filter.GetComponent<Renderer>();lowGrassMaterials=new Material[6];lowGrassDistantMaterials=new Material[6];
            Color[] tints={new Color(.87f,.88f,.75f),new Color(.88f,.79f,.53f),new Color(.66f,.72f,.62f),new Color(.71f,.80f,.60f),new Color(.76f,.83f,.67f),new Color(.82f,.86f,.71f)};
            for(int i=0;i<6;i++){var m=new Material(Shader.Find("Pinefall/Foliage")){mainTexture=renderer.sharedMaterial.mainTexture,color=tints[i],enableInstancing=true};m.SetTexture("_AlphaTex",Resources.Load<Texture2D>("GrassOpacity"));m.SetFloat("_Wind",.055f);m.SetFloat("_FadeDistance",95);lowGrassMaterials[i]=m;fineAssets.Add(m);var distant=new Material(m);distant.shader=Shader.Find("Pinefall/DistantFoliage");lowGrassDistantMaterials[i]=distant;fineAssets.Add(distant);}
            int n=0;for(int z=-3;z<=3;z++)for(int x=-3;x<=3;x++)grassOffsets[n++]=new Vector2Int(x,z);
            System.Array.Sort(grassOffsets,(a,b)=>a.sqrMagnitude.CompareTo(b.sqrMagnitude));
        }
        void DrawLowGrass(Camera camera,Camera renderCamera)
        {
            if(lowGrassMeshes==null||ReserveSession.Arg("--bake-atlas")!="")return;
            var position=camera.transform.position;if(position.y-Height(position.x,position.z)>40||CaveLayout.Underground(position))return;
            var center=new Vector2Int(Mathf.FloorToInt(position.x/48),Mathf.FloorToInt(position.z/48));
            for(int i=groundGrass.Count-1;i>=0;i--){var cell=groundGrass[i];if(Mathf.Abs(cell.key.x-center.x)>3||Mathf.Abs(cell.key.y-center.y)>3){groundGrass.RemoveAt(i);grassPool.Push(cell);}}
            int budget=1;
            foreach(var offset in grassOffsets)
            {
                var key=center+offset;bool found=false;foreach(var cell in groundGrass)if(cell.key==key){found=true;break;}if(found)continue;
                var next=grassPool.Count>0?grassPool.Pop():new GrassCell();FillLowGrass(next,key);groundGrass.Add(next);if(--budget==0)break;
            }
            float range=Mathf.Lerp(58,95,RuntimePerformance.Detail);
            foreach(var cell in groundGrass)
            {
                if(cell.count==0||cell.bounds.SqrDistance(position)>range*range||!GeometryUtility.TestPlanesAABB(drawPlanes,cell.bounds))continue;
                float distance=(cell.bounds.center-position).sqrMagnitude;int level=distance<35*35?0:distance<67*67?1:2;
                Graphics.DrawMeshInstanced(lowGrassMeshes[level],0,(level==0?lowGrassMaterials[cell.biome]:lowGrassDistantMaterials[cell.biome]),cell.matrices,cell.count,null,ShadowCastingMode.Off,true,0,renderCamera,LightProbeUsage.Off);
            }
        }
        void FillLowGrass(GrassCell cell,Vector2Int key)
        {
            cell.key=key;cell.count=0;cell.biome=ReserveBiomes.At((key.x+.5f)*48,(key.y+.5f)*48);
            var random=new System.Random(unchecked(Seed^key.x*73856093^key.y*19349663));
            float density=cell.biome==1?.12f:cell.biome==2?.045f:cell.biome==3?.32f:.95f;
            cell.bounds=new Bounds(OnGround((key.x+.5f)*48,(key.y+.5f)*48),new Vector3(50,35,50));
            for(int i=0;i<cell.matrices.Length;i++)
            {
                if(random.NextDouble()>density)continue;float x=(key.x+(float)random.NextDouble())*48,z=(key.y+(float)random.NextDouble())*48;
                if(Mathf.Abs(x)>Edge||Mathf.Abs(z)>Edge||ReserveWater.HasSurface(x,z)||Clearing(x,z))continue;
                float h=Height(x,z),dx=Height(x+1,z)-h,dz=Height(x,z+1)-h;if(Mathf.Abs(dx)+Mathf.Abs(dz)>1.1f)continue;
                float patch=Mathf.PerlinNoise(x*.055f+17,z*.055f+51);if(patch<.23f)continue;
                cell.bounds.Encapsulate(new Vector3(x,h+.5f,z));float scale=.66f+(float)random.NextDouble()*.53f;var rotation=Quaternion.FromToRotation(Vector3.up,new Vector3(-dx,1,-dz).normalized)*Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                cell.matrices[cell.count++]=Matrix4x4.TRS(new Vector3(x,h-.015f,z),rotation,new Vector3(scale,scale*(.7f+(float)random.NextDouble()*.3f),scale));
            }
        }
    }
}
