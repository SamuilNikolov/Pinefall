using System.Linq;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        Vector2 frontRosterScroll;
        public bool FrontPanelVisible {get{return panel=="lobby"||panel=="session"||panel=="host"||panel=="join"||panel=="browser";}}
        readonly Color frontBase=new Color(.055f,.09f,.13f,.93f),frontLine=new Color(.66f,.79f,.80f,.20f);
        void FrontHeader(string active)
        {
            Rect(0,0,1600,104,frontBase);Text(46,20,320,58,"Pinefall",title,42,paper);Text(238,49,140,25,"Wildfront",small,16,gold);
            string[] tabs={"Play","Outfit","Handbook","Settings"};
            for(int i=0;i<tabs.Length;i++)
            {
                float x=500+i*198;bool selected=active==tabs[i];
                if(selected)Rect(x,101,174,3,gold);
                if(GUI.Button(new Rect(x,29,174,53),tabs[i],button)&&!selected)
                {
                    if(i==0){panel=S.Playing?"lobby":"main";}
                    else {returnPanel=panel;panel=i==1?"character":i==2?"guide":"settings";}
                }
            }
            Text(1340,34,220,37,S.Playing?"Reserve online":"Living Wildfront 0.9",small,16,ally);
        }
        void FrontHero(Rect bounds)
        {
            if(!outfit)outfit=gameObject.AddComponent<CharacterPreview>();
            if(Event.current.type==EventType.Repaint){outfit.Yaw=158;outfit.Walking=false;outfit.Show(S.ModelChoice,S.ColorChoice);}
            if(outfit.Image){var saved=GUI.color;GUI.color=Color.white;GUI.DrawTexture(bounds,outfit.Image,ScaleMode.ScaleToFit,true);GUI.color=saved;}
        }
        void FrontPanel(Rect rect){Rect(rect.x,rect.y,rect.width,rect.height,frontBase);Rect(rect.x,rect.y,rect.width,1,frontLine);}
        bool FrontAction(float x,float y,float width,string label,bool primary=false,bool enabled=true)
        {
            Rect(x,y,width,64,enabled?(primary?gold:new Color(.14f,.22f,.28f,.94f)):new Color(.12f,.16f,.19f,.8f));
            var style=new GUIStyle(button){alignment=TextAnchor.MiddleCenter,fontSize=23,fontStyle=FontStyle.Bold};style.normal.textColor=primary&&enabled?ink:enabled?paper:muted;
            style.hover.textColor=style.normal.textColor;style.active.textColor=style.normal.textColor;bool saved=GUI.enabled;GUI.enabled=enabled;bool pressed=GUI.Button(new Rect(x,y,width,64),label,style);GUI.enabled=saved;return pressed;
        }
        string FrontArena(int players,bool bots)
        {
            int count=bots?Mathf.Max(8,players):Mathf.Max(1,players);float size=RoundArena.SizeFor(RoundArena.TierFor(count));
            return size<3000?"Small reserve / 2 km":size<5000?"Medium reserve / 4 km":"Full reserve / 6.1 km";
        }
        void Main()
        {
            GUI.DrawTexture(new Rect(0,104,700,796),fade);FrontHeader("Play");
            Text(72,166,430,96,"Outlast",title,68,paper);Text(72,256,430,96,"the wild.",title,68,paper);
            Text(77,372,355,108,"Track wildlife. Build your cover.\nBring your friends back alive.",body,24,paper);
            FrontHero(new Rect(420,150,670,668));
            FrontPanel(new Rect(1120,162,408,620));Text(1148,189,350,45,"Choose your expedition",type,26);
            string[] modes={"Solo","Duos","Squads","Free roam"};int[] sizes={1,2,4};
            for(int i=0;i<4;i++)
            {
                bool selected=i==3?!S.BattleMode:S.BattleMode&&S.TeamSize==sizes[i];
                if(Button(1147,252+i*54,354,46,modes[i],selected)){S.BattleMode=i!=3;if(i<3)S.TeamSize=sizes[i];}
            }
            Text(1149,484,348,73,S.BattleMode?"The storm closes. Your team survives.\nUp to 20 players.":"The whole reserve, on your own time.\nExplore, hunt and build together.",small,18,muted);
            if(FrontAction(1148,581,352,"Host a reserve",true))panel="host";
            if(FrontAction(1148,663,352,"Join friends")){panel="browser";if(LanDiscovery.Current)LanDiscovery.Current.Search();}
            Text(1149,747,348,28,"Your computer hosts the session.",small,15,muted);
            FrontPanel(new Rect(74,584,348,155));Glyph(95,604,25,"cave",ice);Text(138,602,250,30,"Below the surface",type,22);
            Text(95,649,292,65,"Crystal caves, hidden caches and new routes through the reserve.",small,18,muted);
            if(Link(78,772,330,"Read the field handbook")){guideTab=3;returnPanel="main";panel="guide";}
            Rect(0,850,1600,50,frontBase);Text(55,862,1100,28,S.Message!=""?S.Message:"Solo, Duos and Squads / Building / Wildlife / Proximity voice",small,16,S.Message!=""?gold:muted);
            if(Link(1410,861,140,"Quit game"))Application.Quit();
        }
        void Connect()
        {
            bool host=panel=="host";FrontHeader("Play");FrontPanel(new Rect(58,145,1484,670));returnPanel="main";
            Text(90,174,900,66,host?"Create your reserve":"Join your friends",title,48);Text(94,248,950,40,host?"Choose the rules. Invite friends once your reserve opens.":"Use the address and port shown on your friend's session card.",body,21,muted);
            Text(95,333,550,28,"Your name",small,18);S.PlayerName=Field(95,373,572,57,S.PlayerName,20);
            if(host)
            {
                Text(95,469,580,28,"Game port",small,18);S.Port=Field(95,509,190,57,S.Port,5);Text(307,516,360,53,"UDP / default 7777",small,19,muted);
                Text(95,602,570,82,"After opening, share your internet address and port. Internet play needs router forwarding or a gaming VPN.",small,20,muted);
            }
            else
            {
                Text(95,457,570,28,"Host address",small,18);S.Address=Field(95,497,572,57,S.Address,70);
                Text(95,579,570,28,"Port",small,18);S.Port=Field(95,619,190,57,S.Port,5);Text(308,627,350,45,"Use the host's UDP port.",small,19,muted);
            }
            Rect(729,324,1,360,frontLine);
            if(host)
            {
                Text(785,327,650,36,"Match rules",type,26);
                if(Button(785,386,320,48,"Wildfront battle royale",S.BattleMode))S.BattleMode=true;
                if(Button(1120,386,320,48,"Free roam",!S.BattleMode))S.BattleMode=false;
                for(int i=0;i<3;i++){int size=new[]{1,2,4}[i];if(Button(785+i*221,455,208,49,new[]{"Solo","Duos","Squads"}[i],S.TeamSize==size))S.TeamSize=size;}
                if(Button(785,526,655,47,S.PracticeBots?"Practice opponents enabled":"Friends only / no practice opponents",S.PracticeBots))S.PracticeBots=!S.PracticeBots;
                Text(790,604,635,78,S.BattleMode?FrontArena(1,S.PracticeBots)+" initially\nThe arena adjusts to the lobby at draft start.":"Full reserve / 6.1 km\nNo closing storm. Invite up to twenty players.",small,20,muted);
            }
            else
            {
                Glyph(793,342,33,"team",ice);Text(848,332,550,43,"Everyone uses the same build",type,27);
                Text(790,407,625,113,"Use the internet address when playing from another location. The host keeps the game open and shares a reachable UDP port.",body,23,paper);
                Text(790,551,625,90,"You can choose your team and mark ready after joining. If a round is already running, you'll spectate until the next draft.",small,21,muted);
            }
            if(FrontAction(95,720,572,host?"Open reserve":"Connect to reserve",true)){if(host)S.Host();else S.Join();}
            if(Link(790,736,500,"Back to Play"))panel="main";
            if(S.Message!="")Text(95,831,1400,50,S.Message,small,19,gold);
        }
        void Lobby()
        {
            var m=WildfrontMatch.Current;if(!m||!P)return;Rect(0,104,1600,796,new Color(.04f,.07f,.10f,.70f));FrontHeader("Play");
            var players=Ranger.Players.Where(p=>p&&!p.IsBot.Value).OrderBy(p=>p.Team.Value).ThenBy(p=>p.NetworkObjectId).ToArray();
            FrontPanel(new Rect(50,153,382,611));Text(76,178,330,40,"Reserve roster",type,27);Text(78,229,325,31,players.Length+" / 20 players connected",small,18,muted);
            frontRosterScroll=GUI.BeginScrollView(new Rect(70,283,342,451),frontRosterScroll,new Rect(0,0,320,Mathf.Max(451,players.Length*67)));
            for(int i=0;i<players.Length;i++)
            {
                var player=players[i];float y=i*67;bool mate=player.Team.Value==P.Team.Value;Rect(0,y,320,58,new Color(.13f,.21f,.25f,.94f));Rect(0,y,3,58,mate?ally:frontLine);
                Text(13,y+5,210,30,player.DisplayName.Value.ToString()+(player==P?" (you)":""),small,19,paper);
                Text(14,y+34,200,23,m.TeamSize.Value==1?"Solo ranger":"Team "+(player.Team.Value+1),small,14,muted);Text(224,y+20,90,25,player.Ready.Value?"Ready":"Choosing",small,15,player.Ready.Value?ally:gold);
            }
            GUI.EndScrollView();
            Text(510,141,530,45,P.DisplayName.Value.ToString(),type,29,paper);Text(514,192,480,30,P.Ready.Value?"Ready for the draft":"Choose your team, then ready up",small,18,P.Ready.Value?ally:gold);FrontHero(new Rect(448,220,610,565));
            FrontPanel(new Rect(1090,153,458,627));Text(1118,179,400,43,m.TeamSize.Value==1?"Solo":m.TeamSize.Value==2?"Duos":"Squads",title,36);
            Text(1120,241,400,68,FrontArena(players.Length,m.BotsEnabled.Value)+"\n"+(m.BotsEnabled.Value?"Practice fills to eight participants.":"Friends only. Seven storm cycles."),small,19,muted);
            if(S.IsHost)
            {
                for(int i=0;i<3;i++){int size=new[]{1,2,4}[i];if(Button(1118+i*135,334,125,43,new[]{"Solo","Duos","Squads"}[i],m.TeamSize.Value==size))m.RulesServerRpc(size,m.BotsEnabled.Value);}
                if(Button(1118,391,400,43,m.BotsEnabled.Value?"Practice opponents: on":"Practice opponents: off",m.BotsEnabled.Value))m.RulesServerRpc(m.TeamSize.Value,!m.BotsEnabled.Value);
            }
            else Text(1120,341,395,66,"The host chooses match rules.\nYour team and ready state are yours.",small,18,muted);
            if(m.TeamSize.Value>1)
            {
                Text(1120,460,390,28,"Choose your team",small,18);
                for(int team=0;team<20/m.TeamSize.Value;team++)if(Button(1118+(team%5)*80,504+(team/5)*44,73,37,(team+1).ToString(),P.Team.Value==team))P.ChangeTeamServerRpc(team);
            }
            if(FrontAction(1118,620,400,P.Ready.Value?"Ready / click to unready":"Ready for the draft",!P.Ready.Value))P.ReadyServerRpc(!P.Ready.Value);
            if(S.IsHost)
            {string label=m.CountdownEnds.Value>0?"Starting in "+Mathf.CeilToInt((float)(m.CountdownEnds.Value-S.Network.ServerTime.Time))+"…":"Start the draft";if(FrontAction(1118,700,400,label,true,m.EveryoneReady))m.RequestStartServerRpc();}
            else Text(1120,716,395,35,"The host starts when everyone is ready.",small,17,muted);
            Text(478,778,580,49,m.StartReason,small,18,gold);
            Rect(0,840,1600,60,frontBase);if(Link(53,856,290,"Connection details"))panel="session";if(Link(394,856,280,"Explore warmup"))ClosePanel();if(Link(1300,856,250,"Leave reserve"))S.Leave();
        }
        void Browser()
        {
            FrontHeader("Play");FrontPanel(new Rect(60,151,1480,655));returnPanel="main";Text(90,177,1300,66,"Find your friends",title,48);Text(95,260,1300,46,"Nearby reserves appear here. Use an address to join over the internet.",body,22,muted);
            int row=0;var discovery=LanDiscovery.Current;
            if(discovery)foreach(var game in discovery.Sessions.Take(4))
            {
                float y=353+row*80;Rect(95,y,1410,68,new Color(.13f,.21f,.25f,.95f));Text(116,y+10,470,45,game.name,type,24);Text(610,y+18,560,34,game.players+" / 20 players  /  "+game.phase+"  /  "+(game.teamSize==1?"Solo":game.teamSize==2?"Duos":"Squads"),small,19,muted);
                if(Button(1250,y+9,235,49,"Choose reserve")){S.Address=game.address;S.Port=game.port.ToString();panel="join";}row++;
            }
            if(row==0){Glyph(100,367,38,"team",ice);Text(167,350,1175,52,"No nearby reserves found yet",type,29);Text(169,415,1070,80,"Ask your friend to host, then refresh. For an internet session, enter the address and port they share.",body,23,muted);}
            if(discovery)Text(99,680,1300,32,discovery.Status,small,17,muted);
            if(FrontAction(95,719,380,"Refresh nearby")){if(discovery)discovery.Search();}if(FrontAction(499,719,615,"Enter address and port",true))panel="join";if(Link(1200,736,290,"Back to Play"))panel="main";
        }
        void SessionCard()
        {
            if(!S.GetComponent<SessionAddress>())S.gameObject.AddComponent<SessionAddress>();FrontHeader("Play");FrontPanel(new Rect(60,151,1480,661));
            Text(91,179,1360,65,"Your reserve is open",title,48);Text(97,261,1330,50,"Friends choose Join friends, then enter the address and port in their separate fields.",body,22,muted);
            FrontPanel(new Rect(95,349,658,274));FrontPanel(new Rect(781,349,724,274));
            Text(122,376,590,33,"Same Wi-Fi or local network",type,25);Text(808,376,660,33,"Internet / another location",type,25);
            Text(120,437,600,60,ReserveSession.LocalAddress(),number,42,paper);Text(806,437,670,60,SessionAddress.PublicIP,number,38,paper);
            if(Button(122,536,603,51,"Copy local address"))GUIUtility.systemCopyBuffer=ReserveSession.LocalAddress();if(Button(808,536,670,51,"Copy internet address"))GUIUtility.systemCopyBuffer=SessionAddress.PublicIP;
            Text(98,653,140,30,"UDP port",small,19,muted);Text(238,638,250,62,S.Port,number,40,gold);if(Button(494,640,230,50,"Copy port"))GUIUtility.systemCopyBuffer=S.Port;
            Text(809,643,651,62,"Internet play needs a reachable forwarded UDP port or a gaming VPN. The public address alone does not verify reachability.",small,18,muted);
            if(FrontAction(97,731,626,S.BattleMode?"Continue to match lobby":"Enter the reserve",true)){if(S.BattleMode)panel="lobby";else ClosePanel();}
            Text(809,741,650,41,"Use the same 0.9 build on every computer.",small,19,paper);
        }
    }
}
