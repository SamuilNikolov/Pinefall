using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinefall
{
    public sealed class ReserveWeather : MonoBehaviour
    {
        public static ReserveWeather Instance;
        public static int PreviewMode;
        public static bool AutoCycle=true;
        public static float PreviewHour=14.3f;
        public Light sun;
        public Material cloudMaterial,rainMaterial,gradeMaterial;
        Material sky;
        Cubemap equipmentEnvironment;
        ParticleSystem rain;
        Transform clouds;
        float cloudiness,wetness,flashUntil;
        int lastLightning;
        public int Mode {get{return WeatherState.Current?WeatherState.Current.Mode.Value:PreviewMode;}}
        public float Hour {get{return WeatherState.Current?WeatherState.Current.Hour.Value:PreviewHour;}}
        void Awake()
        {
            Instance=this;sky=new Material(RenderSettings.skybox);RenderSettings.skybox=sky;
            CreateEquipmentEnvironment();
            cloudMaterial=new Material(cloudMaterial);
            var dome=new GameObject("Moving cloud canopy");clouds=dome.transform;clouds.SetParent(transform);
            dome.AddComponent<MeshFilter>().sharedMesh=Dome();var renderer=dome.AddComponent<MeshRenderer>();renderer.sharedMaterial=cloudMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var emitter=new GameObject("Local rainfall");emitter.transform.SetParent(transform);rain=emitter.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main;main.loop=true;main.startLifetime=1.2f;main.startSpeed=0;main.startSize=.025f;main.startColor=new Color(.74f,.83f,.88f,.4f);main.maxParticles=4000;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=false;
            var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(28,2,28);
            var velocity=rain.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=-2.7f;velocity.y=-20;velocity.z=.5f;
            var emission=rain.emission;emission.rateOverTime=0;
            var pr=rain.GetComponent<ParticleSystemRenderer>();pr.renderMode=ParticleSystemRenderMode.Stretch;pr.lengthScale=2;pr.velocityScale=.025f;pr.sharedMaterial=rainMaterial;pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;
            rain.Play();
        }
        void LateUpdate()
        {
            var camera=Camera.main;if(!camera)return;
            if(!camera.GetComponent<CinematicView>()){var effect=camera.gameObject.AddComponent<CinematicView>();effect.material=gradeMaterial;}
            float targetCloud=Mode==0?.15f:Mode==1?.72f:.97f,targetWet=Mode==2?1:0;
            cloudiness=Mathf.MoveTowards(cloudiness,targetCloud,Time.deltaTime*.11f);wetness=Mathf.MoveTowards(wetness,targetWet,Time.deltaTime*.13f);
            clouds.position=new Vector3(camera.transform.position.x,0,camera.transform.position.z);
            cloudMaterial.SetFloat("_Coverage",cloudiness);cloudMaterial.SetFloat("_Storm",wetness);
            cloudMaterial.SetColor("_CloudColor",Color.Lerp(new Color(.96f,.94f,.86f),new Color(.40f,.46f,.48f),wetness));
            float elevation=Mathf.Sin((Hour-6)/12*Mathf.PI)*55;float warmth=1-Mathf.InverseLerp(15,37,elevation);
            float daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-7,15,elevation));sun.transform.rotation=Quaternion.Euler(Mathf.Max(12,Mathf.Abs(elevation)),-38+(Hour-16.7f)*10,0);
            if(equipmentEnvironment)RenderSettings.reflectionIntensity=Mathf.Lerp(.06f,.85f,daylight)*(1-wetness*.3f);
            sun.color=Color.Lerp(new Color(1,.96f,.86f),new Color(1,.76f,.49f),warmth*.7f);
            sun.intensity=Mathf.Lerp(.14f,Mathf.Lerp(1.35f,.38f,cloudiness*.85f+wetness*.15f),daylight)+(Time.time<flashUntil?3:0);sun.color=Color.Lerp(new Color(.38f,.55f,.86f),sun.color,daylight);
            RenderSettings.ambientSkyColor=Color.Lerp(new Color(.48f,.55f,.63f),new Color(.37f,.45f,.52f),wetness);
            RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.35f,.38f,.34f),new Color(.32f,.38f,.39f),wetness);
            RenderSettings.fogColor=Color.Lerp(new Color(.69f,.76f,.75f),new Color(.40f,.48f,.52f),wetness);
            RenderSettings.fogDensity=Mathf.Lerp(.00085f,.0023f,wetness);
            sky.SetColor("_SkyTint",Color.Lerp(new Color(.48f,.57f,.67f),new Color(.37f,.42f,.47f),cloudiness*.75f));
            RenderSettings.ambientSkyColor*=Mathf.Lerp(.22f,1,daylight);RenderSettings.ambientEquatorColor*=Mathf.Lerp(.25f,1,daylight);RenderSettings.ambientGroundColor=new Color(.20f,.23f,.24f)*Mathf.Lerp(.2f,1,daylight);RenderSettings.fogColor=Color.Lerp(new Color(.055f,.09f,.16f),RenderSettings.fogColor,daylight);sky.SetFloat("_Exposure",Mathf.Lerp(.10f,Mathf.Lerp(1.12f,.72f,wetness),daylight));
            Shader.SetGlobalFloat("_ReserveWet",wetness);
            rain.transform.position=camera.transform.position+Vector3.up*10;
            int biome=ReserveBiomes.At(camera.transform.position.x,camera.transform.position.z);
            bool cold=biome==2||biome==3&&camera.transform.position.y>115,dry=biome==1;
            var rainMain=rain.main;rainMain.startLifetime=cold?4.2f:dry?2:1.2f;rainMain.startSize=cold?.085f:dry?.16f:.025f;rainMain.startColor=cold?new Color(.9f,.95f,1,.75f):dry?new Color(.69f,.54f,.35f,.24f):new Color(.74f,.83f,.88f,.4f);
            var velocity=rain.velocityOverLifetime;velocity.x=cold?-.6f:dry?7:-2.7f;velocity.y=cold?-3:dry?-1:-20;velocity.z=.5f;
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=cold||dry?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;
            var emission=rain.emission;emission.rateOverTime=wetness*(PlayerPrefs.GetInt("quality",1)==0?250:cold?700:dry?200:1100);
            if(dry){RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,new Color(.65f,.52f,.36f),wetness*.7f);}
            ReserveAudio.Weather(wetness*(dry?0:cold?.2f:1),Mathf.Max(cloudiness,wetness));
            if(WeatherState.Current&&lastLightning!=WeatherState.Current.Lightning.Value)
            {lastLightning=WeatherState.Current.Lightning.Value;if(wetness>.5f){flashUntil=Time.time+.16f;StartCoroutine(Thunder());}}
        }
        IEnumerator Thunder(){yield return new WaitForSeconds(1.1f);ReserveAudio.Thunder();}
        void CreateEquipmentEnvironment()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
            // A shared sky/ground environment gives metal a readable reflection
            // without dynamic probe renders or a point light on every pickup.
            const int size=32;equipmentEnvironment=new Cubemap(size,TextureFormat.RGBAHalf,true){name="Reserve diffuse sky reflection"};
            for(int face=0;face<6;face++)
            {
                var colors=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;
                    Vector3 ray=face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1);
                    float height=ray.normalized.y;
                    colors[y*size+x]=height<0?Color.Lerp(new Color(.33f,.35f,.31f),new Color(.13f,.15f,.12f),-height):Color.Lerp(new Color(.53f,.58f,.60f),new Color(.24f,.36f,.53f),height);
                }
                equipmentEnvironment.SetPixels(colors,(CubemapFace)face);
            }
            equipmentEnvironment.Apply(true,false);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=equipmentEnvironment;
        }
        public void Choose(int mode,bool cycle,float hour)
        {
            PreviewMode=mode;AutoCycle=cycle;PreviewHour=hour;
            if(WeatherState.Current)WeatherState.Current.Choose(mode,cycle,hour);
        }
        static Mesh Dome()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int r=0;r<=16;r++)for(int i=0;i<=64;i++)
            {
                float a=i/64f*Mathf.PI*2,polar=r/16f*Mathf.PI*.5f;
                var p=new Vector3(Mathf.Sin(polar)*Mathf.Cos(a),Mathf.Cos(polar)*.55f,Mathf.Sin(polar)*Mathf.Sin(a))*2100;
                v.Add(p);uv.Add(new Vector2(p.x/1100,p.z/1100));
                if(r<16&&i<64){int k=r*65+i;t.AddRange(new[]{k,k+1,k+65,k+1,k+66,k+65});}
            }
            var m=new Mesh{name="Cloud canopy"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();return m;
        }
        void OnDestroy(){Shader.SetGlobalFloat("_ReserveWet",0);if(equipmentEnvironment){RenderSettings.customReflectionTexture=null;Destroy(equipmentEnvironment);}if(sky)Destroy(sky);if(cloudMaterial)Destroy(cloudMaterial);if(Instance==this)Instance=null;}
    }
}
