using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class WildfrontProbe : MonoBehaviour
    {
        [Serializable]public class Person{public string name;public int team,health,kills,hunts,craft,upgrade,weapon,ammo;public Vector3 position;}
        [Serializable]public class Frame{public float time,radius,nextRadius;public int round,phase,stage,winner,weather,animals,rewards;public Person[] people;}
        [Serializable]public class Results{public string mode;public Frame[] frames;public string[] errors;}
        readonly List<Frame> frames=new List<Frame>();readonly List<string> errors=new List<string>();
        float began,nextFrame,duration=40;int step,clientRound;string mode,path;
        void Start(){began=Time.realtimeSinceStartup;mode=ReserveSession.Arg("--smoke");path=ReserveSession.Arg("--report");if(float.TryParse(ReserveSession.Arg("--duration"),out float d))duration=d;Application.logMessageReceived+=Log;}
        void Log(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(s+"\n"+stack);}
        void Update()
        {
            float t=Time.realtimeSinceStartup-began;var p=Ranger.Local;var m=WildfrontMatch.Current;
            if(p&&m&&p.IsSpawned)
            {
                if(mode=="battle-host")
                {
                    if(step==0&&t>6&&Ranger.Players.Count>=2){m.StartRound();p.SelectWeapon(0);step=1;}
                    if(step==1&&t>9){p.SmokeMoveTo(ReserveWorld.OnGround(0,-160,.2f));step=2;}
                    if(step==2&&t>13)
                    {var enemy=Ranger.Players.FirstOrDefault(q=>q&&q!=p&&q.Alive&&q.Team.Value!=p.Team.Value);if(enemy){p.SmokeAimAtPlayer(enemy);p.Fire();}if(p.Kills.Value>0)step=3;}
                    if(step==3&&t>18){m.StartRound();step=4;}
                    if(step==4&&t>21){p.TakeDamageServer(20,-3);m.DropReward(p.transform.position,2);step=5;}
                    if(step==5&&t>22){var reward=p.NearestPickup();if(reward)p.CollectServerRpc(reward.NetworkObjectId);step=6;}
                    if(step==6&&t>23){p.UpgradeServerRpc(0);ReserveWeather.Instance.Choose(2,false,13);step=7;}
                    if(step==7&&t>29){p.SmokeMoveTo(ReserveWorld.OnGround(740,740,.2f));step=8;}
                }
                else if(m.Phase.Value==1&&m.Round.Value!=clientRound)
                {clientRound=m.Round.Value;p.SmokeMoveTo(ReserveWorld.OnGround(0,-142,.2f));}
            }
            if(t>nextFrame){nextFrame=t+1;TakeFrame(t);}
            if(t>duration){TakeFrame(t);Application.Quit();}
        }
        void TakeFrame(float t)
        {
            var m=WildfrontMatch.Current;
            frames.Add(new Frame{time=t,round=m?m.Round.Value:0,phase=m?m.Phase.Value:-1,stage=m?m.Stage.Value:-1,winner=m?m.Winner.Value:-1,radius=m?m.Radius.Value:0,nextRadius=m?m.NextRadius.Value:0,weather=WeatherState.Current?WeatherState.Current.Mode.Value:-1,animals=ReserveSession.Instance.Animals.Count,rewards=FindObjectsOfType<FieldPickup>().Length,
                people=Ranger.Players.Where(p=>p&&p.IsSpawned).Select(p=>new Person{name=p.DisplayName.Value.ToString(),team=p.Team.Value,health=p.Health.Value,kills=p.Kills.Value,hunts=p.Score.Value,craft=p.Craft.Value,upgrade=p.UpgradeLevel(0),weapon=p.Weapon.Value,ammo=p.Ammo.Value,position=p.transform.position}).ToArray()});
            if(!string.IsNullOrEmpty(path))File.WriteAllText(path,JsonUtility.ToJson(new Results{mode=mode,frames=frames.ToArray(),errors=errors.ToArray()},true));
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
