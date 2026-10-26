using UnityEngine;
namespace Pinefall
{
    public sealed class VistaCamera : MonoBehaviour
    {
        public Vector3 lookAt;
        Vector3 initial;
        void Start(){initial=transform.position;}
        void LateUpdate(){transform.position=initial+new Vector3(Mathf.Sin(Time.time*.035f)*1.6f,Mathf.Sin(Time.time*.05f)*.15f,0);transform.LookAt(lookAt);}
    }
}
