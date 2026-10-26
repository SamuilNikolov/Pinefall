using System;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class ReserveUI : MonoBehaviour
    {
        public static ReserveUI Instance;
        public Font serif,sans;
        public float Sensitivity=1.8f;
        public bool ControlsActive {get{return ReserveSession.Instance&&ReserveSession.Instance.Playing&&panel==""&&Application.isFocused&&ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")=="";}}
        string panel="main",returnPanel="main";
        GUIStyle type,title,body,small,field,button,number;
        Texture2D white,fade,scope;
        Color ink=new Color(.07f,.14f,.18f),paper=new Color(.95f,.93f,.85f),muted=new Color(.7f,.77f,.76f),gold=new Color(.84f,.71f,.43f);
        float frameAverage;
        ReserveSession S {get{return ReserveSession.Instance;}}
        Ranger P {get{return Ranger.Local;}}
        void Awake(){Instance=this;Sensitivity=PlayerPrefs.GetFloat("sensitivity",1.8f);}
        void Start()
        {
            if(SessionNotice.Text!=""){S.Message=SessionNotice.Text;SessionNotice.Text="";}
            ApplyQuality(PlayerPrefs.GetInt("quality",1));Unlock();
        }
        void Init()
        {
            if(white)return;
            white=Texture2D.whiteTexture;
            type=Style(24,paper,sans);body=Style(20,paper,sans);small=Style(16,muted,sans);title=Style(104,paper,serif);number=Style(40,paper,sans);
            button=Style(23,paper,sans);button.alignment=TextAnchor.MiddleLeft;button.padding=new RectOffset(22,12,0,0);
            field=Style(24,paper,sans);field.alignment=TextAnchor.MiddleLeft;field.padding=new RectOffset(16,10,0,0);field.normal.background=null;field.focused.textColor=paper;GUI.skin.settings.cursorColor=paper;
            fade=new Texture2D(256,1,TextureFormat.RGBA32,false);
            for(int i=0;i<256;i++){float t=i/255f;fade.SetPixel(i,0,new Color(ink.r,ink.g,ink.b,Mathf.Lerp(.97f,0,Mathf.SmoothStep(0,1,t))));}fade.Apply();
            scope=new Texture2D(256,256,TextureFormat.RGBA32,false);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(127.5f,127.5f))/127;float a=Mathf.Clamp01((r-.94f)/.055f);scope.SetPixel(x,y,new Color(.015f,.02f,.018f,a));}scope.Apply();
        }
        GUIStyle Style(int size,Color color,Font font){var s=new GUIStyle(GUI.skin.label){font=font,fontSize=size,wordWrap=true,richText=false};s.normal.textColor=color;return s;}
        public void BeginPlay(){panel=WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value&&WildfrontMatch.Current.Phase.Value==0?"lobby":"";if(S.IsHost&&!inviteShown&&ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")==""){panel="session";inviteShown=true;if(!S.GetComponent<SessionAddress>())S.gameObject.AddComponent<SessionAddress>();}if(panel==""&&ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")=="")Lock();else Unlock();}
        public void VerificationPanel(string value){panel=value;returnPanel=S.Playing?"pause":"main";Unlock();}
        public void Open(string which){returnPanel=S.Playing?"pause":"main";panel=which;Unlock();}
        void Update()
        {
            frameAverage=Mathf.Lerp(frameAverage,Time.unscaledDeltaTime,.04f);if(outfit&&panel!="character")outfit.Hide();
            if(bindingCapture!="")return;
            if(!S||!S.Playing)return;
            if(WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value&&WildfrontMatch.Current.Phase.Value==2&&panel==""){panel="result";Unlock();}
            int phase=WildfrontMatch.Current?WildfrontMatch.Current.Phase.Value:0;
            if(phase==0&&panel=="result"){panel="lobby";Unlock();}
            if(phase==3||phase==4){panel="draft";Unlock();return;}if(panel=="draft"){panel="";Lock();}
            if(GameBindings.Down("Lobby")&&WildfrontMatch.Current&&WildfrontMatch.Current.Phase.Value==0){panel="lobby";Unlock();}
            if(Input.GetKeyDown(KeyCode.Escape)) {if(panel==""){panel="pause";Unlock();}else{panel="";Lock();}}
            if(GameBindings.Down("Map")){panel=panel=="map"?"":"map";returnPanel="pause";if(panel=="")Lock();else Unlock();}
            if(GameBindings.Down("Inventory")){panel=panel=="loadout"?"":"loadout";returnPanel="pause";if(panel=="")Lock();else Unlock();}
            if(panel==""&&Application.isFocused&&Cursor.lockState!=CursorLockMode.Locked&&ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")=="")Lock();
        }
        void OnApplicationFocus(bool focus){if(!focus&&S&&S.Playing&&panel==""&&ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")==""){panel="pause";Unlock();}}
        static void Lock(){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        static void Unlock(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        void OnGUI()
        {
            if(!S||Event.current.type==EventType.Layout)return;Init();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/900f,1));
            if(S.Playing&&P&&Event.current.type==EventType.Repaint){HUD();if(panel==""&&GameBindings.Held("Roster"))Roster(1120,120);}
            if(panel=="main")Main();
            else if(panel=="host"||panel=="join")Connect();
            else if(panel=="loadout")Loadout();
            else if(panel=="pause")Pause();
            else if(panel=="settings")Settings();else if(panel=="audio")AudioPage();else if(panel=="performance")PerformancePage();
            else if(panel=="map")Map();
            else if(panel=="weather")Weather();
            else if(panel=="result")Result();else if(panel=="lobby")Lobby();else if(panel=="browser")Browser();
            else if(panel=="bindings")BindingsPage();else if(panel=="guide")Guide();else if(panel=="draft")Draft();else if(panel=="session")SessionCard();else if(panel=="character")Character();
            if(S.Connecting)
            {
                Rect(0,0,1600,900,new Color(.04f,.1f,.13f,.94f));Text(110,350,1000,90,"Opening the reserve…",title,56);Text(115,450,800,45,"Establishing your connection. This can take a few seconds.",body);
                if(Button(115,530,280,56,"Cancel connection"))S.CancelConnect();
            }
        }
        void Main()
        {
            GUI.DrawTexture(new Rect(0,0,1150,900),fade);
            Text(96,115,600,140,"Pinefall",title);
            Text(102,242,500,50,"Wildfront",type,34);
            Text(104,315,440,75,"Hunt the wild. Outlast the storm.\nA small-team survival arena.",body);
            if(Button(104,427,370,60,"Host a game",true))panel="host";
            if(Button(104,499,370,60,"Join friends")){panel="browser";if(LanDiscovery.Current)LanDiscovery.Current.Search();}
            if(Button(104,571,370,60,"Explore free roam")){S.BattleMode=false;S.Host();}
            if(Link(104,662,170,"Equipment"))Open("loadout");
            if(Link(290,662,150,"Settings"))Open("settings");if(Link(104,716,370,"How to play"))Open("guide");
            if(Link(104,795,150,"Quit game"))Application.Quit();
            Text(1050,706,450,46,"Six habitats. One closing storm.",type,25);
            Text(1050,756,450,72,"Solo · Duos · Squads  /  Predators & prey\nWeather, wildlife, and rival hunters",small);
            if(S.Message!="")Text(104,715,760,66,S.Message,small,17,new Color(1,.78f,.51f));
            Text(1310,849,220,24,"Wildfront  •  v0.6",small,14);
        }
        void Back(){panel=returnPanel;if(panel=="")Lock();}
        void Heading(string name,string desc)
        {
            Rect(0,0,1600,900,new Color(ink.r,ink.g,ink.b,.95f));
            Text(96,70,1300,95,name,title,64);Text(100,170,1330,65,desc,body);
            if(Link(100,806,250,"Back"))Back();
        }
        void Connect()
        {
            bool host=panel=="host";
            Heading(host?"Open your reserve":"Meet in the meadow",host?"Your computer runs the shared world while you play.":"Enter your friend's address and port to join their reserve.");
            returnPanel="main";
            Text(100,275,480,30,"Your name",small);
            S.PlayerName=Field(100,315,530,60,S.PlayerName,20);
            if(!host){Text(100,410,480,30,"Host address",small);S.Address=Field(100,450,530,60,S.Address,45);}
            else {Text(100,413,530,30,"Friends on your network can use",small);Text(100,453,620,60,ReserveSession.LocalAddress(),number);}
            Text(100,545,320,30,"Port",small);S.Port=Field(100,585,180,58,S.Port,5);
            Text(325,545,340,30,"Preferred team",small);
            for(int t=0;t<(S.TeamSize==4?2:4);t++)if(Button(325+t*78,585,66,58,(t+1).ToString(),S.RequestedTeam==t))S.RequestedTeam=t;
            if(Button(100,692,530,64,host?"Open reserve & enter":"Join reserve",true)){if(host)S.Host();else S.Join();}
            Rect(850,294,2,365,new Color(.4f,.5f,.53f,.4f));
            if(host)
            {
                if(Button(910,260,245,56,"Wildfront",S.BattleMode))S.BattleMode=true;
                if(Button(1170,260,245,56,"Free roam",!S.BattleMode))S.BattleMode=false;
            }
            if(host){for(int mode=0;mode<3;mode++)if(Button(910+mode*165,329,155,40,new[]{"Solo","Duos","Squads"}[mode],S.TeamSize==new[]{1,2,4}[mode]))S.TeamSize=new[]{1,2,4}[mode];if(Button(910,379,490,40,S.PracticeBots?"Practice opponents: on":"Practice opponents: off",S.PracticeBots))S.PracticeBots=!S.PracticeBots;}
            var w=WeaponData.Get(S.SelectedWeapon);Text(910,439,500,52,w.name,number);Text(910,470,500,40,w.cartridge,body);
            Text(910,525,500,90,w.description,small,18);
            if(Link(910,632,300,"Change equipment")){returnPanel=panel;panel="loadout";}
            Text(850,718,620,83,"Across the internet, use a reachable address and forward UDP port "+S.Port+", or connect through a private gaming VPN. The host must keep the game open.",small,17);
            if(S.Message!="")Text(100,765,1320,45,S.Message,small,17,new Color(1,.78f,.51f));
        }
        void LegacyLoadout()
        {
            Heading("Choose your rifle","Each weapon has its own handling, magazine, and effective range. Switch in the field with 1, 2, or 3.");
            int selected=P?P.Weapon.Value:S.SelectedWeapon;
            for(int i=0;i<3;i++)
            {
                float x=100+i*475;var w=WeaponData.All[i];bool active=selected==i;
                Rect(x,280,440,426,new Color(.16f,.25f,.28f,.9f));Rect(x,280,440,3,active?gold:new Color(.3f,.4f,.43f));
                DrawRifle(x+48,355,i,active?gold:muted);
                Text(x+30,435,380,55,w.name,number,37);Text(x+30,494,380,35,w.cartridge,small,18);
                Text(x+30,548,380,85,w.description,small,17);
                Text(x+30,P?625:646,380,30,w.magazine+" rounds   /   "+w.range+" m"+(P?"  /  tuning "+P.UpgradeLevel(i):""),small,17);
                if(P)
                {
                    int level=P.UpgradeLevel(i),cost=level==0?2:4;
                    string label=level>=2?"Fully tuned":P.Craft.Value>=cost?"Upgrade for "+cost+" materials":"Needs "+cost+" materials";
                    if(Button(x+20,663,400,38,label)&&level<2)P.UpgradeServerRpc(i);
                }
                if(Button(x,725,440,56,active?"Equipped":"Equip "+w.name,active))
                {
                    if(P)P.SelectWeapon(i);else S.SelectedWeapon=i;
                    PlayerPrefs.SetInt("weapon",i);PlayerPrefs.Save();
                }
            }
        }
        void DrawRifle(float x,float y,int kind,Color col)
        {
            Rect(x+70,y,240,5,col);Rect(x+60,y-2,110,13,col);Rect(x+20,y+8,180,13,col);Rect(x,y+7,55,27,col);
            Rect(x+90,y+18,12,22,col);
            if(kind==0){Rect(x+105,y-27,75,12,col);Rect(x+122,y-15,7,15,col);Rect(x+165,y-15,7,15,col);}
            if(kind==1){Rect(x+136,y+20,17,33,col);Rect(x+150,y-17,22,15,col);}
            if(kind==2){Rect(x+180,y+5,120,5,col);Rect(x+175,y+8,60,16,col);}
        }
        void Pause()
        {
            GUI.DrawTexture(new Rect(0,0,1250,900),fade);Rect(0,0,1600,900,new Color(0,0,0,.15f));
            Text(100,135,720,100,"A moment in the field",title,58);
            Text(104,247,700,50,S.IsHost?"Your reserve is open. Wildlife and friends keep moving.":"You are connected to your friend's reserve.",body);
            if(Button(104,360,400,60,"Resume hunt",true)){panel="";Lock();}
            if(Button(104,433,400,60,"Equipment"))Open("loadout");
            if(Button(104,506,400,60,"Field map"))Open("map");
            if(Button(104,579,400,60,"Settings"))Open("settings");if(Button(550,433,300,60,"Survival guide"))Open("guide");if(Button(550,506,300,60,"Key bindings"))Open("bindings");
            if(Button(104,652,400,60,"Character & appearance"))Open("character");
            if(S.IsHost&&Link(550,751,350,"Connection details")){panel="session";Unlock();}
            if(Link(104,751,470,S.IsHost?"Close reserve & return to menu":"Leave reserve"))S.Leave();
            Roster(950,210);
            if(S.IsHost&&Ranger.Players.Count<=4){Text(950,610,520,30,"Local address",small);Text(950,650,520,50,ReserveSession.LocalAddress()+":"+S.Port,type,28);}
            if(S.IsHost&&WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value&&WildfrontMatch.Current.Phase.Value!=1)
                if(Button(950,728,470,62,"Open match lobby",true)){if(WildfrontMatch.Current.Phase.Value==2)WildfrontMatch.Current.ReturnLobbyServerRpc();panel="lobby";}
        }
        void Roster(float x,float y)
        {
            Rect(x-24,y-18,480,80+Ranger.Players.Count*52,new Color(.06f,.13f,.16f,.83f));
            Text(x,y,400,40,"Hunters in the reserve",type,24);int i=0;
            foreach(var player in Ranger.Players)
            {
                if(!player)continue;
                string n=player.DisplayName.Value.ToString();if(player.IsOwner)n+=" (you)";
                Text(x,y+65+i*50,300,35,"T"+(player.Team.Value+1)+"  "+n,body,18);Text(x+310,y+65+i*50,150,35,player.Kills.Value+" K / "+player.Score.Value+" H",small,15);i++;
            }
        }
        void Settings()
        {
            Heading("Make yourself comfortable","Settings apply immediately and stay on this computer.");
            if(Button(104,235,285,44,"Sound mix"))Open("audio");if(Button(404,235,290,44,"Performance & frame rate"))Open("performance");Text(104,300,600,40,"Mouse sensitivity",type);Sensitivity=Slider(104,358,590,Sensitivity,.5f,5);Text(740,346,170,45,Sensitivity.ToString("0.0"),type);
            float volume=AudioListener.volume;Text(104,445,600,40,"Sound volume",type);volume=Slider(104,503,590,volume,0,1);AudioListener.volume=volume;Text(740,491,170,45,Mathf.RoundToInt(volume*100)+"%",type);
            Text(104,595,600,40,"Landscape quality",type);
            for(int i=0;i<3;i++)if(Button(104+i*220,656,205,56,new[]{"Light","Balanced","High"}[i],!RuntimePerformance.Automatic&&PlayerPrefs.GetInt("quality",1)==i)){RuntimePerformance.Automatic=false;ApplyQuality(i);RuntimePerformance.ApplyDetail();}
            PlayerPrefs.SetFloat("sensitivity",Sensitivity);PlayerPrefs.SetFloat("volume",volume);
            var voice=ProximityVoice.Current;if(voice){if(Button(900,300,550,56,voice.Enabled?"Microphone enabled":"Enable microphone",voice.Enabled))voice.Enabled=!voice.Enabled;Text(904,373,550,85,"Choose push-to-talk or open mic. Only nearby players hear you. Disable the microphone to stop capture. Headphones reduce speaker feedback.",small,18);Text(904,485,550,35,"Nearby voice volume",small);voice.ReceiveVolume=Slider(904,540,480,voice.ReceiveVolume,0,1);if(Button(904,588,235,46,"Push to talk · "+GameBindings.Label("Talk"),!voice.OpenMic))voice.OpenMic=false;if(Button(1154,588,235,46,"Open mic",voice.OpenMic))voice.OpenMic=true;Text(904,652,490,31,"Open-mic noise gate",small,17);voice.GateThreshold=Slider(904,692,480,voice.GateThreshold,.005f,.06f);}
            var narrator=FieldNarrator.Current;if(narrator){Text(104,745,550,30,"Narrator volume",small,18);narrator.Volume=Slider(104,785,590,narrator.Volume,0,1);if(Button(724,765,147,40,narrator.Captions?"Captions on":"Captions off"))narrator.Captions=!narrator.Captions;}
            if(FieldNarrator.Current&&Button(904,822,545,38,FieldNarrator.Current.Playful?"Occasional commentary: on":"Occasional commentary: off"))FieldNarrator.Current.Playful=!FieldNarrator.Current.Playful;
            if(Button(904,760,265,48,"Key bindings"))Open("bindings");if(Button(1184,760,265,48,"Survival guide"))Open("guide");
        }
        void Weather()
        {
            Heading("Weather & light","Clouds, rain, light, and thunder are shared across the reserve. Weather blends gradually.");
            var weather=ReserveWeather.Instance;if(!weather)return;
            bool canChange=!S.Playing||S.IsHost;
            bool cycle=WeatherState.Current?WeatherState.Current.Cycling.Value:ReserveWeather.AutoCycle;
            Text(104,290,850,40,"Conditions",type,28);
            string[] labels={"Clear skies","Cloud cover","Rain & thunder"};
            for(int i=0;i<3;i++)
            {
                bool clicked=Button(104+i*375,355,350,62,labels[i],weather.Mode==i);
                if(clicked&&canChange)weather.Choose(i,false,weather.Hour);
            }
            Text(104,470,850,40,"Time of day",type,28);
            float[] hours={8,13,16.7f};string[] times={"Morning","Midday","Golden hour"};
            for(int i=0;i<3;i++)
            {
                bool clicked=Button(104+i*375,535,350,62,times[i],Mathf.Abs(weather.Hour-hours[i])<.5f);
                if(clicked&&canChange)weather.Choose(weather.Mode,cycle,hours[i]);
            }
            if(Button(104,660,520,62,cycle?"Automatic weather: on":"Automatic weather: off",cycle)&&canChange)weather.Choose(weather.Mode,!cycle,weather.Hour);
            Text(104,750,1200,40,canChange?"Automatic weather moves through clear skies, cloud cover, and rain about every three minutes.":"The host chooses the weather and light for everyone.",small,19);
        }
        void ApplyQuality(int quality)
        {
            PlayerPrefs.SetInt("quality",quality);QualitySettings.shadowDistance=quality==0?65:quality==1?130:200;
            QualitySettings.shadows=quality==0?ShadowQuality.HardOnly:ShadowQuality.All;QualitySettings.shadowResolution=quality==2?ShadowResolution.High:ShadowResolution.Medium;
            QualitySettings.antiAliasing=quality==0?0:2;QualitySettings.lodBias=quality==0?.65f:1.2f;
            if(ReserveWorld.Instance&&ReserveWorld.Instance.ground){var t=ReserveWorld.Instance.ground;t.detailObjectDistance=quality==0?45:quality==1?85:125;t.detailObjectDensity=quality==0?.35f:quality==1?.6f:.9f;}
        }
        void LegacyMap()
        {
            Heading("Read the landscape","North is up. The reserve is 1,536 metres across. Your outpost sits beside the meadow trail.");
            var world=ReserveWorld.Instance;GUI.DrawTexture(new Rect(100,270,520,520),world.map);
            Rect(100+260-4,270+520*(.5f+160/ReserveWorld.Size)-4,8,8,gold);
            if(P)
            {
                Vector2 p=MapPoint(P.transform.position,100,270,520);Rect(p.x-5,p.y-5,10,10,paper);
                Vector3 ahead=P.transform.position+P.transform.forward*30;var v=MapPoint(ahead,100,270,520);Line(p,v,paper,2);
            }
            Text(700,304,700,60,"Pinefall Reserve",number,43);
            Text(700,395,670,100,"Follow the trail north from the outpost to reach the open meadow. Deer move between the grassland and surrounding pines.",body,22);
            Text(700,550,650,110,"White marker   Your position\nBrass marker   Ranger outpost\nContour lines   8 m elevation",small,20);
            Text(700,705,680,50,"Move quietly. Animals hear running and gunfire.",small,20);
            if(P)Text(700,768,680,44,"This hunt: "+P.Score.Value+" downed  /  "+P.Harvests.Value+" harvests recorded",small,18,gold);
            GUI.BeginGroup(new Rect(100,270,520,520));
            if(WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value)
            {
                MapCircle(WildfrontMatch.Current.Center.Value,WildfrontMatch.Current.Radius.Value,new Color(.77f,.87f,.99f));
                MapCircle(WildfrontMatch.Current.NextCenter.Value,WildfrontMatch.Current.NextRadius.Value,gold);
            }
            GUI.EndGroup();
            Text(121,294,180,30,"Ice desert",small,16,paper);Text(423,297,185,30,"High Rockies",small,16,paper);Text(122,708,180,30,"Dune basin",small,16,paper);Text(430,710,180,30,"Rainforest",small,16,paper);
        }
        void MapCircle(Vector3 center,float radius,Color color)
        {
            var p=MapPoint(center,0,0,520);float r=radius/ReserveWorld.Size*520;
            for(int i=0;i<90;i++){float a=i/90f*Mathf.PI*2,b=(i+1)/90f*Mathf.PI*2;Line(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,color,2);}
        }
        void Result()
        {
            var match=WildfrontMatch.Current;if(!match)return;
            string winner=match.Winner.Value>=0?"Team "+(match.Winner.Value+1)+" outlasted the wild":"The wild claimed everyone";
            Heading(winner,"Wildfront round complete. Everyone returns for the next round.");
            if(P){Text(104,310,900,75,P.Kills.Value+" player eliminations   /   "+P.Score.Value+" hunts",number,37);Text(104,410,850,50,"Hunting rewards helped you build "+P.Craft.Value+" unspent fieldcraft materials.",body);}
            if(S.IsHost){if(Button(104,560,500,64,"Return everyone to lobby",true)){match.ReturnLobbyServerRpc();panel="lobby";}}
            else Text(104,560,900,60,"Waiting for the host to start the next round.",body,25);
            if(Button(104,650,500,60,"Return to menu"))S.Leave();
        }
        Vector2 MapPoint(Vector3 p,float x,float y,float size){return new Vector2(x+size*(p.x/ReserveWorld.Size+.5f),y+size*(.5f-p.z/ReserveWorld.Size));}
        void LegacyHUD()
        {
            if(Time.time<P.DamageFlash){Color hurt=new Color(.7f,.16f,.1f,.36f);Rect(0,0,1600,12,hurt);Rect(0,888,1600,12,hurt);Rect(0,0,12,900,hurt);Rect(1588,0,12,900,hurt);}
            float heading=P.transform.eulerAngles.y;
            Rect(590,25,420,47,new Color(.04f,.1f,.13f,.5f));
            string direction=heading<22.5f||heading>=337.5f?"N":heading<67.5f?"NE":heading<112.5f?"E":heading<157.5f?"SE":heading<202.5f?"S":heading<247.5f?"SW":heading<292.5f?"W":"NW";
            Text(590,34,420,30,direction+"     "+Mathf.RoundToInt(heading).ToString("000")+"°",Centered(type),20);
            if(P.Aiming&&P.Weapon.Value==0)
            {
                float s=760,x=(1600-s)/2,y=(900-s)/2;
                Rect(0,0,x,900,Color.black);Rect(x+s,0,x,900,Color.black);Rect(x,0,s,y,Color.black);Rect(x,y+s,s,y,Color.black);
                GUI.DrawTexture(new Rect(x,y,s,s),scope);
                Rect(799,195,2,510,Color.black);Rect(545,449,510,2,Color.black);
                for(int k=1;k<5;k++){Rect(791,450+k*31,18,2,Color.black);Rect(800+k*35,442,2,16,Color.black);Rect(800-k*35,442,2,16,Color.black);}
            }
            else if(panel=="")
            {
                Color c=P.HitMarker?gold:paper;Rect(788,449,7,2,c);Rect(805,449,7,2,c);Rect(799,438,2,7,c);Rect(799,455,2,7,c);
            }
            if(P.HitMarker){Line(new Vector2(788,438),new Vector2(794,444),gold,2);Line(new Vector2(806,456),new Vector2(812,462),gold,2);Line(new Vector2(788,462),new Vector2(794,456),gold,2);Line(new Vector2(806,444),new Vector2(812,438),gold,2);}
            Text(52,757,600,50,WeaponData.Get(P.Weapon.Value).name,type,29);
            Text(54,808,520,35,WeaponData.Get(P.Weapon.Value).cartridge+"    /    "+(P.Reloading.Value?"Reloading…":P.Ammo.Value+" / "+WeaponData.Get(P.Weapon.Value).magazine),body,19);
            if(P.Stamina<.98f){Rect(54,743,180,3,new Color(.3f,.4f,.4f,.6f));Rect(54,743,180*P.Stamina,3,gold);}
            Text(1170,809,375,34,S.IsHost?"Hosting  /  "+Ranger.Players.Count+" of 8 hunters":"Connected  /  "+Ranger.Players.Count+" hunters",small,18);
            Text(1120,851,440,27,"I  Equipment     M  Map     Esc  Menu",small,16);
            if(P.ActiveNotice!=""&&panel==""){Rect(390,730,820,64,new Color(.04f,.1f,.13f,.7f));Text(411,746,778,45,P.ActiveNotice,Centered(small),17,paper);}
            if(P.FocusAnimal&&panel=="")
            {
                var a=P.FocusAnimal;float d=Vector3.Distance(P.transform.position,a.transform.position);
                string state=a.Dead?(a.Harvested.Value>0?"Harvest recorded":d<4?"E   Record harvest":"Approach to record harvest"):a.State.Value==4?"Defending":a.State.Value==2?"Fleeing":a.Health.Value<a.maxHealth?"Wounded":"";
                Text(610,535,600,35,a.species+"  /  "+Mathf.RoundToInt(d)+" m",small,18,paper);Text(610,574,600,40,state,small,17,gold);
            }
            if(panel==""){Text(52,40,350,40,"Pinefall Reserve",small,18);}
            Rect(54,99,200,6,new Color(.2f,.3f,.31f));Rect(54,99,200*P.Health.Value/100f,6,P.Health.Value<35?new Color(.9f,.4f,.3f):new Color(.6f,.79f,.66f));
            Text(54,116,470,36,"Health "+P.Health.Value+"   /   Team "+(P.Team.Value+1)+"   /   Materials "+P.Craft.Value,small,17,paper);
            var match=WildfrontMatch.Current;
            if(match&&match.Battle.Value)
            {
                string state=match.Phase.Value==0?(S.IsHost?"Warmup. Press Enter to start Wildfront.":"Warmup. Waiting for the host to start."):match.Phase.Value==2?"Round complete":"Storm "+(match.Stage.Value+1)+"  /  "+Mathf.CeilToInt((float)System.Math.Max(0,match.StageEnds.Value-S.Network.ServerTime.Time))+" s  /  "+Mathf.RoundToInt(match.Radius.Value)+" m safe radius";
                Text(420,88,900,40,state,Centered(small),18,gold);
                if(match.Active&&match.Outside(P.transform.position))Text(550,150,500,50,"Inside the storm. Reach the safe zone.",Centered(type),24,new Color(1,.65f,.54f));
            }
            if(!P.Alive)Text(420,600,950,70,"Eliminated. Watching your team until the next round.",type,26,paper);
            var pickup=P.NearestPickup();if(pickup&&P.Alive&&panel=="")Text(550,635,760,50,"E   Collect harvest reward  /  +"+pickup.Healing.Value+" health  /  +"+pickup.Materials.Value+" materials",small,19,gold);
            Text(54,852,700,32,ReserveBiomes.Names[ReserveBiomes.At(P.transform.position.x,P.transform.position.z)]+"    /    U  Upgrade weapon",small,15);
            if(P.view)foreach(var friend in Ranger.Players)
            {
                if(!friend||friend==P||!friend.Alive||friend.Team.Value!=P.Team.Value)continue;
                var point=P.view.WorldToScreenPoint(friend.transform.position+Vector3.up*2);
                if(point.z>0&&point.z<180)Text(point.x/Screen.width*1600-80,(1-point.y/Screen.height)*900,220,35,friend.DisplayName.Value.ToString(),small,15,new Color(.64f,.88f,.81f));
            }
        }
        readonly System.Collections.Generic.Dictionary<GUIStyle,GUIStyle> centeredStyles=new System.Collections.Generic.Dictionary<GUIStyle,GUIStyle>();
        readonly System.Collections.Generic.Dictionary<(GUIStyle,int,Color),GUIStyle> textStyles=new System.Collections.Generic.Dictionary<(GUIStyle,int,Color),GUIStyle>();
        GUIStyle Centered(GUIStyle source){GUIStyle result;if(!centeredStyles.TryGetValue(source,out result)){result=new GUIStyle(source){alignment=TextAnchor.MiddleCenter};centeredStyles.Add(source,result);}return result;}
        public void Text(float x,float y,float w,float h,string text,GUIStyle style,int size=0,Color? color=null)
        {
            var key=(style,size,color??style.normal.textColor);GUIStyle s;if(!textStyles.TryGetValue(key,out s)){s=new GUIStyle(style);if(size>0)s.fontSize=size;s.normal.textColor=key.Item3;textStyles.Add(key,s);}GUI.Label(new Rect(x,y,w,h),text,s);
        }
        public void Rect(float x,float y,float w,float h,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),white);GUI.color=old;}
        bool Button(float x,float y,float w,float h,string label,bool primary=false)
        {
            var r=new Rect(x,y,w,h);bool hover=r.Contains(Event.current.mousePosition);
            Rect(x,y,w,h,primary?(hover?new Color(.92f,.8f,.55f):gold):(hover?new Color(.23f,.36f,.4f,.96f):new Color(.13f,.25f,.29f,.88f)));
            var s=new GUIStyle(button);if(h<46)s.fontSize=Mathf.Clamp(Mathf.FloorToInt(h*.55f),14,23);s.normal.textColor=primary?ink:paper;s.hover.textColor=s.normal.textColor;s.active.textColor=s.normal.textColor;
            return GUI.Button(r,label,s);
        }
        bool Link(float x,float y,float w,string label)
        {
            var r=new Rect(x,y,w,40);bool hover=r.Contains(Event.current.mousePosition);var s=new GUIStyle(small);s.fontSize=18;s.normal.textColor=hover?gold:paper;s.hover.textColor=gold;
            return GUI.Button(r,label,s);
        }
        string Field(float x,float y,float w,float h,string value,int max)
        {
            Rect(x,y,w,h,new Color(.18f,.29f,.33f,.9f));Rect(x,y+h-2,w,2,new Color(.45f,.57f,.58f,.7f));
            return GUI.TextField(new Rect(x,y,w,h),value,max,field);
        }
        float Slider(float x,float y,float w,float val,float min,float max)
        {
            Rect(x,y+10,w,4,new Color(.3f,.43f,.47f));float t=Mathf.InverseLerp(min,max,val);Rect(x,y+10,w*t,4,gold);Rect(x+w*t-7,y,14,24,paper);
            var r=new Rect(x-10,y-12,w+20,50);var ev=Event.current;
            if((ev.type==EventType.MouseDown||ev.type==EventType.MouseDrag)&&r.Contains(ev.mousePosition)&&ev.button==0){val=Mathf.Lerp(min,max,Mathf.Clamp01((ev.mousePosition.x-x)/w));ev.Use();}
            return val;
        }
        void Line(Vector2 a,Vector2 b,Color color,float width)
        {
            // Axis-aligned samples keep IMGUI clipping correct inside a scaled map group.
            int count=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y))/Mathf.Max(.75f,width*.7f)),1,2048);
            for(int i=0;i<=count;i++){Vector2 p=Vector2.Lerp(a,b,i/(float)count);Rect(p.x-width/2,p.y-width/2,width,width,color);}
        }
    }
}
