using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveWorld
    {
        float[,] sampledHeights;int sampledResolution;float sampledScale;
        void CacheTerrainHeights(){var data=ground.terrainData;sampledResolution=data.heightmapResolution;sampledHeights=data.GetHeights(0,0,sampledResolution,sampledResolution);sampledScale=data.size.y;}
        float SampleCachedHeight(float x,float z)
        {
            float px=Mathf.Clamp01(x/Size+.5f)*(sampledResolution-1),pz=Mathf.Clamp01(z/Size+.5f)*(sampledResolution-1);int ix=Mathf.Min((int)px,sampledResolution-2),iz=Mathf.Min((int)pz,sampledResolution-2);float tx=px-ix,tz=pz-iz;
            return Mathf.Lerp(Mathf.Lerp(sampledHeights[iz,ix],sampledHeights[iz,ix+1],tx),Mathf.Lerp(sampledHeights[iz+1,ix],sampledHeights[iz+1,ix+1],tx),tz)*sampledScale;
        }
    }
}
