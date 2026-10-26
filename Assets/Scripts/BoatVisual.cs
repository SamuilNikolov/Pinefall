using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class BoatVisual:MonoBehaviour
    {
        Mesh hull;readonly List<Material> materials=new List<Material>();
        public void Build(Transform root)
        {
            Material paint=WeaponView.Mat("Launch hull",new Color(.22f,.35f,.39f),.35f),rubber=WeaponView.Mat("Marine seats",new Color(.22f,.23f,.21f)),metal=WeaponView.Mat("Marine rails",new Color(.42f,.46f,.46f),.65f),deck=WeaponView.Mat("Deck",new Color(.48f,.43f,.32f));materials.AddRange(new[]{paint,rubber,metal,deck});
            var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();float[] zs={-2.2f,-1.3f,1.1f,2.35f},widths={.75f,1.02f,.91f,.08f};
            for(int section=0;section<zs.Length;section++){float w=widths[section],z=zs[section];v.AddRange(new[]{new Vector3(-w*.65f,-.35f,z),new Vector3(-w,.35f,z),new Vector3(w,.35f,z),new Vector3(w*.65f,-.35f,z)});for(int a=0;a<4;a++)uv.Add(new Vector2(a/3f,section/3f));if(section<3)for(int a=0;a<4;a++){int next=(a+1)%4,n=section*4+a,m=section*4+next;t.AddRange(new[]{n,n+4,m,m,n+4,m+4});}}
            t.AddRange(new[]{0,1,2,0,2,3,12,14,13,12,15,14});hull=new Mesh{name="Motor launch hull"};hull.SetVertices(v);hull.SetTriangles(t,0);hull.SetUVs(0,uv);hull.RecalculateNormals();var body=new GameObject("Boat hull");body.transform.SetParent(root,false);body.AddComponent<MeshFilter>().sharedMesh=hull;body.AddComponent<MeshRenderer>().sharedMaterial=paint;
            WeaponView.Part(root,PrimitiveType.Cube,new Vector3(0,.38f,-.2f),new Vector3(1.55f,.10f,3),deck);foreach(float x in new[]{-.45f,.45f}){WeaponView.Part(root,PrimitiveType.Cube,new Vector3(x,.54f,-.7f),new Vector3(.55f,.18f,.6f),rubber);WeaponView.Part(root,PrimitiveType.Cube,new Vector3(x,.8f,-1),new Vector3(.55f,.65f,.16f),rubber);}
            WeaponView.Part(root,PrimitiveType.Cube,new Vector3(-.42f,.83f,.48f),new Vector3(.6f,.6f,.4f),paint);WeaponView.Part(root,PrimitiveType.Cylinder,new Vector3(-.42f,1.12f,.35f),new Vector3(.33f,.03f,.33f),metal,new Vector3(65,0,0));WeaponView.Part(root,PrimitiveType.Capsule,new Vector3(0,.08f,-2.32f),new Vector3(.45f,.6f,.55f),metal);
            foreach(int side in new[]{-1,1}){WeaponView.Part(root,PrimitiveType.Cylinder,new Vector3(side*.96f,.75f,-.35f),new Vector3(.045f,1.2f,.045f),metal,new Vector3(90,0,0));foreach(float z in new[]{-1.5f,.8f})WeaponView.Part(root,PrimitiveType.Cylinder,new Vector3(side*.96f,.55f,z),new Vector3(.045f,.22f,.045f),metal);}
            foreach(var tr in root.GetComponentsInChildren<Transform>())tr.gameObject.layer=0;
        }
        void OnDestroy(){if(hull)Destroy(hull);foreach(var m in materials)if(m)Destroy(m);}
    }
}
