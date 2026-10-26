using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        CharacterPreview outfit;readonly Queue<string> events=new Queue<string>();float eventUntil;int inventoryKind,inventoryItem;bool lobbyEntered;int dragSlot=-1;bool draggingWeapon;
        public void AddEvent(string message){events.Enqueue(message);while(events.Count>3)events.Dequeue();eventUntil=Time.time+7;}
        void Lobby()
        {
            var m=WildfrontMatch.Current;if(!m||!P)return;
            Rect(0,0,1600,900,ink);Text(75,44,1400,85,S.PlayerName+"'s expedition",title,58);Text(80,140,1400,42,"Choose your team and outfit, then mark yourself ready. Everyone sees the draft once the host starts.",body,21);
            int row=0;foreach(var p in Ranger.Players.Where(p=>p&&!p.IsBot.Value).OrderBy(p=>p.Team.Value).ThenBy(p=>p.NetworkObjectId))
            {float y=230+row*49;Rect(80,y,870,43,new Color(.12f,.22f,.25f));Text(102,y+7,420,32,p.DisplayName.Value.ToString()+(p==P?" (you)":""),body,21);Text(540,y+8,160,30,"Team "+(p.Team.Value+1),small,19);Text(725,y+8,210,30,p.Ready.Value?"Ready":"Choosing…",small,19,p.Ready.Value?ally:gold);row++;}
            Text(1020,239,510,45,m.TeamSize.Value==1?"Solo":m.TeamSize.Value==2?"Duos":"Squads",number,40);
            Text(1022,303,470,65,(m.BotsEnabled.Value?"Practice opponents fill empty slots.":"Friends only. Up to eight players.")+"\n6.1 km reserve · seven storm cycles",small,20);
            if(m.TeamSize.Value>1){Text(1022,409,470,32,"Your team",small,20);for(int t=0;t<8/m.TeamSize.Value;t++)if(Button(1020+(t%2)*225,460+(t/2)*54,210,46,"Team "+(t+1),P.Team.Value==t))P.ChangeTeamServerRpc(t);}
            if(S.IsHost)
            {Text(80,820,600,32,"Host settings",small,16);for(int i=0;i<3;i++){int size=new[]{1,2,4}[i];if(Button(235+i*155,811,140,42,new[]{"Solo","Duos","Squads"}[i],m.TeamSize.Value==size))m.RulesServerRpc(size,m.BotsEnabled.Value);}if(Button(720,811,300,42,m.BotsEnabled.Value?"Practice bots: on":"Practice bots: off"))m.RulesServerRpc(m.TeamSize.Value,!m.BotsEnabled.Value);}
            if(Button(80,710,350,62,P.Ready.Value?"Unready":"Ready for the draft",!P.Ready.Value))P.ReadyServerRpc(!P.Ready.Value);
            if(Button(450,710,270,62,"Preview outfit")){returnPanel="lobby";panel="character";}
            if(Button(740,710,260,62,"Connection details"))panel="session";
            if(S.IsHost&&Button(1050,710,450,62,m.CountdownEnds.Value>0?"Starting in "+Mathf.CeilToInt((float)(m.CountdownEnds.Value-S.Network.ServerTime.Time))+"…":"Start the draft",m.EveryoneReady))m.RequestStartServerRpc();
            Text(82,646,1400,45,m.StartReason,body,21,gold);if(Link(1055,811,220,"Explore warmup"))ClosePanel();if(Link(1300,811,210,"Leave reserve"))S.Leave();
        }
        void Browser()
        {
            Heading("Join a reserve","Nearby sessions appear here. For a friend on the internet, enter the address and port they share with you.");returnPanel="main";
            var discovery=LanDiscovery.Current;int row=0;
            if(discovery)foreach(var game in discovery.Sessions.Take(5))
            {float y=270+row*84;Rect(100,y,1330,70,new Color(.13f,.23f,.26f));Text(123,y+9,460,50,game.name,type,25);Text(600,y+20,530,37,game.players+" / 8 · "+game.phase+" · "+(game.teamSize==1?"Solo":game.teamSize==2?"Duos":"Squads"),small,20);if(Button(1190,y+8,220,54,"Choose session")){S.Address=game.address;S.Port=game.port.ToString();panel="join";}row++;}
            if(row==0)Text(105,310,1250,90,"No local reserves found yet. Ask your friend to open their reserve, or use their address below.",body,27);
            if(discovery)Text(105,699,1250,40,discovery.Status,small,18);
            if(Button(105,748,390,56,"Refresh nearby sessions")){if(discovery)discovery.Search();}if(Button(520,748,580,56,"Enter a friend's address and port",true))panel="join";
        }
        void Character()
        {
            Rect(0,0,1600,900,ink);Text(75,40,1400,80,"Your ranger",title,60);Text(80,140,1400,44,"Preview the actual outfit before applying it. Drag the preview to turn your ranger.",body,22);
            if(!outfit)outfit=gameObject.AddComponent<CharacterPreview>();outfit.Show(S.ModelChoice,S.ColorChoice);GUI.DrawTexture(new Rect(850,210,610,620),outfit.Image,ScaleMode.ScaleToFit);
            var ev=Event.current;if(ev.type==EventType.MouseDrag&&new Rect(850,210,610,620).Contains(ev.mousePosition)){outfit.Yaw-=ev.delta.x;ev.Use();}
            Text(80,232,650,35,"Ranger outfit",type,26);for(int i=0;i<2;i++)if(Button(80+i*340,294,320,62,i==0?"Trail ranger":"Forest ranger",S.ModelChoice==i))S.ModelChoice=i;
            Text(80,403,660,35,"Clothing tint",type,26);string[] labels={"Woodland","Sandstone","Winter","Slate","Rust","Heather"};for(int i=0;i<6;i++)if(Button(80+(i%2)*340,465+(i/2)*69,320,57,labels[i],S.ColorChoice==i))S.ColorChoice=i;
            Text(82,695,670,60,"Your chosen outfit is visible to other players. Press V in the field to switch between first and third person.",small,19);
            if(Button(80,790,450,60,"Apply outfit",true)){S.GearChoice=0;PlayerPrefs.SetInt("model",S.ModelChoice);PlayerPrefs.SetInt("palette",S.ColorChoice);PlayerPrefs.SetInt("gear",0);if(P)P.AppearanceServerRpc(S.ModelChoice,S.ColorChoice,0);outfit.Hide();Back();}
            if(Button(550,790,200,60,"Back")){outfit.Hide();Back();}
            if(Button(865,777,170,44,"Turn left"))outfit.Yaw-=35;if(Button(1050,777,170,44,"Turn right"))outfit.Yaw+=35;if(Button(1235,777,210,44,outfit.Walking?"Standing pose":"Walking pose"))outfit.Walking=!outfit.Walking;
        }
        void Slot(Rect r,string titleText,string count,int kind,int item,Color color,bool occupied=true)
        {
            bool selected=inventoryKind==kind&&inventoryItem==item;Rect(r.x,r.y,r.width,r.height,selected?new Color(.20f,.31f,.33f):new Color(.105f,.19f,.22f));Rect(r.x,r.y,r.width,4,color);
            if(occupied){if(kind==0)GunIcon(r.x+25,r.y+45,r.width-50,70,item,color);else SupplyIcon(r.x+r.width*.5f,r.y+38,kind,item,color);Text(r.x+14,r.y+r.height-53,r.width-28,29,titleText,small,r.width<140?14:18,paper);Text(r.x+14,r.y+r.height-25,r.width-28,25,count,small,15,color);if(GUI.Button(r,GUIContent.none,GUIStyle.none)){inventoryKind=kind;inventoryItem=item;}}
            else Text(r.x+20,r.y+r.height*.5f-15,r.width-40,35,"Empty slot",small,19,muted);
        }
        readonly Texture2D[] weaponIcons=new Texture2D[7];
        void GunIcon(float x,float y,float width,float height,int index,Color color)
        {if(index>=0&&index<7){if(!weaponIcons[index])weaponIcons[index]=Resources.Load<Texture2D>("WeaponIcons/"+index);if(weaponIcons[index]){GUI.DrawTexture(new Rect(x,y,width,height),weaponIcons[index],ScaleMode.ScaleToFit,true);return;}}float length=index==3?width*.47f:index==4?width*.72f:width;Rect(x,y+height*.25f,length,7,color);Rect(x+length*.13f,y+height*.36f,length*.48f,14,color);Rect(x,y+height*.4f,length*.16f,21,color);Rect(x+length*.36f,y+height*.57f,10,22,color);if(index==0||index==5){Rect(x+length*.35f,y,length*.28f,8,color);Rect(x+length*.43f,y+8,5,13,color);}if(index==4||index==6)Rect(x+length*.5f,y+height*.55f,16,index==6?27:20,color);}
        void SupplyIcon(float x,float y,int kind,int item,Color color)
        {if(kind==1){for(int i=0;i<3;i++){Rect(x-27+i*20,y-15,12,36,color);Rect(x-24+i*20,y-22,6,9,color);}}else if(kind==2){Rect(x-30,y-15,60,43,color);Rect(x-5,y-9,10,31,paper);Rect(x-16,y+2,32,9,paper);}else if(kind==5){CircleAt(new Vector2(x,y),23,color,3);Rect(x-14,y-5,28,10,color);}else {Rect(x-23,y-20,46,48,color);Rect(x-12,y-25,24,8,color);}}
        void PriorInventory()
        {
            if(!P){OldLoadout();return;}if(inventoryKind==0&&!P.Owns(inventoryItem))inventoryItem=P.Weapon.Value;
            Rect(0,0,1600,900,ink);Text(75,40,1400,80,"Field inventory",title,60);Text(80,138,1400,40,"Drag weapons between slots. Choose any box to equip, use, tune, or drop its contents.",body,21);
            int[] guns=Enumerable.Range(0,3).Select(P.SlotWeapon).ToArray();var input=Event.current;if(input.type==EventType.MouseDrag&&dragSlot>=0)draggingWeapon=true;if(input.type==EventType.MouseUp&&dragSlot>=0){if(draggingWeapon){for(int target=0;target<3;target++)if(new Rect(80+target*267,248,248,184).Contains(input.mousePosition))P.SwapSlotsServerRpc(dragSlot,target);}dragSlot=-1;draggingWeapon=false;}if(input.type==EventType.MouseDown&&input.button==0)for(int slot=0;slot<3;slot++)if(new Rect(80+slot*267,248,248,184).Contains(input.mousePosition)&&guns[slot]>=0)dragSlot=slot;Text(80,201,800,35,"Weapons",type,24);
            for(int i=0;i<3;i++){int gun=guns[i];Slot(new Rect(80+i*267,248,248,184),gun>=0?WeaponData.Get(gun).name:"",gun>=0?WeaponData.Rarities[P.Rarity(gun)]+" · "+P.MagazineFor(gun)+" loaded":"",0,gun,gun>=0?WeaponData.RarityColor(P.Rarity(gun)):muted,gun>=0);}
            Text(80,458,800,35,"Ammunition",type,23);for(int i=0;i<4;i++)Slot(new Rect(80+i*200,505,182,119),WeaponData.AmmoNames[i],P.ReserveFor(i)+" rounds",1,i,muted);
            Text(80,648,800,35,"Supplies",type,23);int[] kinds={5,2,3,4,6,7},amounts={P.Food.Value,P.Medkits.Value,P.Craft.Value,P.FuelCans.Value,P.Armor.Value,P.ArmorPlates.Value};string[] names={"Food","Medkit","Materials","Fuel",Ranger.ArmorNames[P.ArmorTier.Value],"Plates"};for(int i=0;i<6;i++)Slot(new Rect(80+i*133,695,120,119),names[i],"× "+amounts[i],kinds[i],0,kinds[i]==5?prey:ally);
            Rect(945,248,575,566,new Color(.10f,.18f,.21f));string description=inventoryKind==0?WeaponData.Get(inventoryItem).name:inventoryKind==1?WeaponData.AmmoNames[Mathf.Clamp(inventoryItem,0,3)]+" ammunition":inventoryKind==2?"Medkit":inventoryKind==3?"Field materials":inventoryKind==4?"Fuel can":inventoryKind==6?Ranger.ArmorNames[P.ArmorTier.Value]:inventoryKind==7?"Armor plates":"Field food";Text(975,280,515,65,description,number,37);
            string details=inventoryKind==0?WeaponData.Get(inventoryItem).description+"\n\nMagazine: "+WeaponData.Get(inventoryItem).magazine+" rounds\nEffective range: "+WeaponData.Get(inventoryItem).range+" m\nTuning: "+P.UpgradeLevel(inventoryItem)+" / 2":inventoryKind==1?"Spare ammunition is used when you reload. Drop a small stack for a teammate.":inventoryKind==2?"Restores 45 health. Use from here or press H.":inventoryKind==3?"Repair vehicle engines or tune your weapon. Dropping supplies lets your teammate help.":inventoryKind==4?"Adds 45 fuel to a nearby vehicle. Press F beside it to refuel.":inventoryKind==6?"Absorbs "+Mathf.RoundToInt(Ranger.ArmorReduction(P.ArmorTier.Value)*100)+"% of gunshot and animal damage while durability remains. Storm and starvation bypass armor.":inventoryKind==7?"Restores 30 vest durability. Equip a vest first. Press B to use a plate.":"Restores 35 hunger and 5 health. Low hunger prevents sprinting. Press J to eat.";Text(976,376,510,250,details,body,22);
            if(inventoryKind==0){if(Button(975,650,245,55,"Equip weapon",true))P.SelectWeapon(inventoryItem);if(Button(1235,650,245,55,"Tune weapon"))P.UpgradeServerRpc(inventoryItem);}
            else if(inventoryKind==7){if(Button(975,650,505,55,"Use armor plate · B",true))P.PlateServerRpc();}
            else if(inventoryKind==2||inventoryKind==5){if(Button(975,650,505,55,inventoryKind==5?"Eat food":"Use medkit",true)){if(inventoryKind==5)P.EatServerRpc();else P.HealServerRpc();}}
            if(Button(975,724,505,55,inventoryKind==1?"Drop up to 30 rounds":"Drop one"))P.DropItemServerRpc(inventoryKind,inventoryItem,inventoryKind==1?30:1);
            if(Button(80,836,795,45,"Return to the field · I",true))ClosePanel();if(draggingWeapon&&dragSlot>=0&&guns[dragSlot]>=0)GunIcon(input.mousePosition.x-50,input.mousePosition.y-15,100,40,guns[dragSlot],gold);
        }
    }
}
