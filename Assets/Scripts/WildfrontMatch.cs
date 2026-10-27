using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class WildfrontMatch : NetworkBehaviour
    {
        void Awake(){NetworkSeed.Initialize(this);}
        public static WildfrontMatch Current;
        public NetworkVariable<bool> Battle=new NetworkVariable<bool>(true);
        public NetworkVariable<int> Phase=new NetworkVariable<int>(0); // warmup, active, result
        public NetworkVariable<int> Stage=new NetworkVariable<int>(0);
        public NetworkVariable<int> Round=new NetworkVariable<int>(0);
        public NetworkVariable<int> Winner=new NetworkVariable<int>(-1);
        public NetworkVariable<int> TeamSize=new NetworkVariable<int>(2);
        public NetworkVariable<Vector3> Center=new NetworkVariable<Vector3>(Vector3.zero);
        public NetworkVariable<float> Radius=new NetworkVariable<float>(4320);
        public NetworkVariable<Vector3> NextCenter=new NetworkVariable<Vector3>(Vector3.zero);
        public NetworkVariable<float> NextRadius=new NetworkVariable<float>(3040);
        public NetworkVariable<double> ShrinkAt=new NetworkVariable<double>(0);
        public NetworkVariable<double> StageEnds=new NetworkVariable<double>(0);
        readonly float[] radii={3040,2200,1520,938,500,200,0};
        Vector3 fromCenter;float fromRadius,nextTick,nextDamage,pacing=1;int startingTeams;
        System.Random random;
        public bool Active {get{return Battle.Value&&Phase.Value==1;}}
        public bool Closing{get{return Active&&NetworkManager.ServerTime.Time>=ShrinkAt.Value&&NetworkManager.ServerTime.Time<StageEnds.Value;}}
        public override void OnNetworkSpawn(){Current=this;random=new System.Random(ReserveWorld.Seed);if(IsServer&&float.TryParse(ReserveSession.Arg("--faststorm"),out float value))pacing=Mathf.Clamp(value,1,20);}
        public override void OnNetworkDespawn(){if(Current==this)Current=null;}
        public void StartRound()
        {
            if(!IsServer||!Battle.Value||Phase.Value==1||Phase.Value>=3)return;
            if(BotsEnabled.Value)AddPracticeBots();
            SelectArena();Round.Value++;Winner.Value=-1;SpawnDraft.Current.Begin();
        }
        public int AssignTeam(Ranger player,int requested)
        {
            if(TeamSize.Value==1)return Enumerable.Range(0,RoundArena.MaximumPlayers).FirstOrDefault(t=>!Ranger.Players.Any(p=>p&&p!=player&&p.Team.Value==t));
            requested=Mathf.Clamp(requested,0,RoundArena.MaximumPlayers/TeamSize.Value-1);
            if(Ranger.Players.Count(p=>p&&p!=player&&p.Team.Value==requested)<TeamSize.Value)return requested;
            return Enumerable.Range(0,RoundArena.MaximumPlayers/TeamSize.Value).FirstOrDefault(t=>Ranger.Players.Count(p=>p&&p!=player&&p.Team.Value==t)<TeamSize.Value);
        }
        void AddPracticeBots()
        {
            int count=Ranger.Players.Count(p=>p&&p.IsSpawned);
            for(int i=count;i<8;i++)
            {
                var obj=Instantiate(Resources.Load<GameObject>("Ranger"),ReserveWorld.OnGround(i*3,-165,.3f),Quaternion.identity);var bot=obj.GetComponent<Ranger>();
                bot.IsBot.Value=true;bot.Team.Value=AssignTeam(bot,i/TeamSize.Value);bot.DisplayName.Value=new Unity.Collections.FixedString64Bytes("Scout "+(i+1));obj.GetComponent<NetworkObject>().Spawn();
            }
        }
        public void DeployRound()
        {
            if(!IsServer)return;
            foreach(var p in Ranger.Players.Where(p=>p&&p.IsSpawned))
            {
                if(!SpawnDraft.Current.Participants.Contains(p.NetworkObjectId)){p.Health.Value=0;continue;}
                var entry=SpawnDraft.Current.ForPlayer(p.NetworkObjectId);p.ResetRound(entry.hasPoint?entry.point:ExpeditionLayout.Spawn(entry.region,0));p.ApplyKit(entry.kit,10-entry.bid);p.SetFieldOrders(entry.region);
            }
            foreach(var drop in FindObjectsOfType<FieldPickup>())if(drop.IsSpawned)drop.NetworkObject.Despawn();
            if(ConstructionWorld.Current)ConstructionWorld.Current.ResetWorld();WildlifeDirector.Current.Repopulate();ExpeditionLoot.Populate();
            startingTeams=Ranger.Players.Where(p=>p&&p.IsSpawned&&p.Alive).Select(p=>p.Team.Value).Distinct().Count();
            Stage.Value=0;Winner.Value=-1;Center.Value=Vector3.zero;Radius.Value=RoundArena.Size*.704f;Phase.Value=1;ResetSupplies();PlanStage();
            Debug.Log("PINEFALL_ROUND_START "+Round.Value);
        }
        public static Vector3 TeamStart(int team)
        {
            var points=new[]{new Vector3(-470,0,-360),new Vector3(480,0,-320),new Vector3(-320,0,430),new Vector3(430,0,420)};
            return points[Mathf.Clamp(team,0,3)];
        }
        void PlanStage()
        {
            int index=Mathf.Clamp(Stage.Value,0,radii.Length-1);float radius=radii[index]*(RoundArena.Size/ReserveWorld.Size);
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            float drift=Mathf.Max(0,Radius.Value-radius)*.52f;
            Vector3 next=Center.Value+new Vector3(Mathf.Cos(angle)*drift,0,Mathf.Sin(angle)*drift);
            float centerLimit=RoundArena.Half*.71f;next.x=Mathf.Clamp(next.x,-centerLimit+radius*.25f,centerLimit-radius*.25f);next.z=Mathf.Clamp(next.z,-centerLimit+radius*.25f,centerLimit-radius*.25f);
            if(index==0)next=Vector3.zero;
            NextCenter.Value=next;NextRadius.Value=radius;
            double now=NetworkManager.ServerTime.Time;float[] waiting={90,60,55,45,40,30,20},closing={100,140,125,110,85,60,45};ShrinkAt.Value=now+waiting[index]/pacing;StageEnds.Value=ShrinkAt.Value+closing[index]/pacing;
            fromCenter=Center.Value;fromRadius=Radius.Value;
        }
        void Update()
        {
            TransportTick();
            if(IsServer&&Phase.Value==0)SelectArena();LobbyTick();SupplyTick();BanterTick();if(!IsServer||!Active)return;
            double now=NetworkManager.ServerTime.Time;
            if(Time.time>nextTick)
            {
                nextTick=Time.time+.2f;
                float t=Mathf.Clamp01((float)((now-ShrinkAt.Value)/(StageEnds.Value-ShrinkAt.Value)));
                Center.Value=Vector3.Lerp(fromCenter,NextCenter.Value,t);Radius.Value=Mathf.Lerp(fromRadius,NextRadius.Value,t);
                if(now>=StageEnds.Value)
                {
                    if(Stage.Value<radii.Length-1){Stage.Value++;PlanStage();}
                    else {Radius.Value=0;}
                }
            }
            if(Time.time>nextDamage)
            {
                nextDamage=Time.time+1;
                foreach(var p in Ranger.Players)if(p&&p.IsSpawned&&p.Alive&&Outside(p.transform.position))p.TakeDamageServer(2+Stage.Value*2,-1);
                var teams=Ranger.Players.Where(p=>p&&p.IsSpawned&&p.Alive).Select(p=>p.Team.Value).Distinct().ToArray();
                if(teams.Length==0 || startingTeams>1&&teams.Length==1)
                {Winner.Value=teams.Length==1?teams[0]:-1;Phase.Value=2;NarrateEvent("victory");Debug.Log("PINEFALL_ROUND_END "+Winner.Value);}
            }
        }
        public bool Outside(Vector3 p){return !RoundArena.Contains(p)||new Vector2(p.x-Center.Value.x,p.z-Center.Value.z).magnitude>Radius.Value;}
        public Vector3 EscapePoint(Vector3 p)
        {
            var dir=p-Center.Value;dir.y=0;return Center.Value+dir.normalized*Mathf.Max(8,Radius.Value-65);
        }
        public void DropReward(Vector3 position,int animalKind,bool frenzy=false)
        {
            if(!IsServer)return;
            ExpeditionLoot.Spawn(CaveLayout.LootPosition(position,Vector3.right*.6f),5,0,0,animalKind>=2?3:2);
            if(UnityEngine.Random.value<(frenzy?.65f:.08f))ExpeditionLoot.Spawn(CaveLayout.LootPosition(position,Vector3.left*.6f),9,0,frenzy?2:0,1);
        }
        public void CaptureLateJoin(Ranger player)
        {
            if(!IsServer||!(Active||Phase.Value>=3))return;
            // Joining mid-round is supported as a spectator; everyone starts the next round together.
            player.Health.Value=0;player.InformServer("This round is already underway. You will join the next draft.");
        }
    }
}
