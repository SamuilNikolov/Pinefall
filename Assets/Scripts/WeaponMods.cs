using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public static class WeaponMods
    {
        public static readonly string[] Names={"Empty","Reflex optic","Ranger optic","Marksman optic","Reinforced barrel","Precision barrel","Muzzle brake","Speed loader","Steady grip","Balanced stock"};
        public static readonly string[] Effects={"No attachment","1.5× aim; 10% tighter aimed spread","3× aim; 15% tighter aimed spread","6× aim; 20% tighter aimed spread","12% more bullet damage","25% tighter spread","30% less recoil","20% faster reload","20% tighter spread; 15% less recoil","25% less recoil"};
        public static int Slot(int id){return id<=3?0:id<=6?1:2;}public static string SlotName(int slot){return slot==0?"Optic":slot==1?"Barrel":"Handling";}
        public static bool Compatible(int weapon,int id){return weapon>=0&&weapon<WeaponData.All.Length&&id>=1&&id<=9&&(WeaponData.Get(weapon).category!=WeaponClass.Launcher||id==7)&&(id!=3||weapon==0||weapon==1||weapon==5||weapon==6);}
    }
    public sealed partial class Ranger
    {
        public NetworkVariable<uint> Optics=new NetworkVariable<uint>(0),Barrels=new NetworkVariable<uint>(0),Handling=new NetworkVariable<uint>(0);public NetworkVariable<ulong> ModBag=new NetworkVariable<ulong>(0);
        public int Mod(int weapon,int slot){if(weapon<0||weapon>=WeaponData.All.Length)return 0;uint bits=slot==0?Optics.Value:slot==1?Barrels.Value:Handling.Value;return (int)((bits>>(weapon*4))&15);}
        void SetMod(int weapon,int slot,int id){uint mask=15u<<(weapon*4),value=(uint)id<<(weapon*4);if(slot==0)Optics.Value=(Optics.Value&~mask)|value;else if(slot==1)Barrels.Value=(Barrels.Value&~mask)|value;else Handling.Value=(Handling.Value&~mask)|value;}
        public int ModCount(int id){return id<1||id>9?0:(int)((ModBag.Value>>((id-1)*4))&15);}public void SetModCountServer(int id,int count){if(!IsServer||id<1||id>9)return;int shift=(id-1)*4;ModBag.Value=(ModBag.Value&~(15ul<<shift))|((ulong)Mathf.Clamp(count,0,5)<<shift);}
        public int PackedMods(int weapon){return Mod(weapon,0)|(Mod(weapon,1)<<4)|(Mod(weapon,2)<<8);}
        void ImportMods(int weapon,int bits){for(int slot=0;slot<3;slot++){int id=(bits>>(slot*4))&15;SetMod(weapon,slot,WeaponMods.Compatible(weapon,id)&&WeaponMods.Slot(id)==slot?id:0);}}
        public float DamageFactor(int weapon){return Mod(weapon,1)==4?1.12f:1;}
        public float ReloadFactor(int weapon){return Mod(weapon,2)==7?.8f:1;}
        public float AccuracyFactor(int weapon){return (Mod(weapon,1)==5?.75f:1)*(Mod(weapon,2)==8?.8f:1)*(AimState.Value&&Mod(weapon,0)>0?1-Mod(weapon,0)*.05f-.05f:1);}
        public float RecoilFactor(int weapon){return (Mod(weapon,1)==6?.7f:1)*(Mod(weapon,2)==8?.85f:Mod(weapon,2)==9?.75f:1);}
        public float AimFov{get{int optic=Mod(Weapon.Value,0);float magnification=optic==1?1.5f:optic==2?3:optic==3?6:0;return magnification>0?2*Mathf.Atan(Mathf.Tan(74*.5f*Mathf.Deg2Rad)/magnification)*Mathf.Rad2Deg:WeaponData.Get(Weapon.Value).zoom;}}public bool ScopedView{get{return Aiming&&(Mod(Weapon.Value,0)>=2||Mod(Weapon.Value,0)==0&&(Weapon.Value==0||Weapon.Value==5));}}
        [ServerRpc]public void AttachModServerRpc(int weapon,int id){if(!Alive||!CanAct||!Owns(weapon)||!WeaponMods.Compatible(weapon,id)||ModCount(id)<1)return;int slot=WeaponMods.Slot(id),old=Mod(weapon,slot);if(old==id)return;if(old>0&&ModCount(old)>=5){FeedbackClientRpc("Make space in your attachment pack first.");return;}SetModCountServer(id,ModCount(id)-1);if(old>0)SetModCountServer(old,ModCount(old)+1);SetMod(weapon,slot,id);FeedbackClientRpc(WeaponMods.Names[id]+" fitted: "+WeaponMods.Effects[id]);}
        [ServerRpc]public void DetachModServerRpc(int weapon,int slot){if(!Alive||!CanAct||!Owns(weapon)||slot<0||slot>2)return;int id=Mod(weapon,slot);if(id==0||ModCount(id)>=5)return;SetMod(weapon,slot,0);SetModCountServer(id,ModCount(id)+1);}
    }
}
