using System.IO;
using UnityEditor;
using UnityEngine;
using Pinefall;
public static partial class BuildReserve
{
    static void PrepareRoadCoverage()
    {
        const string path="Assets/Resources/RoadCoverage.png";if(File.Exists(path))return;
        const int resolution=2048;var pixels=new Color32[resolution*resolution];float step=ReserveWorld.Size/resolution;
        for(int road=0;road<ExpeditionLayout.Roads.GetLength(0);road++)
        {
            var a=ExpeditionLayout.Regions[ExpeditionLayout.Roads[road,0]].center;var b=ExpeditionLayout.Regions[ExpeditionLayout.Roads[road,1]].center;var direction=b-a;float lengthSquared=direction.sqrMagnitude;
            int left=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x,b.x)-6+ReserveWorld.Size*.5f)/step),0,resolution-1),right=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x,b.x)+6+ReserveWorld.Size*.5f)/step),0,resolution-1),bottom=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.y,b.y)-6+ReserveWorld.Size*.5f)/step),0,resolution-1),top=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.y,b.y)+6+ReserveWorld.Size*.5f)/step),0,resolution-1);
            for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++){var point=new Vector2((x+.5f)*step-ReserveWorld.Size*.5f,(y+.5f)*step-ReserveWorld.Size*.5f);float t=Mathf.Clamp01(Vector2.Dot(point-a,direction)/Mathf.Max(1,lengthSquared));float distance=(point-a-direction*t).magnitude;byte coverage=(byte)Mathf.RoundToInt((1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.4f,4.5f,distance)))*255);int index=y*resolution+x;if(coverage>pixels[index].r)pixels[index]=new Color32(coverage,coverage,coverage,255);}
        }
        var texture=new Texture2D(resolution,resolution,TextureFormat.RGB24,true);texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=true;platform.maxTextureSize=2048;platform.format=TextureImporterFormat.R8;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
    }
}
