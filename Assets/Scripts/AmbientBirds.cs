using UnityEngine;
namespace Pinefall
{
    public sealed class AmbientBirds:MonoBehaviour
    {
        Transform[] birds=new Transform[32],left=new Transform[32],right=new Transform[32];Material material;Mesh body,wing;
        void Start()
        {
            material=WeaponView.Mat("Distant birds",new Color(.10f,.13f,.14f));var b=GameObject.CreatePrimitive(PrimitiveType.Sphere);body=b.GetComponent<MeshFilter>().sharedMesh;Destroy(b);wing=new Mesh{name="Swept bird wing"};wing.vertices=new[]{new Vector3(0,0,.15f),new Vector3(.5f,0,-.05f),new Vector3(.9f,0,-.28f),new Vector3(.2f,0,-.16f)};wing.triangles=new[]{0,1,3,1,2,3,3,1,0,3,2,1};wing.RecalculateNormals();
            for(int i=0;i<birds.Length;i++){birds[i]=new GameObject("Wild bird").transform;birds[i].SetParent(transform,false);Part(birds[i],body,Vector3.zero,new Vector3(.17f,.17f,.47f));left[i]=Part(birds[i],wing,new Vector3(.03f,0,0),Vector3.one);right[i]=Part(birds[i],wing,new Vector3(-.03f,0,0),new Vector3(-1,1,1));}
        }
        Transform Part(Transform parent,Mesh mesh,Vector3 p,Vector3 scale){var g=new GameObject("Bird silhouette");g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g.transform;}
        void Update(){var camera=Camera.main;if(!camera)return;float time=ReserveSession.Instance&&ReserveSession.Instance.Network.IsListening?(float)ReserveSession.Instance.Network.ServerTime.Time:Time.time;for(int i=0;i<birds.Length;i++){float a=time*.028f+i*2.4f,r=160+(i%8)*85;Vector3 center=camera.transform.position;center.x=Mathf.Round(center.x/800)*800;center.z=Mathf.Round(center.z/800)*800;Vector3 p=center+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);p.y=ReserveWorld.Height(p.x,p.z)+45+(i%5)*8;birds[i].SetPositionAndRotation(p,Quaternion.Euler(0,-a*Mathf.Rad2Deg,Mathf.Sin(a)*12));float flap=Mathf.Sin(time*6+i*1.7f)*28;left[i].localRotation=Quaternion.Euler(0,0,flap);right[i].localRotation=Quaternion.Euler(0,0,-flap);}}
        void OnDestroy(){if(material)Destroy(material);if(wing)Destroy(wing);}
    }
}
