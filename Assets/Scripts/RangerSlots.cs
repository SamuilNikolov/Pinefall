using Unity.Netcode;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<int> Slot0=new NetworkVariable<int>(3),Slot1=new NetworkVariable<int>(-1),Slot2=new NetworkVariable<int>(-1);
        public int SlotWeapon(int index){return index==0?Slot0.Value:index==1?Slot1.Value:Slot2.Value;}
        void SetSlot(int index,int weapon){if(index==0)Slot0.Value=weapon;else if(index==1)Slot1.Value=weapon;else Slot2.Value=weapon;}
        void InitialSlots(int primary){Slot0.Value=primary;Slot1.Value=primary==3?-1:3;Slot2.Value=-1;}
        void AddSlot(int weapon){for(int i=0;i<3;i++)if(SlotWeapon(i)==weapon)return;for(int i=0;i<3;i++)if(SlotWeapon(i)<0){SetSlot(i,weapon);return;}}
        void ClearSlot(int weapon){for(int i=0;i<3;i++)if(SlotWeapon(i)==weapon)SetSlot(i,-1);}
        [ServerRpc]public void SwapSlotsServerRpc(int a,int b)
        {if(!Alive||!CanAct||a<0||a>2||b<0||b>2||a==b)return;int first=SlotWeapon(a),second=SlotWeapon(b);SetSlot(a,second);SetSlot(b,first);}
    }
}
