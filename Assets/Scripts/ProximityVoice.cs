using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class ProximityVoice:MonoBehaviour
    {
        public static ProximityVoice Current;
        public const float Range=75;
        public bool Enabled,OpenMic;public float GateThreshold=.018f;double speechUntil;public bool Transmitting{get;private set;}public int MicrophoneStarts{get;private set;}public string Status="Microphone off";public float ReceiveVolume=1;public int ReceivedFrames;
        readonly byte[] capturePacket=new byte[240],upPacket=new byte[240],downPacket=new byte[240];float[] captureSamples;bool attempted;
        NetworkManager network;AudioClip mic;string device;int readHead;readonly Dictionary<ulong,VoiceReceiver> receivers=new Dictionary<ulong,VoiceReceiver>();readonly Dictionary<ulong,float> rate=new Dictionary<ulong,float>();readonly HashSet<ulong> muted=new HashSet<ulong>();
        void Awake(){Current=this;}
        public bool Speaking(ulong id){VoiceReceiver r;return receivers.TryGetValue(id,out r)&&r&&Time.unscaledTime-r.LastPacket<.3f;}
        public int FramesFrom(ulong id){VoiceReceiver r;return receivers.TryGetValue(id,out r)&&r?r.Frames:0;}
        public bool Muted(ulong id){return muted.Contains(id);}public void ToggleMute(ulong id){if(!muted.Add(id))muted.Remove(id);}
        void Update()
        {
            var n=ReserveSession.Instance.Network;
            if(n&&n.IsListening&&network!=n){network=n;network.CustomMessagingManager.RegisterNamedMessageHandler("voice-up",Up);network.CustomMessagingManager.RegisterNamedMessageHandler("voice-down",Down);}
            if(!Enabled||!ReserveSession.Instance.Playing||!Ranger.Local){StopMic();attempted=false;return;}
            // Explicitly enabling voice warms the device once; a PTT press only gates already-running samples.
            if(!mic&&!attempted)
            {
                attempted=true;var devices=Microphone.devices;if(devices.Length==0){Status="No microphone detected";return;}
                device=devices[0];mic=Microphone.Start(device,true,1,12000);MicrophoneStarts++;readHead=0;
                if(mic)captureSamples=new float[240*mic.channels];
            }
            if(!mic)return;int end=Microphone.GetPosition(device);if(end<0)return;
            bool talk=Ranger.Local.Alive&&Application.isFocused&&(OpenMic||GameBindings.Held("Talk"));
            if(!talk){Transmitting=false;readHead=end;Status=OpenMic?"Open mic ready":"Mic ready · hold N to speak";return;}
            if(!OpenMic&&!Transmitting){readHead=end;Transmitting=true;Status="Speaking · nearby players within 75 m";return;}
            int available=(end-readHead+mic.samples)%mic.samples;
            while(available>=240){mic.GetData(captureSamples,readHead);float energy=0;for(int i=0;i<240;i++){float sample=captureSamples[i*mic.channels];energy+=sample*sample;capturePacket[i]=Encode(sample);}bool gate=GateOpen(Mathf.Sqrt(energy/240),GateThreshold,Time.unscaledTime,ref speechUntil);bool send=!OpenMic||gate;Transmitting=send;Status=OpenMic?(send?"Open mic · speaking nearby":"Open mic ready"):"Speaking · nearby players within 75 m";if(send)Transmit(capturePacket);readHead=(readHead+240)%mic.samples;available-=240;}
        }
        public static bool GateOpen(float rms,float threshold,double now,ref double tail){if(rms>threshold)tail=now+.22;return now<tail;}
        void StopMic(){Transmitting=false;if(mic){Microphone.End(device);Destroy(mic);mic=null;captureSamples=null;}if(!Enabled)Status="Microphone off";}
        public void Prepare(Ranger player)
        {
            if(!player||player.IsBot.Value||player.LocalHuman||receivers.ContainsKey(player.NetworkObjectId))return;
            var go=new GameObject("Nearby voice");go.transform.SetParent(player.transform,false);go.transform.localPosition=Vector3.up*1.6f;receivers[player.NetworkObjectId]=go.AddComponent<VoiceReceiver>();
        }
        public void Transmit(byte[] packet)
        {
            if(!network||!network.IsListening||packet==null||packet.Length!=240)return;
            if(network.IsServer){Route(network.LocalClientId,packet);return;}
            using(var w=new FastBufferWriter(256,Allocator.Temp)){w.WriteValueSafe((ushort)packet.Length);foreach(byte b in packet)w.WriteValueSafe(b);network.CustomMessagingManager.SendNamedMessage("voice-up",NetworkManager.ServerClientId,w,NetworkDelivery.Unreliable);}
        }
        void Up(ulong sender,FastBufferReader r)
        {
            if(!network.IsServer||!r.TryBeginRead(2))return;r.ReadValueSafe(out ushort count);if(count!=240||!r.TryBeginRead(count))return;
            float last;if(rate.TryGetValue(sender,out last)&&Time.unscaledTime-last<.012f)return;rate[sender]=Time.unscaledTime;
            for(int i=0;i<count;i++)r.ReadValueSafe(out upPacket[i]);Route(sender,upPacket);
        }
        void Route(ulong sender,byte[] packet)
        {
            var speaker=Ranger.Players.FirstOrDefault(p=>p&&!p.IsBot.Value&&p.OwnerClientId==sender&&p.Alive);if(!speaker)return;
            foreach(var listener in Ranger.Players)
            {
                if(!listener||listener.IsBot.Value||listener==speaker||Vector3.Distance(listener.transform.position,speaker.transform.position)>=Range)continue;
                if(listener.OwnerClientId==network.LocalClientId){Receive(speaker.NetworkObjectId,packet);continue;}
                using(var w=new FastBufferWriter(256,Allocator.Temp)){w.WriteValueSafe(speaker.NetworkObjectId);w.WriteValueSafe((ushort)packet.Length);foreach(byte b in packet)w.WriteValueSafe(b);network.CustomMessagingManager.SendNamedMessage("voice-down",listener.OwnerClientId,w,NetworkDelivery.Unreliable);}
            }
        }
        void Down(ulong sender,FastBufferReader r)
        {
            if(sender!=NetworkManager.ServerClientId||!r.TryBeginRead(10))return;r.ReadValueSafe(out ulong id);r.ReadValueSafe(out ushort count);if(count!=240||!r.TryBeginRead(count))return;
            for(int i=0;i<count;i++)r.ReadValueSafe(out downPacket[i]);Receive(id,downPacket);
        }
        void Receive(ulong id,byte[] packet)
        {
            if(muted.Contains(id))return;var speaker=Ranger.Players.FirstOrDefault(p=>p&&p.NetworkObjectId==id);if(!speaker||!Ranger.Local||Vector3.Distance(Ranger.Local.transform.position,speaker.transform.position)>=Range)return;
            VoiceReceiver receiver;if(!receivers.TryGetValue(id,out receiver)||!receiver){var go=new GameObject("Nearby voice");go.transform.SetParent(speaker.transform,false);go.transform.localPosition=Vector3.up*1.6f;receiver=go.AddComponent<VoiceReceiver>();receivers[id]=receiver;}
            receiver.Volume=ReceiveVolume;receiver.Push(packet);ReceivedFrames++;
        }
        public static byte Encode(float f)
        {int pcm=Mathf.Clamp(Mathf.RoundToInt(f*32767),-32635,32635),sign=(pcm>>8)&0x80;if(sign!=0)pcm=-pcm;pcm+=132;int exponent=7;for(int mask=0x4000;(pcm&mask)==0&&exponent>0;mask>>=1)exponent--;int mantissa=(pcm>>(exponent+3))&15;return (byte)~(sign|(exponent<<4)|mantissa);}
        public static float Decode(byte value)
        {int u=(~value)&255;int sample=(((u&15)<<3)+132)<<((u>>4)&7);sample-=132;return ((u&128)!=0?-sample:sample)/32768f;}
        public void ResetSession(){Enabled=false;StopMic();network=null;receivers.Clear();rate.Clear();muted.Clear();attempted=false;}
        void OnDestroy(){StopMic();if(network&&network.CustomMessagingManager!=null){network.CustomMessagingManager.UnregisterNamedMessageHandler("voice-up");network.CustomMessagingManager.UnregisterNamedMessageHandler("voice-down");}if(Current==this)Current=null;}
    }
    public sealed class VoiceReceiver:MonoBehaviour
    {
        readonly float[] queue=new float[4096];readonly object gate=new object();int read,write,available;AudioSource source;public float LastPacket,Volume=1;public int Frames;
        void Awake(){source=gameObject.AddComponent<AudioSource>();source.spatialBlend=1;source.priority=16;source.minDistance=2;source.maxDistance=75;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;source.clip=AudioClip.Create("Proximity speech",12000,1,12000,true,Fill);source.loop=true;source.Play();}
        public void Push(byte[] frame){Frames++;LastPacket=Time.unscaledTime;lock(gate){if(available>1440){read=write;available=0;}foreach(byte b in frame){queue[write]=ProximityVoice.Decode(b);write=(write+1)%queue.Length;available++;}}}
        void Fill(float[] output){lock(gate){for(int i=0;i<output.Length;i++){if(available<=0){output[i]=0;continue;}output[i]=queue[read];read=(read+1)%queue.Length;available--;}}}
        void Update(){source.volume=Volume;}
        void OnDestroy(){if(source&&source.clip)Destroy(source.clip);}
    }
}
