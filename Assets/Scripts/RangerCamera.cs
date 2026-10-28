using UnityEngine;
namespace Pinefall
{
    public sealed class RangerCamera:MonoBehaviour
    {
        public Ranger owner;public bool ThirdPerson;
        public bool ExternalView{get{return owner&&(ThirdPerson||owner.CurrentVehicle||owner.Gliding||owner.IsEmoting||ReserveUI.Instance&&ReserveUI.Instance.EmoteWheelOpen)&&!(owner.ScopedView);}}
        void LateUpdate()
        {
            if(!owner||!owner.Alive||owner.Downed.Value)return;
            if(ReserveUI.Instance&&ReserveUI.Instance.ControlsActive&&GameBindings.Down("Camera"))ThirdPerson=!ThirdPerson;
            if(ReserveSession.Arg("--smoke")!=""||ReserveSession.Arg("--capture")!="")return;
            var rotation=owner.transform.rotation*Quaternion.Euler(owner.LookPitch,owner.VehicleLook,0);transform.rotation=rotation;
            Vector3 head=owner.transform.position+Vector3.up*(owner.Sliding.Value?.78f:owner.Crouched.Value?1.17f:1.7f);
            var vehicle=owner.CurrentVehicle;
            if(vehicle)
            {
                rotation=Quaternion.Euler(0,vehicle.VisualRotation.eulerAngles.y,0)*Quaternion.Euler(owner.LookPitch,owner.VehicleLook,0);
                transform.rotation=rotation;head=vehicle.VisualSeat(owner.VehicleSeat.Value)+Vector3.up*1.7f;
            }
            if(ExternalView)
            {
                Vector3 offset=rotation*(owner.CurrentVehicle?new Vector3(.5f,1.1f,-5.5f):new Vector3(.65f,.25f,owner.Aiming?-1.65f:-3.5f));float length=offset.magnitude;
                float nearest=length;foreach(var hit in Physics.SphereCastAll(head,.18f,offset.normalized,length,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){if(owner.CurrentVehicle&&hit.collider.GetComponentInParent<RoadVehicle>()==owner.CurrentVehicle)continue;nearest=Mathf.Min(nearest,hit.distance-.12f);}offset=offset.normalized*Mathf.Max(.15f,nearest);
                transform.position=head+offset;
            }
            else transform.position=head;
        }
    }
}
