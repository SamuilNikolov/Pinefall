using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        readonly List<Rect> mapLabels=new List<Rect>();
        public void VerificationAtlas(Vector2 center,float zoom){mapCenter=center;mapZoom=zoom;}
        Rect MapLabel(Vector2 point,string text,float size,int ordinal,bool water=false)
        {
            if(point.x<0||point.y<0||point.x>size||point.y>size)return default;
            bool major=!water&&(ordinal<12||ordinal==30||ordinal==41),hovering=Vector2.Distance(point,Event.current.mousePosition)<18;
            if(!water&&!major&&mapZoom<1.65f&&!hovering)return default;
            int font=water?13:major?17:14;var style=new GUIStyle(small){fontSize=font,fontStyle=major?FontStyle.Bold:FontStyle.Normal,wordWrap=false,alignment=TextAnchor.MiddleCenter};float width=Mathf.Clamp(style.CalcSize(new GUIContent(text)).x+16,85,260);Rect label=new Rect(0,0,width,25);
            for(int attempt=0;attempt<13;attempt++){float dy=attempt==0?0:(attempt%2==0?-1:1)*((attempt+1)/2)*26;label.x=Mathf.Clamp(point.x-width*.5f,3,size-width-3);label.y=Mathf.Clamp(point.y+10+dy,3,size-27);bool collision=false;foreach(var used in mapLabels)if(used.Overlaps(label)){collision=true;break;}if(!collision)break;if(attempt==12&&!hovering)return default;}
            mapLabels.Add(label);if(Mathf.Abs(label.center.y-point.y)>40)Line(point,new Vector2(label.center.x,label.center.y),new Color(1,1,1,.42f),1);
            Color ink=new Color(.02f,.03f,.04f,.95f);Text(label.x-1,label.y,label.width,label.height,text,style,font,ink);Text(label.x+1,label.y,label.width,label.height,text,style,font,ink);Text(label.x,label.y-1,label.width,label.height,text,style,font,ink);Text(label.x,label.y+1,label.width,label.height,text,style,font,ink);Text(label.x,label.y,label.width,label.height,text,style,font,water?new Color(.74f,.89f,.96f):Color.white);return label;
        }
        void Map()
        {
            Rect(0,0,1600,900,new Color(.035f,.075f,.09f));float span=ReserveWorld.Size/mapZoom;var bounds=new Rect(40,75,790,790);var ev=Event.current;
            if(bounds.Contains(ev.mousePosition)&&ev.type==EventType.ScrollWheel){float old=span;Vector2 uv=new Vector2((ev.mousePosition.x-bounds.x)/bounds.width-.5f,.5f-(ev.mousePosition.y-bounds.y)/bounds.height);mapZoom=Mathf.Clamp(mapZoom*Mathf.Pow(1.18f,-ev.delta.y),1,16);span=ReserveWorld.Size/mapZoom;mapCenter+=uv*(old-span);ev.Use();}
            if(ev.type==EventType.MouseDown&&ev.button==0&&bounds.Contains(ev.mousePosition))dragging=true;if(ev.type==EventType.MouseUp)dragging=false;
            if(dragging&&ev.type==EventType.MouseDrag){mapCenter+=new Vector2(-ev.delta.x,ev.delta.y)*span/bounds.width;ev.Use();}
            float limit=(ReserveWorld.Size-span)/2;mapCenter.x=Mathf.Clamp(mapCenter.x,-limit,limit);mapCenter.y=Mathf.Clamp(mapCenter.y,-limit,limit);
            DrawTactical(bounds,mapCenter,span,true);Text(41,29,780,35,"North ↑     Pinefall Preserve · 4K terrain imagery",small,20,paper);Text(42,870,790,25,"Scroll to zoom · drag to pan · M closes the atlas",small,17,muted);
            Text(857,58,670,89,"Reserve map",type,43);Text(863,166,645,70,"Named villages anchor the reserve. Scroll to reveal smaller hamlets; drag to pan. Hover an icon for details.",body,22);
            Color[] colors={paper,ally,prey,new Color(.83f,.55f,1),new Color(.65f,.46f,.95f),gold};string[] labels={"You and your heading","Teammates · names shown","Living wildlife within 160 m","Shared supply drop","Current safe-zone boundary","Next safe zone and field order"};
            for(int i=0;i<labels.Length;i++){Glyph(865,275+i*43,23,new[]{"sprint","shield","paw","crate","ring","ring"}[i],colors[i]);Text(901,273+i*43,603,37,labels[i],small,20,paper);}
            Glyph(865,530,23,"car",new Color(.52f,.76f,.94f));Glyph(899,530,23,"bike",new Color(.52f,.76f,.94f));Glyph(933,530,23,"boat",new Color(.52f,.76f,.94f));Text(973,527,530,30,"Live transport · hover for engine and fuel",small,16,muted);
            if(P){int col=Mathf.Clamp(Mathf.FloorToInt((P.transform.position.x/ReserveWorld.Size+.5f)*8),0,7),row=Mathf.Clamp(Mathf.FloorToInt((.5f-P.transform.position.z/ReserveWorld.Size)*8),0,7);Text(862,565,640,35,"Grid "+(char)('A'+col)+(row+1)+" · "+ReserveBiomes.Label(P.transform.position.x,P.transform.position.z),type,24,gold);Text(862,612,640,60,"Nearest: "+ExpeditionLayout.Regions[ExpeditionLayout.Nearest(P.transform.position)].name+(P.SurveyVisible?"\nRed pings: survey snapshots · expires soon":"\nEnemy players hidden · survey beacons reveal briefly."),small,19,paper);}
            if(Button(862,700,190,53,"− Zoom out"))mapZoom=Mathf.Max(1,mapZoom/1.5f);if(Button(1067,700,190,53,"+ Zoom in"))mapZoom=Mathf.Min(16,mapZoom*1.5f);if(Button(1272,700,250,53,"Center on me")){if(P)mapCenter=new Vector2(P.transform.position.x,P.transform.position.z);mapZoom=Mathf.Max(4,mapZoom);}
            if(Button(862,783,660,62,"Return to the field",true))ClosePanel();
        }
    }
}
