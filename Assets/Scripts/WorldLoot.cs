using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class WorldLoot:NetworkBehaviour
    {
        void Awake(){NetworkSeed.Initialize(this);WorldInterest.Install(this);}
        public NetworkVariable<int> Tuning=new NetworkVariable<int>(0);
        public static readonly List<WorldLoot> All=new List<WorldLoot>();
        public NetworkVariable<int> Kind=new NetworkVariable<int>(0),Item=new NetworkVariable<int>(0),Rarity=new NetworkVariable<int>(0),Amount=new NetworkVariable<int>(1);
        public string Description {get{return Kind.Value==15?"Supply relay · call a contestable supply flight":Kind.Value==14?"Launchpad · F5 to attach to an allied floor":Kind.Value==13?BuildPiece.Materials[Mathf.Clamp(Item.Value,0,2)]+" building material ×"+Amount.Value:Kind.Value==12?"Survey beacon · reveals positions for 12 seconds":Kind.Value==11?"Sealed field cache · equipment and supplies":Kind.Value==9?"Rich meat · +20 health, +15 hunger":Kind.Value==8?WeaponMods.Names[Mathf.Clamp(Item.Value,1,9)]:Kind.Value==0?WeaponData.Rarities[Mathf.Clamp(Rarity.Value,0,4)]+" "+WeaponData.Get(Item.Value).name:Kind.Value==1?WeaponData.AmmoNames[Mathf.Clamp(Item.Value,0,WeaponData.AmmoNames.Length-1)]+" ammunition ×"+Amount.Value:Kind.Value==2?"Medkit ×"+Amount.Value:Kind.Value==3?"Repair materials ×"+Amount.Value:Kind.Value==4?"Fuel can ×"+Amount.Value:Kind.Value==6?Ranger.ArmorNames[Mathf.Clamp(Item.Value,1,3)]+" · "+Amount.Value+" durability":Kind.Value==7?"Armor plates ×"+Amount.Value:"Field food ×"+Amount.Value;}}
        public override void OnNetworkSpawn(){All.Add(this);var visual=gameObject.AddComponent<LootVisual>();PresentationBudget.Register(visual,190);}
        public override void OnNetworkDespawn(){WorldInterest.Forget(NetworkObject);All.Remove(this);}
        public static WorldLoot Near(Vector3 p,float distance=3.4f)
        {WorldLoot best=null;foreach(var item in All){if(!item)continue;float d=Vector3.Distance(p,item.transform.position);if(d<distance){distance=d;best=item;}}return best;}
    }
}
