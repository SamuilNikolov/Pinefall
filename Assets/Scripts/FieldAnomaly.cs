using UnityEngine;
using UnityEngine.Rendering;

namespace Pinefall
{
    // Uses the existing replicated cache item: no hidden client-only reward state.
    public sealed class FieldAnomaly : MonoBehaviour
    {
        WorldLoot cache; float nextPulse;
        void Awake(){cache=GetComponent<WorldLoot>();nextPulse=Time.time+2;}
        void Update()
        {
            if(!cache||!cache.IsServer||!cache.IsSpawned||Time.time<nextPulse)return;
            nextPulse=Time.time+2;
            if(!WildfrontMatch.Current||!WildfrontMatch.Current.Active)return;
            foreach(var player in Ranger.Players)
                if(player&&player.Fighting&&!player.CurrentVehicle&&(player.transform.position-transform.position).sqrMagnitude<7*7)
                    player.TakeDamageServer(4,-3);
        }
        public static void Populate(System.Random random)
        {
            int count=RoundArena.Size<3000?6:RoundArena.Size<5000?10:14;
            for(int site=0;site<count;site++)for(int attempt=0;attempt<80;attempt++)
            {
                float x=((float)random.NextDouble()-.5f)*(RoundArena.Size-200),z=((float)random.NextDouble()-.5f)*(RoundArena.Size-200);
                if(ReserveWater.HasSurface(x,z)||ReserveWorld.Clearing(x,z))continue;
                float h=ReserveWorld.Height(x,z);
                if(Mathf.Abs(ReserveWorld.Height(x+3,z)-h)+Mathf.Abs(ReserveWorld.Height(x,z+3)-h)>2.4f)continue;
                bool close=false;foreach(var item in WorldLoot.All)if(item&&item.Kind.Value==11&&item.Item.Value==1&&(item.transform.position-new Vector3(x,h,z)).sqrMagnitude<180*180){close=true;break;}
                if(close)continue;
                ExpeditionLoot.Spawn(new Vector3(x,h+.4f,z),11,1,3,1);break;
            }
        }
        public static void CreateVisual(Transform parent)
        {
            var emitter=new GameObject("Charged anomaly motes");emitter.transform.SetParent(parent,false);
            var particles=emitter.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.playOnAwake=false;main.maxParticles=48;main.startLifetime=3;main.startSpeed=.55f;main.startSize=.055f;main.startColor=new Color(.39f,.65f,1,.65f);
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=3.4f;shape.rotation=new Vector3(90,0,0);
            var emission=particles.emission;emission.rateOverTime=12;
            var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.orbitalY=.6f;velocity.y=.45f;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=ReserveEffects.ParticleSurface;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;particles.Play();
        }
    }
}
