using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Pinefall
{
    [DefaultExecutionOrder(100)]
    public sealed class StormView : MonoBehaviour
    {
        public Material material;
        Transform wall;Mesh mesh;
        void Start()
        {
            var g=new GameObject("Storm front");wall=g.transform;mesh=new Mesh{name="Storm perimeter"};
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int i=0;i<=160;i++)
            {
                float a=i/160f*Mathf.PI*2;v.Add(new Vector3(Mathf.Cos(a),-25,Mathf.Sin(a)));v.Add(new Vector3(Mathf.Cos(a),300,Mathf.Sin(a)));uv.Add(new Vector2(i/160f*12,0));uv.Add(new Vector2(i/160f*12,1));
                if(i<160){int k=i*2;t.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
            }
            mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        void LateUpdate()
        {
            var match=WildfrontMatch.Current;if(!wall)return;
            wall.gameObject.SetActive(match&&match.Active);
            if(match&&match.Active)
            {
                wall.position=match.Center.Value;wall.localScale=new Vector3(Mathf.Max(1,match.Radius.Value),1,Mathf.Max(1,match.Radius.Value));
                if(Ranger.Local&&match.Outside(Ranger.Local.transform.position)){RenderSettings.fogDensity=.012f;RenderSettings.fogColor=new Color(.27f,.33f,.45f);}
            }
        }
        void OnDestroy(){if(wall)Destroy(wall.gameObject);if(mesh)Destroy(mesh);}
    }
}
