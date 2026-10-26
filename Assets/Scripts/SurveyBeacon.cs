using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<int> SurveyCharges=new NetworkVariable<int>(0);
        double surveyEnds,nextSurvey;
        public Vector3[] SurveyPoints{get;private set;}=new Vector3[0];
        public double SurveyVisibleUntil{get;private set;}
        public double SurveyWarningUntil{get;private set;}
        public bool SurveyVisible{get{return IsSpawned&&NetworkManager&&NetworkManager.IsListening&&NetworkManager.ServerTime.Time<SurveyVisibleUntil;}}
        public bool SurveyWarning{get{return IsSpawned&&NetworkManager&&NetworkManager.IsListening&&NetworkManager.ServerTime.Time<SurveyWarningUntil;}}
        [ServerRpc]public void SurveyServerRpc()
        {
            if(!Fighting||!CanAct||SurveyCharges.Value<=0||NetworkManager.ServerTime.Time<surveyEnds)return;
            SurveyCharges.Value--;surveyEnds=NetworkManager.ServerTime.Time+12;nextSurvey=0;SurveyWarningClientRpc(surveyEnds);SurveyTick();
        }
        void SurveyTick()
        {
            if(!IsServer||!IsSpawned)return;double now=NetworkManager.ServerTime.Time;
            if(!Fighting||!CanAct){surveyEnds=0;return;}if(now>=surveyEnds||now<nextSurvey)return;nextSurvey=now+4;
            var recipients=Players.Where(p=>p&&p.Alive&&!p.IsBot.Value&&p.Team.Value==Team.Value).Select(p=>p.OwnerClientId).Distinct().ToArray();if(recipients.Length==0)return;
            var points=Players.Where(p=>p&&p.Fighting&&p.Team.Value!=Team.Value).Select(p=>p.transform.position).ToArray();
            SurveyPulseClientRpc(points,surveyEnds,new ClientRpcParams{Send=new ClientRpcSendParams{TargetClientIds=recipients}});
        }
        [ClientRpc]void SurveyPulseClientRpc(Vector3[] points,double ends,ClientRpcParams rpc=default)
        {if(Local){Local.SurveyPoints=points;Local.SurveyVisibleUntil=ends;}ReserveAudio.Cue("radio",transform.position,.28f);}
        [ClientRpc]void SurveyWarningClientRpc(double ends)
        {if(Local){Local.SurveyWarningUntil=ends;Local.ShowNotice("Survey beacon active: positions are exposed for 12 seconds.",5);}}
        void ResetSurvey(){SurveyCharges.Value=0;surveyEnds=nextSurvey=0;SurveyPoints=new Vector3[0];SurveyVisibleUntil=SurveyWarningUntil=0;}
    }
    public static class SupplySpecials
    {
        public static int Choose(float roll){return roll<.35f?1:roll<.60f?2:0;}
        public static bool Release(Vector3 center,int reward)
        {
            if(reward==1)
            {
                for(int i=0;i<48;i++){float angle=i*2.39996f,r=5+i*.85f;float x=center.x+Mathf.Cos(angle)*r,z=center.z+Mathf.Sin(angle)*r;if(Mathf.Abs(x)>ReserveWorld.Edge||Mathf.Abs(z)>ReserveWorld.Edge||ReserveWater.HasSurface(x,z))continue;var at=ReserveWorld.OnGround(x,z,.65f);if(Physics.CheckBox(at+Vector3.up*.5f,new Vector3(.7f,.6f,1.3f),Quaternion.identity,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))continue;ExpeditionLoot.SpawnVehicle(at,5,100,85);return true;}
                reward=2;
            }
            if(reward==2){ExpeditionLoot.Spawn(ReserveWater.LootPosition(center.x+1,center.z+1),12,0,3,1);return true;}return false;
        }
    }
}
