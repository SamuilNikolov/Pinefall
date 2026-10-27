using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pinefall
{
    public sealed class ReserveSession : MonoBehaviour
    {
        public static ReserveSession Instance;
        public NetworkManager Network { get; private set; }
        public bool Playing { get; private set; }
        public bool Connecting { get; private set; }
        public string Message="", PlayerName="Ranger", Address="127.0.0.1", Port="7777";
        public int SelectedWeapon;
        public int RequestedTeam;
        public bool BattleMode=true;
        public int TeamSize=2,ModelChoice,ColorChoice,GearChoice;
        public bool PracticeBots;
        public bool IsHost { get { return Network && Network.IsHost; } }
        public readonly List<Wildlife> Animals=new List<Wildlife>();
        public Camera vistaCamera;
        public GameObject previewHerd;
        float connectStarted;
        bool leaving;
        readonly HashSet<ulong> pendingApprovals=new HashSet<ulong>();

        void Awake()
        {
            Instance=this;
            Application.runInBackground=true;Application.targetFrameRate=PlayerPrefs.GetInt("perf.target",90);
            QualitySettings.vSyncCount=0;
            PlayerName=PlayerPrefs.GetString("rangerName","Ranger");Address=PlayerPrefs.GetString("address","127.0.0.1");
            SelectedWeapon=PlayerPrefs.GetInt("weapon",0);
            RequestedTeam=PlayerPrefs.GetInt("team",0);
            ModelChoice=PlayerPrefs.GetInt("model",0);ColorChoice=PlayerPrefs.GetInt("palette",0);GearChoice=PlayerPrefs.GetInt("gear",0);
            var net=new GameObject("Reserve networking");
            var transport=net.AddComponent<UnityTransport>();transport.MaxPacketQueueSize=1024;transport.MaxSendQueueSize=1024*1024;transport.DisconnectTimeoutMS=30000;
            Network=net.AddComponent<NetworkManager>();
            Network.NetworkConfig=new NetworkConfig{NetworkTransport=transport,TickRate=20,ClientConnectionBufferTimeout=30,SpawnTimeout=25,ConnectionApproval=true,EnableSceneManagement=false,ForceSamePrefabs=true};
            Network.NetworkConfig.PlayerPrefab=Resources.Load<GameObject>("Ranger");
            Network.AddNetworkPrefab(Network.NetworkConfig.PlayerPrefab);
            Network.AddNetworkPrefab(Resources.Load<GameObject>("Deer"));
            Network.AddNetworkPrefab(Resources.Load<GameObject>("Stag"));
            Network.AddNetworkPrefab(Resources.Load<GameObject>("ReserveWeather"));
            Network.AddNetworkPrefab(Resources.Load<GameObject>("WildfrontMatch"));
            Network.AddNetworkPrefab(Resources.Load<GameObject>("FieldReward"));
            Network.AddNetworkPrefab(Resources.Load<GameObject>("Wolf"));
            Network.AddNetworkPrefab(Resources.Load<GameObject>("Fox"));
            Network.AddNetworkPrefab(Resources.Load<GameObject>("Ox"));
            foreach(string prefab in new[]{"SpawnDraft","WorldLoot","RoadVehicle","Hare","Boar","Ibex","Antelope","ArcticFox","SnowHare","JungleBoar","Elk","Horse","Alpaca","SupplyDrop","Shark","ConstructionWorld","BuildPiece","Rocket","LaunchPad"})Network.AddNetworkPrefab(Resources.Load<GameObject>(prefab));
            Network.ConnectionApprovalCallback=Approve;
            Network.OnServerStarted+=PopulateWildlife;
            Network.OnClientConnectedCallback+=Connected;
            Network.OnClientDisconnectCallback+=Disconnected;
            Network.OnTransportFailure+=TransportFailed;
        }
        void Start()
        {
            ReserveAudio.Ensure();if(Arg("--silence")=="1")AudioListener.volume=0;
            gameObject.AddComponent<ShotHistory>();gameObject.AddComponent<RuntimePerformance>();gameObject.AddComponent<PresentationBudget>();gameObject.AddComponent<FieldNarrator>();gameObject.AddComponent<ProximityVoice>();gameObject.AddComponent<LanDiscovery>();gameObject.AddComponent<WorldInterest>();
            if(Arg("--bake-atlas")!="")gameObject.AddComponent<AtlasBakeProbe>();
            string mode=Arg("--mode");
            if(Arg("--name")!="")PlayerName=Arg("--name");
            if(Arg("--port")!="")Port=Arg("--port");
            if(Arg("--address")!="")Address=Arg("--address");
            if(Arg("--team")!="")int.TryParse(Arg("--team"),out RequestedTeam);
            if(Arg("--battle")=="0")BattleMode=false;
            if(Arg("--teamsize")!="")int.TryParse(Arg("--teamsize"),out TeamSize);
            PracticeBots=Arg("--bots")=="1";
            if(mode=="host"||mode=="solo")Host();else if(mode=="client")Join();
            if(Arg("--smoke")=="frontier")gameObject.AddComponent<LivingFrontierProbe>();
            else if(Arg("--smoke")=="fieldcraft")gameObject.AddComponent<FieldcraftProbe>();
            else if(Arg("--smoke")=="performance")gameObject.AddComponent<PerformanceProbe>();
            else if(Arg("--smoke")=="living")gameObject.AddComponent<LivingReserveProbe>();
            else if(Arg("--smoke")=="polish")gameObject.AddComponent<PolishProbe>();
            else if(Arg("--smoke").StartsWith("expedition"))gameObject.AddComponent<ExpeditionProbe>();
            else if(Arg("--smoke").StartsWith("battle"))gameObject.AddComponent<WildfrontProbe>();
            else if(Arg("--smoke")!="")gameObject.AddComponent<SmokeProbe>();
            if(Arg("--capture")!=""&&!CaptureProbe.Running){var probe=new GameObject("Visual verification");probe.AddComponent<CaptureProbe>();DontDestroyOnLoad(probe);}
        }
        static readonly string[] launchArguments=Environment.GetCommandLineArgs();
        public static string Arg(string key)
        {
            var a=launchArguments;for(int i=0;i<a.Length-1;i++)if(a[i]==key)return a[i+1];return "";
        }
        void Update()
        {
            if(Connecting&&Time.unscaledTime-connectStarted>40){Network.Shutdown();Connecting=false;Message="Connection timed out. Keep the host open, use the same build, and check UDP forwarding and Windows permission for the host game copy.";}
        }
        void Approve(NetworkManager.ConnectionApprovalRequest req,NetworkManager.ConnectionApprovalResponse res)
        {
            string protocol=Encoding.UTF8.GetString(req.Payload??new byte[0]);
            if(protocol=="PinefallReserve/9"&&Ranger.Players.Count+pendingApprovals.Count>=RoundArena.MaximumPlayers){var bot=Ranger.Players.Find(p=>p&&p.IsBot.Value);if(bot)bot.NetworkObject.Despawn();}
            res.Approved=protocol=="PinefallReserve/9"&&Ranger.Players.Count+pendingApprovals.Count<RoundArena.MaximumPlayers;
            if(res.Approved)pendingApprovals.Add(req.ClientNetworkId);
            res.CreatePlayerObject=true;res.Pending=false;
            if(!res.Approved)res.Reason=protocol!="PinefallReserve/9"?"Game versions differ. Use the same demo build.":"This reserve already has twenty players (including practice bots).";
            int index=Network.ConnectedClientsIds.Count;
            res.Position=ReserveWorld.OnGround(index*2.5f,-160,.3f);res.Rotation=Quaternion.identity;
        }
        bool Prepare()
        {
            if(Connecting||Playing)return false;
            ushort port;
            if(!ushort.TryParse(Port,out port)||port==0){Message="Use a port from 1 to 65535.";return false;}
            PlayerName=CleanName(PlayerName);
            if(Arg("--smoke")==""&&Arg("--capture")==""){PlayerPrefs.SetInt("team",RequestedTeam);
            PlayerPrefs.SetString("rangerName",PlayerName);PlayerPrefs.SetInt("weapon",SelectedWeapon);PlayerPrefs.SetString("address",Address);PlayerPrefs.Save();}
            Network.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes("PinefallReserve/9");
            Network.GetComponent<UnityTransport>().SetConnectionData(Address.Trim(),port,"0.0.0.0");
            Message="";Connecting=true;connectStarted=Time.unscaledTime;return true;
        }
        public void Host()
        {
            if(!Prepare())return;
            try {if(!Network.StartHost()){Connecting=false;Message="Could not start the host. The port may already be in use.";}}
            catch(Exception e){Connecting=false;Message="Could not host: "+e.Message;Debug.LogException(e);}
        }
        public void Join()
        {
            IPAddress ip;
            if(!IPAddress.TryParse(Address.Trim(),out ip)||ip.AddressFamily!=AddressFamily.InterNetwork){Message="Enter the host's IPv4 address, for example 192.168.1.20.";return;}
            if(!Prepare())return;
            try {if(!Network.StartClient()){Connecting=false;Message="Could not start the connection.";}}
            catch(Exception e){Connecting=false;Message="Could not connect: "+e.Message;Debug.LogException(e);}
        }
        public void CancelConnect(){Network.Shutdown();Connecting=false;Message="Connection cancelled.";}
        void Connected(ulong id)
        {
            pendingApprovals.Remove(id);Debug.Log("PINEFALL_CONNECTED "+id);
            if(id!=Network.LocalClientId)return;
            EnterLocalPlayer();
        }
        public void EnterLocalPlayer()
        {
            Playing=true;Connecting=false;Message="";vistaCamera.gameObject.SetActive(false);
            if(previewHerd)previewHerd.SetActive(false);
        }
        void Disconnected(ulong id)
        {
            pendingApprovals.Remove(id);Debug.Log("PINEFALL_DISCONNECTED "+id);
            if(leaving)return;
            if(id==Network.LocalClientId || !Network.IsListening)
            {
                string why=Network.DisconnectReason;
                if(Playing){StartCoroutine(LeaveRoutine(string.IsNullOrEmpty(why)?"The host closed the reserve.":why));}
                else {Connecting=false;Message=string.IsNullOrEmpty(why)?"Could not connect. Check the host address and UDP port.":why;}
            }
        }
        void TransportFailed(){Message="The network connection failed. Try another port or check the host address.";if(Playing)StartCoroutine(LeaveRoutine(Message));else Connecting=false;}
        public void Leave(){if(!leaving)StartCoroutine(LeaveRoutine(""));}
        IEnumerator LeaveRoutine(string message)
        {
            pendingApprovals.Clear();ReserveEffects.Reset();leaving=true;Playing=false;Connecting=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(ProximityVoice.Current)ProximityVoice.Current.ResetSession();if(FieldNarrator.Current)FieldNarrator.Current.ResetSession();
            var director=GetComponent<WildlifeDirector>();if(director)Destroy(director);var aquatic=GetComponent<SharkDirector>();if(aquatic)Destroy(aquatic);
            if(ShotHistory.Current)ShotHistory.Current.ResetSession();GetComponent<WorldInterest>().ResetSession();Network.Shutdown();float deadline=Time.realtimeSinceStartup+5;while(Network.ShutdownInProgress&&Time.realtimeSinceStartup<deadline)yield return null;yield return null;
            Animals.Clear();vistaCamera.gameObject.SetActive(true);if(previewHerd)previewHerd.SetActive(true);Message=message;leaving=false;ReserveUI.Instance.ReturnToMain();

        }
        void PopulateWildlife()
        {
            Instantiate(Resources.Load<GameObject>("ConstructionWorld")).GetComponent<NetworkObject>().Spawn();
            var weather=Instantiate(Resources.Load<GameObject>("ReserveWeather")).GetComponent<WeatherState>();
            weather.Mode.Value=ReserveWeather.PreviewMode;weather.Cycling.Value=ReserveWeather.AutoCycle;weather.Hour.Value=ReserveWeather.PreviewHour;weather.GetComponent<NetworkObject>().Spawn();
            var match=Instantiate(Resources.Load<GameObject>("WildfrontMatch")).GetComponent<WildfrontMatch>();match.Battle.Value=BattleMode;match.BotsEnabled.Value=PracticeBots;match.TeamSize.Value=TeamSize==1?1:TeamSize==4?4:2;match.GetComponent<NetworkObject>().Spawn();
            Instantiate(Resources.Load<GameObject>("SpawnDraft")).GetComponent<NetworkObject>().Spawn();
            gameObject.AddComponent<WildlifeDirector>();gameObject.AddComponent<SharkDirector>();ExpeditionLoot.Populate();
            /* WildlifeDirector owns the randomized habitat populations.
            var random=new System.Random(591);
            for(int i=0;i<24;i++)
            {
                float x,z;
                if(i<6){x=18+(i%3)*9;z=-92+(i/3)*35;}
                else
                {
                    var centers=new[]{new Vector2(-40,-50),new Vector2(-500,-280),new Vector2(-330,480),new Vector2(430,440),new Vector2(500,-220)};
                    var location=centers[(i-6)%5];x=location.x+(float)(random.NextDouble()-.5)*140;z=location.y+(float)(random.NextDouble()-.5)*140;
                }
                int biome=ReserveBiomes.At(x,z);
                string species=biome==1?"Fox":biome==2?"Wolf":biome==3?"Ox":biome==4?(i%2==0?"Ox":"Deer"):i%3==0?"Stag":"Deer";
                var obj=Instantiate(Resources.Load<GameObject>(species),ReserveWorld.OnGround(x,z),Quaternion.Euler(0,random.Next(360),0));
                obj.GetComponent<NetworkObject>().Spawn();
            }
            */
        }
        public static string CleanName(string name)
        {
            if(string.IsNullOrWhiteSpace(name))return "Ranger";
            var b=new StringBuilder();foreach(char c in name.Trim()){if(!char.IsControl(c)&&c!='<'&&c!='>')b.Append(c);if(b.Length>=20)break;}
            return b.Length==0?"Ranger":b.ToString();
        }
        public static string LocalAddress()
        {
            try{foreach(var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)if(ip.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(ip))return ip.ToString();}catch{}
            return "127.0.0.1";
        }
        void OnApplicationQuit(){if(Network)Network.Shutdown();}
    }
    public static class SessionNotice { public static string Text=""; }
}
