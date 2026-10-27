using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class FieldNarrator:MonoBehaviour
    {
        public static FieldNarrator Current;
        public float Volume=.7f;public bool Captions=true,Playful=true;public string Subtitle="";public int Announcements;
        struct Line{public string key,caption;public float at;public int priority;}
        readonly Queue<Line> queue=new Queue<Line>();readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        AudioSource source;float until;int currentPriority;
        public static readonly Dictionary<string,string> Script=new Dictionary<string,string>{
            {"lobby","Welcome to Wildfront. Spend wisely, land boldly, and try not to become a cautionary tale."},
            {"deploy","The reserve is open. Find a weapon, find a route, and remember: stealth is cheaper than ammunition."},
            {"warning","Storm movement in twelve seconds. If your plan begins with one more chest, revise it."},
            {"closing","The storm is moving. Wildlife is enraged. Apparently we have upset the entire food chain."},
            {"safe","New safe zone marked. Your old hiding place has been rezoned as a terrible idea."},
            {"inbound","Supply aircraft inbound. Look up, then look around. You are unlikely to be the only customer."},
            {"landed","Supply crate on the ground. Premium equipment. Extremely competitive parking."},
            {"claimed","Supply crate claimed. That was either excellent timing, or somebody else was reloading."},
            {"victory","We have our survivors. Well played. The reserve would like its ammunition back, but we understand."},
            {"elim0","Hunter eliminated. Their equipment is now available for a very informal inheritance process."},
            {"elim1","Another hunter down. A brief, ambitious contribution to the local economy."},
            {"elim2","Elimination confirmed. A moment of silence. Preferably behind cover."},
            {"banter0","A useful field tip: every shortcut through an open field comes with complimentary long-range attention."},
            {"banter1","If a rabbit starts chasing you, stop checking the guidebook and start running."},
            {"banter2","You can hoard supplies, or you can reach the safe zone. Ideally, both. The storm is not sentimental."},
            {"banter3","Nothing says subtle approach quite like a motorcycle arriving through a kitchen."},
            {"warning1","Twelve seconds until the storm moves. Please conclude any unnecessary arguments with the scenery."},
            {"closing1","The ring is closing. The animals are angry, and the weather is taking their side."},
            {"safe1","Next safe zone marked. A fresh destination for all your questionable decisions."},
            {"inbound1","Supply flight approaching. A box full of opportunities, descending into a neighborhood full of opinions."},
            {"landed1","Crate landed. It contains valuable equipment and absolutely no guarantee of personal safety."},
            {"claimed1","Supplies secured. Remember to reload before celebrating. We have had incidents."},
            {"elim3","One fewer hunter. More loot per square metre. Nature loves efficiency."},
            {"elim4","Hunter eliminated. Their rotation has become a permanent stop."},
            {"banter4","Those abandoned houses have survived years of storms. Let us see how they handle your driving."},
            {"banter5","A launchpad is a wonderful way to leave a problem. Landing is where the next problem begins."},
            {"banter6","The mountains offer spectacular views of people who forgot to bring ammunition."},
            {"banter7","In the caves, footsteps echo. So do bad decisions. Travel accordingly."},
            {"mutation","Storm mutation detected. A larger animal has entered the reserve. Better loot, considerably worse manners."},
            {"relay","A supply relay has been activated. Incoming flight marked. This invitation was not team exclusive."}
        };
        public static string Text(string key){return Script.TryGetValue(key,out var text)?text:key;}
        void Awake(){Current=this;source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;source.priority=24;foreach(var key in Script.Keys)clips[key]=Resources.Load<AudioClip>("Announcer/"+key);}
        public void Announce(string key,string caption="")
        {
            if(!clips.ContainsKey(key))return;
            int priority=key=="victory"?6:key.StartsWith("closing")?5:key.StartsWith("warning")?4:key=="deploy"?3:key.StartsWith("banter")?0:2;
            if(priority==0&&(!Playful||queue.Count>0||source.isPlaying))return;
            if(priority>=4)
            {
                if(source.isPlaying&&currentPriority<priority)source.Stop();
                int count=queue.Count;for(int i=0;i<count;i++){var pending=queue.Dequeue();if(pending.priority>=priority)queue.Enqueue(pending);}
            }
            Announcements++;if(queue.Count>=8){if(priority<3)return;queue.Dequeue();}
            queue.Enqueue(new Line{key=key,caption=caption,at=Time.unscaledTime,priority=priority});
        }
        void Update()
        {
            source.volume=Volume;
            if(!source.isPlaying&&queue.Count>0)
            {
                var line=queue.Dequeue();if(Time.unscaledTime-line.at>12&&line.priority<4)return;
                source.clip=clips[line.key];currentPriority=line.priority;Subtitle=line.caption==""?Text(line.key):line.caption;
                until=Time.unscaledTime+(source.clip?source.clip.length:4)+.6f;if(source.clip)source.Play();
            }
            if(Time.unscaledTime>until)Subtitle="";
        }
        public void ResetSession(){queue.Clear();source.Stop();Subtitle="";}
        void OnDestroy(){if(Current==this)Current=null;}
    }
    public sealed partial class WildfrontMatch
    {
        [Unity.Netcode.ClientRpc]public void MutationAlertClientRpc(){if(FieldNarrator.Current)FieldNarrator.Current.Announce("mutation");}
        [Unity.Netcode.ClientRpc]public void NarrateClientRpc(string key,string caption=""){if(FieldNarrator.Current)FieldNarrator.Current.Announce(key,caption);}
        public void NarrateEvent(string key,string caption="")
        {if(!IsServer)return;if(FieldNarrator.Script.ContainsKey(key+"1")&&Random.value<.5f)key+="1";NarrateClientRpc(key,caption);}
        float nextBanter;
        void BanterTick(){if(!IsServer||!Active||Time.time<nextBanter)return;nextBanter=Time.time+Random.Range(70,110);if(!Closing)NarrateEvent("banter"+Random.Range(0,8));}
    }
}
