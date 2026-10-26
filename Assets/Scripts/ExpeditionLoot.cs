using System;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public static class ExpeditionLoot
    {
        public static WorldLoot Spawn(Vector3 p,int kind,int item,int rarity,int amount,int tuning=0)
        {
            var obj=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("WorldLoot"),p,Quaternion.Euler(0,UnityEngine.Random.Range(0,360),0));var l=obj.GetComponent<WorldLoot>();
            l.Kind.Value=kind;l.Item.Value=item;l.Rarity.Value=rarity;l.Amount.Value=amount;l.Tuning.Value=tuning;obj.GetComponent<NetworkObject>().Spawn();return l;
        }
        public static RoadVehicle SpawnVehicle(Vector3 p,int type,int condition,int fuel){var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("RoadVehicle"),p,Quaternion.identity);var v=go.GetComponent<RoadVehicle>();v.Type.Value=type;v.Condition.Value=condition;v.Fuel.Value=fuel;go.GetComponent<NetworkObject>().Spawn();return v;}
        public static void Populate()
        {
            foreach(var l in UnityEngine.Object.FindObjectsOfType<WorldLoot>())if(l.IsSpawned)l.NetworkObject.Despawn();
            foreach(var v in UnityEngine.Object.FindObjectsOfType<RoadVehicle>())if(v.IsSpawned)v.NetworkObject.Despawn();
            var rng=new System.Random(Guid.NewGuid().GetHashCode());
            for(int r=0;r<ExpeditionLayout.Regions.Length;r++)
            {
                var district=ExpeditionLayout.Regions[r];if(!district.minor)for(int material=0;material<3;material++)Spawn(ReserveWorld.OnGround(district.center.x-6+material*2,district.center.y-10,.4f),13,material,0,30);
                for(int house=0;house<ExpeditionLayout.HouseCount(district);house++)for(int n=0;n<6;n++)
                {
                    var p=district.center+VillageFootprints.Offset(district,house)+new Vector2(-2+(n%3)*2,-1+(n/3)*2.5f);
                    int kind=n==0?0:n==1?1:n==2?5:n==3?3:n==5?(house%2==0?6:7):8;int weapon=district.biome==1||district.biome==2||district.biome==3?new[]{0,1,5,6}[rng.Next(4)]:new[]{1,2,3,4,6}[rng.Next(5)];
                    if(rng.NextDouble()<.02)weapon=7;
                    // Every region can roll each rarity; high-value sites shift the probabilities.
                    double roll=rng.NextDouble();int tier=roll<.53-district.quality*.055?0:roll<.82-district.quality*.025?1:roll<.955-district.quality*.012?2:roll<.995-district.quality*.004?3:4;
                    if(weapon==7)tier=Mathf.Max(2,tier);int armorTier=Mathf.Clamp(tier+1,1,3);Spawn(ReserveWorld.OnGround(p.x,p.y,.4f),kind,kind==0?weapon:kind==1?WeaponData.Get(weapon).ammoType:kind==6?armorTier:kind==8?rng.Next(1,10):0,tier,kind==0?-1:kind==1?WeaponData.Get(weapon).magazine*2:kind==3?2:kind==6?Ranger.ArmorMaximum(armorTier):kind==7?2:1);
                }
                if(district.minor)Spawn(ReserveWorld.OnGround(district.center.x+7,district.center.y-9,.4f),11,0,2,1);
                if(r%2==0)Spawn(ReserveWorld.OnGround(district.center.x+5,district.center.y-12,.4f),4,0,0,1);
                var vp=ExpeditionLayout.VehiclePoint(r);var location=district.vehicle==4?ReserveWater.BoatPoint(district.center):ReserveWorld.OnGround(vp.x,vp.y,.65f);var g=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("RoadVehicle"),location,Quaternion.Euler(0,r*47,0));var car=g.GetComponent<RoadVehicle>();
                car.Type.Value=district.vehicle;car.Condition.Value=r==3||r==6?0:rng.Next(30,85);car.Fuel.Value=rng.Next(12,54);g.GetComponent<NetworkObject>().Spawn();
            }
            int stopIndex=0;foreach(var p in ExpeditionLayout.MobilityStops()){Spawn(ReserveWorld.OnGround(p.x+2,p.y,.4f),4,0,0,2);Spawn(ReserveWorld.OnGround(p.x-2,p.y,.4f),3,0,0,3);Spawn(ReserveWorld.OnGround(p.x,p.y+2,.4f),11,0,1,1);SpawnVehicle(ReserveWorld.OnGround(p.x,p.y+6,.65f),stopIndex++%3==0?5:stopIndex%4,75,65);}
            foreach(var p in ExpeditionLayout.BoatStops()){SpawnVehicle(p,4,90,70);Spawn(ReserveWater.LootPosition(p.x+2,p.z),4,0,0,1);}

        }
    }
}
