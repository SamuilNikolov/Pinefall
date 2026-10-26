using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<bool> AimState=new NetworkVariable<bool>(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        public NetworkVariable<ulong> MagazineState=new NetworkVariable<ulong>(0);
        public NetworkVariable<double> ReloadStarted=new NetworkVariable<double>(0);
        static readonly float[] Recoil={1.5f,.62f,1.8f,.85f,.33f,1.12f,.72f,2.8f};
        float cameraKick,localBloom,serverBloom;
        public float ShotBloom{get{return localBloom;}}
        public float ReloadFraction{get{return Reloading.Value?Mathf.Clamp01((float)(NetworkManager.ServerTime.Time-ReloadStarted.Value)/(WeaponData.Get(Weapon.Value).reload*ReloadFactor(Weapon.Value))):0;}}
        void GunHandlingTick()
        {
            cameraKick=Mathf.MoveTowards(cameraKick,0,Time.deltaTime*4.5f);localBloom=Mathf.MoveTowards(localBloom,0,Time.deltaTime*2.1f);serverBloom=Mathf.MoveTowards(serverBloom,0,Time.deltaTime*2.1f);
            if(LocalHuman&&AimState.Value!=Aiming)AimState.Value=Aiming;
            if(IsServer){ulong bits=0;for(int i=0;i<WeaponData.All.Length;i++)bits|=(ulong)Mathf.Clamp(magazines[i],0,63)<<(i*6);if(bits!=MagazineState.Value)MagazineState.Value=bits;}
        }
        void FireKick()
        {cameraKick=Mathf.Min(6,cameraKick+Recoil[Mathf.Clamp(Weapon.Value,0,WeaponData.All.Length-1)]*RecoilFactor(Weapon.Value)*(Crouched.Value?.78f:1));localBloom=Mathf.Min(2,localBloom+.23f);}
        float ShotSpread(WeaponData data,bool aimed,bool crouching)
        {float factor=data.pellets>1?(aimed?.78f:1.08f):aimed?.58f:2.1f;factor*=crouching?.82f:1;factor*=1+Mathf.Clamp(Velocity.Value,0,10)*.045f+serverBloom*.20f;serverBloom=Mathf.Min(2,serverBloom+.18f);float mods=(Mod(Weapon.Value,1)==5?.75f:1)*(Mod(Weapon.Value,2)==8?.8f:1)*(aimed&&Mod(Weapon.Value,0)>0?1-Mod(Weapon.Value,0)*.05f-.05f:1);return data.spread*factor*mods;}
        void SetUpgrade(int index,int value)
        {value=Mathf.Clamp(value,0,2);if(index==0)Upgrade0.Value=value;else if(index==1)Upgrade1.Value=value;else if(index==2)Upgrade2.Value=value;else if(index>=3&&index<7){int shift=(index-3)*2;OtherUpgrades.Value=(OtherUpgrades.Value&~(3<<shift))|(value<<shift);}}
    }
}
