using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class BridgeSurface:MonoBehaviour{}
    public sealed partial class Ranger
    {
        public NetworkVariable<bool> Swimming=new NetworkVariable<bool>(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        float vehicleLook;public float VehicleLook{get{return CurrentVehicle?vehicleLook:0;}}
        bool SwimMove(Vector3 direction,bool sprint)
        {
            float level;bool water=ReserveWater.Surface(transform.position.x,transform.position.z,out level)&&ReserveWater.Depth(transform.position.x,transform.position.z)>1.1f&&transform.position.y<level+.4f;
            if(Swimming.Value!=water)Swimming.Value=water;if(!water)return false;
            float y=Mathf.Clamp((level-.85f-transform.position.y)*5,-3,3);controller.Move((direction.normalized*(sprint?4:2.7f)+Vector3.up*y)*Time.deltaTime);vertical=0;return true;
        }
        public bool SwimServerBot(Vector3 direction){return IsServer&&IsBot.Value&&SwimMove(direction,false);}
        void MountedLook(bool controls)
        {
            if(!controls)return;vehicleLook=Mathf.Repeat(vehicleLook+Input.GetAxisRaw("Mouse X")*ReserveUI.Instance.Sensitivity+180,360)-180;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*ReserveUI.Instance.Sensitivity,-70,65);
            bool armed=VehicleSeat.Value!=0||CurrentVehicle&&CurrentVehicle.Type.Value==4;Aiming=armed&&GameBindings.Held("Aim")&&!Reloading.Value;view.fieldOfView=Mathf.Lerp(view.fieldOfView,Aiming?AimFov:80,Time.deltaTime*10);
            for(int i=0;i<3;i++)if(GameBindings.Down("Weapon "+(i+1)))SelectWeapon(SlotWeapon(i));if(GameBindings.Down("Reload"))ReloadServerRpc();
            bool trigger=WeaponData.Get(Weapon.Value).automatic?GameBindings.Held("Fire"):GameBindings.Down("Fire");if(armed&&trigger)Fire();
        }
    }
}
