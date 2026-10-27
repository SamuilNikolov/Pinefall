using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<bool> IsBot=new NetworkVariable<bool>(false);
        public NetworkVariable<int> Owned=new NetworkVariable<int>(1<<3),RarityBits=new NetworkVariable<int>(0),OtherUpgrades=new NetworkVariable<int>(0);
        public NetworkVariable<int> RifleReserve=new NetworkVariable<int>(0),RocketReserve=new NetworkVariable<int>(0),LightReserve=new NetworkVariable<int>(0),ShellReserve=new NetworkVariable<int>(0),PistolReserve=new NetworkVariable<int>(24),Medkits=new NetworkVariable<int>(0),FuelCans=new NetworkVariable<int>(0);
        public NetworkVariable<int> Appearance=new NetworkVariable<int>(0),Palette=new NetworkVariable<int>(0),Headgear=new NetworkVariable<int>(0);
        public NetworkVariable<ulong> VehicleId=new NetworkVariable<ulong>(RoadVehicle.Empty);
        public NetworkVariable<int> VehicleSeat=new NetworkVariable<int>(-1);
        float nextDrive;
        public bool LocalHuman {get{return IsOwner&&!IsBot.Value;}}
        public bool CanAct {get{return !Downed.Value&&(!WildfrontMatch.Current||WildfrontMatch.Current.Phase.Value<2);}}
        public float LookPitch {get{return pitch-cameraKick;}}
        public bool Owns(int index){return index>=0&&index<WeaponData.All.Length&&(Owned.Value&(1<<index))!=0;}
        public int Rarity(int index){return (RarityBits.Value>>(Mathf.Clamp(index,0,WeaponData.All.Length-1)*3))&7;}
        public int ReserveFor(int type){return type==0?RifleReserve.Value:type==1?LightReserve.Value:type==2?ShellReserve.Value:type==4?RocketReserve.Value:PistolReserve.Value;}
        void SetReserve(int type,int value){value=Mathf.Clamp(value,0,type==4?12:400);if(type==0)RifleReserve.Value=value;else if(type==1)LightReserve.Value=value;else if(type==2)ShellReserve.Value=value;else if(type==4)RocketReserve.Value=value;else PistolReserve.Value=value;}
        public RoadVehicle CurrentVehicle
        {get{NetworkObject obj;return VehicleId.Value!=RoadVehicle.Empty&&NetworkManager&&NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(VehicleId.Value,out obj)?obj.GetComponent<RoadVehicle>():null;}}
        public void InitializeEquipment(bool sandbox)
        {
            if(!IsServer)return;LaunchPads.Value=0;ResetBuildingMaterials();ResetCombatCadence();ResetSurvey();magazines=WeaponData.All.Select(w=>w.magazine).ToArray();
            Optics.Value=Barrels.Value=Handling.Value=0;ModBag.Value=0;Owned.Value=sandbox?((1<<Mathf.Clamp(ReserveSession.Instance.SelectedWeapon,0,WeaponData.All.Length-1))|(1<<3)):1<<3;RarityBits.Value=0;RifleReserve.Value=LightReserve.Value=ShellReserve.Value=sandbox?100:0;PistolReserve.Value=sandbox?120:24;RocketReserve.Value=sandbox?3:0;
            Weapon.Value=sandbox?Mathf.Clamp(ReserveSession.Instance.SelectedWeapon,0,WeaponData.All.Length-1):3;Ammo.Value=magazines[Weapon.Value];Medkits.Value=sandbox?2:0;FuelCans.Value=sandbox?1:0;Food.Value=1;Hunger.Value=85;ArmorTier.Value=0;Armor.Value=0;ArmorPlates.Value=0;InitialSlots(Weapon.Value);
        }
        public void ApplyKit(int kitIndex,int unspent)
        {
            if(!IsServer)return;var kit=StartingKit.All[Mathf.Clamp(kitIndex,0,StartingKit.All.Length-1)];InitializeEquipment(false);
            InitialSlots(kit.primary);Owned.Value|=1<<kit.primary;Weapon.Value=kit.primary;Ammo.Value=magazines[kit.primary];
            RarityBits.Value=kit.rarity<<(kit.primary*3);ArmorTier.Value=kit.armor;Armor.Value=ArmorMaximum(kit.armor);Craft.Value=kit.materials;Medkits.Value=kit.medkits;FuelCans.Value=kit.fuel;
            int extra=Mathf.Max(0,unspent-kit.cost)/2;int type=WeaponData.Get(kit.primary).ammoType;SetReserve(type,ReserveFor(type)+WeaponData.Get(kit.primary).magazine*(kit.spareMags+extra));
        }
        public int CycleWeapon(int direction)
        {int slot=0;for(int i=0;i<3;i++)if(SlotWeapon(i)==Weapon.Value)slot=i;for(int i=1;i<=3;i++){int weapon=SlotWeapon((slot+direction*i+6)%3);if(Owns(weapon))return weapon;}return Weapon.Value;}
        void CompleteReload()
        {
            int type=WeaponData.Get(Weapon.Value).ammoType,need=WeaponData.Get(Weapon.Value).magazine-Ammo.Value;
            int count=Mathf.Min(need,ReserveFor(type));SetReserve(type,ReserveFor(type)-count);Ammo.Value+=count;magazines[Weapon.Value]=Ammo.Value;Reloading.Value=false;
        }
        [ServerRpc]public void AppearanceServerRpc(int model,int palette,int gear)
        {Appearance.Value=Mathf.Clamp(model,0,1);Palette.Value=Mathf.Clamp(palette,0,5);Headgear.Value=Mathf.Clamp(gear,0,2);}
        [ServerRpc]public void ScavengeServerRpc(ulong id)
        {
            NetworkObject obj;if(!Alive||!CanAct||!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id,out obj))return;
            var loot=obj.GetComponent<WorldLoot>();if(!loot||Vector3.Distance(transform.position,loot.transform.position)>4)return;string collected=loot.Description;
            if(loot.Kind.Value==0)
            {
                int index=Mathf.Clamp(loot.Item.Value,0,WeaponData.All.Length-1);int old=Rarity(index);bool fresh=!Owns(index);Owned.Value|=1<<index;
                if(fresh&&WeaponCount>3){Owned.Value&=~(1<<index);FeedbackClientRpc("Three weapon slots are full. Drop a gun in your inventory first.");return;}
                if(fresh||loot.Rarity.Value>old){int mask=7<<(index*3);RarityBits.Value=(RarityBits.Value&~mask)|(loot.Rarity.Value<<(index*3));}
                if(loot.Amount.Value<0)SetReserve(WeaponData.Get(index).ammoType,ReserveFor(WeaponData.Get(index).ammoType)+WeaponData.Get(index).magazine);
                int rounds=loot.Amount.Value<0?WeaponData.Get(index).magazine:loot.Amount.Value;
                if(fresh)AddSlot(index);if(fresh)ImportMods(index,loot.Tuning.Value);
                if(fresh){if(Weapon.Value>=0)magazines[Weapon.Value]=Ammo.Value;magazines[index]=rounds;Weapon.Value=index;Ammo.Value=rounds;Reloading.Value=false;}
                else {SetReserve(WeaponData.Get(index).ammoType,ReserveFor(WeaponData.Get(index).ammoType)+rounds);for(int slot=0;slot<3;slot++){int modId=(loot.Tuning.Value>>(slot*4))&15;if(!WeaponMods.Compatible(index,modId))continue;if(ModCount(modId)<5)SetModCountServer(modId,ModCount(modId)+1);else ExpeditionLoot.Spawn(loot.transform.position+Vector3.right*.3f,8,modId,0,1);}}
            }
            else if(loot.Kind.Value==14){if(LaunchPads.Value>=3){FeedbackClientRpc("Launchpad pack full. Carry at most three.");return;}LaunchPads.Value++;}
            else if(loot.Kind.Value==15){double now=NetworkManager.ServerTime.Time;if(loot.Amount.Value>now){FeedbackClientRpc("Relay cooling down. Come back shortly.");return;}if(!WildfrontMatch.Current||!WildfrontMatch.Current.Active)return;loot.Amount.Value=Mathf.CeilToInt((float)now+180);WildfrontMatch.Current.SpawnSupply(loot.transform.position+Vector3.right*18);WildfrontMatch.Current.NarrateEvent("relay");FeedbackClientRpc("Supply flight called. The crate is visible to every team.");return;}
            else if(loot.Kind.Value==13){int material=loot.Item.Value;if(material<0||material>2)return;int accepted=Mathf.Min(999-BuildingMaterial(material),loot.Amount.Value);if(accepted<=0)return;AddBuildingMaterial(material,accepted);loot.Amount.Value-=accepted;if(loot.Amount.Value>0)return;}
            else if(loot.Kind.Value==12){if(SurveyCharges.Value>=2){FeedbackClientRpc("Survey beacon pack full (two). Use or share one.");return;}SurveyCharges.Value++;}
            else if(loot.Kind.Value==11){var at=loot.transform.position;ExpeditionLoot.Spawn(at+Vector3.left*.65f,8,UnityEngine.Random.Range(1,10),2,1);ExpeditionLoot.Spawn(at+Vector3.right*.65f,2,0,0,1);ExpeditionLoot.Spawn(at+Vector3.forward*.65f,7,0,1,2);FeedbackClientRpc("Field cache opened: an attachment, medicine and armor plates.");}
            else if(loot.Kind.Value==9){if(Health.Value>=100&&Hunger.Value>=100){FeedbackClientRpc("You are already well fed and healthy. Leave the rich meat for a teammate.");return;}Health.Value=Mathf.Min(100,Health.Value+20);Hunger.Value=Mathf.Min(100,Hunger.Value+15);}
            else if(loot.Kind.Value==8){int modId=loot.Item.Value;if(modId<1||modId>9||ModCount(modId)>=5){FeedbackClientRpc("Attachment stack full.");return;}SetModCountServer(modId,ModCount(modId)+1);}
            else if(loot.Kind.Value==6){if(ArmorTier.Value>0)DropItem(6,0,1);ArmorTier.Value=Mathf.Clamp(loot.Item.Value,1,3);Armor.Value=Mathf.Clamp(loot.Amount.Value,0,ArmorMaximum(ArmorTier.Value));VestEquippedClientRpc();}
            else
            {
                int kind=loot.Kind.Value,type=loot.Item.Value,have=kind==1?ReserveFor(type):kind==2?Medkits.Value:kind==3?Craft.Value:kind==4?FuelCans.Value:kind==7?ArmorPlates.Value:Food.Value;
                int capacity=kind==1?(type==4?12:400):kind==2||kind==4?5:kind==3?50:kind==7?6:8;int accepted=Mathf.Min(Mathf.Max(0,capacity-have),loot.Amount.Value);
                if(accepted<=0){FeedbackClientRpc("That inventory stack is full. Drop or use some first.");return;}
                if(kind==1)SetReserve(type,have+accepted);else if(kind==2)Medkits.Value+=accepted;else if(kind==3)Craft.Value+=accepted;else if(kind==4)FuelCans.Value+=accepted;else if(kind==7)ArmorPlates.Value+=accepted;else Food.Value+=accepted;
                loot.Amount.Value-=accepted;if(loot.Amount.Value>0){FeedbackClientRpc("Collected "+accepted+". The rest stays on the ground.");return;}
            }
            loot.NetworkObject.Despawn();FeedbackClientRpc("Collected "+collected);
        }

        [ServerRpc]public void HealServerRpc()
        {if(!Alive||!CanAct||Health.Value>=100||Medkits.Value<=0)return;Medkits.Value--;Health.Value=Mathf.Min(100,Health.Value+45);FeedbackClientRpc("Medkit used: +45 health.");}
        public void InformServer(string text){if(IsServer)FeedbackClientRpc(text);}
        [ClientRpc]void FeedbackClientRpc(string text){if(LocalHuman)ShowNotice(text,3);}
        [ServerRpc]public void EnterVehicleServerRpc(ulong id)
        {NetworkObject obj;if(NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id,out obj)){var vehicle=obj.GetComponent<RoadVehicle>();if(vehicle&&!vehicle.Enter(this))FeedbackClientRpc("Cannot enter: check the engine, free seats, and distance.");}}
        [ServerRpc]public void ExitVehicleServerRpc(){ExitVehicleServer();}
        public void ExitVehicleServer()
        {var v=CurrentVehicle;if(!IsServer||!v)return;Vector3 pos=v.ExitPoint(VehicleSeat.Value);v.Remove(this);VehicleExitClientRpc(pos);}
        [ClientRpc]void VehicleExitClientRpc(Vector3 point){if(LocalHuman)SmokeMoveTo(point);}
        [ServerRpc]public void ServiceVehicleServerRpc(ulong id,int service)
        {
            NetworkObject obj;if(!Alive||!CanAct||!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id,out obj))return;
            var v=obj.GetComponent<RoadVehicle>();if(!v||Vector3.Distance(transform.position,v.transform.position)>5)return;
            if(service==0&&Craft.Value>=2&&v.Condition.Value<100){Craft.Value-=2;v.Condition.Value=Mathf.Min(100,v.Condition.Value+30);FeedbackClientRpc("Engine repaired. Two materials used.");}
            else if(service==1&&FuelCans.Value>0&&v.Fuel.Value<90){FuelCans.Value--;v.Fuel.Value=Mathf.Min(100,v.Fuel.Value+45);FeedbackClientRpc("Vehicle refuelled.");}
            else if(service==2&&Craft.Value>=3&&v.Tune.Value<2){Craft.Value-=3;v.Tune.Value++;FeedbackClientRpc("Vehicle tuned: better speed and boost.");}
            else FeedbackClientRpc("Need supplies, or this service is already complete.");
        }
        bool VehicleControls(bool controls)
        {
            var vehicle=CurrentVehicle;if(!vehicle)return false;
            if(controller)controller.enabled=false;transform.SetPositionAndRotation(vehicle.SeatPosition(VehicleSeat.Value),vehicle.transform.rotation);
            if(Velocity.Value!=0)Velocity.Value=0;MountedLook(controls);
            if(controls)
            {
                if(GameBindings.Down("Interact"))ExitVehicleServerRpc();
                if(VehicleSeat.Value==0&&Time.time>nextDrive){nextDrive=Time.time+.05f;vehicle.DriveServerRpc(GameBindings.Vertical,GameBindings.Horizontal,GameBindings.Held("Brake"),GameBindings.Held("Sprint"));}
            }
            return true;
        }
        public string Interaction()
        {
            string use=GameBindings.Label("Interact");if(CurrentVehicle)return use+" Exit   "+GameBindings.Label("Forward")+"/"+GameBindings.Label("Backward")+" Drive   Mouse Look   "+GameBindings.Label("Sprint")+" Boost";
            var friend=NearbyDowned();if(friend)return "Hold "+use+" · Revive "+friend.DisplayName.Value;var supply=SupplyDrop.Near(transform.position);if(supply)return use+" · Open supply crate";var loot=WorldLoot.Near(transform.position);if(loot)return use+" · "+loot.Description;var reward=NearestPickup();if(reward)return use+" · Collect harvest";var car=RoadVehicle.Near(transform.position);if(car)return car.Label+" · "+use+" Enter · "+GameBindings.Label("Repair")+" Repair · "+GameBindings.Label("Refuel")+" Refuel · "+GameBindings.Label("Vehicle upgrade")+" Upgrade";return "";
        }
        public void BotShoot(Vector3 direction){if(IsServer&&Alive)ServerBotFire(transform.position+Vector3.up*1.5f,direction.normalized);}
    }
}
