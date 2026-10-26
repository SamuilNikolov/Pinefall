using System.Collections;
using UnityEngine;
namespace Pinefall
{
    public sealed class WorldSign:MonoBehaviour
    {
        public float Width=6.2f,Height=.52f;
        IEnumerator Start(){yield return null;yield return null;var renderer=GetComponent<Renderer>();if(!renderer)yield break;var size=renderer.localBounds.size;if(size.x>.001f&&size.y>.001f)transform.localScale=Vector3.one*Mathf.Min(Width/size.x,Height/size.y);}
    }
}
