using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Pinefall
{
    // Opt-in visual and session lifecycle verification in the actual packaged renderer.
    public sealed partial class CaptureProbe : MonoBehaviour
    {
        public static bool Running;
        string folder;
        IEnumerator Start()
        {
            Running=true;folder=ReserveSession.Arg("--capture");Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(3);
            yield return Shot("menu");
            ReserveUI.Instance.VerificationPanel("loadout");yield return Shot("equipment");
            ReserveUI.Instance.VerificationPanel("host");yield return Shot("host");
            ReserveSession.Instance.SelectedWeapon=0;ReserveSession.Instance.BattleMode=false;ReserveSession.Instance.Port="7798";ReserveSession.Instance.PlayerName="FieldTest";ReserveSession.Instance.Host();
            yield return new WaitForSecondsRealtime(3);
            if(!Ranger.Local){Debug.LogError("CAPTURE_HOST_FAILED");Application.Quit();yield break;}
            if(ReserveSession.Arg("--fieldcraft-capture")=="1"){yield return FieldcraftShots();Application.Quit();yield break;}
            ReserveUI.Instance.VerificationPanel("");yield return Shot("play");Ranger.Local.SetModCountServer(2,1);Ranger.Local.SetModCountServer(4,1);Ranger.Local.SetModCountServer(7,1);ReserveUI.Instance.VerificationPanel("loadout");yield return Shot("inventory");ReserveUI.Instance.VerificationPanel("bindings");yield return Shot("controls");ReserveUI.Instance.VerificationPanel("guide");yield return Shot("guide");ReserveUI.Instance.VerificationPanel("");
            var animal=ReserveSession.Instance.Animals.First(a=>!a.Dead);
            Ranger.Local.SmokeMoveTo(ReserveWorld.OnGround(animal.transform.position.x,animal.transform.position.z-7,.2f));Ranger.Local.SmokeAimAt(animal);
            yield return Shot("wildlife");
            Ranger.Local.SmokeScoped=true;yield return Shot("scope");Ranger.Local.SmokeScoped=false;
            ExportMap();ReserveUI.Instance.VerificationPanel("map");yield return Shot("map");if(ReserveSession.Arg("--quick-ui")=="1"){ReserveUI.Instance.VerificationAtlas(new Vector2(1000,850),3.5f);yield return Shot("map-villages");ReserveUI.Instance.VerificationPanel("audio");yield return Shot("audio-mix");ReserveUI.Instance.VerificationPanel("performance");yield return Shot("performance-settings");Ranger.Local.Hunger.Value=18;ReserveUI.Instance.VerificationPanel("");yield return Shot("hunger-low");Ranger.Local.Hunger.Value=85;Ranger.Local.SmokeMoveTo(ReserveWorld.OnGround(900,500,1.5f));Ranger.Local.view.transform.rotation=Quaternion.LookRotation(new Vector3(.5f,-.08f,1));yield return Shot("autumn");Application.Quit();yield break;}
            ReserveUI.Instance.VerificationPanel("pause");yield return Shot("pause");
            ReserveUI.Instance.VerificationPanel("session");yield return Shot("connection");
            WildfrontMatch.Current.Battle.Value=true;ReserveUI.Instance.VerificationPanel("lobby");yield return Shot("lobby");WildfrontMatch.Current.Battle.Value=false;ReserveUI.Instance.VerificationPanel("character");yield return Shot("character");
            Ranger.Local.AppearanceServerRpc(1,2,1);Ranger.Local.view.GetComponent<RangerCamera>().ThirdPerson=true;ReserveUI.Instance.VerificationPanel("");Ranger.Local.view.transform.position=Ranger.Local.transform.position+Ranger.Local.transform.forward*3.6f+Vector3.up*1.7f;Ranger.Local.view.transform.LookAt(Ranger.Local.transform.position+Vector3.up);yield return Shot("ranger");Ranger.Local.view.GetComponent<RangerCamera>().ThirdPerson=false;Ranger.Local.view.transform.localPosition=Vector3.up*1.7f;
            var car=FindObjectsOfType<RoadVehicle>().First();Ranger.Local.SmokeMoveTo(ReserveWorld.OnGround(car.transform.position.x+3,car.transform.position.z,.3f));car.Condition.Value=100;Ranger.Local.EnterVehicleServerRpc(car.NetworkObjectId);yield return new WaitForSecondsRealtime(.2f);Ranger.Local.view.transform.position=car.transform.position-car.transform.forward*5.5f+Vector3.up*3;Ranger.Local.view.transform.LookAt(car.transform.position+Vector3.up*.7f);yield return Shot("vehicle");Ranger.Local.ExitVehicleServerRpc();
            var lake=ReserveWater.Lakes[0];Ranger.Local.SmokeMoveTo(ReserveWorld.OnGround(lake.center.x-lake.radius.x-15,lake.center.y,2));Ranger.Local.view.transform.LookAt(new Vector3(lake.center.x,lake.level,lake.center.y));ReserveUI.Instance.VerificationPanel("");yield return Shot("lake");
            var boat=FindObjectsOfType<RoadVehicle>().First(v=>v.Type.Value==4);Ranger.Local.SmokeMoveTo(boat.transform.position+Vector3.up*5-boat.transform.forward*11);Ranger.Local.view.transform.LookAt(boat.transform.position);yield return Shot("motor-launch");
            var supply=WildfrontMatch.Current.SpawnSupply(ReserveWorld.OnGround(0,-120));supply.Began.Value-=19;Ranger.Local.SmokeMoveTo(ReserveWorld.OnGround(15,-150,12));Ranger.Local.view.transform.LookAt(supply.transform.position+Vector3.up*90);yield return Shot("supply-drop");supply.NetworkObject.Despawn();
            Vector2[] vistas={new Vector2(-80,-480),new Vector2(-2120,-1080),new Vector2(-1320,2160),new Vector2(1720,1880),new Vector2(2120,-1040),new Vector2(510,-2240)};
            for(int biome=0;biome<vistas.Length;biome++)
            {
                Ranger.Local.SmokeMoveTo(ReserveWorld.OnGround(vistas[biome].x,vistas[biome].y,1.5f));Ranger.Local.view.transform.rotation=Quaternion.LookRotation(new Vector3(.3f,-.09f,1));ReserveUI.Instance.VerificationPanel("");yield return Shot("biome-"+biome);
            }
            ReserveUI.Instance.VerificationPanel("");WeatherState.Current.Choose(0,false,23);yield return Shot("night");WeatherState.Current.Choose(0,false,14);
            if(ReserveWeather.Instance)
            {
                ReserveWeather.Instance.Choose(2,false,13);ReserveUI.Instance.VerificationPanel("");yield return new WaitForSecondsRealtime(11);
                yield return Shot("rain");ReserveUI.Instance.VerificationPanel("weather");yield return Shot("weather");
                ReserveWeather.Instance.Choose(0,true,16.7f);
            }
            if(WildfrontMatch.Current)
            {
                WildfrontMatch.Current.Battle.Value=true;WildfrontMatch.Current.StartRound();ReserveUI.Instance.VerificationPanel("draft");yield return Shot("draft");SpawnDraft.Current.SubmitBidServerRpc(3,2);yield return new WaitForSecondsRealtime(.3f);SpawnDraft.Current.PickRegionServerRpc(1);yield return new WaitForSecondsRealtime(8);
                WildfrontMatch.Current.ShrinkAt.Value=ReserveSession.Instance.Network.ServerTime.Time-200;WildfrontMatch.Current.StageEnds.Value=ReserveSession.Instance.Network.ServerTime.Time+100;WildfrontMatch.Current.NextRadius.Value=500;WildfrontMatch.Current.NextCenter.Value=Vector3.zero;Ranger.Local.SmokeMoveTo(ReserveWorld.OnGround(2200,-330,1));Ranger.Local.view.transform.rotation=Quaternion.LookRotation(Vector3.left);ReserveUI.Instance.VerificationPanel("");yield return Shot("storm");
            }
            ReserveSession.Instance.Leave();yield return new WaitForSecondsRealtime(4);
            Debug.Log("PINEFALL_REENTRY_MENU "+(!ReserveSession.Instance.Playing));
            ReserveSession.Instance.Port="7798";ReserveSession.Instance.Host();yield return new WaitForSecondsRealtime(3);
            Debug.Log("PINEFALL_REENTRY_HOST "+ReserveSession.Instance.Playing);
            yield return Shot("reentry");
            Application.Quit();
        }
        void ExportMap()
        {
            var source=ReserveWorld.Instance.map;var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);Graphics.Blit(source,target);var previous=RenderTexture.active;RenderTexture.active=target;var copy=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();File.WriteAllBytes(Path.Combine(folder,"atlas-texture.png"),copy.EncodeToPNG());RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Destroy(copy);
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(.65f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.5f);
            var camera=Camera.main;
            if(camera)
            {
                var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var old=camera.targetTexture;camera.targetTexture=rt;ReserveWorld.Instance.DrawFoliage(camera,camera);camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(folder,name+"-world.png"),tex.EncodeToPNG());
                RenderTexture.active=previous;camera.targetTexture=old;Destroy(tex);rt.Release();Destroy(rt);
            }
        }
    }
}
