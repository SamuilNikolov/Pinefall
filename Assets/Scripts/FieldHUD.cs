using System.Linq;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        void PriorFieldHUD()
        {
            if(panel!=""&&panel!="pause")return;var m=WildfrontMatch.Current;var weapon=WeaponData.Get(P.Weapon.Value);bool scoped=P.ScopedView;
            if(scoped){float s=760,x=420,y=70;Rect(0,0,x,900,Color.black);Rect(x+s,0,x,900,Color.black);Rect(x,0,s,y,Color.black);Rect(x,y+s,s,y,Color.black);GUI.DrawTexture(new Rect(x,y,s,s),scope);Rect(799,190,2,520,Color.black);Rect(540,449,520,2,Color.black);for(int i=1;i<5;i++)Rect(791,450+i*32,18,2,Color.black);}
            Text(40,28,450,34,"Pinefall / "+(P.Downed.Value?"Downed":P.Alive?"Team "+(P.Team.Value+1):"Spectating"),small,19,P.Downed.Value?danger:paper);
            float heading=P.transform.eulerAngles.y;string[] directions={"N","NE","E","SE","S","SW","W","NW"};Text(610,26,380,35,directions[Mathf.RoundToInt(heading/45)%8]+"  "+Mathf.RoundToInt(heading).ToString("000")+"°",Centered(type),23);
            if(m&&m.Battle.Value)
            {
                string status=m.Phase.Value==0?"Warmup · L opens the lobby":m.Phase.Value==2?"Round complete":S.Network.ServerTime.Time<m.ShrinkAt.Value?"Next storm  "+Clock(m.ShrinkAt.Value-S.Network.ServerTime.Time):"Storm closing  "+Clock(m.StageEnds.Value-S.Network.ServerTime.Time);
                Rect(564,74,472,43,new Color(.04f,.09f,.12f,.85f));Text(574,82,452,30,status,Centered(small),19,gold);
                Text(1230,32,330,32,Ranger.Players.Count(p=>p&&p.Alive)+" remaining    "+P.Kills.Value+" eliminations",small,18,paper);
                if(m.Active&&m.Outside(P.transform.position))Text(500,137,600,36,"Storm damage · reach the safe zone",Centered(type),22,danger);
            }
            if(!scoped&&panel==""){DrawTactical(new Rect(1350,90,210,210),new Vector2(P.transform.position.x,P.transform.position.z),360,false);Text(1355,308,200,26,"M Map · amber wildlife",small,13,prey);}
            int row=0;foreach(var friend in Ranger.Players.Where(p=>p&&p!=P&&p.Team.Value==P.Team.Value))
            {float y=95+row*53;Color color=friend.Downed.Value?new Color(.84f,.67f,1):friend.Alive?ally:muted;Text(43,y,300,30,friend.DisplayName.Value.ToString()+(friend.Downed.Value?" · downed":!friend.Alive?" · out":""),small,18,color);Bar(44,y+34,210,friend.Health.Value/100f,color);row++;
                if(P.view&&friend.Alive){var point=P.view.WorldToScreenPoint(friend.transform.position+Vector3.up*(friend.Downed.Value?.8f:2.05f));if(point.z>0&&point.z<350)Text(point.x/Screen.width*1600-85,(1-point.y/Screen.height)*900,250,40,friend.DisplayName.Value+" · "+Mathf.RoundToInt(point.z)+" m",small,16,color);}}
            if(Time.time<eventUntil){int n=0;foreach(string e in events)Text(43,308+n++*28,590,29,e,small,16,paper);}
            if(panel==""&&P.Fighting)
            {
                if(!scoped){float gap=P.Aiming?5:9+Mathf.Clamp(P.Velocity.Value,0,7)*1.2f+P.ShotBloom*9;Color color=P.HitMarker?gold:paper;Rect(800-gap-8,449,7,2,color);Rect(800+gap,449,7,2,color);Rect(799,450-gap-8,2,7,color);Rect(799,450+gap,2,7,color);}
                if(P.HitMarker){Line(new Vector2(787,437),new Vector2(795,445),gold,2);Line(new Vector2(805,455),new Vector2(813,463),gold,2);Line(new Vector2(787,463),new Vector2(795,455),gold,2);Line(new Vector2(805,445),new Vector2(813,437),gold,2);}
                string interaction=P.Interaction();if(interaction!=""){Rect(350,704,580,42,new Color(.035f,.08f,.10f,.88f));Text(364,712,552,30,interaction,Centered(small),18,gold);}
                if(P.FocusAnimal){var animal=P.FocusAnimal;Text(623,520,450,30,animal.species+" · "+Mathf.RoundToInt(Vector3.Distance(P.transform.position,animal.transform.position))+" m",small,18,paper);Text(623,555,450,30,animal.Dead?"Food and supplies nearby":animal.State.Value==5?"Warning · back away":animal.State.Value==4?"Attacking":animal.State.Value==6?"Investigating":animal.State.Value==2?"Fleeing":"Grazing",small,16,prey);}
            }
            Rect(35,770,310,99,new Color(.035f,.08f,.10f,.82f));Text(50,777,285,28,"Health "+P.Health.Value+(P.ArmorTier.Value>0?"   Armor "+P.Armor.Value:"")+(P.Downed.Value?" · downed":""),small,17,P.Downed.Value?danger:paper);Bar(50,811,278,P.Health.Value/100f,ally);if(P.ArmorTier.Value>0){Rect(50,820,278,3,new Color(.15f,.25f,.35f));Rect(50,820,278*P.Armor.Value/Ranger.ArmorMaximum(P.ArmorTier.Value),3,new Color(.43f,.68f,1));}Text(50,824,278,25,"Hunger "+Mathf.CeilToInt(P.Hunger.Value)+(P.Hunger.Value<20?" · cannot sprint":""),small,14,P.Hunger.Value<20?danger:paper);Bar(50,851,278,P.Hunger.Value/100f,prey);Bar(50,863,278,P.Stamina,gold);
            int[] guns=Enumerable.Range(0,7).Where(P.Owns).ToArray();for(int slot=0;slot<5;slot++)
            {float x=948+slot*124;bool gun=slot<3,exists=!gun||P.SlotWeapon(slot)>=0;int id=gun&&exists?P.SlotWeapon(slot):-1;Color color=gun?(exists?WeaponData.RarityColor(P.Rarity(id)):muted):slot==3?prey:ally;Rect(x,774,114,95,new Color(.035f,.08f,.10f,P.Weapon.Value==id&&exists?.96f:.75f));Rect(x,774,114,3,color);if(gun&&exists){GunIcon(x+12,799,90,35,id,color);Text(x+11,841,99,26,(slot+1)+"  "+WeaponData.Get(id).name,small,12,paper);}else if(!gun){SupplyIcon(x+57,808,slot==3?5:2,0,color);Text(x+10,845,100,25,(slot==3?"4  Food ×"+P.Food.Value:"5  Med ×"+P.Medkits.Value),small,13,paper);}else Text(x+13,810,92,35,"Empty",small,15,muted);}
            Text(954,721,280,35,weapon.name,small,20,WeaponData.RarityColor(P.Rarity(P.Weapon.Value)));Text(1300,704,250,52,P.Reloading.Value?"Reloading…":P.Ammo.Value.ToString("00")+" / "+P.ReserveFor(weapon.ammoType),number,36);
            Text(41,884,600,25,"I Inventory   V Camera   Esc Menu",small,12,muted);Text(1289,884,280,25,weapon.cartridge,small,12,muted);
            if(m&&m.Active&&!scoped){Text(43,663,365,28,"Field order",small,14,gold);Text(43,692,365,55,P.FieldOrder,small,18,paper);}
            if(P.ActiveNotice!=""&&panel==""){Rect(387,628,826,44,new Color(.035f,.08f,.10f,.9f));Text(404,636,790,34,P.ActiveNotice,Centered(small),18,paper);}
            if(P.ReviveTarget.Value!=ulong.MaxValue){Text(530,590,540,35,"Reviving teammate · keep holding E",Centered(small),19,ally);Bar(570,633,460,P.ReviveProgress.Value,ally);}
            if(P.Downed.Value)Text(430,593,760,64,"Downed · "+Clock(P.BleedEnds.Value-S.Network.ServerTime.Time)+" remaining\nA teammate can hold E nearby to revive you.",Centered(type),24,danger);
            else if(!P.Alive)Text(435,590,730,55,"Eliminated · watching your teammates",Centered(type),25,paper);
            var narrator=FieldNarrator.Current;if(narrator&&narrator.Captions&&narrator.Subtitle!="")Text(435,184,730,64,narrator.Subtitle,Centered(small),19,paper);
            var voice=ProximityVoice.Current;if(voice&&voice.Transmitting)Text(45,255,430,32,"Speaking · nearby players",small,17,ally);
            var car=P.CurrentVehicle;if(car){Text(450,665,760,30,Mathf.RoundToInt(Mathf.Abs(car.Speed.Value)*3.6f)+" km/h   ·   Engine "+car.Condition.Value+"%   Fuel "+Mathf.RoundToInt(car.Fuel.Value)+"%   Boost "+Mathf.RoundToInt(car.Charge.Value)+"%",Centered(small),19,gold);}
        }
    }
}
