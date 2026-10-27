using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Pinefall;

public static partial class BuildReserve
{
    static void PrepareLivingResources()
    {
        foreach (string file in new[] { "MeadowDetail", "MeadowDetailNormal", "MeadowDetailArm", "ForestDetail", "ForestDetailNormal", "ForestDetailArm", "FirNeedles", "FirAlpha", "FirNormal", "GrassAlpha" })
        {
            var importer = AssetImporter.GetAtPath("Assets/Art/Terrain/" + file + ".jpg") as TextureImporter;
            if (!importer) continue; importer.maxTextureSize = 2048; importer.mipmapEnabled = true; importer.anisoLevel = 8;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (file.EndsWith("Normal")) importer.textureType = TextureImporterType.NormalMap;
            else if (file.EndsWith("Arm") || file.EndsWith("Alpha")) importer.sRGBTexture = false;
            EditorUtility.SetDirty(importer); importer.SaveAndReimport();
        }
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(R + "LaunchPad.prefab"))
        {
            var root = new GameObject("LaunchPad"); root.AddComponent<NetworkObject>(); root.AddComponent<LaunchPad>();
            PrefabUtility.SaveAsPrefabAsset(root, R + "LaunchPad.prefab"); Object.DestroyImmediate(root);
        }
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(R + "RealGrass.prefab"))
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RealModels/grass_medium_01/grass_medium_01.gltf");
            if (source)
            {
                var root = new GameObject("RealGrass"); var model = Object.Instantiate(source, root.transform);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>()) if (renderer.gameObject.name != "grass_medium_01_mid_a_LOD0") renderer.gameObject.SetActive(false);
                Normalize(model, .65f); PrefabUtility.SaveAsPrefabAsset(root, R + "RealGrass.prefab"); Object.DestroyImmediate(root);
            }
        }
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(R+"RealGroundGrass.prefab"))
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RealModels/grass_medium_01/grass_medium_01.gltf");
            var root=new GameObject("RealGroundGrass");var model=Object.Instantiate(source,root.transform);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())if(renderer.gameObject.name!="grass_medium_01_tiny_a_LOD0")renderer.gameObject.SetActive(false);
            Normalize(model,.26f);PrefabUtility.SaveAsPrefabAsset(root,R+"RealGroundGrass.prefab");Object.DestroyImmediate(root);
        }
        var terrain = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/BiomeTerrain.mat");
        if (terrain)
        {
            string[] slots={"_Meadow","_MeadowNormal","_MeadowArm","_Forest","_ForestNormal","_ForestArm"};
            string[] files={"MeadowDetail","MeadowDetailNormal","MeadowDetailArm","ForestDetail","ForestDetailNormal","ForestDetailArm"};
            for(int i=0;i<slots.Length;i++)terrain.SetTexture(slots[i],AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/"+files[i]+".jpg"));
            EditorUtility.SetDirty(terrain);
        }
        var needle = new Material(Shader.Find("Pinefall/Foliage"));
        needle.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/FirNeedles.jpg");
        needle.SetTexture("_AlphaTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/FirAlpha.jpg"));
        needle.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/FirNormal.jpg"));
        needle.SetFloat("_Wind", .04f); needle.enableInstancing = true; Save(needle, R + "LivingFir.mat");
        var far=new Material(needle);far.shader=Shader.Find("Pinefall/DistantFoliage");far.enableInstancing=true;Save(far,R+"LivingFirFar.mat");
        AssetDatabase.SaveAssets();
    }

    static void CreateSettlementDetail()
    {
        foreach (var district in ExpeditionLayout.Regions)
        {
            var root = GameObject.Find(district.name); if (!root) continue;
            var detail = root.AddComponent<SettlementDetail>(); detail.full = root.GetComponentsInChildren<Renderer>(); detail.center = root.transform.position;
            var proxy = new GameObject("Distant village silhouette"); proxy.transform.SetParent(root.transform, false);
            for (int house = 0; house < ExpeditionLayout.HouseCount(district); house++)
            {
                var offset = VillageFootprints.Offset(district, house); float height = ReserveWorld.Height(district.center.x + offset.x, district.center.y + offset.y) - root.transform.position.y;
                var wall = Part("Distant walls", PrimitiveType.Cube, new Vector3(offset.x, height + 1.6f, offset.y), new Vector3(8, 3.2f, 7), wood, proxy.transform);
                var top = Part("Distant roof", PrimitiveType.Cube, new Vector3(offset.x, height + 3.7f, offset.y), new Vector3(8.8f, .45f, 8), roof, proxy.transform);
                Object.DestroyImmediate(wall.GetComponent<Collider>()); Object.DestroyImmediate(top.GetComponent<Collider>());
            }
            detail.distant = proxy.GetComponentsInChildren<Renderer>(); foreach (var renderer in detail.distant) renderer.enabled = false;
        }
    }
}
