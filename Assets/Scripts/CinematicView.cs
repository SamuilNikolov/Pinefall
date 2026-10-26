using UnityEngine;
namespace Pinefall
{
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicView : MonoBehaviour
    {
        public Material material;
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(material){var m=WildfrontMatch.Current;material.SetFloat("_InStorm",m&&m.Active&&Ranger.Local&&m.Outside(Ranger.Local.ObserverPosition)?1:0);material.SetFloat("_Hurt",Ranger.Local?Mathf.Clamp01((Ranger.Local.DamageFlash-Time.time)/.65f):0);Graphics.Blit(source,destination,material);}else Graphics.Blit(source,destination);
        }
    }
}
