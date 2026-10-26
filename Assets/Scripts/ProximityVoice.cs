using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class ProximityVoice : MonoBehaviour
    {
        public const int SampleRate=16000, FrameSamples=320;
        public const float Range=75;
        public static ProximityVoice Current;
        public bool Enabled,OpenMic; public float GateThreshold=.014f,ReceiveVolume=1;
        public bool Transmitting{get;private set;} public int MicrophoneStarts{get;private set;}
        public string Status="Microphone off"; public int ReceivedFrames;
        readonly byte[] capture=new byte[FrameSamples],up=new byte[FrameSamples],down=new byte[FrameSamples];
        readonly Dictionary<ulong,VoiceReceiver> receivers=new Dictionary<ulong,VoiceReceiver>();
        readonly Dictionary<ulong,VoicePacketBudget> rates=new Dictionary<ulong,VoicePacketBudget>();
        readonly HashSet<ulong> muted=new HashSet<ulong>();
        NetworkManager network; AudioClip mic; string device; float[] native; int nativeFrame,readHead; bool attempted,talking;
        uint sequence; double speechUntil; float envelope;
        void Awake(){Current=this;}
        public bool Speaking(ulong id){return receivers.TryGetValue(id,out var r)&&r&&Time.unscaledTime-r.LastPacket<.35f;}
        public int FramesFrom(ulong id){return receivers.TryGetValue(id,out var r)&&r?r.Frames:0;}
        public bool Muted(ulong id){return muted.Contains(id);}
        public void ToggleMute(ulong id){if(!muted.Add(id))muted.Remove(id);}
        void Register()
        {
            var next=ReserveSession.Instance.Network;
            if(!next||!next.IsListening||network==next)return;
            network=next;network.CustomMessagingManager.RegisterNamedMessageHandler("voice-up",Up);network.CustomMessagingManager.RegisterNamedMessageHandler("voice-down",Down);
        }
        void Update()
        {
            Register();
            if(!Enabled||!ReserveSession.Instance.Playing||!Ranger.Local){StopMic();attempted=false;return;}
            if(!mic&&!attempted)
            {
                attempted=true;var devices=Microphone.devices;
                if(devices.Length==0){Status="No microphone detected";return;}
                device=devices[0];
                try
                {
                    Microphone.GetDeviceCaps(device,out int minimum,out int maximum);
                    int requested=maximum==0?48000:Mathf.Clamp(48000,Mathf.Max(8000,minimum),Mathf.Max(8000,maximum));
                    mic=Microphone.Start(device,true,2,requested);MicrophoneStarts++;
                    if(mic){nativeFrame=Mathf.Max(1,mic.frequency/50);native=new float[nativeFrame*mic.channels];readHead=0;}
                }
                catch(System.Exception e){Status="Microphone unavailable: "+e.Message;return;}
            }
            if(!mic)return;int end=Microphone.GetPosition(device);if(end<0)return;
            bool active=Ranger.Local.Alive&&Application.isFocused&&(OpenMic||GameBindings.Held("Talk"));
            if(!active){talking=Transmitting=false;readHead=end;Status=OpenMic?"Open mic ready":"Mic ready - hold "+GameBindings.Label("Talk");return;}
            if(!talking){talking=true;readHead=end;return;}
            int available=(end-readHead+mic.samples)%mic.samples;
            if(available>nativeFrame*10){readHead=(end-nativeFrame*5+mic.samples)%mic.samples;available=nativeFrame*5;}
            while(available>=nativeFrame)
            {
                mic.GetData(native,readHead);float energy=0;
                for(int i=0;i<FrameSamples;i++)
                {
                    int begin=i*nativeFrame/FrameSamples,finish=Mathf.Max(begin+1,(i+1)*nativeFrame/FrameSamples);float value=0;int count=0;
                    for(int source=begin;source<finish&&source<nativeFrame;source++)for(int channel=0;channel<mic.channels;channel++){value+=native[source*mic.channels+channel];count++;}
                    value/=Mathf.Max(1,count);energy+=value*value;capture[i]=Encode(value);
                }
                float rms=Mathf.Sqrt(energy/FrameSamples);envelope=Mathf.Max(rms,envelope*.86f);
                bool send=!OpenMic||GateOpen(envelope,GateThreshold,Time.unscaledTime,ref speechUntil);
                Transmitting=send;Status=OpenMic?(send?"Open mic - speaking nearby":"Open mic ready"):"Speaking - nearby players within 75 m";
                if(send)Transmit(capture);readHead=(readHead+nativeFrame)%mic.samples;available-=nativeFrame;
            }
        }
        public static bool GateOpen(float rms,float threshold,double now,ref double tail){if(rms>threshold)tail=now+.30;return now<tail;}
        void StopMic(){Transmitting=talking=false;if(mic){Microphone.End(device);Destroy(mic);mic=null;native=null;}if(!Enabled)Status="Microphone off";}
        public void Prepare(Ranger player)
        {
            if(!player||player.IsBot.Value||player.LocalHuman||receivers.ContainsKey(player.NetworkObjectId))return;
            var obj=new GameObject("Nearby voice");obj.transform.SetParent(player.transform,false);obj.transform.localPosition=Vector3.up*1.6f;receivers[player.NetworkObjectId]=obj.AddComponent<VoiceReceiver>();
        }
        public void Transmit(byte[] packet)
        {
            Register();if(!network||!network.IsListening||packet==null||packet.Length!=FrameSamples)return;uint next=++sequence;
            if(network.IsServer){Route(network.LocalClientId,next,packet);return;}
            using(var writer=new FastBufferWriter(384,Allocator.Temp))
            {writer.WriteValueSafe(next);writer.WriteValueSafe((ushort)FrameSamples);foreach(byte b in packet)writer.WriteValueSafe(b);network.CustomMessagingManager.SendNamedMessage("voice-up",NetworkManager.ServerClientId,writer,NetworkDelivery.Unreliable);}
        }
        void Up(ulong sender,FastBufferReader reader)
        {
            if(!network.IsServer||!reader.TryBeginRead(6))return;reader.ReadValueSafe(out uint seq);reader.ReadValueSafe(out ushort count);if(count!=FrameSamples||!reader.TryBeginRead(count))return;
            if(!rates.TryGetValue(sender,out var budget)){budget=new VoicePacketBudget();rates.Add(sender,budget);}
            // Frames arrive in bursts. A per-callback 12 ms rejection discarded most valid speech.
            if(!budget.Accept(Time.unscaledTime))return;
            for(int i=0;i<FrameSamples;i++)reader.ReadValueSafe(out up[i]);Route(sender,seq,up);
        }
        void Route(ulong sender,uint seq,byte[] packet)
        {
            var speaker=Ranger.Players.FirstOrDefault(p=>p&&!p.IsBot.Value&&p.OwnerClientId==sender&&p.Alive);if(!speaker)return;
            foreach(var listener in Ranger.Players)
            {
                if(!listener||listener.IsBot.Value||listener==speaker||(listener.transform.position-speaker.transform.position).sqrMagnitude>=Range*Range)continue;
                if(listener.OwnerClientId==network.LocalClientId){Receive(speaker.NetworkObjectId,seq,packet);continue;}
                using(var writer=new FastBufferWriter(384,Allocator.Temp))
                {writer.WriteValueSafe(speaker.NetworkObjectId);writer.WriteValueSafe(seq);writer.WriteValueSafe((ushort)FrameSamples);foreach(byte b in packet)writer.WriteValueSafe(b);network.CustomMessagingManager.SendNamedMessage("voice-down",listener.OwnerClientId,writer,NetworkDelivery.Unreliable);}
            }
        }
        void Down(ulong sender,FastBufferReader reader)
        {
            if(sender!=NetworkManager.ServerClientId||!reader.TryBeginRead(14))return;
            reader.ReadValueSafe(out ulong id);reader.ReadValueSafe(out uint seq);reader.ReadValueSafe(out ushort count);if(count!=FrameSamples||!reader.TryBeginRead(count))return;
            for(int i=0;i<FrameSamples;i++)reader.ReadValueSafe(out down[i]);Receive(id,seq,down);
        }
        void Receive(ulong id,uint seq,byte[] packet)
        {
            if(muted.Contains(id))return;var speaker=Ranger.Players.FirstOrDefault(p=>p&&p.NetworkObjectId==id);
            if(!speaker||!Ranger.Local||(speaker.transform.position-Ranger.Local.transform.position).sqrMagnitude>=Range*Range)return;
            if(!receivers.TryGetValue(id,out var receiver)||!receiver){Prepare(speaker);if(!receivers.TryGetValue(id,out receiver)||!receiver)return;}
            receiver.Volume=ReceiveVolume;if(receiver.Push(seq,packet))ReceivedFrames++;
        }
        public static byte Encode(float value)
        {int pcm=Mathf.Clamp(Mathf.RoundToInt(value*32767),-32635,32635),sign=(pcm>>8)&128;if(sign!=0)pcm=-pcm;pcm+=132;int exponent=7;for(int mask=0x4000;(pcm&mask)==0&&exponent>0;mask>>=1)exponent--;return (byte)~(sign|(exponent<<4)|((pcm>>(exponent+3))&15));}
        public static float Decode(byte value)
        {int u=~value&255,sample=(((u&15)<<3)+132)<<((u>>4)&7);sample-=132;return ((u&128)!=0?-sample:sample)/32768f;}
        void Unregister(){if(network&&network.CustomMessagingManager!=null){network.CustomMessagingManager.UnregisterNamedMessageHandler("voice-up");network.CustomMessagingManager.UnregisterNamedMessageHandler("voice-down");}network=null;}
        public void ResetSession(){Enabled=false;StopMic();Unregister();foreach(var receiver in receivers.Values)if(receiver)Destroy(receiver.gameObject);receivers.Clear();rates.Clear();muted.Clear();attempted=false;envelope=0;sequence=0;}
        void OnDestroy(){StopMic();Unregister();if(Current==this)Current=null;}
    }
    public sealed class VoiceReceiver:MonoBehaviour
    {
        readonly VoiceJitterBuffer buffer=new VoiceJitterBuffer();AudioSource source;int outputRate;public int PlaybackBlocks;
        public float LastPacket=-10,Volume=1;public int Frames;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();source.spatialBlend=1;source.priority=16;source.minDistance=2;source.maxDistance=75;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;
            outputRate=AudioSettings.outputSampleRate;source.clip=AudioClip.Create("Proximity audio clock",outputRate,1,outputRate,false);source.loop=true;source.Play();
        }
        public bool Push(uint sequence,byte[] frame){if(Time.unscaledTime-LastPacket>.3f)buffer.Reset();LastPacket=Time.unscaledTime;bool accepted=buffer.Push(sequence,frame);if(accepted)Frames++;return accepted;}
        void OnAudioFilterRead(float[] data,int channels){buffer.RenderDevice(data,channels,outputRate);PlaybackBlocks++;}
        void Update(){source.volume=Volume;}
        void OnDestroy(){if(source&&source.clip)Destroy(source.clip);}
    }
}
