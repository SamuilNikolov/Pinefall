using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public static class Emotes
    {
        public static readonly string[] Names={"Wave","Cheer","Point","Salute","Clap","Shuffle"};
        public static readonly float[] Seconds={3.5f,4,3,3,4.2f,5.5f};
        public static int Sector(Vector2 offset)
        {if(offset.sqrMagnitude<68*68)return -1;float angle=Mathf.Repeat(Mathf.Atan2(offset.x,offset.y)*Mathf.Rad2Deg+30,360);return Mathf.FloorToInt(angle/60);}
    }
    public sealed partial class Ranger
    {
        public NetworkVariable<int> EmoteId=new NetworkVariable<int>(-1);
        public NetworkVariable<double> EmoteStarted=new NetworkVariable<double>();
        public NetworkVariable<double> EmoteUntil=new NetworkVariable<double>();
        float nextEmote;Vector3 emoteAnchor;bool predictedEmoteCancel;
        public bool IsEmoting{get{return IsSpawned&&EmoteId.Value>=0&&EmoteId.Value<Emotes.Names.Length&&NetworkManager.ServerTime.Time<EmoteUntil.Value&&(!IsOwner||!predictedEmoteCancel);}}
        public void RequestEmote(int id){if(!LocalHuman)return;predictedEmoteCancel=false;Crouched.Value=Sliding.Value=false;EmoteServerRpc(id);}
        [ServerRpc]public void EmoteServerRpc(int id)
        {
            var match=WildfrontMatch.Current;
            if(id<0||id>=Emotes.Names.Length||!Alive||!CanAct||CurrentVehicle||Swimming.Value||Gliding||Building||Reloading.Value||Velocity.Value>.6f||Time.time<nextEmote||match&&match.Battle.Value&&match.Phase.Value>=2)return;
            if(!Physics.Raycast(transform.position+Vector3.up*.3f,Vector3.down,1,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))return;
            nextEmote=Time.time+.45f;emoteAnchor=transform.position;predictedEmoteCancel=false;EmoteStarted.Value=NetworkManager.ServerTime.Time;EmoteUntil.Value=EmoteStarted.Value+Emotes.Seconds[id];EmoteId.Value=id;
        }
        public void CancelEmoteServer(){if(IsServer&&EmoteId.Value>=0){EmoteId.Value=-1;EmoteUntil.Value=0;}}
        [ServerRpc]public void CancelEmoteServerRpc(){CancelEmoteServer();}
        void EmoteTick()
        {
            if(IsServer&&EmoteId.Value>=0&&(!Alive||!CanAct||CurrentVehicle||Gliding||Swimming.Value||Reloading.Value||Velocity.Value>.6f||(transform.position-emoteAnchor).sqrMagnitude>.5f||NetworkManager.ServerTime.Time>=EmoteUntil.Value))CancelEmoteServer();
            if(LocalHuman&&IsEmoting&&ReserveUI.Instance&&ReserveUI.Instance.ControlsActive&&
                (GameBindings.Horizontal!=0||GameBindings.Vertical!=0||GameBindings.Down("Fire")||GameBindings.Down("Aim")||GameBindings.Down("Jump")||GameBindings.Down("Crouch")||GameBindings.Down("Reload")||GameBindings.Down("Build")||GameBindings.Down("Pickaxe")))
            {predictedEmoteCancel=true;CancelEmoteServerRpc();}
        }
        sealed class GestureRig
        {
            public readonly List<ArmChain> arms=new List<ArmChain>();public Transform spine,leftLeg,rightLeg;public Quaternion spineRest,leftRest,rightRest;
        }
        readonly Dictionary<Transform,GestureRig> gestureRigs=new Dictionary<Transform,GestureRig>();
        public void ApplyEmotePose(Transform model)
        {
            if(!model||!IsEmoting)return;
            if(!gestureRigs.TryGetValue(model,out var rig))
            {
                rig=new GestureRig();
                foreach(var bone in model.GetComponentsInChildren<Transform>(true))
                {
                    string name=bone.name.ToLowerInvariant();
                    if(name=="spine_02"){rig.spine=bone;rig.spineRest=bone.localRotation;}
                    if(name=="thigh_l"){rig.leftLeg=bone;rig.leftRest=bone.localRotation;}
                    if(name=="thigh_r"){rig.rightLeg=bone;rig.rightRest=bone.localRotation;}
                    if(name!="upperarm_l"&&name!="upperarm_r")continue;int side=name.EndsWith("_l")?-1:1;Transform lower=null,hand=null;
                    foreach(var joint in bone.GetComponentsInChildren<Transform>(true)){if(joint.name.ToLowerInvariant()==(side<0?"lowerarm_l":"lowerarm_r"))lower=joint;if(joint.name.ToLowerInvariant()==(side<0?"hand_l":"hand_r"))hand=joint;}
                    if(lower&&hand)rig.arms.Add(new ArmChain{upper=bone,lower=lower,hand=hand,side=side});
                }
                gestureRigs.Add(model,rig);
            }
            float t=(float)(NetworkManager.ServerTime.Time-EmoteStarted.Value),remaining=(float)(EmoteUntil.Value-NetworkManager.ServerTime.Time);
            float blend=Mathf.SmoothStep(0,1,t/.22f)*Mathf.SmoothStep(0,1,remaining/.28f);int id=EmoteId.Value;
            foreach(var arm in rig.arms)
            {
                int side=arm.side;Vector3 target=new Vector3(side*.32f,.92f,.03f);
                if(id==0&&side>0)target=new Vector3(.45f+Mathf.Sin(t*8)*.12f,1.91f,.12f);
                if(id==1)target=new Vector3(side*(.47f+Mathf.Sin(t*7)*.05f),1.94f+Mathf.Sin(t*7)*.06f,.10f);
                if(id==2&&side>0)target=new Vector3(.20f,1.45f,.80f);
                if(id==3&&side>0)target=new Vector3(.19f,1.79f,.18f);
                if(id==4)target=new Vector3(side*(.055f+(Mathf.Sin(t*9)*.5f+.5f)*.23f),1.40f,.42f);
                if(id==5)target=new Vector3(side*.43f,1.24f+Mathf.Sin(t*7+side)*.14f,.16f+Mathf.Cos(t*7+side)*.13f);
                GestureArm(arm,transform.TransformPoint(target),transform.right*side+Vector3.up*.12f,blend);
            }
            if(rig.spine)rig.spine.localRotation=Quaternion.Slerp(rig.spine.localRotation,rig.spineRest*Quaternion.Euler(id==1?-5:0,id==5?Mathf.Sin(t*7)*9:0,id==5?Mathf.Sin(t*7)*7:0),blend);
            if(id==5){float step=Mathf.Sin(t*7)*22;if(rig.leftLeg)rig.leftLeg.localRotation=rig.leftRest*Quaternion.Euler(step,0,-4);if(rig.rightLeg)rig.rightLeg.localRotation=rig.rightRest*Quaternion.Euler(-step,0,4);}
        }
        static void GestureArm(ArmChain arm,Vector3 target,Vector3 pole,float blend)
        {
            var oldUpper=arm.upper.rotation;var oldLower=arm.lower.rotation;Vector3 shoulder=arm.upper.position;
            float a=Vector3.Distance(shoulder,arm.lower.position),b=Vector3.Distance(arm.lower.position,arm.hand.position);if(a<.01f||b<.01f)return;
            Vector3 delta=target-shoulder;float length=Mathf.Clamp(delta.magnitude,.01f,a+b-.005f);Vector3 direction=delta.normalized;
            pole=Vector3.ProjectOnPlane(pole,direction).normalized;float cosine=Mathf.Clamp((a*a+length*length-b*b)/(2*a*length),-1,1);
            Vector3 elbow=shoulder+direction*(cosine*a)+pole*(Mathf.Sqrt(1-cosine*cosine)*a);
            arm.upper.rotation=Quaternion.FromToRotation(arm.lower.position-shoulder,elbow-shoulder)*arm.upper.rotation;
            arm.lower.rotation=Quaternion.FromToRotation(arm.hand.position-arm.lower.position,target-arm.lower.position)*arm.lower.rotation;
            arm.upper.rotation=Quaternion.Slerp(oldUpper,arm.upper.rotation,blend);arm.lower.rotation=Quaternion.Slerp(oldLower,arm.lower.rotation,blend);
        }
    }
}
