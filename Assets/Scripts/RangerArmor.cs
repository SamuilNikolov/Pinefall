using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<int> ArmorTier=new NetworkVariable<int>(0),Armor=new NetworkVariable<int>(0),ArmorPlates=new NetworkVariable<int>(0);
        public static readonly string[] ArmorNames={"No vest","Light vest","Field vest","Heavy vest"};
        public static int ArmorMaximum(int tier){return tier==1?50:tier==2?75:tier==3?100:0;}
        public static float ArmorReduction(int tier){return tier==1?.25f:tier==2?.35f:tier==3?.50f:0;}
        int ProtectDamage(int amount,int source)
        {if(Armor.Value<=0||ArmorTier.Value<=0||source<0&&source!=-2)return amount;int covered=Mathf.Min(Armor.Value,Mathf.CeilToInt(amount*ArmorReduction(ArmorTier.Value)));Armor.Value-=covered;return amount-covered;}
        [ServerRpc]public void PlateServerRpc()
        {if(!Alive||!CanAct)return;if(ArmorTier.Value<=0){FeedbackClientRpc("Equip a vest before using an armor plate.");return;}if(ArmorPlates.Value<=0||Armor.Value>=ArmorMaximum(ArmorTier.Value))return;ArmorPlates.Value--;Armor.Value=Mathf.Min(ArmorMaximum(ArmorTier.Value),Armor.Value+30);FeedbackClientRpc("Armor repaired by 30 points.");}
    }
}
