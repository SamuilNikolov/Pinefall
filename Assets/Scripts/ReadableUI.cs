using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        void Glyph(float x,float y,float size,string kind,Color color){var previous=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(x,y,size,size),RasterIcons.Get(kind));GUI.color=previous;}
        void SurvivalPanel()
        {
            Rect(28,731,356,150,panelColor);Text(43,738,325,24,P.DisplayName.Value.ToString(),small,16,paper);
            Glyph(43,766,19,"health",healthColor);Bar(73,772,248,P.Health.Value/100f,healthColor);Text(330,761,48,25,P.Health.Value.ToString(),small,16,paper);
            Glyph(43,790,18,"shield",shieldColor);Bar(73,796,248,P.ArmorTier.Value>0?P.Armor.Value/(float)Ranger.ArmorMaximum(P.ArmorTier.Value):0,shieldColor);Text(329,787,50,25,P.Armor.Value.ToString(),small,15,P.Armor.Value>0?paper:muted);
            Color food=P.Hunger.Value<20?danger:P.Hunger.Value<40?gold:prey;Glyph(43,814,19,"food",food);for(int i=0;i<10;i++){Rect(73+i*25,820,22,8,new Color(.18f,.22f,.25f));Rect(73+i*25,820,22*Mathf.Clamp01(P.Hunger.Value/10-i),8,food);}Text(330,811,48,25,Mathf.CeilToInt(P.Hunger.Value).ToString(),small,15,food);
            Text(73,832,300,22,P.HungerStatus+"   "+GameBindings.Label("Food")+" Eat ×"+P.Food.Value,small,13,food);Glyph(44,856,17,"sprint",gold);Bar(73,861,248,P.Stamina,gold);Text(329,851,48,25,Mathf.RoundToInt(P.Stamina*100)+"%",small,12,muted);
            if(P.Hunger.Value<40&&panel==""){Rect(560,278,480,42,panelColor);Glyph(577,286,24,"food",food);Text(612,285,414,30,P.Hunger.Value<20?"Eat food to restore sprinting":"Hunt or search houses for food",small,17,food);}
        }
        void WildlifeSightings()
        {
            if(!P.view||P.ScopedView||panel!="")return;int visible=0;
            foreach(var animal in S.Animals){if(!animal||animal.Dead||animal.Aquatic)continue;Vector3 point=animal.transform.position+Vector3.up*1.7f;float distance=Vector3.Distance(P.transform.position,point);if(distance>95||distance<6)continue;var at=P.view.WorldToScreenPoint(point);if(at.z<0||at.x<80||at.x>Screen.width-80||at.y<80||at.y>Screen.height-80)continue;if(Physics.Linecast(P.view.transform.position,point,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))continue;float x=at.x/Screen.width*1600,y=(1-at.y/Screen.height)*900;Glyph(x-8,y-21,16,"paw",animal.Enraged.Value?shieldColor:prey);if(animal==P.FocusAnimal)Text(x+13,y-25,140,25,Mathf.RoundToInt(distance)+" m",small,13,prey);if(++visible>=4)break;}
        }
        void AudioPage()
        {
            Rect(0,0,1600,900,new Color(.04f,.065f,.085f));Text(104,80,1380,85,"Sound mix",title,62,paper);Text(104,176,1380,55,"Adjust the layers you hear. Voice and narrator controls remain in Settings.",body,23,muted);string[] descriptions={"Your quieter surface footsteps and nearby movement","Recorded firearm reports, reloads and equipment cues","Nearby calls, warning growls and animal movement","Wind, rain and a quiet daytime bird bed"};
            for(int i=0;i<Soundscape.Channels.Length;i++){float y=285+i*115;string channel=Soundscape.Channels[i];Text(104,y,500,32,channel,type,25,paper);Text(104,y+38,780,29,descriptions[i],small,17,muted);float value=Slider(930,y+30,420,Soundscape.Level(channel),0,1);Soundscape.SetLevel(channel,value);Text(1380,y+11,110,35,Mathf.RoundToInt(value*100)+"%",type,23,paper);}
            if(Button(104,795,410,55,"Restore sound mix")){foreach(var channel in Soundscape.Channels)PlayerPrefs.DeleteKey("sound."+channel);}if(Button(1120,795,340,55,"Back to Settings",true)){PlayerPrefs.Save();Open("settings");}
        }
    }
}
