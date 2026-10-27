using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        bool emoteWasHeld,emoteVerification;Vector2 emotePointer;int emoteChoice=-1,inputConsumedFrame=-1;Texture2D emoteRing,emoteWedge;
        public bool EmoteWheelOpen{get{return panel=="emotes";}}
        public void VerificationEmotes(Vector2 pointer){panel="emotes";emoteVerification=true;SetEmotePointer(pointer);}
        public void SetEmotePointer(Vector2 offset){emotePointer=Vector2.ClampMagnitude(offset,285);emoteChoice=Emotes.Sector(emotePointer);}
        public void TickEmoteHold(bool held)
        {
            if(held&&!emoteWasHeld&&panel==""&&S.Playing&&P&&P.Alive&&P.CanAct&&!P.CurrentVehicle)
            {panel="emotes";emotePointer=Vector2.zero;emoteChoice=-1;inputConsumedFrame=Time.frameCount;if(ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")=="")Lock();}
            if(EmoteWheelOpen&&!held)FinishEmoteWheel(true);
            emoteWasHeld=held;
        }
        public void FinishEmoteWheel(bool commit)
        {
            if(!EmoteWheelOpen)return;int chosen=emoteChoice;emoteVerification=false;panel="";inputConsumedFrame=Time.frameCount;
            if(commit&&chosen>=0&&P)P.RequestEmote(chosen);if(ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")=="")Lock();
        }
        void EmoteWheelInput()
        {
            if(emoteVerification)return;
            if(Input.GetKeyDown(KeyCode.Escape)||!Application.isFocused||!P||!P.Alive){FinishEmoteWheel(false);return;}
            SetEmotePointer(emotePointer+new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"))*18);
            if(Input.GetMouseButtonDown(0)){FinishEmoteWheel(true);return;}if(Input.GetMouseButtonDown(1)){FinishEmoteWheel(false);return;}
            TickEmoteHold(GameBindings.Held("Emotes"));
        }
        void PrepareEmoteWheel()
        {
            if(emoteRing)return;const int n=256;emoteRing=new Texture2D(n,n,TextureFormat.RGBA32,false);emoteWedge=new Texture2D(n,n,TextureFormat.RGBA32,false);
            var ring=new Color32[n*n];var wedge=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=(x-127.5f)/127.5f,dy=(y-127.5f)/127.5f,r=Mathf.Sqrt(dx*dx+dy*dy);float a=Mathf.Atan2(dx,dy)*Mathf.Rad2Deg;bool inside=r>.46f&&r<.99f;float edge=Mathf.Abs(Mathf.Repeat(a+30,60)-30);
                ring[y*n+x]=new Color(1,1,1,inside?(edge>28.7f?.28f:.95f):0);wedge[y*n+x]=new Color(1,1,1,inside&&Mathf.Abs(a)<28.6f?1:0);
            }
            emoteRing.SetPixels32(ring);emoteRing.Apply(false,true);emoteWedge.SetPixels32(wedge);emoteWedge.Apply(false,true);
        }
        void EmoteWheel()
        {
            PrepareEmoteWheel();Rect(0,0,1600,900,new Color(.02f,.04f,.07f,.35f));Vector2 center=new Vector2(800,435);var bounds=new Rect(545,180,510,510);var tint=GUI.color;
            GUI.color=new Color(.07f,.10f,.15f,.96f);GUI.DrawTexture(bounds,emoteRing);GUI.color=tint;
            if(emoteChoice>=0){var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(emoteChoice*60,center);GUI.color=new Color(.30f,.55f,.69f,.96f);GUI.DrawTexture(bounds,emoteWedge);GUI.color=tint;GUI.matrix=matrix;}
            for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;var at=center+new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle))*181;EmoteFigure(at,i,i==emoteChoice?paper:muted);}
            Text(570,95,460,65,"Emotes",Centered(type),34,paper);Text(691,398,218,62,emoteChoice<0?"Cancel":Emotes.Names[emoteChoice],Centered(type),27,emoteChoice<0?muted:ice);
            Text(590,726,420,42,"Release "+GameBindings.Label("Emotes")+" to select",Centered(body),23,paper);
            Text(420,784,760,64,"Move toward an emote / return to center to cancel\nClick to select / right click or Esc to cancel",Centered(small),18,muted);
            var pointer=center+new Vector2(emotePointer.x,-emotePointer.y);CircleAt(pointer,6,ice,3);
        }
        void EmoteFigure(Vector2 at,int id,Color color)
        {
            Vector2 head=at+new Vector2(0,-29),shoulder=at+new Vector2(0,-10),hip=at+new Vector2(0,22);CircleAt(head,9,color,5);Line(shoulder,hip,color,6);
            Vector2 left=at+new Vector2(-26,12),right=at+new Vector2(26,12);
            if(id==0)right=at+new Vector2(23,-40);if(id==1){left=at+new Vector2(-29,-39);right=at+new Vector2(29,-39);}if(id==2)right=at+new Vector2(40,-15);if(id==3)right=at+new Vector2(14,-30);if(id==4){left=at+new Vector2(-5,-1);right=at+new Vector2(7,-1);}if(id==5){left=at+new Vector2(-27,-13);right=at+new Vector2(24,4);}
            Line(shoulder+Vector2.left*4,left,color,5);Line(shoulder+Vector2.right*4,right,color,5);Line(hip,at+new Vector2(id==5?-23:-14,52),color,5);Line(hip,at+new Vector2(id==5?28:14,id==5?41:52),color,5);
        }
    }
}
