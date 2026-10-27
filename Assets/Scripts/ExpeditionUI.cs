using System.Linq;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        Texture2D ringShape,dotShape;
        bool inviteShown;float mapZoom=1;Vector2 mapCenter;bool dragging;int draftBid,draftKit,intent=-1;ulong draftOwner=ulong.MaxValue;Vector2 draftScroll;
        readonly Color ally=new Color(.34f,.86f,.77f),prey=new Color(1,.64f,.29f),danger=new Color(.95f,.42f,.32f);
        const float TrackingRange=160;
        void ClosePanel(){panel="";Lock();}
        void OnDestroy(){if(ringShape)Destroy(ringShape);if(dotShape)Destroy(dotShape);if(fade)Destroy(fade);if(scope)Destroy(scope);if(emoteRing)Destroy(emoteRing);if(emoteWedge)Destroy(emoteWedge);if(Instance==this)Instance=null;}
        void LegacySessionCard()
        {
            Rect(0,0,1600,900,new Color(.035f,.08f,.10f,1));Text(96,70,1300,95,"Your reserve is open",title,64);Text(100,170,1330,65,"Share the address below. Friends choose Join friends and enter the address and port in their separate fields.",body);if(!S.GetComponent<SessionAddress>())S.gameObject.AddComponent<SessionAddress>();
            Text(104,280,800,35,"SAME WI-FI / LOCAL NETWORK",small,17,gold);Text(104,333,850,70,ReserveSession.LocalAddress(),number,53);
            Text(104,430,800,35,"INTERNET CONNECTION",small,17,gold);Text(104,480,900,65,SessionAddress.PublicIP,number,38);
            Rect(1080,280,360,245,new Color(.15f,.26f,.28f));Text(1110,309,290,35,"PORT",small,18,gold);Text(1105,366,300,80,S.Port,number,69);Text(1110,466,290,35,"Protocol: UDP",small,18);
            if(Button(104,583,320,56,"Copy local address"))GUIUtility.systemCopyBuffer=ReserveSession.LocalAddress();
            if(Button(440,583,320,56,"Copy internet address"))GUIUtility.systemCopyBuffer=SessionAddress.PublicIP;
            if(Button(1080,583,360,56,"Copy port"))GUIUtility.systemCopyBuffer=S.Port;
            Text(104,671,1290,70,"Internet guests need your internet address and a reachable UDP port. Port forwarding or a gaming VPN may be needed. The game cannot check your router's settings.",small,19);
            if(Button(104,759,510,60,S.BattleMode?"Open match lobby":"Enter the field",true)){if(WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value&&WildfrontMatch.Current.Phase.Value==0){panel="lobby";Unlock();}else ClosePanel();}
            Text(720,774,700,45,"Find these details again in Esc → Connection details.",small,18);
        }
        void OldCharacter()
        {
            Heading("Your field identity","Choose a ranger, clothing tint, and headgear. Your choices are visible to other players.");
            for(int m=0;m<2;m++)if(Button(104+m*360,285,335,65,m==0?"Ranger A":"Ranger B",S.ModelChoice==m))S.ModelChoice=m;
            Text(104,401,1000,35,"Clothing palette",type,25);
            string[] names={"Woodland","Sandstone","Winter","Slate","Rust","Heather"};for(int c=0;c<6;c++)if(Button(104+c*225,466,210,58,names[c],S.ColorChoice==c))S.ColorChoice=c;
            Text(104,586,1000,35,"Headgear",type,25);for(int g=0;g<3;g++)if(Button(104+g*360,648,335,58,new[]{"Uncovered","Field cap","Helmet"}[g],S.GearChoice==g))S.GearChoice=g;
            if(Button(104,739,510,58,"Apply appearance",true)){PlayerPrefs.SetInt("model",S.ModelChoice);PlayerPrefs.SetInt("palette",S.ColorChoice);PlayerPrefs.SetInt("gear",S.GearChoice);if(P)P.AppearanceServerRpc(S.ModelChoice,S.ColorChoice,S.GearChoice);Back();}
        }
        void OldLoadout()
        {
            if(!P){Heading("Field equipment","Eight distinct weapon classes. Battle royale equipment comes from your draft budget and scavenging.");for(int i=0;i<WeaponData.All.Length;i++){int row=i/2,col=i%2;float x=100+col*720,y=270+row*122;var w=WeaponData.All[i];if(Button(x,y,680,52,w.name+" · "+w.cartridge,S.SelectedWeapon==i)){S.SelectedWeapon=i;PlayerPrefs.SetInt("weapon",i);}Text(x+10,y+61,660,47,w.description,small,16);}if(Link(950,804,500,"Customize your ranger")){panel="character";returnPanel="loadout";}return;}
            Heading("Field inventory","I closes inventory · three weapon slots in battle royale · dropped supplies can be picked up by anyone nearby.");
            int rowIndex=0;
            for(int i=0;i<WeaponData.All.Length;i++)if(P.Owns(i))
            {
                float y=254+rowIndex*64;int index=i;Color rarity=WeaponData.RarityColor(P.Rarity(i));Rect(100,y,760,57,new Color(.12f,.22f,.25f));Rect(100,y,4,57,rarity);
                Text(120,y+7,340,45,(P.Weapon.Value==i?"▸ ":"")+WeaponData.Get(i).name,body,22,rarity);Text(440,y+12,165,35,P.MagazineFor(i)+" loaded",small,17);
                if(Button(600,y+6,115,44,"Equip",P.Weapon.Value==i))P.SelectWeapon(index);if(Button(725,y+6,120,44,"Drop"))P.DropItemServerRpc(0,index,1);rowIndex++;
            }
            Text(100,755,760,35,"U tunes the held weapon with fieldcraft materials.  V changes camera.",small,17);
            int[] amounts={P.RifleReserve.Value,P.LightReserve.Value,P.ShellReserve.Value,P.PistolReserve.Value};
            for(int i=0;i<4;i++){float y=254+i*55;Text(925,y+9,345,35,WeaponData.AmmoNames[i]+" ammo  ·  "+amounts[i],body,21);if(Button(1275,y,200,44,"Drop "+Mathf.Min(amounts[i],30)))P.DropItemServerRpc(1,i,30);}
            InventorySupply(483,"Food",P.Food.Value,5,"Eat",()=>P.EatServerRpc());InventorySupply(548,"Medkits",P.Medkits.Value,2,"Use",()=>P.HealServerRpc());InventorySupply(613,"Materials",P.Craft.Value,3,"Tune",()=>P.UpgradeServerRpc(P.Weapon.Value));InventorySupply(678,"Fuel cans",P.FuelCans.Value,4,"",null);
            Text(930,762,560,45,"J Eat food   ·   H Medkit   ·   E Pick up supplies",small,18,gold);
        }
        void InventorySupply(float y,string label,int amount,int kind,string action,System.Action use)
        {Text(925,y+9,280,40,label+"  ·  "+amount,body,22);if(use!=null&&Button(1175,y,120,45,action))use();if(Button(1310,y,165,45,"Drop one"))P.DropItemServerRpc(kind,0,1);}
        void Draft()
        {
            var draft=SpawnDraft.Current;var match=WildfrontMatch.Current;if(!draft||!P||!match)return;bool bidding=match.Phase.Value==3,myTurn=draft.TurnPlayer.Value==P.NetworkObjectId;
            if(draftOwner!=draft.TurnPlayer.Value){draftOwner=draft.TurnPlayer.Value;var mine=draft.ForPlayer(P.NetworkObjectId);draftBid=mine.bid;draftKit=mine.kit;intent=mine.intent;}
            Rect(0,0,1600,900,new Color(.035f,.08f,.10f,.98f));Text(65,28,1400,70,bidding?"The expedition draft":"Choose your landing ground",title,53);
            var current=Ranger.Players.FirstOrDefault(p=>p&&p.NetworkObjectId==draft.TurnPlayer.Value);Text(70,103,1450,40,(myTurn?"YOUR TURN":current?current.DisplayName.Value.ToString()+" is choosing":"Waiting")+"  ·  "+Mathf.CeilToInt((float)System.Math.Max(0,draft.Deadline.Value-S.Network.ServerTime.Time))+" seconds",body,22,gold);
            float savedZoom=mapZoom;mapZoom=1;DrawTactical(new Rect(70,168,590,590),Vector2.zero,RoundArena.Size,true);mapZoom=savedZoom;
            GUI.BeginGroup(new Rect(70,168,590,590));foreach(var e in draft.Entries){if(!e.hasPoint&&!e.hasPreview&&e.region<0&&e.intent<0)continue;Vector3 position=e.hasPoint?e.point:e.hasPreview?e.preview:ExpeditionLayout.Spawn(e.region>=0?e.region:e.intent,0);var q=ProjectMap(position,Vector2.zero,RoundArena.Size,590);var who=Ranger.Players.FirstOrDefault(p=>p&&p.NetworkObjectId==e.player);Color color=e.team==P.Team.Value?ally:gold;if(e.region>=0)CircleAt(q,60/RoundArena.Size*590,color,1);CircleAt(q,9,color,2);Text(q.x+12,q.y-10,185,30,(who?who.DisplayName.Value.ToString():"Hunter")+(e.hasPoint?"":" · planned"),small,15,color);}GUI.EndGroup();
            var area=new Rect(70,168,590,590);var click=Event.current;if(myTurn&&click.type==EventType.MouseDown&&click.button==0&&area.Contains(click.mousePosition)){Vector2 uv=(click.mousePosition-area.position)/590;var point=new Vector3((uv.x-.5f)*RoundArena.Size,0,(.5f-uv.y)*RoundArena.Size);point.x=Mathf.Clamp(point.x,-ReserveWorld.Edge,ReserveWorld.Edge);point.z=Mathf.Clamp(point.z,-ReserveWorld.Edge,ReserveWorld.Edge);if(FreeSpawn.Finite(point)){draft.PreviewPointServerRpc(point);}click.Use();}
            Text(70,785,590,65,"Click anywhere to plan your landing. All decisions are public until deployment. Teammates can land separately.",small,17);
            if(bidding)
            {
                Text(710,170,800,40,"10 tokens per player",number,34);Text(710,220,800,54,"Spend on selection priority or a better kit. Every 2 unused tokens add one spare magazine.",small,18);
                Text(710,285,400,35,"Priority bid: "+draftBid,body,23);if(myTurn){if(Button(1120,277,100,43,"−")){draftBid=Mathf.Max(0,draftBid-1);draft.PreviewServerRpc(draftBid,draftKit,intent);}if(Button(1230,277,100,43,"+")){draftBid=Mathf.Min(10-StartingKit.All[draftKit].cost,draftBid+1);draft.PreviewServerRpc(draftBid,draftKit,intent);}}
                for(int k=0;k<StartingKit.All.Length;k++){var kit=StartingKit.All[k];if(Button(710,339+k*41,800,37,kit.name+"  ·  "+kit.cost+" tokens",draftKit==k)&&myTurn&&draftBid+kit.cost<=10){draftKit=k;draft.PreviewServerRpc(draftBid,draftKit,intent);}}
                Text(710,681,800,37,"Landing preview: "+(draft.ForPlayer(P.NetworkObjectId).hasPreview?"custom map point":intent>=0?ExpeditionLayout.Regions[intent].name:"click anywhere"),small,17,ally);Text(710,716,800,34,StartingKit.All[draftKit].description,small,15);
                if(myTurn&&Button(710,752,800,55,"Commit bid & kit · "+(10-draftBid-StartingKit.All[draftKit].cost)+" tokens left",true))draft.SubmitBidServerRpc(draftBid,draftKit);
            }
            else {Text(710,168,800,70,"Click any point on the map, then confirm your landing. Higher bids choose first.",body,24);var mine=draft.ForPlayer(P.NetworkObjectId);if(myTurn&&mine.hasPreview&&Button(710,775,800,54,"Confirm landing here",true))draft.PickPointServerRpc(mine.preview);}
            if(!bidding&&P.ActiveNotice!="")Text(710,837,800,46,P.ActiveNotice,small,17,danger);
            if(!bidding)
            {draftScroll=GUI.BeginScrollView(new Rect(705,252,820,506),draftScroll,new Rect(0,0,790,Mathf.Max(506,draft.Entries.Count*58)));int row=0;foreach(var e in draft.Entries){var player=Ranger.Players.FirstOrDefault(p=>p&&p.NetworkObjectId==e.player);float y=row*58;Text(5,y,780,55,(row+1)+". "+(player?player.DisplayName.Value.ToString():"Disconnected")+" · Bid "+e.bid+" · "+StartingKit.All[e.kit].name+"\n"+(e.region>=0?(e.hasPoint?"Map point near ":"")+ExpeditionLayout.Regions[e.region].name:"Choosing next…"),small,18,e.team==P.Team.Value?ally:paper);row++;}GUI.EndScrollView();}
            else {float y=827;var list=new System.Collections.Generic.List<DraftEntry>();foreach(var e in draft.Entries)list.Add(e);string entries=string.Join("  /  ",list.Select(e=>{var p=Ranger.Players.FirstOrDefault(q=>q&&q.NetworkObjectId==e.player);return (p?p.DisplayName.Value.ToString():"Hunter")+": "+e.bid+" + "+StartingKit.All[e.kit].cost+(e.locked?" ✓":"");}));Text(710,y,810,58,list.Count<=8?entries:list.Count+" players · "+list.Count(e=>e.locked)+" bids committed",small,16);}
        }
        void OldMap()
        {
            Rect(0,0,1600,900,new Color(.035f,.08f,.10f,1));Text(65,28,1400,75,"Field atlas",title,61);Text(70,110,1450,35,"6.1 km across · scroll to zoom · drag to pan · M to close",small,20);
            var rect=new Rect(70,164,660,660);float span=ReserveWorld.Size/mapZoom;var ev=Event.current;
            if(rect.Contains(ev.mousePosition)&&ev.type==EventType.ScrollWheel){mapZoom=Mathf.Clamp(mapZoom*Mathf.Pow(1.18f,-ev.delta.y),1,8);ev.Use();}
            if(ev.type==EventType.MouseDown&&ev.button==0&&rect.Contains(ev.mousePosition))dragging=true;
            if(ev.type==EventType.MouseUp)dragging=false;
            if(dragging&&ev.type==EventType.MouseDrag){mapCenter+=new Vector2(-ev.delta.x,ev.delta.y)*span/rect.width;ev.Use();}
            float limit=(ReserveWorld.Size-span)/2;mapCenter.x=Mathf.Clamp(mapCenter.x,-limit,limit);mapCenter.y=Mathf.Clamp(mapCenter.y,-limit,limit);
            DrawTactical(rect,mapCenter,span,true);
            Text(800,182,690,60,"Pinefall / Wildfront",number,43);Text(804,264,680,70,"Teal: teammates     White: you\nAmber: nearby living wildlife",body,23);
            Text(804,370,670,90,"Wildlife tracking reaches 120 metres around you. Enemy players are hidden after deployment.",small,21);
            if(P){Text(804,493,680,60,ReserveBiomes.Names[ReserveBiomes.At(P.transform.position.x,P.transform.position.z)],type,29,gold);Text(804,551,680,70,"Nearest site: "+ExpeditionLayout.Regions[ExpeditionLayout.Nearest(P.transform.position)].name,body,22);}
            if(Button(804,668,170,55,"− Zoom out"))mapZoom=Mathf.Max(1,mapZoom/1.5f);if(Button(991,668,170,55,"+ Zoom in"))mapZoom=Mathf.Min(8,mapZoom*1.5f);
            if(Button(1178,668,280,55,"Center on me")){if(P)mapCenter=new Vector2(P.transform.position.x,P.transform.position.z);mapZoom=Mathf.Max(mapZoom,3);}
            if(Button(804,759,654,60,"Return to field",true))ClosePanel();
        }
        Vector2 ProjectMap(Vector3 p,Vector2 center,float span,float size){return new Vector2((p.x-center.x)/span+.5f,.5f-(p.z-center.y)/span)*size;}
        void EnsureMapShapes()
        {
            if(ringShape)return;ringShape=new Texture2D(256,256,TextureFormat.RGBA32,false);dotShape=new Texture2D(64,64,TextureFormat.RGBA32,false);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(127.5f,127.5f));ringShape.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(2-Mathf.Abs(d-125.5f))));}ringShape.Apply();
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));dotShape.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31-d)));}dotShape.Apply();
        }
        void CircleAt(Vector2 p,float r,Color color,float width=1){EnsureMapShapes();Color old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(p.x-r,p.y-r,r*2,r*2),ringShape);GUI.color=old;}
        void Dot(Vector2 p,float size,Color color){EnsureMapShapes();Color old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),dotShape);GUI.color=old;}
        void DrawTactical(Rect rect,Vector2 center,float span,bool labels)
        {
            mapLabels.Clear();GUI.BeginGroup(rect);float size=rect.width;
            Rect(0,0,size,size,new Color(.10f,.19f,.20f));if(ReserveWorld.Instance&&ReserveWorld.Instance.map){float fraction=span/ReserveWorld.Size;var uv=new Rect(.5f+center.x/ReserveWorld.Size-fraction/2,.5f+center.y/ReserveWorld.Size-fraction/2,fraction,fraction);GUI.DrawTextureWithTexCoords(new Rect(0,0,size,size),ReserveWorld.Instance.map,uv);}
            if(labels)for(int grid=0;grid<8;grid++){float low=(grid/8f-.5f)*ReserveWorld.Size,high=((grid+1)/8f-.5f)*ReserveWorld.Size;float left=ProjectMap(new Vector3(low,0,0),center,span,size).x,right=ProjectMap(new Vector3(high,0,0),center,span,size).x;float top=ProjectMap(new Vector3(0,0,-low),center,span,size).y,bottom=ProjectMap(new Vector3(0,0,-high),center,span,size).y;if(left>0&&left<size)Rect(left,0,1,size,new Color(1,1,1,.13f));if(top>0&&top<size)Rect(0,top,size,1,new Color(1,1,1,.13f));if(right>0&&left<size)Text(Mathf.Max(4,left+5),1,45,25,((char)('A'+grid)).ToString(),small,13,paper);if(bottom>0&&top<size)Text(3,Mathf.Max(26,top+26),32,25,(grid+1).ToString(),small,13,paper);}
            for(int i=0;i<ExpeditionLayout.Regions.Length;i++){var region=ExpeditionLayout.Regions[i];var point=ProjectMap(new Vector3(region.center.x,0,region.center.y),center,span,size);if(labels&&(i<12||i==30||i==41||mapZoom>=1.65f))Glyph(point.x-6,point.y-6,12,"house",gold);else Rect(point.x-2,point.y-2,4,4,gold);if(labels&&Vector2.Distance(Event.current.mousePosition,point)<15){Rect(15,size-92,size-30,78,new Color(.05f,.08f,.12f,.96f));Text(28,size-84,size-55,70,region.name+"\n"+region.description,small,16,paper);}}
            if(WildfrontMatch.Current)foreach(var vehicle in WildfrontMatch.Current.TransportPins){var at=ProjectMap(vehicle.position,center,span,size);if(at.x<8||at.y<8||at.x>size-8||at.y>size-8)continue;Color transport=vehicle.condition<=0?muted:vehicle.fuel<=5?gold:new Color(.58f,.83f,.98f);Dot(at,21,new Color(.035f,.065f,.085f,.90f));Glyph(at.x-7,at.y-7,14,vehicle.type==6?"launchpad":vehicle.type==4?"boat":vehicle.type==5?"bike":"car",transport);if(labels&&Vector2.Distance(at,Event.current.mousePosition)<14){Rect(15,size-65,size-30,50,new Color(.04f,.075f,.09f,.97f));Text(28,size-58,size-55,36,vehicle.type==6?"Launchpad / available to every team":RoadVehicle.Names[Mathf.Clamp(vehicle.type,0,5)]+"   Engine "+vehicle.condition+"%   Fuel "+vehicle.fuel+"%",small,18,transport);}}

            if(labels)for(int i=0;i<ExpeditionLayout.Regions.Length;i++){var site=ExpeditionLayout.Regions[i];MapLabel(ProjectMap(new Vector3(site.center.x,0,site.center.y),center,span,size),site.name,size,i);}
            var match=WildfrontMatch.Current;
            if(match&&match.Active){CircleAt(ProjectMap(match.Center.Value,center,span,size),match.Radius.Value/span*size,new Color(.65f,.46f,.95f),2);CircleAt(ProjectMap(match.NextCenter.Value,center,span,size),match.NextRadius.Value/span*size,gold,1.5f);}
            foreach(var drop in SupplyDrop.All)if(drop&&!drop.Opened.Value){var marker=ProjectMap(drop.transform.position,center,span,size);CircleAt(marker,10,new Color(.83f,.55f,1),2);Rect(marker.x-4,marker.y-4,8,8,new Color(.83f,.55f,1));if(labels)MapLabel(marker,drop.Landed?"Supply crate":"Incoming supply",size,20);}
            foreach(var cave in CaveLayout.Sites){var point=ProjectMap(new Vector3(cave.center.x,0,cave.center.y),center,span,size);if(point.x<10||point.y<10||point.x>size-10||point.y>size-10)continue;Glyph(point.x-8,point.y-8,16,"cave",gold);if(labels&&mapZoom>=1.65f)MapLabel(point,cave.name,size,48);}

            if(labels)for(int i=0;i<ReserveWater.Lakes.Length;i++){var lake=ReserveWater.Lakes[i];var at=ProjectMap(new Vector3(lake.center.x,0,lake.center.y),center,span,size);MapLabel(at,lake.name,size,0,true);}
            if(P&&P.SurveyVisible)foreach(var enemy in P.SurveyPoints){var ping=ProjectMap(enemy,center,span,size);CircleAt(ping,8,danger,2);Dot(ping,4,danger);}
            if(P)
            {
                if(match&&match.Active&&P.OrderStep.Value==0){var site=ExpeditionLayout.Regions[P.OrderRegion.Value].center;var goal=ProjectMap(new Vector3(site.x,0,site.y),center,span,size);CircleAt(goal,12,gold,2);if(labels)Text(goal.x+17,goal.y+8,200,30,"Field order",small,14,gold);}
                var local=ProjectMap(P.transform.position,center,span,size);CircleAt(local,TrackingRange/span*size,new Color(1,.68f,.32f,.65f),1);
                foreach(var a in S.Animals)if(a&&!a.Dead&&(a.transform.position-P.transform.position).sqrMagnitude<=TrackingRange*TrackingRange)Dot(ProjectMap(a.transform.position,center,span,size),labels?6:5,prey);
                // Deliberately no enemy-player branch: live maps expose teammates only.
                foreach(var player in Ranger.Players)if(player&&player!=P&&player.Team.Value==P.Team.Value&&player.Alive){var point=ProjectMap(player.transform.position,center,span,size);Dot(point,8,ally);if(labels)Text(point.x+9,point.y-15,190,30,player.DisplayName.Value.ToString(),small,14,ally);}
                Dot(local,9,paper);Line(local,ProjectMap(P.transform.position+P.transform.forward*(span/16),center,span,size),paper,2);
            }
            GUI.EndGroup();
        }
        void Bar(float x,float y,float width,float amount,Color color){Rect(x,y,width,6,new Color(.25f,.31f,.30f,.85f));Rect(x,y,width*Mathf.Clamp01(amount),6,color);}
        string Clock(double t){int seconds=Mathf.Max(0,Mathf.CeilToInt((float)t));return (seconds/60)+":"+(seconds%60).ToString("00");}
        void OldHUD()
        {
            if(panel!=""&&panel!="pause")return;
            if(Time.time<P.DamageFlash){Rect(0,0,1600,10,new Color(.8f,.16f,.1f,.55f));Rect(0,890,1600,10,new Color(.8f,.16f,.1f,.55f));}
            Rect(32,28,330,179,new Color(.035f,.09f,.11f,.82f));Text(50,39,290,30,"WILDFRONT  /  "+(P.Alive?"IN THE FIELD":"SPECTATING"),small,15,gold);
            Text(50,80,290,29,"HEALTH   "+P.Health.Value,small,16,paper);Bar(50,115,290,P.Health.Value/100f,P.Health.Value<30?danger:ally);
            Text(50,133,290,28,"HUNGER   "+Mathf.CeilToInt(P.Hunger.Value)+(P.Hunger.Value<20?" · SPRINT DISABLED":""),small,15,P.Hunger.Value<20?danger:paper);Bar(50,164,290,P.Hunger.Value/100f,prey);Bar(50,186,290,P.Stamina,gold);
            float heading=P.transform.eulerAngles.y;string[] directions={"N","NE","E","SE","S","SW","W","NW"};Text(500,25,600,40,directions[Mathf.RoundToInt(heading/45)%8]+"   "+Mathf.RoundToInt(heading).ToString("000")+"°",Centered(type),23);
            var match=WildfrontMatch.Current;
            if(match&&match.Battle.Value)
            {
                string status=match.Phase.Value==0?(S.IsHost?"WARMUP · ENTER TO START THE DRAFT":"WARMUP · WAITING FOR HOST"):match.Phase.Value==2?"ROUND COMPLETE":S.Network.ServerTime.Time<match.ShrinkAt.Value?"NEXT STORM IN  "+Clock(match.ShrinkAt.Value-S.Network.ServerTime.Time):"STORM CLOSING  "+Clock(match.StageEnds.Value-S.Network.ServerTime.Time);
                Rect(493,77,614,47,new Color(.045f,.1f,.15f,.85f));Text(501,84,598,33,status,Centered(small),19,gold);
                int alive=Ranger.Players.Count(p=>p&&p.Alive);Text(1200,37,356,35,alive+" HUNTERS LEFT   ·   "+P.Kills.Value+" ELIMS",small,19,paper);
                if(match.Active&&match.Outside(P.transform.position))Text(475,148,650,40,"STORM DAMAGE · MOVE TO THE SAFE ZONE",Centered(type),22,danger);
            }
            if(panel=="")
            {
                bool scopeActive=P.Aiming&&(P.Weapon.Value==0||P.Weapon.Value==5);
                if(scopeActive){float s=760,x=420,y=70;Rect(0,0,x,900,Color.black);Rect(x+s,0,x,900,Color.black);Rect(x,0,s,y,Color.black);Rect(x,y+s,s,y,Color.black);GUI.DrawTexture(new Rect(x,y,s,s),scope);Rect(799,190,2,520,Color.black);Rect(540,449,520,2,Color.black);for(int k=1;k<5;k++)Rect(790,450+k*32,20,2,Color.black);}
                else {Color c=P.HitMarker?gold:paper;Rect(788,449,7,2,c);Rect(805,449,7,2,c);Rect(799,438,2,7,c);Rect(799,455,2,7,c);}
                if(P.HitMarker){Line(new Vector2(787,437),new Vector2(795,445),gold,2);Line(new Vector2(805,455),new Vector2(813,463),gold,2);Line(new Vector2(787,463),new Vector2(795,455),gold,2);Line(new Vector2(805,445),new Vector2(813,437),gold,2);}
                if(!scopeActive){DrawTactical(new Rect(1318,103,238,238),new Vector2(P.transform.position.x,P.transform.position.z),360,false);Text(1318,349,260,30,"M ATLAS  ·  AMBER: WILDLIFE",small,13,prey);}
                string interaction=P.Interaction();if(interaction!=""){Rect(370,628,860,44,new Color(.035f,.09f,.11f,.88f));Text(385,636,830,35,interaction,Centered(small),18,gold);}
            }
            var w=WeaponData.Get(P.Weapon.Value);Rect(32,746,470,126,new Color(.035f,.09f,.11f,.85f));Color rarity=WeaponData.RarityColor(P.Rarity(P.Weapon.Value));Rect(32,746,4,126,rarity);Text(54,756,420,40,w.name,type,27,rarity);
            Text(54,802,230,51,P.Reloading.Value?"RELOADING":P.Ammo.Value.ToString("00")+" / "+P.ReserveFor(w.ammoType),number,35);Text(288,813,200,35,w.cartridge,small,15);
            Rect(530,789,670,83,new Color(.035f,.09f,.11f,.78f));Text(548,799,630,31,ReserveBiomes.Names[ReserveBiomes.At(P.transform.position.x,P.transform.position.z)]+" · "+ExpeditionLayout.Regions[ExpeditionLayout.Nearest(P.transform.position)].name,small,19,paper);
            string objective=P.Hunger.Value<25?"Hunt wildlife or find food · J to eat":match&&match.Active?P.FieldOrder:"Explore, gather supplies, and prepare your team";Text(548,838,630,28,objective,small,16,gold);
            Text(1220,770,350,34,"Food "+P.Food.Value+"   Med "+P.Medkits.Value+"   Tools "+P.Craft.Value,small,17);Text(1220,812,350,60,"I Inventory · V Camera\nJ Eat · H Heal · R Reload · Esc Menu",small,16,paper);
            if(P.ActiveNotice!=""&&panel==""){Rect(365,687,870,47,new Color(.035f,.09f,.11f,.88f));Text(385,698,830,35,P.ActiveNotice,Centered(small),18,paper);}
            int row=0;foreach(var friend in Ranger.Players.Where(p=>p&&p!=P&&p.Team.Value==P.Team.Value))
            {Text(50,238+row*49,310,35,(friend.Alive?"● ":"× ")+friend.DisplayName.Value.ToString()+"  "+friend.Health.Value,small,18,friend.Alive?ally:muted);row++;
                if(P.view&&friend.Alive){var point=P.view.WorldToScreenPoint(friend.transform.position+Vector3.up*2.05f);if(point.z>0&&point.z<300)Text(point.x/Screen.width*1600-100,(1-point.y/Screen.height)*900,250,42,friend.DisplayName.Value.ToString()+" · "+Mathf.RoundToInt(point.z)+" m",small,16,ally);}}
            if(P.FocusAnimal&&panel==""&&P.Alive){var a=P.FocusAnimal;Text(620,515,470,35,a.species+" · "+Mathf.RoundToInt(Vector3.Distance(P.transform.position,a.transform.position))+" m",small,18,paper);Text(620,553,470,35,a.Dead?"Harvest down · collect food nearby":a.State.Value==4?"Defending territory":a.State.Value==2?"Fleeing":"Wildlife",small,16,prey);}
            if(!P.Alive)Text(435,570,850,60,"Eliminated · watching your teammates",type,28,paper);
            var voice=ProximityVoice.Current;if(voice)Text(53,218+row*49,430,29,voice.Status,small,14,voice.Transmitting?ally:muted);
            var car=P.CurrentVehicle;if(car){Rect(405,555,790,55,new Color(.035f,.09f,.11f,.9f));Text(425,568,750,40,Mathf.RoundToInt(Mathf.Abs(car.Speed.Value)*3.6f)+" km/h · Engine "+car.Condition.Value+"% · Fuel "+Mathf.RoundToInt(car.Fuel.Value)+"% · Boost "+Mathf.RoundToInt(car.Charge.Value)+"%",Centered(small),21,gold);}
        }
    }
}
