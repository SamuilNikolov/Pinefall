using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public struct DraftEntry:INetworkSerializable,IEquatable<DraftEntry>
    {
        public ulong player;public int team,bid,kit,region,order,intent;public bool locked,hasPoint,hasPreview;public Vector3 point,preview;
        public void NetworkSerialize<T>(BufferSerializer<T> s)where T:IReaderWriter{s.SerializeValue(ref player);s.SerializeValue(ref team);s.SerializeValue(ref bid);s.SerializeValue(ref kit);s.SerializeValue(ref region);s.SerializeValue(ref order);s.SerializeValue(ref intent);s.SerializeValue(ref locked);s.SerializeValue(ref hasPoint);s.SerializeValue(ref hasPreview);s.SerializeValue(ref point);s.SerializeValue(ref preview);}
        public bool Equals(DraftEntry b){return player==b.player&&team==b.team&&bid==b.bid&&kit==b.kit&&region==b.region&&order==b.order&&intent==b.intent&&locked==b.locked&&hasPoint==b.hasPoint&&hasPreview==b.hasPreview&&point==b.point&&preview==b.preview;}
    }
    public sealed class SpawnDraft:NetworkBehaviour
    {
        public static SpawnDraft Current;
        public NetworkList<DraftEntry> Entries;
        public NetworkVariable<ulong> TurnPlayer=new NetworkVariable<ulong>(ulong.MaxValue);
        public NetworkVariable<double> Deadline=new NetworkVariable<double>(0);
        public readonly HashSet<ulong> Participants=new HashSet<ulong>();int turn;
        void Awake(){Entries=new NetworkList<DraftEntry>();}
        public override void OnNetworkSpawn(){Current=this;}
        public override void OnNetworkDespawn(){if(Current==this)Current=null;}
        public DraftEntry ForPlayer(ulong id){foreach(var e in Entries)if(e.player==id)return e;return new DraftEntry{player=id,region=-1,intent=-1};}
        public bool Claimed(int region){foreach(var e in Entries)if(!e.hasPoint&&e.region==region)return true;return false;}
        public void Begin()
        {
            if(!IsServer)return;Participants.Clear();Entries.Clear();
            foreach(var p in Ranger.Players.Where(p=>p&&p.IsSpawned).OrderBy(p=>p.NetworkObjectId))
            {Participants.Add(p.NetworkObjectId);Entries.Add(new DraftEntry{player=p.NetworkObjectId,team=p.Team.Value,region=-1,intent=-1,kit=0,bid=0,order=Entries.Count});}
            turn=0;WildfrontMatch.Current.Phase.Value=3;NextTurn();
        }
        Ranger Sender(ulong id){return Ranger.Players.FirstOrDefault(p=>p&&!p.IsBot.Value&&p.OwnerClientId==id&&p.NetworkObjectId==TurnPlayer.Value);}
        [ServerRpc(RequireOwnership=false)]public void PreviewServerRpc(int bid,int kit,int intent,ServerRpcParams rpc=default)
        {if(Sender(rpc.Receive.SenderClientId))SetBid(bid,kit,intent,false);}
        [ServerRpc(RequireOwnership=false)]public void SubmitBidServerRpc(int bid,int kit,ServerRpcParams rpc=default)
        {if(Sender(rpc.Receive.SenderClientId))SetBid(bid,kit,ForPlayer(TurnPlayer.Value).intent,true);}
        void SetBid(int bid,int kit,int intent,bool commit)
        {
            if(WildfrontMatch.Current.Phase.Value!=3||bid<0||bid>10||kit<0||kit>=StartingKit.All.Length||StartingKit.All[kit].cost+bid>10)return;
            var e=Entries[turn];e.bid=bid;e.kit=kit;e.intent=Mathf.Clamp(intent,-1,ExpeditionLayout.Regions.Length-1);e.locked=commit;Entries[turn]=e;
            if(commit){turn++;if(turn>=Entries.Count)Rank();else NextTurn();}
        }
        [ServerRpc(RequireOwnership=false)]public void PickRegionServerRpc(int region,ServerRpcParams rpc=default)
        {if(Sender(rpc.Receive.SenderClientId))Pick(region);}
        void Pick(int region)
        {
            if(WildfrontMatch.Current.Phase.Value!=4||region<0||region>=ExpeditionLayout.Regions.Length||!RoundArena.Includes(ExpeditionLayout.Regions[region])||Claimed(region))return;
            var e=Entries[turn];e.region=region;Entries[turn]=e;turn++;NextTurn();
        }
        [ServerRpc(RequireOwnership=false)]public void PreviewPointServerRpc(Vector3 requested,ServerRpcParams rpc=default)
        {if(!Sender(rpc.Receive.SenderClientId)||!FreeSpawn.Finite(requested))return;var e=Entries[turn];e.preview=RoundArena.Clamp(new Vector3(requested.x,0,requested.z));e.hasPreview=true;e.intent=ExpeditionLayout.Nearest(e.preview);Entries[turn]=e;}
        [ServerRpc(RequireOwnership=false)]public void PickPointServerRpc(Vector3 requested,ServerRpcParams rpc=default)
        {var player=Sender(rpc.Receive.SenderClientId);if(!player||WildfrontMatch.Current.Phase.Value!=4)return;foreach(var chosen in Entries){if(chosen.region<0||chosen.player==player.NetworkObjectId)continue;var claimed=chosen.hasPoint?chosen.point:ExpeditionLayout.Spawn(chosen.region,0);var difference=requested-claimed;difference.y=0;if(chosen.team!=player.Team.Value&&difference.sqrMagnitude<60*60){player.InformServer("An opposing landing reserves this 60 m area. Choose outside its ring.");return;}}Vector3 resolved;if(!RoundArena.Contains(requested,12)||!FreeSpawn.Resolve(requested,out resolved)){player.InformServer("That surface is obstructed. Choose another point nearby.");return;}var e=Entries[turn];e.point=resolved;e.hasPoint=true;e.region=ExpeditionLayout.Nearest(resolved);Entries[turn]=e;turn++;NextTurn();}
        void Rank()
        {
            int rotation=WildfrontMatch.Current.Round.Value;var sorted=new List<DraftEntry>();foreach(var e in Entries)sorted.Add(e);
            sorted=sorted.OrderByDescending(e=>e.bid).ThenBy(e=>(e.order+rotation)%Mathf.Max(1,sorted.Count)).ToList();
            Entries.Clear();for(int i=0;i<sorted.Count;i++){var e=sorted[i];e.order=i;Entries.Add(e);}
            turn=0;WildfrontMatch.Current.Phase.Value=4;NextTurn();
        }
        void NextTurn()
        {
            if(turn>=Entries.Count){TurnPlayer.Value=ulong.MaxValue;WildfrontMatch.Current.DeployRound();return;}
            TurnPlayer.Value=Entries[turn].player;Deadline.Value=NetworkManager.ServerTime.Time+(WildfrontMatch.Current.Phase.Value==3?25:18);
        }
        void Update()
        {
            if(!IsServer||!WildfrontMatch.Current||turn>=Entries.Count)return;int phase=WildfrontMatch.Current.Phase.Value;if(phase!=3&&phase!=4)return;
            var p=Ranger.Players.FirstOrDefault(q=>q&&q.NetworkObjectId==TurnPlayer.Value);
            if(p&&!p.IsBot.Value&&NetworkManager.ServerTime.Time<=Deadline.Value)return;
            if(phase==3){int bid=p&&p.IsBot.Value?(Entries[turn].order+WildfrontMatch.Current.Round.Value)%5:Entries[turn].bid;int kit=p&&p.IsBot.Value?2:Entries[turn].kit;SetBid(bid,kit,Entries[turn].order%ExpeditionLayout.Regions.Length,true);}
            else {int preferred=Entries[turn].intent<0?Entries[turn].order:Entries[turn].intent;for(int i=0;i<ExpeditionLayout.Regions.Length;i++){int r=(preferred+i)%ExpeditionLayout.Regions.Length;if(RoundArena.Includes(ExpeditionLayout.Regions[r])&&!Claimed(r)){Pick(r);break;}}}
        }
    }
}
