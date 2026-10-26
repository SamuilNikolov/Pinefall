using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
 public sealed class FieldNarrator:MonoBehaviour
 {
 public static FieldNarrator Current;public float Volume=.7f;public bool Captions=true,Playful=true;public string Subtitle="";public int Announcements;
 struct Line{public string key,caption;}readonly Queue<Line> queue=new Queue<Line>();readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();AudioSource source;float until;
 public static readonly Dictionary<string,string> Script=new Dictionary<string,string>{{"lobby","Welcome to Wildfront. Pick your kit, place your bid, and try to look like you have a plan."},{"deploy","The hunt is on. Find supplies, watch the sky, and remember: the deer were here first."},{"warning","Storm movement in twelve seconds. This would be an excellent time to consult a map."},{"closing","The storm is closing. Wildlife is enraged. Congratulations, you have annoyed an entire ecosystem."},{"safe","New safe zone marked. Your previous camping spot has received a poor weather review."},{"inbound","Supply aircraft inbound. Something useful is falling from the sky. Finally."},{"landed","Supply crate landed. Free equipment, with a complimentary risk of ambush."},{"claimed","Supply crate claimed. Sharing is caring. Apparently, so is looting first."},{"victory","Expedition complete. To the survivors: well played. To everyone else: excellent compost."},{"elim0","Hunter eliminated. The reserve is one guest quieter."},{"elim1","Another hunter down. A bold strategy, briefly."},{"elim2","Elimination confirmed. Their backpack is now a community resource."},{"banter0","Friendly reminder: you cannot negotiate with the storm. We tried. Terrible customer service."},{"banter1","If the wildlife is staring at you, that is usually not admiration."},{"banter2","A working vehicle beats a heroic walk. Especially the walking part."},{"banter3","There is no prize for carrying all your ammunition into the afterlife."}};
 public static string Text(string key){return Script.ContainsKey(key)?Script[key]:key;}
 void Awake(){Current=this;source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;source.priority=24;foreach(var key in Script.Keys)clips[key]=Resources.Load<AudioClip>("Announcer/"+key);}
 public void Announce(string key,string caption=""){if(!clips.ContainsKey(key)||key.StartsWith("banter")&&(!Playful||queue.Count>0||source.isPlaying))return;Announcements++;if(queue.Count<32)queue.Enqueue(new Line{key=key,caption=caption});}
 void Update(){source.volume=Volume;if(!source.isPlaying&&queue.Count>0){var line=queue.Dequeue();source.clip=clips[line.key];Subtitle=line.caption==""?Text(line.key):line.caption;until=Time.unscaledTime+(source.clip?source.clip.length:3)+.6f;if(source.clip)source.Play();}if(Time.unscaledTime>until)Subtitle="";}
 public void ResetSession(){queue.Clear();source.Stop();Subtitle="";}void OnDestroy(){if(Current==this)Current=null;}
 }
 public sealed partial class WildfrontMatch
 {
 [Unity.Netcode.ClientRpc]public void NarrateClientRpc(string key,string caption=""){if(FieldNarrator.Current)FieldNarrator.Current.Announce(key,caption);}
 float nextBanter;void BanterTick(){if(!IsServer||!Active||Time.time<nextBanter)return;nextBanter=Time.time+Random.Range(75,115);if(!Closing)NarrateClientRpc("banter"+Random.Range(0,4));}
 }
}
