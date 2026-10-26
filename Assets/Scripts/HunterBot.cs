using System.Linq;
using UnityEngine;
namespace Pinefall
{
    public sealed class HunterBot:MonoBehaviour
    {
        Ranger p;CharacterController controller;Vector3 goal;float think,shot,vertical;
        void Start(){p=GetComponent<Ranger>();controller=GetComponent<CharacterController>();goal=transform.position;}
        void Update()
        {
            if(!p.IsServer||!p.Alive||!WildfrontMatch.Current||!WildfrontMatch.Current.Active)return;
            if(!p.CanAct){p.Velocity.Value=0;return;}
            if(Time.time>think)
            {
                think=Time.time+.35f;
                if(p.Hunger.Value<65&&p.Food.Value>0)p.EatServerRpc();if(p.Health.Value<60&&p.Medkits.Value>0)p.HealServerRpc();if(p.ArmorTier.Value>0&&p.Armor.Value<Ranger.ArmorMaximum(p.ArmorTier.Value)-25&&p.ArmorPlates.Value>0)p.PlateServerRpc();
                var match=WildfrontMatch.Current;
                var enemy=Ranger.Players.Where(q=>q&&q.Alive&&q.Team.Value!=p.Team.Value).OrderBy(q=>(q.transform.position-transform.position).sqrMagnitude).FirstOrDefault();
                bool engaged=false;
                if(enemy&&Vector3.Distance(transform.position,enemy.transform.position)<110)
                {
                    Vector3 eye=transform.position+Vector3.up*1.5f,direction=enemy.transform.position+Vector3.up*1.1f-eye;RaycastHit hit;
                    if(Physics.Raycast(eye,direction.normalized,out hit,115,~(1<<10),QueryTriggerInteraction.Collide)&&hit.collider.GetComponentInParent<Ranger>()==enemy)
                    {goal=enemy.transform.position;engaged=true;if(Time.time>shot){shot=Time.time+.45f;p.BotShoot(direction+UnityEngine.Random.insideUnitSphere*2.2f);}}
                }
                var rescue=Ranger.Players.Where(q=>q&&q!=p&&q.Alive&&q.Downed.Value&&q.Team.Value==p.Team.Value).OrderBy(q=>(q.transform.position-transform.position).sqrMagnitude).FirstOrDefault();
                if(!engaged&&rescue&&(rescue.transform.position-transform.position).sqrMagnitude<80*80){goal=rescue.transform.position;if((goal-transform.position).sqrMagnitude<9)p.RevivePulseServerRpc(rescue.NetworkObjectId,true);engaged=true;}
                if(!engaged)
                {
                    if(Vector2.Distance(new Vector2(transform.position.x,transform.position.z),new Vector2(match.NextCenter.Value.x,match.NextCenter.Value.z))>match.NextRadius.Value*.8f)goal=match.NextCenter.Value;
                    else if(p.Hunger.Value<65)
                    {var food=WorldLoot.All.Where(l=>l&&l.Kind.Value==5).OrderBy(l=>(l.transform.position-transform.position).sqrMagnitude).FirstOrDefault();if(food){goal=food.transform.position;if(Vector3.Distance(goal,transform.position)<3)p.ScavengeServerRpc(food.NetworkObjectId);}else{var a=ReserveSession.Instance.Animals.Where(a=>a&&!a.Dead).OrderBy(a=>(a.transform.position-transform.position).sqrMagnitude).FirstOrDefault();if(a){goal=a.transform.position;if(Vector3.Distance(goal,transform.position)<55)p.BotShoot(a.transform.position+Vector3.up*.6f-transform.position-Vector3.up*1.5f);}}}
                    else if(Vector3.Distance(goal,transform.position)<4)goal=transform.position+UnityEngine.Random.insideUnitSphere*65;
                }
                if(p.Ammo.Value<1)p.ReloadServerRpc();
            }
            var dir=goal-transform.position;dir.y=0;float speed=p.ReviveTarget.Value!=ulong.MaxValue?0:dir.magnitude>12?4:dir.magnitude>3?2:0;
            if(Physics.Raycast(transform.position+Vector3.up,dir.normalized,2.5f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))dir=Quaternion.Euler(0,70,0)*dir;
            if(dir.sqrMagnitude>.1f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),Time.deltaTime*4);
            vertical=controller.isGrounded?-2:vertical-18*Time.deltaTime;if(!p.SwimServerBot(dir.normalized*speed))controller.Move((dir.normalized*speed+Vector3.up*vertical)*Time.deltaTime);else vertical=0;p.Velocity.Value=speed;
            if(transform.position.y<ReserveWorld.Height(transform.position.x,transform.position.z)-2){controller.enabled=false;transform.position=ReserveWorld.OnGround(transform.position.x,transform.position.z,.2f);controller.enabled=true;}
        }
    }
}
