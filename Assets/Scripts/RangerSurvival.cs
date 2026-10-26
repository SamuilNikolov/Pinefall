using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<float> Hunger=new NetworkVariable<float>(85);
        public NetworkVariable<int> Food=new NetworkVariable<int>(1);
        public NetworkVariable<int> OrderStep=new NetworkVariable<int>(0),OrderRegion=new NetworkVariable<int>(0);
        public NetworkVariable<int> OrderCycle=new NetworkVariable<int>(0);readonly System.Collections.Generic.HashSet<int> orderVisited=new System.Collections.Generic.HashSet<int>();int orderHunts,orderStage;float nextOrder;
        float hungerTick,starveAt;int lastHungerBand=3;
        public bool CanSprint{get{return Hunger.Value>=20;}}
        public string HungerStatus{get{return Hunger.Value<=0?"Starving · losing health":Hunger.Value<20?"Hungry · sprint locked":Hunger.Value<40?"Find food soon":Hunger.Value>=75?"Well fed":"Fed";}}
        public int WeaponCount{get{int count=0;for(int i=0;i<7;i++)if(Owns(i))count++;return count;}}
        public int MagazineFor(int i){return i==Weapon.Value?Ammo.Value:IsServer?magazines[Mathf.Clamp(i,0,6)]:(int)((MagazineState.Value>>(Mathf.Clamp(i,0,6)*6))&63);}
        void SurvivalTick()
        {
            if(!IsServer||!Alive||!CanAct||Time.time<hungerTick||WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value&&!WildfrontMatch.Current.Active)return;hungerTick=Time.time+1;
            Hunger.Value=Mathf.Max(0,Hunger.Value-(Velocity.Value>5?.15f:CurrentVehicle?.045f:.065f));int band=Hunger.Value<=0?0:Hunger.Value<20?1:Hunger.Value<40?2:3;if(band<lastHungerBand)FeedbackClientRpc(band==2?"Getting hungry. Hunt wildlife or search houses for food.":band==1?"Hunger is low: eat food to restore sprinting.":"Starving: food will stop the health loss.");lastHungerBand=band;
            if(Hunger.Value<=0&&Time.time>=starveAt){starveAt=Time.time+3;TakeDamageServer(3,-3);}
            var match=WildfrontMatch.Current;if(!match||!match.Active)return;
            var site=ExpeditionLayout.Regions[OrderRegion.Value].center;
            if(OrderStep.Value==3&&Time.time>nextOrder){OrderCycle.Value++;SetFieldOrders(ExpeditionLayout.Nearest(transform.position));FeedbackClientRpc("New field orders received. Check your map for the next stop.");}
            if(OrderStep.Value==0&&Vector2.Distance(new Vector2(transform.position.x,transform.position.z),site)<40){orderVisited.Add(OrderRegion.Value);OrderStep.Value=1;orderHunts=Score.Value;Craft.Value=Mathf.Min(50,Craft.Value+2);Food.Value=Mathf.Min(8,Food.Value+1);FeedbackClientRpc("Field order complete: site reached. +2 materials and food. Next: hunt wildlife.");}
            else if(OrderStep.Value==1&&Score.Value>orderHunts){OrderStep.Value=2;orderStage=match.Stage.Value+1;Craft.Value=Mathf.Min(50,Craft.Value+2);FeedbackClientRpc("Field order complete: successful hunt. +2 materials. Next: survive a storm cycle inside the safe zone.");}
            else if(OrderStep.Value==2&&match.Stage.Value>=orderStage&&!match.Outside(transform.position)){OrderStep.Value=3;nextOrder=Time.time+30;int mod=1+(OrderCycle.Value*3+OrderRegion.Value)%9;SetModCountServer(mod,ModCount(mod)+1);Medkits.Value=Mathf.Min(5,Medkits.Value+1);FeedbackClientRpc("Field orders complete: medkit and attachment awarded. New orders arrive shortly.");}
        }
        public void SetFieldOrders(int spawnRegion)
        {
            if(!IsServer)return;orderVisited.Add(spawnRegion);if(orderVisited.Count>=ExpeditionLayout.Regions.Length){orderVisited.Clear();orderVisited.Add(spawnRegion);}OrderStep.Value=0;orderHunts=0;var center=ExpeditionLayout.Regions[spawnRegion].center;int best=-1;float distance=float.MaxValue;
            for(int i=0;i<ExpeditionLayout.Regions.Length;i++){float d=(ExpeditionLayout.Regions[i].center-center).sqrMagnitude;if(i!=spawnRegion&&!orderVisited.Contains(i)&&d<distance){distance=d;best=i;}}OrderRegion.Value=Mathf.Max(0,best);
        }
        public string FieldOrder{get{return OrderStep.Value==0?"Reach "+ExpeditionLayout.Regions[OrderRegion.Value].name:OrderStep.Value==1?"Hunt wildlife · +2 materials":OrderStep.Value==2?"Survive a storm cycle in the safe zone":"Orders complete · new assignment shortly";}}
        [ServerRpc]public void EatServerRpc()
        {if(!Alive||!CanAct||Food.Value<=0||Hunger.Value>=100)return;Food.Value--;Hunger.Value=Mathf.Min(100,Hunger.Value+35);Health.Value=Mathf.Min(100,Health.Value+5);FeedbackClientRpc("Food eaten: +35 hunger, +5 health.");}
        [ServerRpc]public void DropItemServerRpc(int kind,int item,int quantity)
        {if(!Alive||!CanAct||CurrentVehicle)return;DropItem(kind,item,quantity);}
        void DropItem(int kind,int item,int quantity)
        {
            int amount=0,rarity=0,tuning=0;quantity=Mathf.Clamp(quantity,1,400);
            if(kind==0)
            {
                if(!Owns(item))return;tuning=PackedMods(item);ImportMods(item,0);rarity=Rarity(item);amount=MagazineFor(item);Owned.Value&=~(1<<item);ClearSlot(item);magazines[item]=0;
                if(Weapon.Value==item){Weapon.Value=CycleWeapon(1);if(!Owns(Weapon.Value))Weapon.Value=-1;Ammo.Value=Weapon.Value>=0?magazines[Weapon.Value]:0;Reloading.Value=false;}
            }
            else if(kind==12){amount=Mathf.Min(quantity,SurveyCharges.Value);SurveyCharges.Value-=amount;}
            else if(kind==8){if(item<1||item>9||ModCount(item)==0)return;amount=1;SetModCountServer(item,ModCount(item)-1);}
            else if(kind==1){if(item<0||item>3)return;amount=Mathf.Min(quantity,ReserveFor(item));SetReserve(item,ReserveFor(item)-amount);}
            else if(kind==2){amount=Mathf.Min(quantity,Medkits.Value);Medkits.Value-=amount;}
            else if(kind==3){amount=Mathf.Min(quantity,Craft.Value);Craft.Value-=amount;}
            else if(kind==4){amount=Mathf.Min(quantity,FuelCans.Value);FuelCans.Value-=amount;}
            else if(kind==5){amount=Mathf.Min(quantity,Food.Value);Food.Value-=amount;}else if(kind==6){if(ArmorTier.Value<=0)return;item=ArmorTier.Value;amount=Armor.Value;ArmorTier.Value=0;Armor.Value=0;}else if(kind==7){amount=Mathf.Min(quantity,ArmorPlates.Value);ArmorPlates.Value-=amount;}else return;
            if(kind!=0&&kind!=6&&amount==0)return;
            var p=transform.position+transform.forward*1.25f+transform.right*UnityEngine.Random.Range(-.65f,.65f);
            ExpeditionLoot.Spawn(ReserveWater.LootPosition(p.x,p.z),kind,item,rarity,amount,tuning);
        }
        void DropBackpack()
        {
            for(int id=1;id<=9;id++)while(ModCount(id)>0)DropItem(8,id,1);
            for(int i=0;i<7;i++)if(Owns(i))DropItem(0,i,1);
            for(int i=0;i<4;i++)DropItem(1,i,400);
            DropItem(12,0,2);DropItem(2,0,5);DropItem(3,0,400);DropItem(4,0,5);DropItem(5,0,8);DropItem(6,0,1);DropItem(7,0,6);
        }
    }
}
