using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
namespace Pinefall
{
    // Opt-in build-time photography of the actual world; no networking, player icons, or HUD are baked.
    public sealed class AtlasBakeProbe:MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(2);var world=ReserveWorld.Instance;if(ReserveWeather.Instance)ReserveWeather.Instance.gameObject.SetActive(false);if(Camera.main)Camera.main.enabled=false;foreach(var sign in FindObjectsOfType<TextMesh>())sign.GetComponent<Renderer>().enabled=false;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.64f,.72f,.78f);RenderSettings.ambientEquatorColor=new Color(.61f,.64f,.56f);RenderSettings.ambientGroundColor=new Color(.35f,.38f,.32f);RenderSettings.ambientIntensity=1;Shader.SetGlobalFloat("_ReserveWet",0);if(RenderSettings.sun){RenderSettings.sun.transform.rotation=Quaternion.Euler(53,-38,0);RenderSettings.sun.intensity=1.2f;RenderSettings.sun.color=new Color(1,.98f,.91f);RenderSettings.sun.shadowStrength=.65f;RenderSettings.sun.shadowBias=.02f;RenderSettings.sun.shadowResolution=LightShadowResolution.VeryHigh;}QualitySettings.shadowDistance=6500;QualitySettings.shadowCascades=2;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;world.ground.heightmapPixelError=2;
            var go=new GameObject("Cartography camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=ReserveWorld.Size*.5f;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.23f,.23f);camera.nearClipPlane=.5f;camera.farClipPlane=5000;camera.cullingMask=~((1<<8)|(1<<9)|(1<<10)|(1<<11));camera.transform.SetPositionAndRotation(new Vector3(0,2200,0),Quaternion.Euler(90,0,0));camera.allowHDR=false;camera.allowMSAA=true;var target=new RenderTexture(4096,4096,24,RenderTextureFormat.ARGB32){antiAliasing=2};target.Create();camera.targetTexture=target;world.DrawAtlasFoliage(camera);camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(4096,4096,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,4096,4096),0,0);image.Apply();var path=ReserveSession.Arg("--bake-atlas");File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=prior;Debug.Log("PINEFALL_ATLAS_BAKED "+path+" 4096x4096");Application.Quit();
        }
    }
}
