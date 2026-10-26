using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveWorld
    {
        Mesh MakeBroadleaf()
        {
            var points=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int rings=16,sides=24;
            for(int ring=0;ring<=rings;ring++){float latitude=ring/(float)rings*Mathf.PI,y=7+Mathf.Cos(latitude)*3.4f;for(int side=0;side<=sides;side++){float angle=side/(float)sides*Mathf.PI*2;float radius=Mathf.Sin(latitude)*(3.1f+Mathf.Sin(angle*5+ring*.4f)*.35f+Mathf.Cos(angle*3-ring*.3f)*.24f);points.Add(new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius));uv.Add(new Vector2(side/(float)sides,ring/(float)rings));if(ring<rings&&side<sides){int n=ring*(sides+1)+side;triangles.AddRange(new[]{n,n+sides+1,n+1,n+1,n+sides+1,n+sides+2});}}}
            var mesh=new Mesh{name="Amberwood broadleaf canopy"};mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();fineAssets.Add(mesh);return mesh;
        }
    }
}
