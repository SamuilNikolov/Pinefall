using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Wildlife:NetworkBehaviour
    {
        void Awake(){NetworkSeed.Initialize(this);WorldInterest.Install(this);}
        public string species="Deer";public int kind,maxHealth=100;
        public NetworkVariable<int> Health=new NetworkVariable<int>(100),Harvested=new NetworkVariable<int>(0);
        public NetworkVariable<byte> State=new NetworkVariable<byte>(0);
        public NetworkVariable<float> MoveSpeed=new NetworkVariable<float>(0);
        public NetworkVariable<bool> Enraged=new NetworkVariable<bool>(false);public bool Aquatic{get{return species=="Shark";}}public bool NaturallyHostile{get{return naturallyHostile;}}
        public Vector3 HitCenter=new Vector3(0,.7f,0),HitSize=new Vector3(.8f,1.4f,1.6f);
        public bool Dead{get{return Health.Value<=0;}}public VisualRig rig;
        float nextRelevance,nextSimulation,simulatedAt;bool nearPlayer=true;float simulationStep;Vector3 target,home;float decision,fleeUntil,aggroUntil,warningUntil,nextAttack,diedAt;bool charging;System.Random rng;
        public float CorpseAge{get{return Dead?Time.time-diedAt:0;}}
        bool boar,small,naturallyHostile;bool Boar{get{return boar;}}
        bool Small{get{return small;}}
        public override void OnNetworkSpawn()
        {WildlifeHitboxes.Measure(this);string lower=species.ToLowerInvariant();boar=lower.Contains("boar");small=lower.Contains("hare")||kind==1;naturallyHostile=boar||lower.Contains("wolf")||lower.Contains("bear")||Aquatic;rig=GetComponentInChildren<VisualRig>();ReserveSession.Instance.Animals.Add(this);gameObject.AddComponent<WildlifeSound>();PresentationBudget.RegisterWildlife(this);simulatedAt=Time.time;home=target=transform.position;rng=new System.Random((int)NetworkObjectId*971+ReserveWorld.Seed);decision=Time.time+(float)rng.NextDouble();if(IsServer)Health.Value=maxHealth;}
        public override void OnNetworkDespawn(){WorldInterest.Forget(NetworkObject);if(ReserveSession.Instance)ReserveSession.Instance.Animals.Remove(this);}
        public bool Sees(Ranger p)
        {
            var delta=p.transform.position-transform.position;float range=Enraged.Value?95:kind==2?58:kind==3?22:p.Crouched.Value?14:p.Velocity.Value>5?60:35;
            if(delta.sqrMagnitude>range*range)return false;if(delta.sqrMagnitude>100&&Vector3.Dot(transform.forward,delta.normalized)<-.25f)return false;
            Vector3 start=transform.position+Vector3.up*(Small?.4f:.9f),end=p.transform.position+Vector3.up*1.05f;RaycastHit hit;
            return !Physics.Raycast(start,(end-start).normalized,out hit,Vector3.Distance(start,end),~((1<<8)|(1<<10)),QueryTriggerInteraction.Collide)||hit.collider.GetComponentInParent<Ranger>()==p;
        }
        void Update()
        {
            if(!IsSpawned)return;MutationTick();if(rig&&rig.gameObject.activeInHierarchy)rig.Pose(MoveSpeed.Value,Dead,State.Value==4&&MoveSpeed.Value<.15f);FrenzyVisual();if(!IsServer||Dead)return;Enraged.Value=WildfrontMatch.Current&&WildfrontMatch.Current.Closing;if(Aquatic){AquaticTick();return;}if(Time.time>nextRelevance){nextRelevance=Time.time+.65f+(NetworkObjectId%7)*.02f;nearPlayer=false;foreach(var human in Ranger.Players)if(human&&human.Alive&&(human.transform.position-transform.position).sqrMagnitude<420*420){nearPlayer=true;break;}}if(!nearPlayer&&Time.time<nextSimulation)return;simulationStep=nearPlayer?Time.deltaTime:Mathf.Min(.5f,Time.time-simulatedAt);simulatedAt=Time.time;nextSimulation=Time.time+.25f;
            if(Time.time>=decision)
            {
                decision=Time.time+.45f;Ranger threat=null;float distance=9999;
                foreach(var p in Ranger.Players){if(!p||!p.IsSpawned||!p.Fighting)continue;float d=Vector3.Distance(transform.position,p.transform.position);if(d<distance&&Sees(p)){distance=d;threat=p;}else if(!threat&&p.Velocity.Value>5&&d<65&&Time.time>fleeUntil&&Time.time>aggroUntil){target=p.transform.position;if(NaturallyHostile||Enraged.Value){State.Value=6;aggroUntil=Time.time+5;}else Flee(p.transform.position);}}
                var match=WildfrontMatch.Current;bool storm=match&&match.Active&&new Vector2(transform.position.x-match.Center.Value.x,transform.position.z-match.Center.Value.z).magnitude>match.Radius.Value-40;
                if(storm&&!Enraged.Value){target=match.EscapePoint(transform.position);State.Value=2;fleeUntil=Time.time+3;home=target;}
                else if(threat&&(NaturallyHostile||Enraged.Value)&&(Enraged.Value||Health.Value>maxHealth*.28f)&&(Enraged.Value||Vector3.Distance(transform.position,home)<190))
                {
                    target=threat.transform.position;
                    if(kind==3&&State.Value!=4){if(State.Value!=5){warningUntil=Time.time+1.7f;State.Value=5;}if(distance<8||Time.time>warningUntil){State.Value=4;charging=true;aggroUntil=Time.time+4;}}
                    else if(kind!=3||Enraged.Value){State.Value=4;aggroUntil=Time.time+8;}
                    if(State.Value==4&&distance<(kind==3?2.35f:1.85f)&&Time.time>nextAttack)
                    {nextAttack=Time.time+(kind==3?1.7f:1.35f);if(Sees(threat))threat.TakeDamageServer(Mathf.RoundToInt((kind==3?22:16)*(Boss.Value?1.7f:1)),-2);if(kind==3){State.Value=5;warningUntil=Time.time+1.5f;charging=false;}}
                }
                else if(threat)Flee(threat.transform.position);
                else if(Time.time>fleeUntil&&Time.time>aggroUntil)
                {
                    if(State.Value==2||State.Value==4||State.Value==5||State.Value==6){State.Value=0;charging=false;target=transform.position;}
                    if(Vector3.Distance(transform.position,target)<3)
                    {if(rng.NextDouble()<.57){State.Value=0;decision=Time.time+1.8f+(float)rng.NextDouble()*4;}else{State.Value=1;target=home+new Vector3((float)(rng.NextDouble()-.5)*90,0,(float)(rng.NextDouble()-.5)*90);target.y=ReserveWorld.Height(target.x,target.z);}}
                }
            }
            float speed=State.Value==4?(kind==3?charging?7.8f:3.8f:6.7f):State.Value==2?(Small?9.2f:8.2f):State.Value==6?1.8f:State.Value==1?Small?.9f:1.15f:0;
            speed*=Boss.Value?1.15f:1;var dir=target-transform.position;dir.y=0;
            if(State.Value==5&&dir.sqrMagnitude>1)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),simulationStep*2);
            if(speed>0&&dir.sqrMagnitude>(State.Value==4?2.0f:1))
            {
                Vector3 forward=dir.normalized,next=transform.position+forward*speed*simulationStep;
                bool blocked=Physics.SphereCast(transform.position+Vector3.up*.55f,.16f,forward,out RaycastHit obstacle,1.5f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
                if(blocked||ReserveWater.Depth(next.x,next.z)>1.1f||Mathf.Abs(ReserveWorld.Height(next.x,next.z)-transform.position.y)>Mathf.Max(.15f,speed*simulationStep*.7f)){forward=Quaternion.Euler(0,NetworkObjectId%2==0?72:-72,0)*forward;next=transform.position+forward*speed*simulationStep;}
                next.x=Mathf.Clamp(next.x,-ReserveWorld.Edge,ReserveWorld.Edge);next.z=Mathf.Clamp(next.z,-ReserveWorld.Edge,ReserveWorld.Edge);next.y=ReserveWorld.Height(next.x,next.z);transform.position=next;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(forward),simulationStep*4);
            }
            else speed=0;if(Mathf.Abs(MoveSpeed.Value-speed)>.1f)MoveSpeed.Value=speed;
        }
        public void HearShot(Vector3 source)
        {if(!IsServer||Dead)return;float d=Vector3.Distance(transform.position,source);if(NaturallyHostile||Enraged.Value){if(d<110&&State.Value!=4){target=source;State.Value=6;aggroUntil=Time.time+7;}}else if(d<150)Flee(source);}
        public void Flee(Vector3 source)
        {if(!IsServer||Dead)return;Vector3 dir=transform.position-source;dir.y=0;if(dir.sqrMagnitude<.1f)dir=transform.forward;target=transform.position+dir.normalized*(Small?85:120);State.Value=2;fleeUntil=Time.time+8;decision=Time.time+.5f;}
        public bool Hit(int damage,Vector3 source)
        {
            if(!IsServer||Dead)return false;Health.Value=Mathf.Max(0,Health.Value-damage);
            if(Dead){State.Value=3;MoveSpeed.Value=0;diedAt=Time.time;Debug.Log("PINEFALL_ANIMAL_DOWN "+NetworkObjectId);return true;}
            if((NaturallyHostile||Enraged.Value)&&(Enraged.Value||Health.Value>maxHealth*.28f)){target=source;State.Value=4;charging=true;aggroUntil=Time.time+12;}else Flee(source);
            if(kind<2)foreach(var a in ReserveSession.Instance.Animals)if(a&&a!=this&&!a.Dead&&a.kind<2&&(a.transform.position-transform.position).sqrMagnitude<32*32)a.Flee(source);return false;
        }
        public bool Harvest(){if(!IsServer||!Dead||Harvested.Value!=0)return false;Harvested.Value=1;return true;}
        public void ResetWildlife(){if(!IsServer)return;Health.Value=maxHealth;Harvested.Value=0;State.Value=0;MoveSpeed.Value=0;transform.position=home;target=home;decision=Time.time+2;aggroUntil=fleeUntil=0;}
    }
}
