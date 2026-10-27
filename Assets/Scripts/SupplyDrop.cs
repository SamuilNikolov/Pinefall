using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class SupplyDrop:NetworkBehaviour
    {
        void Awake(){NetworkSeed.Initialize(this);}
        public static readonly List<SupplyDrop> All=new List<SupplyDrop>();
        public NetworkVariable<double> Began=new NetworkVariable<double>(0);public NetworkVariable<bool> Opened=new NetworkVariable<bool>(false);
        public bool Landed{get{return NetworkManager&&NetworkManager.ServerTime.Time>=Began.Value+26;}}
        Transform crate,canopy,plane;Material fabric,metal,paint;bool announced;readonly List<LineRenderer> ropes=new List<LineRenderer>();
        public override void OnNetworkSpawn(){All.Add(this);Build();}
        public override void OnNetworkDespawn(){All.Remove(this);}
        public static SupplyDrop Near(Vector3 p){foreach(var d in All)if(d&&d.Landed&&!d.Opened.Value&&(d.transform.position-p).sqrMagnitude<4.5f*4.5f)return d;return null;}
        void Build()
        {
            fabric=WeaponView.Mat("Supply parachute",new Color(.73f,.66f,.47f));metal=WeaponView.Mat("Supply latches",new Color(.20f,.23f,.22f),.55f);paint=WeaponView.Mat("Aircraft paint",new Color(.40f,.45f,.43f),.3f);
            crate=new GameObject("Supply crate").transform;crate.SetParent(transform,false);WeaponView.Part(crate,PrimitiveType.Cube,Vector3.zero,new Vector3(1.35f,1.1f,1.15f),metal);foreach(float x in new[]{-.48f,.48f})WeaponView.Part(crate,PrimitiveType.Cube,new Vector3(x,0,0),new Vector3(.13f,1.17f,1.22f),fabric);
            canopy=new GameObject("Parachute").transform;canopy.SetParent(crate,false);canopy.localPosition=Vector3.up*5;WeaponView.Part(canopy,PrimitiveType.Sphere,Vector3.zero,new Vector3(6,.8f,6),fabric);
            for(int i=0;i<4;i++){var g=new GameObject("Suspension cord");g.transform.SetParent(crate,false);var line=g.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;float x=i%2==0?-1:1,z=i/2==0?-1:1;line.SetPosition(0,new Vector3(x*.5f,.5f,z*.5f));line.SetPosition(1,new Vector3(x*2,5,z*2));line.startWidth=line.endWidth=.025f;line.sharedMaterial=metal;ropes.Add(line);}
            plane=new GameObject("Supply aircraft").transform;plane.SetParent(transform,false);WeaponView.Part(plane,PrimitiveType.Capsule,Vector3.zero,new Vector3(4,11,4),paint,new Vector3(90,0,0));WeaponView.Part(plane,PrimitiveType.Cube,new Vector3(0,0,-1),new Vector3(29,.35f,4.8f),paint,new Vector3(0,0,-2));WeaponView.Part(plane,PrimitiveType.Cube,new Vector3(0,2,-9),new Vector3(10,.28f,2.8f),paint);WeaponView.Part(plane,PrimitiveType.Cube,new Vector3(0,3.3f,-8.6f),new Vector3(.4f,5.5f,3),paint);
            foreach(float side in new[]{-1f,1f})foreach(float x in new[]{6f,10f})WeaponView.Part(plane,PrimitiveType.Capsule,new Vector3(side*x,-.8f,.6f),new Vector3(1.3f,2.3f,1.3f),metal,new Vector3(90,0,0));
            foreach(var t in GetComponentsInChildren<Transform>())t.gameObject.layer=0;
        }
        void Update()
        {
            if(!IsSpawned)return;float elapsed=(float)(NetworkManager.ServerTime.Time-Began.Value);plane.gameObject.SetActive(elapsed<23);plane.localPosition=new Vector3((elapsed-10)*115,220,0);plane.localRotation=Quaternion.Euler(0,90,0);
            crate.gameObject.SetActive(elapsed>=10&&!Opened.Value);crate.localPosition=Vector3.up*(Landed?.6f:Mathf.Lerp(220,.6f,Mathf.Clamp01((elapsed-10)/16)));canopy.gameObject.SetActive(!Landed);foreach(var r in ropes)r.enabled=!Landed;
            if(IsServer&&Landed&&!announced){announced=true;if(WildfrontMatch.Current)WildfrontMatch.Current.NarrateEvent("landed");}
        }
        [ServerRpc(RequireOwnership=false)]public void OpenServerRpc(ServerRpcParams rpc=default)
        {
            var player=Ranger.Players.Find(p=>p&&!p.IsBot.Value&&p.OwnerClientId==rpc.Receive.SenderClientId);
            if(!Landed||Opened.Value||!player||!player.Fighting||!player.CanAct||(player.transform.position-transform.position).sqrMagnitude>25)return;
            Opened.Value=true;Vector3 p=transform.position;int special=SupplySpecials.Choose(UnityEngine.Random.value),bikesBefore=RoadVehicle.All.Count(v=>v.Type.Value==5);if(SupplySpecials.Release(p,special))player.InformServer(RoadVehicle.All.Count(v=>v.Type.Value==5)>bikesBefore?"Bonus motorcycle delivered nearby. Look for the blue bike icon.":"Survey beacon delivered beside the crate. Collect it, then press your Survey key to scan.");int weapon=UnityEngine.Random.Range(0,WeaponData.All.Length);int rarity=UnityEngine.Random.value<.22f?4:3;
            ExpeditionLoot.Spawn(p+new Vector3(-.9f,.15f,0),0,weapon,rarity,-1);ExpeditionLoot.Spawn(p+new Vector3(.9f,.15f,0),1,WeaponData.Get(weapon).ammoType,0,WeaponData.Get(weapon).magazine*3);ExpeditionLoot.Spawn(p+new Vector3(0,.15f,.8f),2,0,0,2);ExpeditionLoot.Spawn(p+new Vector3(0,.15f,-.8f),5,0,0,3);ExpeditionLoot.Spawn(p+new Vector3(.7f,.15f,.7f),6,3,3,100);ExpeditionLoot.Spawn(p+new Vector3(-.7f,.15f,-.7f),7,0,0,3);if(WildfrontMatch.Current)WildfrontMatch.Current.NarrateEvent("claimed");
        }
        public override void OnDestroy(){All.Remove(this);if(fabric)Destroy(fabric);if(metal)Destroy(metal);if(paint)Destroy(paint);base.OnDestroy();}
    }
    public sealed partial class WildfrontMatch
    {
        double nextSupply;int supplyCount,announcedStage=-1;bool warningSaid,closingSaid;
        public SupplyDrop SpawnSupply(Vector3 target,bool immediate=false)
        {
            if(!IsServer)return null;float surface;target.y=ReserveWater.Surface(target.x,target.z,out surface)?surface+.2f:ReserveWorld.Height(target.x,target.z)+.2f;var go=Instantiate(Resources.Load<GameObject>("SupplyDrop"),target,Quaternion.identity);var d=go.GetComponent<SupplyDrop>();d.Began.Value=NetworkManager.ServerTime.Time-(immediate?27:0);go.GetComponent<NetworkObject>().Spawn();NarrateEvent("inbound");supplyCount++;return d;
        }
        void SupplyTick()
        {
            if(!IsServer||!Active)return;double now=NetworkManager.ServerTime.Time;
            if(announcedStage!=Stage.Value){announcedStage=Stage.Value;warningSaid=closingSaid=false;if(Stage.Value>0)NarrateEvent("safe");}
            if(!warningSaid&&now>=ShrinkAt.Value-12){warningSaid=true;NarrateEvent("warning");}
            if(!closingSaid&&now>=ShrinkAt.Value){closingSaid=true;NarrateEvent("closing");}
            if(now<nextSupply||supplyCount>=6||NextRadius.Value<200)return;nextSupply=now+140;float a=UnityEngine.Random.value*Mathf.PI*2,r=NextRadius.Value*Mathf.Sqrt(UnityEngine.Random.value)*.70f;Vector3 goal=NextCenter.Value+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);goal.x=Mathf.Clamp(goal.x,-ReserveWorld.Edge,ReserveWorld.Edge);goal.z=Mathf.Clamp(goal.z,-ReserveWorld.Edge,ReserveWorld.Edge);SpawnSupply(goal);
        }
        void ResetSupplies(){foreach(var d in SupplyDrop.All.ToArray())if(d&&d.IsSpawned)d.NetworkObject.Despawn();supplyCount=0;announcedStage=-1;nextSupply=NetworkManager.ServerTime.Time+65;NarrateEvent("deploy");}
    }
}
