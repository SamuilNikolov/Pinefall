using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<bool> Ready=new NetworkVariable<bool>(false);
        [ServerRpc]public void ReadyServerRpc(bool value)
        {var m=WildfrontMatch.Current;if(!m||m.Phase.Value!=0||IsBot.Value)return;Ready.Value=value;if(!value)m.CancelCountdown();}
        [ServerRpc]public void ChangeTeamServerRpc(int team)
        {
            var m=WildfrontMatch.Current;if(!m||m.Phase.Value!=0||m.TeamSize.Value==1)return;
            team=Mathf.Clamp(team,0,RoundArena.MaximumPlayers/m.TeamSize.Value-1);if(Team.Value==team)return;
            if(Players.Count(p=>p&&p!=this&&!p.IsBot.Value&&p.Team.Value==team)>=m.TeamSize.Value){FeedbackClientRpc("That team is full. Choose another team.");return;}
            Team.Value=team;Ready.Value=false;m.CancelCountdown();
        }
    }
    public sealed partial class WildfrontMatch
    {
        public NetworkVariable<bool> BotsEnabled=new NetworkVariable<bool>(false);
        public NetworkVariable<double> CountdownEnds=new NetworkVariable<double>(0);
        int rosterHash;
        public bool EveryoneReady{get{var humans=Ranger.Players.Where(p=>p&&p.IsSpawned&&!p.IsBot.Value).ToArray();return humans.Length>0&&humans.All(p=>p.Ready.Value);}}
        public string StartReason{get{return Phase.Value!=0?"Return to the lobby before starting again.":!EveryoneReady?"Every player must be ready before the host starts.":CountdownEnds.Value>0?"Match starts shortly. Unready to cancel.":"Everyone is ready. The host can start the draft.";}}
        public void CancelCountdown(){if(IsServer)CountdownEnds.Value=0;}
        public bool TryStartRound()
        {
            if(!IsServer||!Battle.Value||Phase.Value!=0||!EveryoneReady||CountdownEnds.Value>0)return false;
            CountdownEnds.Value=NetworkManager.ServerTime.Time+5;NarrateEvent("lobby");return true;
        }
        [ServerRpc(RequireOwnership=false)]public void RequestStartServerRpc(ServerRpcParams rpc=default)
        {if(rpc.Receive.SenderClientId==Unity.Netcode.NetworkManager.ServerClientId)TryStartRound();}
        [ServerRpc(RequireOwnership=false)]public void RulesServerRpc(int size,bool bots,ServerRpcParams rpc=default)
        {
            if(rpc.Receive.SenderClientId!=Unity.Netcode.NetworkManager.ServerClientId||Phase.Value!=0)return;
            if(size!=1&&size!=2&&size!=4)return;RemoveBots();TeamSize.Value=size;BotsEnabled.Value=bots;ReserveSession.Instance.PracticeBots=bots;ReserveSession.Instance.TeamSize=size;
            int i=0;foreach(var p in Ranger.Players.Where(p=>p&&!p.IsBot.Value).OrderBy(p=>p.NetworkObjectId)){p.Team.Value=i++/size;p.Ready.Value=false;}CancelCountdown();
        }
        void RemoveBots(){foreach(var p in Ranger.Players.Where(p=>p&&p.IsBot.Value).ToArray())p.NetworkObject.Despawn();}
        [ServerRpc(RequireOwnership=false)]public void ReturnLobbyServerRpc(ServerRpcParams rpc=default)
        {
            if(rpc.Receive.SenderClientId!=Unity.Netcode.NetworkManager.ServerClientId||Phase.Value!=2)return;
            RemoveBots();Phase.Value=0;CancelCountdown();int i=0;
            foreach(var p in Ranger.Players.Where(p=>p)){p.ResetRound(ReserveWorld.OnGround(i++*3,-160,.3f));p.InitializeEquipment(false);p.Ready.Value=false;}
        }
        void LobbyTick()
        {
            if(!IsServer||Phase.Value!=0)return;int hash=17;
            foreach(var p in Ranger.Players.Where(p=>p&&!p.IsBot.Value).OrderBy(p=>p.NetworkObjectId))hash=unchecked(hash*31+(int)p.NetworkObjectId*11+p.Team.Value);
            if(hash!=rosterHash){rosterHash=hash;CancelCountdown();foreach(var p in Ranger.Players.Where(p=>p&&!p.IsBot.Value))p.Ready.Value=false;}
            if(CountdownEnds.Value>0){if(!EveryoneReady)CancelCountdown();else if(NetworkManager.ServerTime.Time>=CountdownEnds.Value){CountdownEnds.Value=0;StartRound();}}
        }
    }
}
