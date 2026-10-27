using UnityEngine;
namespace Pinefall
{
    [DefaultExecutionOrder(200)]
    public sealed class RuntimePerformance:MonoBehaviour
    {
        public static RuntimePerformance Current;public static bool HideHud;
        public static float Detail=1;
        public static float ViewDistance {get{return Mathf.Clamp(PlayerPrefs.GetFloat("view.distance",900),750,1800);}}
        void LateUpdate(){var camera=Camera.main;if(!camera||ReserveSession.Arg("--bake-atlas")!="")return;camera.farClipPlane=Mathf.Max(Ranger.Local&&Ranger.Local.Aiming?850:650,ViewDistance*(.75f+Detail*.25f));RenderSettings.fogDensity=Mathf.Max(RenderSettings.fogDensity,1.9f/camera.farClipPlane);SettlementDetail.Tick(camera);Shader.SetGlobalFloat("_ReserveViewDistance",camera.farClipPlane);}
        public static bool Automatic{get{return PlayerPrefs.GetInt("perf.auto",1)==1;}set{PlayerPrefs.SetInt("perf.auto",value?1:0);}}
        public static int Target{get{return PlayerPrefs.GetInt("perf.target",90);}set{PlayerPrefs.SetInt("perf.target",value);}}
        public float FrameMs{get;private set;}=16.7f;float nextTune,stableSince;bool lastFocus;int appliedTarget;
        void Awake(){Current=this;nextTune=Time.unscaledTime+8;stableSince=Time.unscaledTime;QualitySettings.vSyncCount=0;QualitySettings.maxQueuedFrames=2;ApplyDetail();}
        void Update()
        {
            FrameMs=Mathf.Lerp(FrameMs,Mathf.Min(Time.unscaledDeltaTime*1000,200),.05f);bool focused=Application.isFocused||ReserveSession.Arg("--smoke")!=""||ReserveSession.Arg("--capture")!="";int target=ReserveSession.Arg("--uncapped")=="1"?-1:focused?(Target==0?-1:Target):30;
            if(target!=appliedTarget||focused!=lastFocus){Application.targetFrameRate=target;appliedTarget=target;lastFocus=focused;}
            if(!Automatic||!focused||target<0||Time.unscaledTime<nextTune)return;nextTune=Time.unscaledTime+3;
            float budget=1000f/target;if(FrameMs>budget*1.20f){Detail=Mathf.Max(.35f,Detail-.12f);stableSince=Time.unscaledTime;ApplyDetail();}else if(FrameMs<=budget*1.08f&&Time.unscaledTime-stableSince>10&&Detail<1){Detail=Mathf.Min(1,Detail+.05f);stableSince=Time.unscaledTime;ApplyDetail();}
        }
        public static void ApplyDetail()
        {
            if(!Automatic){Detail=PlayerPrefs.GetInt("quality",1)==0?.5f:PlayerPrefs.GetInt("quality",1)==2?1:.8f;return;}
            QualitySettings.shadowDistance=Mathf.Lerp(45,130,Detail);QualitySettings.shadows=Detail<.5f?ShadowQuality.HardOnly:ShadowQuality.All;QualitySettings.shadowResolution=Detail<.65f?ShadowResolution.Low:ShadowResolution.Medium;QualitySettings.antiAliasing=Detail<.5f?0:2;
            if(ReserveWorld.Instance&&ReserveWorld.Instance.ground)ReserveWorld.Instance.ground.heightmapPixelError=Mathf.Lerp(18,7,Detail);
        }
        void OnDestroy(){if(Current==this)Current=null;}
    }
    public sealed partial class ReserveUI
    {
        void PerformancePage()
        {
            Rect(0,0,1600,900,ink);Text(80,55,1440,80,"Performance",title,54,paper);Text(84,153,1390,55,"Auto responds to frame time on your computer. It adjusts distant detail and shadows while keeping the HUD sharp.",body,22,muted);
            Text(85,250,1320,40,"Graphics device: "+SystemInfo.graphicsDeviceName,small,22,paper);Text(85,304,1320,40,Screen.width+" × "+Screen.height+"    "+(RuntimePerformance.Current?Mathf.RoundToInt(1000/RuntimePerformance.Current.FrameMs)+" FPS · "+RuntimePerformance.Current.FrameMs.ToString("0.0")+" ms":""),small,22,gold);
            if(Button(85,386,430,56,RuntimePerformance.Automatic?"Auto performance: on":"Auto performance: off",RuntimePerformance.Automatic)){RuntimePerformance.Automatic=!RuntimePerformance.Automatic;RuntimePerformance.ApplyDetail();}
            Text(85,482,1200,35,"Frame-rate target",type,26,paper);int[] rates={60,90,120,144,0};for(int i=0;i<rates.Length;i++)if(Button(85+i*274,540,253,55,rates[i]==0?"Uncapped":rates[i]+" FPS",RuntimePerformance.Target==rates[i]))RuntimePerformance.Target=rates[i];
            Text(85,625,1300,28,"View distance: "+Mathf.RoundToInt(RuntimePerformance.ViewDistance)+" m",small,20,paper);float view=Slider(85,673,1060,RuntimePerformance.ViewDistance,750,1800);if(Event.current.type!=EventType.Repaint&&Event.current.type!=EventType.Layout)PlayerPrefs.SetFloat("view.distance",view);
            bool show=PlayerPrefs.GetInt("perf.overlay",1)==1;if(Button(85,761,430,55,show?"Frame-rate display: on":"Frame-rate display: off"))PlayerPrefs.SetInt("perf.overlay",show?0:1);if(Button(1120,761,380,55,"Back to Settings",true)){PlayerPrefs.Save();Open("settings");}
        }
    }
}
