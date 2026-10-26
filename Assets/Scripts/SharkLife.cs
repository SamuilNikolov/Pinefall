using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Wildlife
    {
        Renderer[] frenzyRenderers;MaterialPropertyBlock frenzyBlock;bool lastFrenzy,lastBoss;bool colored;Vector3 aquaticTarget;
        void FrenzyVisual(){if(colored&&lastFrenzy==Enraged.Value&&lastBoss==Boss.Value)return;lastBoss=Boss.Value;lastFrenzy=Enraged.Value;colored=true;if(frenzyRenderers==null)frenzyRenderers=GetComponentsInChildren<Renderer>(true);if(frenzyBlock==null)frenzyBlock=new MaterialPropertyBlock();foreach(var r in frenzyRenderers){if(Enraged.Value){r.GetPropertyBlock(frenzyBlock);var mat=r.sharedMaterial;Color normal=mat&&mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor"):mat&&mat.HasProperty("_Color")?mat.color:Color.white;Color tint=Color.Lerp(normal,Boss.Value?new Color(1,.53f,.17f):new Color(.72f,.43f,.78f),Boss.Value?.65f:.40f);frenzyBlock.SetColor("_BaseColor",tint);frenzyBlock.SetColor("_Color",tint);r.SetPropertyBlock(frenzyBlock);}else r.SetPropertyBlock(null);}}
        void AquaticTick()
        {
            var prey=Ranger.Players.Where(p=>p&&p.Fighting&&p.Swimming.Value&&!p.CurrentVehicle&&(p.transform.position-transform.position).sqrMagnitude<65*65).OrderBy(p=>(p.transform.position-transform.position).sqrMagnitude).FirstOrDefault();
            if(prey){aquaticTarget=prey.transform.position;State.Value=4;if((prey.transform.position-transform.position).sqrMagnitude<3*3&&Time.time>nextAttack){nextAttack=Time.time+1.5f;prey.TakeDamageServer(18,-2);}}
            else if(Time.time>decision||(aquaticTarget-transform.position).sqrMagnitude<16){decision=Time.time+5;float angle=Random.value*Mathf.PI*2;aquaticTarget=home+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*Random.Range(20,90);State.Value=1;}
            Vector3 direction=aquaticTarget-transform.position;direction.y=0;float speed=prey?7:2.2f;var next=transform.position+direction.normalized*speed*Time.deltaTime;float water;if(ReserveWater.Depth(next.x,next.z)>1.5f&&ReserveWater.Surface(next.x,next.z,out water)){next.y=water-.95f;transform.position=next;if(direction.sqrMagnitude>.1f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),Time.deltaTime*3);}else{aquaticTarget=home;decision=0;}MoveSpeed.Value=speed;
        }
    }
    public sealed class SharkDirector:MonoBehaviour
    {
        float next;void Start(){Spawn();}void Update(){if(Time.time>next){next=Time.time+25;Spawn();}}
        public void Spawn(){if(!ReserveSession.Instance.Network.IsServer)return;foreach(var lake in ReserveWater.Lakes){int count=ReserveSession.Instance.Animals.Count(a=>a&&a.Aquatic&&!a.Dead&&Vector2.Distance(new Vector2(a.transform.position.x,a.transform.position.z),lake.center)<lake.radius.x+100);for(int i=count;i<3;i++){float angle=i*2.1f;float x=lake.center.x+Mathf.Cos(angle)*lake.radius.x*.4f,z=lake.center.y+Mathf.Sin(angle)*lake.radius.y*.4f;var g=Instantiate(Resources.Load<GameObject>("Shark"),new Vector3(x,lake.level-.95f,z),Quaternion.Euler(0,i*120,0));g.GetComponent<NetworkObject>().Spawn();}}}
    }
}
