using UnityEngine;
using Pinefall;
public static partial class BuildReserve
{
    static void VillageDetails(Transform site,District district)
    {
        if(!district.minor){Part("Main street",PrimitiveType.Cube,new Vector3(0,-.01f,28),new Vector3(9,.035f,128),stoneMaterial(),site);Part("Cross street",PrimitiveType.Cube,new Vector3(0,-.005f,17),new Vector3(143,.035f,8),stoneMaterial(),site);}
        Part("Market awning",PrimitiveType.Cube,new Vector3(27,2.7f,-11),new Vector3(7,.16f,4),roof,site,new Vector3(0,0,4));
        foreach(float x in new[]{-3f,3f})Part("Market post",PrimitiveType.Cylinder,new Vector3(27+x,1.3f,-10),new Vector3(.12f,1.3f,.12f),wood,site);
        Part("Market counter",PrimitiveType.Cube,new Vector3(27,.85f,-10),new Vector3(6,.15f,1.2f),wood,site);
        for(int i=0;i<4;i++)Part("Trade crate",PrimitiveType.Cube,new Vector3(25+i*1.2f,1.08f,-10),new Vector3(.7f,.4f,.65f),i%2==0?wood:metal,site);
        foreach(float x in new[]{-51f,51f})for(int rail=0;rail<4;rail++){Part("Village fence post",PrimitiveType.Cube,new Vector3(x,.75f,-2+rail*8),new Vector3(.18f,1.5f,.18f),wood,site);if(rail<3)foreach(float y in new[]{.55f,1.05f})Part("Village fence rail",PrimitiveType.Cube,new Vector3(x,y,2+rail*8),new Vector3(.12f,.1f,7.7f),wood,site);}
        Part("Water tank",PrimitiveType.Cylinder,new Vector3(-29,1.2f,-8),new Vector3(2.3f,1.2f,2.3f),metal,site);Part("Tank roof",PrimitiveType.Cylinder,new Vector3(-29,2.43f,-8),new Vector3(2.5f,.055f,2.5f),roof,site);
        foreach(float x in new[]{-37f,37f}){Part("Wood pile cover",PrimitiveType.Cube,new Vector3(x,.9f,-13),new Vector3(4,.13f,2.5f),roof,site);for(int i=0;i<4;i++)Part("Stored timber",PrimitiveType.Cylinder,new Vector3(x-1.4f+i*.9f,.45f,-13),new Vector3(.45f,1.1f,.45f),wood,site,new Vector3(90,0,0));}
        if(district.biome==0||district.biome==5)for(int row=0;row<5;row++)Part("Old vegetable bed",PrimitiveType.Cube,new Vector3(-35+row*1.5f,.04f,-28),new Vector3(.55f,.08f,9),wood,site);
    }
    static void ExtraDocks()
    {
        foreach(var point in ExpeditionLayout.BoatStops()){var root=new GameObject("Launch mooring").transform;root.position=point+Vector3.left*4;Part("Mooring deck",PrimitiveType.Cube,new Vector3(0,-.2f,0),new Vector3(2.4f,.18f,5.5f),wood,root);foreach(float z in new[]{-2f,2f}){Part("Mooring post",PrimitiveType.Cylinder,new Vector3(-1.05f,-.05f,z),new Vector3(.19f,.6f,.19f),wood,root);}}
    }
}
