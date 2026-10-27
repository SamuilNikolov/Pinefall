using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit refresh commands preserve asset GUIDs and leave routine builds inexpensive.
public static partial class BuildReserve
{
    public static void InspectRoadGrades()
    {
        var data=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Generated/ReserveTerrain.asset");float worst=0;Vector2 where=Vector2.zero;int link=-1;
        for(int road=0;road<Pinefall.ExpeditionLayout.Roads.GetLength(0);road++)
        {
            var a=Pinefall.ExpeditionLayout.Regions[Pinefall.ExpeditionLayout.Roads[road,0]].center;var b=Pinefall.ExpeditionLayout.Regions[Pinefall.ExpeditionLayout.Roads[road,1]].center;
            Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized;
            for(int n=2;n<9;n++)
            {
                Vector2 point=Vector2.Lerp(a,b,n/10f);bool near=false;foreach(var site in Pinefall.ExpeditionLayout.Regions)if((site.center-point).sqrMagnitude<130*130)near=true;if(near)continue;
                float h=data.GetInterpolatedHeight(point.x/Pinefall.ReserveWorld.Size+.5f,point.y/Pinefall.ReserveWorld.Size+.5f);
                var q=point+side*20;float slope=Mathf.Abs(data.GetInterpolatedHeight(q.x/Pinefall.ReserveWorld.Size+.5f,q.y/Pinefall.ReserveWorld.Size+.5f)-h)/20;
                if(slope>worst){worst=slope;where=point;link=road;}
            }
        }
        Debug.Log("ROAD_GRADE_WORST "+worst+" point="+where+" link="+link);
        var natural=typeof(Pinefall.ReserveWorld).GetMethod("NaturalHeight",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        for(int i=-2;i<=2;i++){var q=where+new Vector2(i*10,0);Debug.Log("ROAD_HEIGHT "+q+" raw="+natural.Invoke(null,new object[]{q.x,q.y})+" analytic="+Pinefall.ReserveWorld.Height(q.x,q.y)+" data="+data.GetInterpolatedHeight(q.x/Pinefall.ReserveWorld.Size+.5f,q.y/Pinefall.ReserveWorld.Size+.5f));}
    }

    [MenuItem("Pinefall/Regenerate road coverage")]
    public static void RegenerateRoadCoverage()
    {
        const string path = "Assets/Resources/RoadCoverage.png";
        // Delete only the derived image. Its companion .meta retains references/import identity.
        if (File.Exists(path)) File.Delete(path);
        PrepareRoadCoverage();
        AssetDatabase.SaveAssets();
        Debug.Log("PINEFALL_ROAD_COVERAGE_REFRESHED");
    }

    [MenuItem("Pinefall/Refresh atlas and handbook")]
    public static void RefreshAtlasAndHandbook()
    {
        PrepareAtlasTexture();
        ExportHandbookData();
        AssetDatabase.SaveAssets();
        Debug.Log("PINEFALL_ATLAS_HANDBOOK_REFRESHED");
    }
}
