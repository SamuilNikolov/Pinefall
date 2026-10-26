using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public static class SoftGeometry
    {
        static Mesh cube;
        static Mesh tyre;
        public static Mesh Tyre
        {
            get
            {
                if(tyre)return tyre;var vertices=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();
                for(int i=0;i<=40;i++)for(int j=0;j<=12;j++){float a=i/40f*Mathf.PI*2,b=j/12f*Mathf.PI*2;float radius=.24f+Mathf.Cos(b)*.12f;vertices.Add(new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(b)*.115f,Mathf.Sin(a)*radius));uv.Add(new Vector2(i/40f,j/12f));if(i<40&&j<12){int n=i*13+j;indices.AddRange(new[]{n,n+1,n+13,n+1,n+14,n+13});}}
                tyre=new Mesh{name="Rounded tyre casing"};tyre.SetVertices(vertices);tyre.SetTriangles(indices,0);tyre.SetUVs(0,uv);tyre.RecalculateNormals();return tyre;
            }
        }
        public static Mesh BeveledCube
        {
            get
            {
                if(cube)return cube;var v=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
                Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
                foreach(var n in normals)
                {
                    Vector3 u=Vector3.Cross(n,Mathf.Abs(n.y)>.5f?Vector3.forward:Vector3.up),w=Vector3.Cross(n,u);int start=v.Count;
                    for(int y=0;y<=4;y++)for(int x=0;x<=4;x++)
                    {Vector3 p=n*.5f+u*(x/4f-.5f)+w*(y/4f-.5f);Vector3 inner=new Vector3(Mathf.Clamp(p.x,-.455f,.455f),Mathf.Clamp(p.y,-.455f,.455f),Mathf.Clamp(p.z,-.455f,.455f));v.Add(inner+(p-inner).normalized*.045f);uv.Add(new Vector2(x/4f,y/4f));if(x<4&&y<4){int k=start+y*5+x;triangles.AddRange(new[]{k,k+1,k+5,k+1,k+6,k+5});}}
                }
                cube=new Mesh{name="Soft machined edges"};cube.SetVertices(v);cube.SetTriangles(triangles,0);cube.SetUVs(0,uv);cube.RecalculateNormals();cube.RecalculateBounds();return cube;
            }
        }
    }
}
