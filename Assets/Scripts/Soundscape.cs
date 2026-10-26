using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class Soundscape:MonoBehaviour
    {
        static Soundscape current;readonly Dictionary<string,AudioClip[]> banks=new Dictionary<string,AudioClip[]>();readonly Dictionary<string,int> last=new Dictionary<string,int>();
        bool wasReloading;AudioSource[] voices;AudioLowPassFilter[] filters;AudioSource birds;float wildlifeAt;
        public static readonly string[] Channels={"Footsteps","Weapons","Wildlife","Ambience"};
        public static float Level(string channel){return PlayerPrefs.GetFloat("sound."+channel,channel=="Footsteps"?.45f:channel=="Ambience"?.50f:.8f);}
        public static void SetLevel(string channel,float value){PlayerPrefs.SetFloat("sound."+channel,Mathf.Clamp01(value));}
        public static void Ensure()
        {
            if(current)return;var obj=new GameObject("Recorded reserve soundscape");current=obj.AddComponent<Soundscape>();current.Prepare();
        }
        void Prepare()
        {
            voices=new AudioSource[32];filters=new AudioLowPassFilter[32];for(int i=0;i<voices.Length;i++){var obj=new GameObject("Sound voice "+i);obj.transform.SetParent(transform);voices[i]=obj.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].dopplerLevel=0;voices[i].rolloffMode=AudioRolloffMode.Linear;filters[i]=obj.AddComponent<AudioLowPassFilter>();}
            foreach(string bank in new[]{"grass","gravel","sand","snow","wood","rifle","carbine","shotgun","pistol","smg","support","pig","canid","goat","elk","cloth","reload","radio"}){var list=new List<AudioClip>();for(int i=0;i<6;i++){var clip=Resources.Load<AudioClip>("Audio07/"+bank+i);if(clip)list.Add(clip);}banks[bank]=list.ToArray();}
            birds=gameObject.AddComponent<AudioSource>();birds.clip=Resources.Load<AudioClip>("Audio07/birds0");birds.loop=true;birds.volume=0;birds.playOnAwake=false;if(birds.clip)birds.Play();
        }
        public static bool HasBank(string name){Ensure();return current.banks.ContainsKey(name)&&current.banks[name].Length>0;}
        static AudioClip Pick(string bank)
        {
            if(!current.banks.ContainsKey(bank)||current.banks[bank].Length==0)return null;var clips=current.banks[bank];int index=Random.Range(0,clips.Length);int previous;if(clips.Length>1&&current.last.TryGetValue(bank,out previous)&&index==previous)index=(index+1)%clips.Length;current.last[bank]=index;return clips[index];
        }
        static void Emit(string bank,Vector3 point,float volume,string channel,float range,bool local=false,float pitch=1)
        {
            Ensure();float level=Level(channel);var camera=Camera.main;if(level<=0||!camera)return;float distance=Vector3.Distance(camera.transform.position,point);if(!local&&distance>range)return;var clip=Pick(bank);if(!clip)return;
            int slot=-1;for(int i=0;i<current.voices.Length;i++)if(!current.voices[i].isPlaying){slot=i;break;}if(slot<0)return;
            var source=current.voices[slot];source.transform.position=point;source.clip=clip;source.spatialBlend=local?0:1;source.volume=Mathf.Clamp01(volume*level);source.pitch=pitch;source.minDistance=channel=="Weapons"?6:2;source.maxDistance=range;source.priority=channel=="Weapons"?20:channel=="Footsteps"?80:150;
            bool obstructed=!local&&distance>5&&Physics.Linecast(camera.transform.position,point+Vector3.up*.4f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);current.filters[slot].cutoffFrequency=obstructed?1800:channel=="Footsteps"?4500:17500;if(obstructed)source.volume*=.50f;source.Play();
        }
        public static void Gun(Vector3 point,int weapon,float volume){string bank=weapon==0||weapon==5?"rifle":weapon==2?"shotgun":weapon==3?"pistol":weapon==4?"smg":weapon==6?"support":"carbine";bool local=Camera.main&&(Camera.main.transform.position-point).sqrMagnitude<4;Emit(bank,point,volume*.85f,"Weapons",650,local,Random.Range(.975f,1.025f));}
        public static void Step(Vector3 point,float volume,bool gravel,bool local=false)
        {
            int b=ReserveBiomes.At(point.x,point.z);string bank=gravel?"gravel":b==1?"sand":b==2||b==3&&point.y>115?"snow":"grass";RaycastHit hit;if(Physics.Raycast(point+Vector3.up*.35f,Vector3.down,out hit,.8f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)&&hit.collider.GetComponent<Terrain>()==null)bank="wood";Emit(bank,point,volume,"Footsteps",24,local,Random.Range(.96f,1.04f));
        }
        public static void Animal(Wildlife animal,bool alarm)
        {
            Ensure();if(Time.time<current.wildlifeAt||!Camera.main||Vector3.Distance(Camera.main.transform.position,animal.transform.position)>100)return;current.wildlifeAt=Time.time+1.3f;
            string name=animal.species.ToLowerInvariant(),bank=name.Contains("boar")?"pig":name.Contains("wolf")||name.Contains("fox")?"canid":name.Contains("elk")||name.Contains("stag")?"elk":name.Contains("ibex")||name.Contains("alpaca")?"goat":"cloth";
            Emit(bank,animal.transform.position,(alarm?.48f:.28f)*(bank=="cloth"?.3f:1),"Wildlife",alarm?100:75,false,Random.Range(.95f,1.04f));
        }
        public static void Cue(string bank,Vector3 point,float volume){Emit(bank,point,volume,"Weapons",30,true);}
        void Update(){var player=Ranger.Local;bool reloading=player&&player.Reloading.Value;if(reloading&&!wasReloading)Cue("reload",player.transform.position,.28f);wasReloading=reloading;if(!birds)return;var cam=Camera.main;float target=0;if(cam){int b=ReserveBiomes.At(cam.transform.position.x,cam.transform.position.z);float hour=WeatherState.Current?WeatherState.Current.Hour.Value:12;target=(b==0||b==4||b==5)&&hour>5&&hour<20?.075f*Level("Ambience"):0;}birds.volume=Mathf.MoveTowards(birds.volume,target,Time.deltaTime*.025f);}
        void OnDestroy(){if(current==this)current=null;}
    }
    public sealed class WildlifeSound:MonoBehaviour
    {
        Wildlife animal;float next;byte state;void Start(){animal=GetComponent<Wildlife>();next=Time.time+Random.Range(8,28);}
        void Update(){if(!animal||animal.Aquatic||animal.Dead)return;bool alarm=(animal.State.Value==4||animal.State.Value==2)&&state!=animal.State.Value;state=animal.State.Value;if(alarm||Time.time>next){next=Time.time+Random.Range(22,48);Soundscape.Animal(animal,alarm);}}
    }
}
