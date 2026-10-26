using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Pinefall
{
    public sealed class PerformanceProbe:MonoBehaviour
    {
        [Serializable]public class Sample{public string scene;public int frames;public float averageFps,medianMs,p95Ms,p99Ms;}
        [Serializable]public class Report{public string gpu,cpu;public int width,height,microphoneStarts;public List<Sample> samples=new List<Sample>();public List<string> errors=new List<string>();}
        Report report=new Report();void Awake(){Application.logMessageReceived+=Log;}void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(message+"\n"+stack);}
        IEnumerator Start()
        {
            float deadline=Time.realtimeSinceStartup+60;while(!Ranger.Local&&Time.realtimeSinceStartup<deadline)yield return null;if(!Ranger.Local){report.errors.Add("Player did not initialize");Finish();yield break;}
            int requestedPlayers;int.TryParse(ReserveSession.Arg("--perf-players"),out requestedPlayers);deadline=Time.realtimeSinceStartup+40;while(Ranger.Players.Count<requestedPlayers&&Time.realtimeSinceStartup<deadline)yield return null;
            report.gpu=SystemInfo.graphicsDeviceName;report.cpu=SystemInfo.processorType;report.width=Screen.width;report.height=Screen.height;var player=Ranger.Local;WeatherState.Current.Choose(0,false,14);bool savedAuto=RuntimePerformance.Automatic;RuntimePerformance.Automatic=false;RuntimePerformance.Detail=1;Application.targetFrameRate=ReserveSession.Arg("--uncapped")=="1"?-1:RuntimePerformance.Target;
            string[] names={"Meadow HUD","Meadow without HUD","Named village","Autumn woods","Rainforest","Waterfront"};Vector2[] points={new Vector2(12,-150),new Vector2(12,-150),ExpeditionLayout.Regions[0].center+new Vector2(0,-40),new Vector2(900,500),new Vector2(2120,-1040),new Vector2(-500,480)};
            for(int scene=0;scene<names.Length;scene++)
            {
                player.SmokeMoveTo(ReserveWorld.OnGround(points[scene].x,points[scene].y,.3f));player.view.transform.rotation=Quaternion.LookRotation(new Vector3(.15f,-.04f,1));RuntimePerformance.HideHud=scene==1;yield return new WaitForSecondsRealtime(2);var times=new List<float>();float end=Time.realtimeSinceStartup+10;while(Time.realtimeSinceStartup<end){times.Add(Time.unscaledDeltaTime*1000);yield return null;}times.Sort();var sample=new Sample{scene=names[scene],frames=times.Count,averageFps=1000/times.Average(),medianMs=times[times.Count/2],p95Ms=times[(int)(times.Count*.95f)],p99Ms=times[(int)(times.Count*.99f)]};report.samples.Add(sample);Debug.Log("PERF_SCENE "+JsonUtility.ToJson(sample));Write();
            }
            RuntimePerformance.HideHud=false;RuntimePerformance.Automatic=savedAuto;Finish();
        }
        void Write(){report.microphoneStarts=ProximityVoice.Current?ProximityVoice.Current.MicrophoneStarts:0;File.WriteAllText(ReserveSession.Arg("--report"),JsonUtility.ToJson(report,true));}
        void Finish(){Write();Application.Quit();}void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
