using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Pinefall;

public static partial class BuildReserve
{
    static void PrepareFieldcraft()
    {
        const string prefix = "Assets/Resources/";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefix + "ConstructionWorld.prefab"))
        {
            var manager = new GameObject("Construction world"); manager.AddComponent<NetworkObject>(); manager.AddComponent<ConstructionWorld>();
            PrefabUtility.SaveAsPrefabAsset(manager, prefix + "ConstructionWorld.prefab"); Object.DestroyImmediate(manager);
        }
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefix + "BuildPiece.prefab"))
        {
            var piece = new GameObject("Player structure"); piece.AddComponent<NetworkObject>(); piece.AddComponent<BuildPiece>();
            var network = piece.AddComponent<NetworkTransform>(); network.SyncScaleX = network.SyncScaleY = network.SyncScaleZ = false; network.Interpolate = false;
            PrefabUtility.SaveAsPrefabAsset(piece, prefix + "BuildPiece.prefab"); Object.DestroyImmediate(piece);
        }
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefix + "Rocket.prefab"))
        {
            var rocket = new GameObject("Rocket projectile"); rocket.AddComponent<NetworkObject>(); rocket.AddComponent<RocketProjectile>();
            var network = rocket.AddComponent<NetworkTransform>(); network.SyncScaleX = network.SyncScaleY = network.SyncScaleZ = false; network.Interpolate = true;
            PrefabUtility.SaveAsPrefabAsset(rocket, prefix + "Rocket.prefab"); Object.DestroyImmediate(rocket);
        }
        for (int material = 0; material < 3; material++)
        {
            string path = prefix + "Build" + BuildPiece.Materials[material] + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path)) continue;
            var surface = new Material(Shader.Find("Standard"));
            string texture = material == 0 ? "WeatheredWood" : material == 1 ? "Rock" : "WeatheredMetal";
            surface.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/" + texture + ".jpg");
            surface.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/" + texture + "Normal.jpg")); surface.EnableKeyword("_NORMALMAP");
            surface.SetFloat("_Metallic", material == 2 ? .55f : 0); surface.SetFloat("_Glossiness", material == 2 ? .27f : .12f);
            surface.mainTextureScale = new Vector2(1, 1); AssetDatabase.CreateAsset(surface, path);
        }
        ExportHandbookData();AssetDatabase.SaveAssets();
    }
}
