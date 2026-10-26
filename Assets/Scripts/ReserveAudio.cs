using UnityEngine;

namespace Pinefall
{
    public sealed class ReserveAudio : MonoBehaviour
    {
        static ReserveAudio instance;
        AudioClip rifle,carbine,shotgun,foot,gravel,sandStep,snowStep,thunder;
        AudioSource ambient,rainSource;

        public static void Ensure()
        {
            if(instance)return;var g=new GameObject("Reserve soundscape");instance=g.AddComponent<ReserveAudio>();
            instance.Build();Soundscape.Ensure();
        }
        void Build()
        {
            rifle=Shot("Rifle",.75f,48);carbine=Shot("Carbine",.42f,75);shotgun=Shot("Shotgun",.8f,34);
            var rng=new System.Random(43);int rate=22050;
            var data=new float[rate*8];float low=0;
            for(int i=0;i<data.Length;i++){low=Mathf.Lerp(low,(float)(rng.NextDouble()*2-1),.015f);data[i]=low*.2f*(.75f+.25f*Mathf.Sin(i/(float)rate*.8f));}
            var wind=AudioClip.Create("Wind through pines",data.Length,1,rate,false);wind.SetData(data,0);
            ambient=gameObject.AddComponent<AudioSource>();ambient.clip=wind;ambient.loop=true;ambient.volume=.12f*Soundscape.Level("Ambience");ambient.Play();
            var step=new float[3200];low=0;
            for(int i=0;i<step.Length;i++){low=Mathf.Lerp(low,(float)(rng.NextDouble()*2-1),.35f);step[i]=low*Mathf.Exp(-i/550f)*.6f;}
            foot=AudioClip.Create("Gravel footstep",step.Length,1,rate,false);foot.SetData(step,0);
            var crunch=new float[4400];for(int i=0;i<crunch.Length;i++){float e=Mathf.Exp(-i/850f);float noise=(float)(rng.NextDouble()*2-1);crunch[i]=noise*e*(.25f+.3f*Mathf.Abs(Mathf.Sin(i*.071f)));}
            gravel=AudioClip.Create("Trail gravel",crunch.Length,1,rate,false);gravel.SetData(crunch,0);
            var rainfall=new float[rate*9];low=0;
            for(int i=0;i<rainfall.Length;i++){float n=(float)(rng.NextDouble()*2-1);low=Mathf.Lerp(low,n,.14f);rainfall[i]=(low*.5f+n*.12f)*.45f;}
            var rainClip=AudioClip.Create("Rain in the canopy",rainfall.Length,1,rate,false);rainClip.SetData(rainfall,0);rainSource=gameObject.AddComponent<AudioSource>();rainSource.clip=rainClip;rainSource.loop=true;rainSource.volume=0;rainSource.Play();
            var rumble=new float[rate*5];low=0;
            for(int i=0;i<rumble.Length;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)(rng.NextDouble()*2-1),.045f);float envelope=Mathf.Min(t*12,1)*Mathf.Exp(-t*.85f);rumble[i]=(low*.9f+Mathf.Sin(t*45)*.03f)*envelope;}
            thunder=AudioClip.Create("Distant thunder",rumble.Length,1,rate,false);thunder.SetData(rumble,0);
            sandStep=Foley("Soft sand",.10f,650,.19f);snowStep=Foley("Packed snow",.25f,950,.25f);
            AudioListener.volume=PlayerPrefs.GetFloat("volume",.65f);
        }
        AudioClip Shot(string name,float seconds,float bass)
        {
            int rate=22050;var d=new float[(int)(rate*seconds)];var rng=new System.Random((int)bass);float noise=0;
            for(int i=0;i<d.Length;i++)
            {
                float t=i/(float)rate;noise=Mathf.Lerp(noise,(float)(rng.NextDouble()*2-1),.55f);
                float crack=noise*Mathf.Exp(-t*85)*.9f;
                float boom=(noise*.6f+Mathf.Sin(t*bass*6.283f)*.25f)*Mathf.Exp(-t*13);
                float tail=noise*Mathf.Exp(-t*6)*.1f;d[i]=Mathf.Clamp(crack+boom+tail,-1,1);
            }
            var clip=AudioClip.Create(name,d.Length,1,rate,false);clip.SetData(d,0);return clip;
        }
        public static void Gun(Vector3 pos,int weapon,float volume)
        {
            Soundscape.Gun(pos,weapon,volume);
        }
        public static void Step(Vector3 pos,float volume,bool onGravel=false,bool local=false){Soundscape.Step(pos,volume,onGravel,local);}
        public static void Cue(string bank,Vector3 pos,float volume){Soundscape.Cue(bank,pos,volume);}
        AudioClip Foley(string name,float filter,float fall,float gain){var rng=new System.Random(name.Length*71);var samples=new float[4200];float low=0;for(int i=0;i<samples.Length;i++){low=Mathf.Lerp(low,(float)rng.NextDouble()*2-1,filter);samples[i]=low*Mathf.Exp(-i/fall)*gain;}var c=AudioClip.Create(name,samples.Length,1,22050,false);c.SetData(samples,0);return c;}
        public static void Weather(float wet,float cloud)
        {
            if(!instance)return;if(instance.rainSource)instance.rainSource.volume=wet*.30f*Soundscape.Level("Ambience");if(instance.ambient)instance.ambient.volume=(.12f+cloud*.12f)*Soundscape.Level("Ambience");
        }
        public static void Thunder(){if(instance)Play(instance.thunder,Camera.main?Camera.main.transform.position+Vector3.up*15:Vector3.zero,1,1,300);}
        static void Play(AudioClip clip,Vector3 pos,float volume,float pitch,float distance)
        {
            var g=new GameObject(clip.name);g.transform.position=pos;var source=g.AddComponent<AudioSource>();source.clip=clip;source.volume=volume;source.pitch=pitch;source.spatialBlend=1;source.minDistance=3;source.maxDistance=distance;source.rolloffMode=AudioRolloffMode.Linear;source.Play();Destroy(g,clip.length+1);
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
    public static class ReserveEffects
    {
        static Material dust;
        public static void Impact(Vector3 position,Vector3 normal,bool animal)
        {
            if(!dust)dust=WeaponView.Mat("Impact dust",new Color(.57f,.49f,.31f));
            var p=GameObject.CreatePrimitive(PrimitiveType.Sphere);p.name="Impact";Object.Destroy(p.GetComponent<Collider>());p.transform.position=position+normal*.025f;p.transform.localScale=Vector3.one*.09f;p.GetComponent<Renderer>().sharedMaterial=dust;
            Object.Destroy(p,1.5f);
            var line=new GameObject("Impact sparks");line.transform.position=position;var ps=line.AddComponent<ParticleSystem>();var main=ps.main;main.duration=.2f;main.loop=false;main.startLifetime=.4f;main.startSpeed=1.7f;main.startSize=.07f;main.startColor=new Color(.65f,.58f,.43f,.7f);main.maxParticles=9;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,7)});
            var renderer=line.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("Dust");ps.Play();Object.Destroy(line,1);
        }
    }
}
