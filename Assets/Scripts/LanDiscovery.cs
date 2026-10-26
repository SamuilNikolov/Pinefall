using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
namespace Pinefall
{
    // Only local-network discovery. Internet guests retain the explicit address/port join path.
    public sealed class LanDiscovery:MonoBehaviour
    {
        const int DiscoveryPort=47776;const string Query="PINEFALL_FIND_8";
        [Serializable]public class SessionInfo{public string name,address,phase;public int port,players,teamSize;[NonSerialized]public float seen;}
        public static LanDiscovery Current;public readonly List<SessionInfo> Sessions=new List<SessionInfo>();
        public string Status="Search your local network for reserves.";UdpClient host,browser;float nextSearch;bool searching;
        void Awake(){Current=this;}
        public void Search()
        {
            Sessions.Clear();searching=true;nextSearch=0;Status="Searching your local network…";
            if(browser==null)try{browser=new UdpClient(0);browser.EnableBroadcast=true;}catch(SocketException){Status="Local discovery is unavailable. Use an address to join.";}
        }
        void Update()
        {
            var s=ReserveSession.Instance;
            if(s&&s.IsHost&&host==null)try{host=new UdpClient();host.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);host.Client.Bind(new IPEndPoint(IPAddress.Any,DiscoveryPort));}catch(SocketException){if(host!=null)host.Close();host=null;}
            if(host!=null)for(int i=0;i<8&&host.Available>0;i++)
            {
                try{IPEndPoint peer=new IPEndPoint(IPAddress.Any,0);byte[] packet=host.Receive(ref peer);if(packet.Length>64||Encoding.UTF8.GetString(packet)!=Query)continue;var m=WildfrontMatch.Current;
                    var info=new SessionInfo{name=s.PlayerName+"'s reserve",port=int.Parse(s.Port),players=Ranger.Players.Count,teamSize=m?m.TeamSize.Value:s.TeamSize,phase=m&&m.Active?"In progress":m&&m.Phase.Value>=3?"Drafting":"Lobby"};byte[] reply=Encoding.UTF8.GetBytes(JsonUtility.ToJson(info));host.Send(reply,reply.Length,peer);}catch(Exception e){if(!(e is SocketException))Debug.LogWarning("Local discovery reply unavailable.");}
            }
            if(!searching||browser==null)return;
            if(Time.unscaledTime>=nextSearch){nextSearch=Time.unscaledTime+2;try{byte[] query=Encoding.UTF8.GetBytes(Query);browser.Send(query,query.Length,new IPEndPoint(IPAddress.Broadcast,DiscoveryPort));browser.Send(query,query.Length,new IPEndPoint(IPAddress.Loopback,DiscoveryPort));}catch(SocketException){Status="Discovery was blocked. You can still enter an address.";}}
            for(int i=0;i<16&&browser.Available>0;i++)try
            {IPEndPoint peer=new IPEndPoint(IPAddress.Any,0);byte[] bytes=browser.Receive(ref peer);if(bytes.Length>768)continue;var info=JsonUtility.FromJson<SessionInfo>(Encoding.UTF8.GetString(bytes));if(info==null||info.port<1||info.port>65535||info.name==null)continue;info.name=ReserveSession.CleanName(info.name);info.address=peer.Address.ToString();info.seen=Time.unscaledTime;Sessions.RemoveAll(x=>x.address==info.address&&x.port==info.port);Sessions.Add(info);Status="Select a reserve, or enter an internet address manually.";}catch(Exception){Status="Waiting for local reserves…";}
            Sessions.RemoveAll(x=>Time.unscaledTime-x.seen>7);
        }
        void OnDestroy(){if(host!=null)host.Close();if(browser!=null)browser.Close();if(Current==this)Current=null;}
    }
}
