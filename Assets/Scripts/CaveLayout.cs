using UnityEngine;

namespace Pinefall
{
    public sealed class CaveSite
    {
        public readonly string name; public readonly Vector2 center; public readonly float yaw,length,width,lift;
        float start,end; bool sampled; float[] floors; Vector3[] nodes; int[,] links;
        public CaveSite(string name,float x,float z,float yaw,float length,float width,float lift)
        {this.name=name;center=new Vector2(x,z);this.yaw=yaw;this.length=length;this.width=width;this.lift=lift;}
        float Bend(float along){return Mathf.Sin(along/length*Mathf.PI*2)*width*.8f;}
        public Vector3 Local(Vector3 point){var p=Quaternion.Euler(0,-yaw,0)*(point-new Vector3(center.x,0,center.y));p.x-=Bend(p.z);return p;}
        public Vector3 World(float side,float along){return new Vector3(center.x,0,center.y)+Quaternion.Euler(0,yaw,0)*new Vector3(side+Bend(along),0,along);}
        public void Sample()
        {
            if(sampled)return;sampled=true;var a=World(0,-length*.5f);var b=World(0,length*.5f);
            start=ReserveWorld.Height(a.x,a.z)-.35f;end=ReserveWorld.Height(b.x,b.z)-.35f;
            nodes=new Vector3[14];floors=new float[9];
            for(int i=0;i<9;i++){float t=i/8f,along=(t-.5f)*length;nodes[i]=World(0,along);float overburden=Mathf.Sin(t*Mathf.PI)*18;floors[i]=Mathf.Min(Mathf.Lerp(start,end,t)-Mathf.Sin(t*Mathf.PI)*4.2f,ReserveWorld.Height(nodes[i].x,nodes[i].z)-overburden-.35f);nodes[i].y=floors[i];}
            // A side chamber, a second treasure alcove, and a loop back to the main route.
            nodes[9]=World(-18,-length*.15f);nodes[10]=World(-34,-length*.10f);
            nodes[11]=World(22,length*.12f);nodes[12]=World(32,length*.21f);nodes[13]=World(20,length*.32f);
            for(int i=9;i<14;i++)nodes[i].y=Mathf.Min(Floor(Local(nodes[i]).z)-.7f,ReserveWorld.Height(nodes[i].x,nodes[i].z)-13);
            links=new int[,]{{0,1},{1,2},{2,3},{3,4},{4,5},{5,6},{6,7},{7,8},{3,9},{9,10},{5,11},{11,12},{12,13},{13,7}};
        }
        public float Floor(float along){Sample();float t=Mathf.Clamp01(along/length+.5f);float at=t*8;int i=Mathf.Min(7,Mathf.FloorToInt(at));return Mathf.Lerp(floors[i],floors[i+1],at-i);}
        public Vector3 Room(int index){Sample();return nodes[index==0?3:index==1?10:12];}
        // A single connected vertical void at each column: winding passages unite with
        // asymmetric chambers. This is shared by terrain meshing and underground gameplay.
        public void Profile(float x,float z,out float floor,out float roof,out float horizontal)
        {
            Sample();floor=10000;roof=-10000;horizontal=-10000;float closestFloor=0,closestRoof=0;float best=-10000;
            for(int i=0;i<links.GetLength(0);i++)
            {
                var a=nodes[links[i,0]];var b=nodes[links[i,1]];var delta=new Vector2(b.x-a.x,b.z-a.z);
                float t=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.z),delta)/Mathf.Max(.001f,delta.sqrMagnitude));
                float distance=Vector2.Distance(new Vector2(x,z),new Vector2(a.x,a.z)+delta*t);
                bool entry=i==0||i==7;float radius=entry?2.8f:3.5f;
                float f=Mathf.Lerp(a.y,b.y,t),h=entry?3.7f:4.8f;float edge=radius-distance;
                if(edge>best){best=edge;closestFloor=f;closestRoof=f+h;}
                horizontal=Mathf.Max(horizontal,edge);
                if(edge<=0)continue;float arch=Mathf.Sqrt(Mathf.Clamp01(1-distance*distance/(radius*radius)));
                floor=Mathf.Min(floor,f);roof=Mathf.Max(roof,f+h*arch);
            }
            for(int roomIndex=0;roomIndex<3;roomIndex++)
            {
                int index=roomIndex==0?3:roomIndex==1?10:12;var room=nodes[index];float rx=index==3?10:8.5f,rz=index==3?14:10;
                var local=Quaternion.Euler(0,-yaw,0)*new Vector3(x-room.x,0,z-room.z);
                float radial=Mathf.Sqrt(local.x*local.x/(rx*rx)+local.z*local.z/(rz*rz));float edge=(1-radial)*rx;
                horizontal=Mathf.Max(horizontal,edge);if(edge>best){best=edge;closestFloor=room.y;closestRoof=room.y+8;}
                if(radial>=1)continue;floor=Mathf.Min(floor,room.y);roof=Mathf.Max(roof,room.y+8*Mathf.Sqrt(1-radial*radial));
            }
            if(floor>9000){floor=closestFloor;roof=closestRoof;}
        }
        public bool Contains(Vector3 point)
        {
            var local=Local(point);if(Mathf.Abs(local.x)>55||Mathf.Abs(local.z)>length*.55f)return false;
            Profile(point.x,point.z,out float floor,out float roof,out float edge);return edge>-.7f&&point.y>floor-2&&point.y<roof+1.5f;
        }
        public float Ground(Vector3 point){Profile(point.x,point.z,out float floor,out _,out _);return floor;}
    }

    public static class CaveLayout
    {
        public static readonly CaveSite[] Sites = {
            new CaveSite("Badger Burrow", 620, -420, 70, 140, 7, 24),
            new CaveSite("Hollow Heights", -690, 630, 25, 170, 8, 28),
            new CaveSite("Basalt Borough", 1450, 1150, 115, 230, 9, 38),
            new CaveSite("Echo Escarpment", -1600, -900, 80, 200, 8, 36),
            new CaveSite("Glacier Galleries", -1400, 2200, 10, 220, 9, 38),
            new CaveSite("Summit Shafts", 2200, 1900, 130, 280, 10, 48)
        };
        public static float ExteriorLift(float x, float z)
        {
            float lift = 0;
            foreach (var cave in Sites)
            {
                var p = cave.Local(new Vector3(x, 0, z));
                if (Mathf.Abs(p.x) > 120 || Mathf.Abs(p.z) > cave.length * .7f) continue;
                float along = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(cave.length * .22f, cave.length * .50f, Mathf.Abs(p.z)));
                lift += cave.lift * along * Mathf.Exp(-p.x * p.x / 3300);
            }
            return lift;
        }
        public static bool Underground(Vector3 point) { foreach (var site in Sites) if (site.Contains(point)) return true; return false; }
        public static float SupportHeight(Vector3 point)
        { foreach (var site in Sites) if (site.Contains(point)) return site.Ground(point); return ReserveWorld.Height(point.x, point.z); }
        public static Vector3 LootPosition(Vector3 player, Vector3 offset)
        { Vector3 result = player + offset; if (Underground(player)) { result.y = SupportHeight(player) + .35f; return result; } return ReserveWater.LootPosition(result.x, result.z); }
    }
}
