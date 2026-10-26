using UnityEditor;
using UnityEngine;
using Pinefall;
public static partial class BuildReserve
{
    static int bridgeIndex;
    static void Bridges()
    {
        bridgeIndex=0;for(int road=0;road<ExpeditionLayout.Roads.GetLength(0);road++)
        {
            var a=ExpeditionLayout.Regions[ExpeditionLayout.Roads[road,0]].center;var b=ExpeditionLayout.Regions[ExpeditionLayout.Roads[road,1]].center;float start=-1;
            for(int i=0;i<=200;i++){float t=i/200f;var p=Vector2.Lerp(a,b,t);bool wet=ReserveWater.Depth(p.x,p.y)>.5f;if(wet&&start<0)start=t;if((!wet||i==200)&&start>=0){CreateBridge(a,b,start,t);start=-1;}}
        }
    }
    static void CreateBridge(Vector2 a,Vector2 b,float from,float to)
    {
        float length=Vector2.Distance(a,b);var dir=(b-a).normalized;Vector2 p=Vector2.Lerp(a,b,from)-dir*18,q=Vector2.Lerp(a,b,to)+dir*18;var mid=(p+q)*.5f;
        float level=Mathf.Max(ReserveWater.Level(mid.x,mid.y)+4.7f,Mathf.Max(ReserveWorld.Height(p.x,p.y),ReserveWorld.Height(q.x,q.y))+.6f);float span=Vector2.Distance(p,q);
        var root=new GameObject("Road bridge "+bridgeIndex).transform;root.position=new Vector3(mid.x,level,mid.y);root.rotation=Quaternion.LookRotation(new Vector3(dir.x,0,dir.y));root.gameObject.AddComponent<BridgeSurface>();
        Part("Bridge deck",PrimitiveType.Cube,new Vector3(0,-.2f,0),new Vector3(7,.4f,span),wood,root);
        foreach(float side in new[]{-3.3f,3.3f}){Part("Bridge rail",PrimitiveType.Cube,new Vector3(side,.8f,0),new Vector3(.12f,.12f,span),metal,root);for(float z=-span/2;z<span/2;z+=10)Part("Bridge post",PrimitiveType.Cube,new Vector3(side,.4f,z),new Vector3(.14f,1.1f,.14f),wood,root);}
        foreach(int side in new[]{-1,1})
        {
            var shore=side<0?p-dir*32:q+dir*32;float ground=ReserveWorld.Height(shore.x,shore.y)-level;float inner=side*span/2,outer=inner+side*32;
            var mesh=new Mesh{name="Bridge approach"};mesh.vertices=new[]{new Vector3(-3.5f,0,inner),new Vector3(3.5f,0,inner),new Vector3(-3.5f,ground,outer),new Vector3(3.5f,ground,outer)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=side>0?new[]{0,2,1,1,2,3}:new[]{0,1,2,1,3,2};mesh.RecalculateNormals();Save(mesh,"Assets/Generated/Bridge"+bridgeIndex+"_"+side+".asset");var ramp=new GameObject("Bridge approach");ramp.transform.SetParent(root,false);ramp.AddComponent<MeshFilter>().sharedMesh=mesh;ramp.AddComponent<MeshRenderer>().sharedMaterial=wood;ramp.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        bridgeIndex++;
    }
}
