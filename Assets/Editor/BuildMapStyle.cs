using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using Pinefall;
public static partial class BuildReserve
{
    static string AtlasHash(string path){using(var sha=SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    static void RefineAtlasWater(string path)
    {
        const string stamp="atlas-style.sha256";string hash=AtlasHash(path);if(File.Exists(stamp)&&File.ReadAllText(stamp)==hash)return;var image=new Texture2D(2,2,TextureFormat.RGB24,false);image.LoadImage(File.ReadAllBytes(path));var pixels=image.GetPixels32();int width=image.width,height=image.height;for(int y=0;y<height;y++){float z=(y/(height-1f)-.5f)*ReserveWorld.Size;for(int x=0;x<width;x++){float wx=(x/(width-1f)-.5f)*ReserveWorld.Size;float level;if(!ReserveWater.Surface(wx,z,out level))continue;var old=pixels[x+y*width];if(old.b<old.r*1.03f)continue;float depth=ReserveWater.Depth(wx,z);pixels[x+y*width]=Color.Lerp(new Color(.24f,.53f,.57f),new Color(.065f,.27f,.36f),Mathf.Clamp01(depth/8));}}image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);File.WriteAllText(stamp,AtlasHash(path));
    }
}
