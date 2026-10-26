using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit refresh commands preserve asset GUIDs and leave routine builds inexpensive.
public static partial class BuildReserve
{
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
