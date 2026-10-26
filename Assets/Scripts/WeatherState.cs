using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed class WeatherState : NetworkBehaviour
    {
        void Awake(){NetworkSeed.Initialize(this);}
        public static WeatherState Current;
        public NetworkVariable<int> Mode=new NetworkVariable<int>(0);
        public NetworkVariable<bool> Cycling=new NetworkVariable<bool>(true);
        public NetworkVariable<float> Hour=new NetworkVariable<float>(16.7f);
        public NetworkVariable<int> Lightning=new NetworkVariable<int>(0);
        float nextChange,nextThunder;public const float DaySeconds=300;public static float AdvanceHour(float hour,float seconds){return Mathf.Repeat(hour+seconds*24/DaySeconds,24);}
        public override void OnNetworkSpawn(){Current=this;nextChange=Time.time+180;nextThunder=Time.time+28;}
        public override void OnNetworkDespawn(){if(Current==this)Current=null;}
        void Update()
        {
            if(!IsServer||!IsSpawned)return;
            if(ReserveSession.Instance&&ReserveSession.Instance.Playing)Hour.Value=AdvanceHour(Hour.Value,Time.deltaTime);
            if(Cycling.Value&&Time.time>nextChange){Mode.Value=(Mode.Value+1)%3;nextChange=Time.time+180;}
            if(Mode.Value==2&&Time.time>nextThunder){Lightning.Value++;nextThunder=Time.time+Random.Range(24,45);}
        }
        public void Choose(int mode,bool cycling,float hour)
        {
            if(!IsServer)return;Mode.Value=Mathf.Clamp(mode,0,2);Cycling.Value=cycling;Hour.Value=Mathf.Repeat(hour,24);nextChange=Time.time+180;
        }
    }
}
