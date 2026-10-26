using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class WildlifeDirector:MonoBehaviour
    {
        public const int PerBiome=32,Maximum=300;
        public static WildlifeDirector Current;
        public static readonly string[][] Habitats={new[]{"Deer","Stag","Hare","Boar","Elk"},new[]{"Fox","Hare","Antelope","Horse"},new[]{"Wolf","ArcticFox","SnowHare","Elk"},new[]{"Ibex","Alpaca","Stag","Wolf","Hare"},new[]{"JungleBoar","Deer","Ox","Hare","Fox"},new[]{"Boar","Deer","Hare","Fox","Ox"}};
        System.Random rng;float next,cleanup;
        void Start(){Current=this;rng=new System.Random(Guid.NewGuid().GetHashCode());Repopulate();}
        public void Repopulate()
        {
            if(rng==null)rng=new System.Random(Guid.NewGuid().GetHashCode());
            foreach(var a in ReserveSession.Instance.Animals.ToArray())if(a&&a.IsSpawned)a.NetworkObject.Despawn();
            for(int b=0;b<Habitats.Length;b++)for(int i=0;i<PerBiome;i++)Spawn(b,true);var sharks=GetComponent<SharkDirector>();if(sharks)sharks.Spawn();
        }
        void Update()
        {
            if(!ReserveSession.Instance.Network.IsServer)return;
            if(Time.time>cleanup){cleanup=Time.time+12;foreach(var a in ReserveSession.Instance.Animals.ToArray())if(a&&a.Dead&&a.CorpseAge>80)a.NetworkObject.Despawn();}
            if(Time.time<next)return;next=Time.time+6+(float)rng.NextDouble()*4;
            int biome=rng.Next(Habitats.Length);if(ReserveSession.Instance.Animals.Count(a=>a&&!a.Aquatic&&!a.Dead&&ReserveBiomes.At(a.transform.position.x,a.transform.position.z)==biome)<PerBiome)Spawn(biome,false);
            if(ReserveSession.Instance.Animals.Count(a=>a&&!a.Dead)<Maximum)foreach(var player in Ranger.Players.Where(p=>p&&p.Fighting&&!p.IsBot.Value))
            {
                if(ReserveSession.Instance.Animals.Count(a=>a&&!a.Dead&&(a.transform.position-player.transform.position).sqrMagnitude<320*320)>=9)continue;
                for(int attempt=0;attempt<16;attempt++)
                {
                    float angle=(float)rng.NextDouble()*Mathf.PI*2,distance=145+(float)rng.NextDouble()*100;Vector3 offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*distance;
                    if(Vector3.Dot(offset.normalized,player.transform.forward)>.15f)continue;Vector3 p=player.transform.position+offset;
                    if(Mathf.Abs(p.x)>ReserveWorld.Edge||Mathf.Abs(p.z)>ReserveWorld.Edge||ReserveWorld.Clearing(p.x,p.z)||ReserveWater.HasSurface(p.x,p.z)||WildfrontMatch.Current.Active&&WildfrontMatch.Current.Outside(p))continue;
                    if(Ranger.Players.Any(q=>q&&q.Alive&&(new Vector2(q.transform.position.x-p.x,q.transform.position.z-p.z)).sqrMagnitude<125*125))continue;
                    int b=ReserveBiomes.At(p.x,p.z);string type=Habitats[b][rng.Next(Habitats[b].Length)];for(int member=0;member<3;member++){if(ReserveSession.Instance.Animals.Count(a=>a&&!a.Dead)>=Maximum)break;var at=p+new Vector3(member*7,0,member*4);if(ReserveWater.HasSurface(at.x,at.z)||ReserveWorld.Clearing(at.x,at.z))continue;var obj=Instantiate(Resources.Load<GameObject>(type),ReserveWorld.OnGround(at.x,at.z),Quaternion.Euler(0,rng.Next(360),0));obj.GetComponent<NetworkObject>().Spawn();}break;
                }
            }
        }
        void Spawn(int biome,bool initial)
        {
            if(ReserveSession.Instance.Animals.Count(a=>a&&!a.Dead)>=Maximum)return;
            for(int attempt=0;attempt<100;attempt++)
            {
                float x=(float)(rng.NextDouble()-.5f)*(ReserveWorld.Size-130),z=(float)(rng.NextDouble()-.5f)*(ReserveWorld.Size-130);
                if(ReserveWater.HasSurface(x,z)||ReserveBiomes.At(x,z)!=biome||ReserveWorld.Clearing(x,z))continue;Vector3 pos=ReserveWorld.OnGround(x,z);
                if(Ranger.Players.Any(p=>p&&p.Alive&&Vector3.Distance(pos,p.transform.position)<(initial?95:150)))continue;
                if(WildfrontMatch.Current&&WildfrontMatch.Current.Active&&WildfrontMatch.Current.Outside(pos))continue;
                string type=Habitats[biome][rng.Next(Habitats[biome].Length)];var g=Instantiate(Resources.Load<GameObject>(type),pos,Quaternion.Euler(0,rng.Next(360),0));g.GetComponent<NetworkObject>().Spawn();return;
            }
        }
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
