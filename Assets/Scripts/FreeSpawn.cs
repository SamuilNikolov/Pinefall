using UnityEngine;
namespace Pinefall
{
    public static class FreeSpawn
    {
        public static bool Finite(Vector3 p){return !float.IsNaN(p.x)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.z)&&Mathf.Abs(p.x)<=ReserveWorld.Edge&&Mathf.Abs(p.z)<=ReserveWorld.Edge;}
        public static bool Resolve(Vector3 requested,out Vector3 result)
        {
            result=default;if(!Finite(requested))return false;
            for(int attempt=0;attempt<65;attempt++){float radius=attempt==0?0:2+attempt/8*2,angle=attempt*2.39996f,x=Mathf.Clamp(requested.x+Mathf.Cos(angle)*radius,-ReserveWorld.Edge,ReserveWorld.Edge),z=Mathf.Clamp(requested.z+Mathf.Sin(angle)*radius,-ReserveWorld.Edge,ReserveWorld.Edge);float water;var p=ReserveWorld.OnGround(x,z,.25f);if(ReserveWater.Surface(x,z,out water)&&ReserveWater.Depth(x,z)>1.1f)p.y=water-.85f;
                if(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.45f,.32f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))continue;result=p;return true;}return false;
        }
    }
}
