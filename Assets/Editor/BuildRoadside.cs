using UnityEngine;
public static partial class BuildReserve
{
    static void RoadsideStops()
    {
        foreach(var p in Pinefall.ExpeditionLayout.MobilityStops()){var root=new GameObject("Roadside repair lay-by").transform;root.position=Pinefall.ReserveWorld.OnGround(p.x,p.y);Part("Service roof",PrimitiveType.Cube,new Vector3(0,3,0),new Vector3(6,.18f,4),roof,root);foreach(int side in new[]{-1,1})Part("Shelter post",PrimitiveType.Cube,new Vector3(side*2.7f,1.5f,1.6f),new Vector3(.16f,3,.16f),wood,root);Part("Work bench",PrimitiveType.Cube,new Vector3(0,.85f,1),new Vector3(2.6f,.18f,.8f),wood,root);Part("Parts case",PrimitiveType.Cube,new Vector3(-.7f,1.1f,1),new Vector3(.65f,.35f,.4f),metal,root);Label("Repair & refuel",new Vector3(-2.3f,2.8f,-2.1f),.09f,24,root,new Color(.9f,.9f,.8f),Vector3.zero);}
    }
}
