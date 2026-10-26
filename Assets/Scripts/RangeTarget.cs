using UnityEngine;
namespace Pinefall
{
    public sealed class RangeTarget : MonoBehaviour
    {
        float until;Vector3 initial;
        void Awake(){initial=transform.localScale;}
        public void Hit(){until=Time.time+.4f;}
        void Update(){transform.localScale=initial*(Time.time<until?.88f:1);}
    }
}
