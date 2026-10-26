using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class PolishProbe:MonoBehaviour
    {
        [Serializable]class Report{public List<string> passed=new List<string>(),failed=new List<string>(),errors=new List<string>();public float longestReturn;public int rehosts;}
        Report report=new Report();void Awake(){Application.logMessageReceived+=Log;}void Log(string m,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)report.errors.Add(m+"\n"+stack);}void Check(bool value,string label){(value?report.passed:report.failed).Add(label);Debug.Log("POLISH_CHECK "+label+" "+value);Write();}void Write(){File.WriteAllText(ReserveSession.Arg("--report"),JsonUtility.ToJson(report,true));}
        IEnumerator Start()
        {
            var session=ReserveSession.Instance;float deadline=Time.realtimeSinceStartup+30;while(!Ranger.Local&&Time.realtimeSinceStartup<deadline)yield return null;if(!Ranger.Local){Check(false,"Host initialized");Application.Quit();yield break;}yield return new WaitForSecondsRealtime(1);var p=Ranger.Local;p.ApplyKit(5,10);
            for(int id=1;id<=9;id++){p.SetModCountServer(id,1);p.AttachModServerRpc(0,id);Check(p.Mod(0,WeaponMods.Slot(id))==id,"Attachment "+WeaponMods.Names[id]+" fits its correct slot");}p.SetModCountServer(3,1);p.AttachModServerRpc(3,3);Check(p.Mod(3,0)==0&&p.ModCount(3)==1,"Incompatible marksman optic stays in the pack");
            p.SetModCountServer(5,1);p.AttachModServerRpc(0,5);Check(p.AccuracyFactor(0)<1,"Precision barrel tightens actual firing spread");p.SetModCountServer(7,1);p.AttachModServerRpc(0,7);Check(p.ReloadFactor(0)==.8f,"Speed loader shortens the actual reload");p.DetachModServerRpc(0,2);Check(p.Mod(0,2)==0&&p.ModCount(7)==1,"Removing an attachment returns it to the pack");p.SetModCountServer(6,1);p.AttachModServerRpc(0,6);Check(p.RecoilFactor(0)==.7f,"Muzzle brake reduces recoil");
            var lake=ReserveWater.Lakes[0];p.SmokeMoveTo(new Vector3(lake.center.x,lake.level-.85f,lake.center.y));p.Swimming.Value=true;p.ArmorTier.Value=0;p.Armor.Value=0;p.Health.Value=100;var fish=Instantiate(Resources.Load<GameObject>("Shark"),p.transform.position+Vector3.forward*2,Quaternion.identity).GetComponent<Wildlife>();fish.NetworkObject.Spawn();yield return new WaitForSecondsRealtime(1.9f);Check(p.Health.Value<100&&fish.State.Value==4,"Sharks attack a swimmer");fish.NetworkObject.Despawn();
            var boat=FindObjectsOfType<RoadVehicle>().First(v=>v.Type.Value==4);boat.Condition.Value=100;p.SmokeMoveTo(boat.transform.position+Vector3.right*1.5f);p.EnterVehicleServerRpc(boat.NetworkObjectId);p.Health.Value=100;var safeFish=Instantiate(Resources.Load<GameObject>("Shark"),boat.transform.position+Vector3.forward*2,Quaternion.identity).GetComponent<Wildlife>();safeFish.NetworkObject.Spawn();yield return new WaitForSecondsRealtime(1.9f);Check(p.CurrentVehicle==boat&&p.Health.Value==100&&safeFish.State.Value!=4,"Sharks cannot attack boat occupants");safeFish.NetworkObject.Despawn();p.ExitVehicleServerRpc();p.Swimming.Value=false;
            p.SmokeMoveTo(ReserveWorld.OnGround(0,-145,.3f));p.Health.Value=100;p.TakeDamageServer(5,-1);Check(p.DamageFlash>Time.time+.4f,"Damage starts a visible hit-feedback pulse");
            int world=ReserveWorld.Instance.GetInstanceID(),atlas=ReserveWorld.Instance.map.GetInstanceID();for(int cycle=0;cycle<3;cycle++){float began=Time.realtimeSinceStartup;session.Leave();deadline=began+8;while((session.Playing||session.Network.ShutdownInProgress||Ranger.Local)&&Time.realtimeSinceStartup<deadline)yield return null;yield return new WaitForSecondsRealtime(.3f);report.longestReturn=Mathf.Max(report.longestReturn,Time.realtimeSinceStartup-began);Check(!session.Playing&&ReserveWorld.Instance.GetInstanceID()==world&&ReserveWorld.Instance.map.GetInstanceID()==atlas,"Return "+(cycle+1)+" preserves terrain and atlas without regeneration");session.Host();deadline=Time.realtimeSinceStartup+15;while(!Ranger.Local&&Time.realtimeSinceStartup<deadline)yield return null;yield return new WaitForSecondsRealtime(.7f);Check(Ranger.Local&&session.Network.IsHost&&FindObjectsOfType<WildlifeDirector>().Length==1&&FindObjectsOfType<SharkDirector>().Length==1,"Rehost "+(cycle+1)+" restores one clean session");report.rehosts++;}
            Check(ProximityVoice.Current.MicrophoneStarts==0,"Polish verification never records the microphone");Write();Application.Quit();
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
