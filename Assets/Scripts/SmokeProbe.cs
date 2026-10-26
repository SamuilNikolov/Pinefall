using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Pinefall
{
    // An opt-in packaged-game integration probe. It drives the same owner actions and RPCs as play.
    public sealed class SmokeProbe : MonoBehaviour
    {
        [Serializable]public class PlayerSample{public string name;public ulong id;public Vector3 position;public int weapon,ammo,score;public bool reloading;}
        [Serializable]public class AnimalSample{public ulong id;public int health,state,harvested;public Vector3 position;}
        [Serializable]public class Snapshot{public float seconds;public int players,animals;public bool playing;public PlayerSample[] roster;public AnimalSample[] wildlife;}
        [Serializable]public class Report{public string mode;public string[] errors;public Snapshot[] snapshots;public float averageFps;}
        List<Snapshot> snapshots=new List<Snapshot>();List<string> errors=new List<string>();
        float began,nextSample,duration=30,deltaSum;int stage,frames;string mode,path;Wildlife target;bool finishing;
        void Start(){began=Time.realtimeSinceStartup;mode=ReserveSession.Arg("--smoke");path=ReserveSession.Arg("--report");float.TryParse(ReserveSession.Arg("--duration"),out duration);if(duration<5)duration=30;Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors.Add(message+"\n"+stack);}
        void Update()
        {
            float t=Time.realtimeSinceStartup-began;deltaSum+=Time.unscaledDeltaTime;frames++;
            var p=Ranger.Local;var session=ReserveSession.Instance;
            if(p&&session.Playing)
            {
                if(mode=="host")
                {
                    if(stage==0&&t>7&&Ranger.Players.Count>=2)
                    {
                        target=session.Animals.FirstOrDefault(a=>a&&!a.Dead);if(target){p.SmokeMoveTo(ReserveWorld.OnGround(target.transform.position.x,target.transform.position.z-18,.2f));p.SmokeAimAt(target);stage=1;}
                    }
                    if(stage==1&&t>8){p.SmokeAimAt(target);p.Fire();stage=2;}
                    if(stage==2&&t>11){p.SelectWeapon(1);stage=3;}
                    if(stage==3&&t>12){target=session.Animals.FirstOrDefault(a=>a&&!a.Dead);if(target){p.SmokeMoveTo(ReserveWorld.OnGround(target.transform.position.x,target.transform.position.z-16,.2f));p.SmokeAimAt(target);p.Fire();}stage=4;}
                    if(stage==4&&t>14){p.ReloadServerRpc();stage=5;}
                }
                else
                {
                    if(stage==0&&t>3){p.SelectWeapon(2);stage=1;}
                    if(stage==1&&t>6){p.SmokeMoveTo(ReserveWorld.OnGround(12,-150,.2f));stage=2;}
                    if(stage==2&&t>12){p.SelectWeapon(1);stage=3;}
                }
            }
            if(t>=nextSample){nextSample=t+2;SnapshotNow(t);}
            if(t>duration&&!finishing){finishing=true;SnapshotNow(t);Write();Application.Quit();}
        }
        void SnapshotNow(float t)
        {
            snapshots.Add(new Snapshot{seconds=t,playing=ReserveSession.Instance.Playing,players=Ranger.Players.Count,animals=ReserveSession.Instance.Animals.Count,
                roster=Ranger.Players.Where(p=>p&&p.IsSpawned).Select(p=>new PlayerSample{name=p.DisplayName.Value.ToString(),id=p.OwnerClientId,position=p.transform.position,weapon=p.Weapon.Value,ammo=p.Ammo.Value,score=p.Score.Value,reloading=p.Reloading.Value}).ToArray(),
                wildlife=ReserveSession.Instance.Animals.Where(a=>a&&a.IsSpawned).Select(a=>new AnimalSample{id=a.NetworkObjectId,health=a.Health.Value,state=a.State.Value,harvested=a.Harvested.Value,position=a.transform.position}).ToArray()});
            Write();
        }
        void Write()
        {
            if(string.IsNullOrEmpty(path))return;
            File.WriteAllText(path,JsonUtility.ToJson(new Report{mode=mode,errors=errors.ToArray(),snapshots=snapshots.ToArray(),averageFps=frames/Mathf.Max(.001f,deltaSum)},true));
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
