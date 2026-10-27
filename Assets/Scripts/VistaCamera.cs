using UnityEngine;
namespace Pinefall
{
    public sealed class VistaCamera : MonoBehaviour
    {
        public Vector3 lookAt;
        Vector3 initial;bool configured;
        public void SetView(Vector3 position,Vector3 target){configured=true;initial=position;lookAt=target;transform.position=position;transform.LookAt(target);}
        void Start(){if(!configured)initial=transform.position;}
        void LateUpdate(){transform.position=initial+new Vector3(Mathf.Sin(Time.time*.035f)*1.6f,Mathf.Sin(Time.time*.05f)*.15f,0);transform.LookAt(lookAt);}
    }
}
