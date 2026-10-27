using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<bool> Downed=new NetworkVariable<bool>(false);
        public NetworkVariable<double> BleedEnds=new NetworkVariable<double>(0);
        public NetworkVariable<ulong> ReviveTarget=new NetworkVariable<ulong>(ulong.MaxValue);
        public NetworkVariable<float> ReviveProgress=new NetworkVariable<float>(0);
        public bool Fighting{get{return Alive&&!Downed.Value;}}
        int knockCount,lastDowner=-1;float reviveBegan,lastRevivePulse,nextReviveInput;ulong localRevive=ulong.MaxValue;
        public Ranger NearbyDowned(float distance=3)
        {return Players.Where(p=>p&&p!=this&&p.Alive&&p.Downed.Value&&p.Team.Value==Team.Value&&(p.transform.position-transform.position).sqrMagnitude<distance*distance).OrderBy(p=>(p.transform.position-transform.position).sqrMagnitude).FirstOrDefault();}
        void ResetRescue(){Downed.Value=false;BleedEnds.Value=0;knockCount=0;lastDowner=-1;CancelRevive();}
        void CancelRevive(){if(!IsServer)return;ReviveTarget.Value=ulong.MaxValue;ReviveProgress.Value=0;reviveBegan=0;}
        [ServerRpc]public void RevivePulseServerRpc(ulong id,bool held)
        {
            var friend=Players.FirstOrDefault(p=>p&&p.NetworkObjectId==id);
            if(!held||!Fighting||!CanAct||CurrentVehicle||!friend||!friend.Downed.Value||!friend.Alive||friend.Team.Value!=Team.Value||(friend.transform.position-transform.position).sqrMagnitude>10.24f){CancelRevive();return;}
            if(ReviveTarget.Value!=id){CancelRevive();ReviveTarget.Value=id;reviveBegan=Time.time;}lastRevivePulse=Time.time;
        }
        bool ReviveControls(bool controls)
        {
            if(!controls||!GameBindings.Held("Interact")||CurrentVehicle)
            {if(localRevive!=ulong.MaxValue){RevivePulseServerRpc(localRevive,false);localRevive=ulong.MaxValue;}return false;}
            var friend=NearbyDowned();if(!friend)return false;localRevive=friend.NetworkObjectId;
            if(Time.time>=nextReviveInput){nextReviveInput=Time.time+.1f;RevivePulseServerRpc(localRevive,true);}Aiming=false;Crouched.Value=true;if(Velocity.Value!=0)Velocity.Value=0;return true;
        }
        void RescueTick()
        {
            if(!IsServer)return;
            if(Downed.Value&&(NetworkManager.ServerTime.Time>=BleedEnds.Value||!Players.Any(p=>p&&p!=this&&p.Fighting&&p.Team.Value==Team.Value)))EliminateServer(lastDowner);
            if(ReviveTarget.Value==ulong.MaxValue)return;
            var friend=Players.FirstOrDefault(p=>p&&p.NetworkObjectId==ReviveTarget.Value);
            if(!Fighting||!CanAct||!friend||!friend.Alive||!friend.Downed.Value||Time.time-lastRevivePulse>.45f||(friend.transform.position-transform.position).sqrMagnitude>10.24f){CancelRevive();return;}
            ReviveProgress.Value=Mathf.Clamp01((Time.time-reviveBegan)/3);
            if(ReviveProgress.Value>=1){friend.Downed.Value=false;friend.Health.Value=35;friend.Hunger.Value=Mathf.Max(25,friend.Hunger.Value);friend.BleedEnds.Value=0;friend.FeedbackClientRpc("Your teammate revived you. Find cover and heal.");FeedbackClientRpc("Teammate revived.");CancelRevive();}
        }
        void EliminateServer(int attacker)
        {
            if(!IsServer||!Alive)return;Health.Value=0;Downed.Value=false;BleedEnds.Value=0;CancelRevive();ExitVehicleServer();DropBackpack();respawnAfter=Time.time+6;
            var killer=attacker>=0?Players.FirstOrDefault(p=>p&&p.NetworkObjectId==(ulong)attacker&&p.Team.Value!=Team.Value):null;
            if(killer)killer.Kills.Value++;Debug.Log("PINEFALL_RANGER_DOWN "+NetworkObjectId+" by "+attacker);
            if(WildfrontMatch.Current){WildfrontMatch.Current.NarrateEvent("elim"+Random.Range(0,5),DisplayName.Value+" eliminated"+(killer?" by "+killer.DisplayName.Value:"")+".");WildfrontMatch.Current.AnnounceClientRpc(DisplayName.Value+" was eliminated"+(killer?" by "+killer.DisplayName.Value:" in the wild")+".");}
        }
    }
    public sealed partial class WildfrontMatch
    {
        [ClientRpc]public void AnnounceClientRpc(string message){if(ReserveUI.Instance)ReserveUI.Instance.AddEvent(message);}
    }
}
