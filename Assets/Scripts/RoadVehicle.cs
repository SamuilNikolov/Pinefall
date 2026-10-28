using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class RoadVehicle:NetworkBehaviour
    {
        void Awake(){NetworkSeed.Initialize(this);WorldInterest.Install(this);}
        public static readonly System.Collections.Generic.List<RoadVehicle> All=new System.Collections.Generic.List<RoadVehicle>();
        public const ulong Empty=ulong.MaxValue;static readonly float[] topSpeeds={29f,43f,35f,25f,29f,48f};
        public static readonly string[] Names={"Estate wagon","Dune buggy","Service pickup","Utility van","Motor launch","Trail motorcycle"};
        public NetworkVariable<int> Type=new NetworkVariable<int>(0),Condition=new NetworkVariable<int>(65),Tune=new NetworkVariable<int>(0);
        public NetworkVariable<float> Fuel=new NetworkVariable<float>(35),Charge=new NetworkVariable<float>(60),Speed=new NetworkVariable<float>(0);
        public NetworkVariable<ulong> Seat0=new NetworkVariable<ulong>(Empty),Seat1=new NetworkVariable<ulong>(Empty),Seat2=new NetworkVariable<ulong>(Empty),Seat3=new NetworkVariable<ulong>(Empty);
        public NetworkVariable<float> Steering=new NetworkVariable<float>(0);float parkedAt;float heading,wheelAngle;float throttle,steer,lastInput,impactAt;bool brake,boost;
        readonly RaycastHit[] roadHits = new RaycastHit[32];
        Vector3 previousPosition; Quaternion previousRotation; bool poseReady;
        float RenderFraction { get { return Mathf.Clamp01((Time.time-Time.fixedTime)/Time.fixedDeltaTime); } }
        public Vector3 VisualPosition { get { return IsServer&&poseReady?Vector3.Lerp(previousPosition,transform.position,RenderFraction):transform.position; } }
        public Quaternion VisualRotation { get { return IsServer&&poseReady?Quaternion.Slerp(previousRotation,transform.rotation,RenderFraction):transform.rotation; } }
        public Vector3 VisualSeat(int seat) { return VisualPosition+VisualRotation*transform.InverseTransformPoint(SeatPosition(seat)); }
        public int Capacity {get{return Type.Value==1||Type.Value==5?2:4;}}
        public string Label {get{return Names[Mathf.Clamp(Type.Value,0,5)]+(Condition.Value<=0?" · engine dead":Fuel.Value<=0?" · empty tank":Condition.Value<40?" · damaged":"");}}
        public ulong Occupant(int index){return index==0?Seat0.Value:index==1?Seat1.Value:index==2?Seat2.Value:Seat3.Value;}
        void SetSeat(int i,ulong value){if(i==0)Seat0.Value=value;else if(i==1)Seat1.Value=value;else if(i==2)Seat2.Value=value;else Seat3.Value=value;}
        public override void OnNetworkSpawn(){All.Add(this);heading=transform.eulerAngles.y;var visual=gameObject.AddComponent<VehicleVisual>();PresentationBudget.Register(visual,480);var box=GetComponent<BoxCollider>();if(Type.Value==4){box.size=new Vector3(2,1.1f,4.6f);box.center=new Vector3(0,.1f,0);}else if(Type.Value==5){box.size=new Vector3(.70f,1.35f,2.5f);box.center=new Vector3(0,.25f,0);}}
        public override void OnNetworkDespawn(){WorldInterest.Forget(NetworkObject);All.Remove(this);}
        public Vector3 SeatPosition(int index){return Type.Value==5?transform.TransformPoint(new Vector3(0,-.55f,index==0?-.1f:-.65f)):transform.TransformPoint(new Vector3(index%2==0?-.43f:.43f,-.45f,index<2?.32f:-.75f));}
        public bool Enter(Ranger player)
        {
            if(!IsServer||!player.Alive||player.VehicleId.Value!=Empty||Vector3.Distance(player.transform.position,transform.position)>5||Condition.Value<=0||!player.CanAct)return false;
            var driver=Ranger.Players.FirstOrDefault(p=>p&&p.NetworkObjectId==Seat0.Value);
            if(driver&&driver.Team.Value!=player.Team.Value)return false;
            for(int i=0;i<Capacity;i++)if(Occupant(i)==Empty){SetSeat(i,player.NetworkObjectId);player.VehicleId.Value=NetworkObjectId;player.VehicleSeat.Value=i;return true;}return false;
        }
        public void Remove(Ranger player){if(!IsServer)return;for(int i=0;i<4;i++)if(Occupant(i)==player.NetworkObjectId)SetSeat(i,Empty);player.VehicleId.Value=Empty;player.VehicleSeat.Value=-1;}
        public Vector3 ExitPoint(int seat)
        {
            float side=seat%2==0?-1:1;
            foreach(var local in new[]{new Vector3(side*2.2f,0,0),new Vector3(-side*2.2f,0,0),new Vector3(0,0,-3)})
            {Vector3 p=transform.TransformPoint(local);float water;if(Type.Value==4&&ReserveWater.Surface(p.x,p.z,out water))return new Vector3(p.x,water-.8f,p.z);p=ReserveWorld.OnGround(p.x,p.z,.2f);if(!Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.45f,.34f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))return p;}
            return ReserveWorld.OnGround(transform.position.x,transform.position.z-3,.4f);
        }
        [ServerRpc(RequireOwnership=false,Delivery=RpcDelivery.Unreliable)]public void DriveServerRpc(float gas,float turn,bool stop,bool turbo,ServerRpcParams rpc=default)
        {
            if(float.IsNaN(gas)||float.IsInfinity(gas)||float.IsNaN(turn)||float.IsInfinity(turn))return;
            var p=Ranger.Players.FirstOrDefault(q=>q&&q.OwnerClientId==rpc.Receive.SenderClientId&&!q.IsBot.Value&&q.NetworkObjectId==Seat0.Value);
            if(!p||!p.Alive||!p.CanAct||p.VehicleId.Value!=NetworkObjectId)return;
            throttle=Mathf.Clamp(gas,-1,1);steer=Mathf.Clamp(turn,-1,1);brake=stop;boost=turbo;lastInput=Time.time;
        }
        void FixedUpdate()
        {
            if(!IsServer||!IsSpawned)return;
            previousPosition=transform.position;previousRotation=transform.rotation;poseReady=true;
            for(int i=0;i<4;i++)if(Occupant(i)!=Empty&&!Ranger.Players.Any(p=>p&&p.NetworkObjectId==Occupant(i)&&p.Alive))SetSeat(i,Empty);
            if(Time.time-lastInput>.35f||Seat0.Value==Empty){throttle=0;steer=0;brake=false;boost=false;}
            bool parked=Seat0.Value==Empty&&Seat1.Value==Empty&&Seat2.Value==Empty&&Seat3.Value==Empty&&Mathf.Abs(Speed.Value)<.02f&&throttle==0;
            if(parked){if(parkedAt==0)parkedAt=Time.time;return;}if(parkedAt>0){Charge.Value=Mathf.Min(100,Charge.Value+(Time.time-parkedAt)*3.5f);parkedAt=0;}
            float speed=Speed.Value;
            float top=topSpeeds[Mathf.Clamp(Type.Value,0,5)]*(.3f+.7f*Condition.Value/100f)*(1+Tune.Value*.12f);
            bool turbo=boost&&Charge.Value>1&&Fuel.Value>0&&Condition.Value>20&&throttle>0;
            if(turbo){top*=1.4f;Charge.Value=Mathf.Max(0,Charge.Value-Time.fixedDeltaTime*18);}else Charge.Value=Mathf.Min(100,Charge.Value+Time.fixedDeltaTime*3.5f);
            float target=Condition.Value<=0||Fuel.Value<=0?0:throttle>=0?top*throttle:top*.33f*throttle;
            if(Type.Value!=4)
            {
                float rise=Vector3.Dot(transform.forward,Vector3.up)*Mathf.Sign(target);
                target*=Mathf.Lerp(1,.62f,Mathf.Clamp01(rise/.75f));
            }
            speed=Mathf.MoveTowards(speed,target,Time.fixedDeltaTime*(brake?34:throttle!=0?16f:3.8f));
            wheelAngle=Mathf.MoveTowards(wheelAngle,steer*Mathf.Lerp(34,19,Mathf.Clamp01(Mathf.Abs(speed)/top)),Time.fixedDeltaTime*190);Steering.Value=wheelAngle;
            float yawRate=Type.Value==4?steer*90*Mathf.Clamp01(Mathf.Abs(speed)/3)*Mathf.Sign(speed):Mathf.Clamp(speed/2.45f*Mathf.Tan(wheelAngle*Mathf.Deg2Rad)*Mathf.Rad2Deg,-105,105);
            heading=Mathf.Repeat(heading+yawRate*Time.fixedDeltaTime,360);Quaternion rotation=Quaternion.Euler(0,heading,0);Vector3 delta=rotation*Vector3.forward*speed*Time.fixedDeltaTime;
            if(Mathf.Abs(speed)>.2f)
            {
                bool blocked=false;
                int hits=Physics.BoxCastNonAlloc(transform.position+Vector3.up*.45f,new Vector3(Type.Value==5?.30f:Type.Value==4?.90f:.74f,.42f,Type.Value==5?1.05f:Type.Value==4?2.05f:1.55f),delta.normalized,roadHits,transform.rotation,delta.magnitude+.12f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
                for(int i=0;i<hits;i++)
                {
                    var hit=roadHits[i];var collider=hit.collider;
                    if(!collider||collider.transform.IsChildOf(transform)||collider is TerrainCollider)continue;
                    bool bridge=collider.GetComponentInParent<BridgeSurface>();
                    bool roadSurface=(bridge&&!collider.name.Contains("rail")&&!collider.name.Contains("post"))||collider.GetComponentInParent<BuildPiece>()||collider.name.Contains("Cavern");
                    if(roadSurface&&hit.normal.y>.60f)continue;
                    blocked=true;break;
                }
                if(hits==roadHits.Length)blocked=true;
                if(blocked){if(Mathf.Abs(speed)>5&&Time.time>impactAt){Condition.Value=Mathf.Max(0,Condition.Value-Mathf.RoundToInt(Mathf.Abs(speed)));impactAt=Time.time+1;}speed=0;delta=Vector3.zero;}
            }
            Vector3 next=transform.position+delta;next.x=Mathf.Clamp(next.x,-ReserveWorld.Edge,ReserveWorld.Edge);next.z=Mathf.Clamp(next.z,-ReserveWorld.Edge,ReserveWorld.Edge);
            float y=Surface(next)+.65f;if(Type.Value==4){if(!ReserveWater.HasSurface(next.x,next.z)||ReserveWater.Depth(next.x,next.z)<.35f){next=transform.position;speed=0;}y=ReserveWater.Level(next.x,next.z)+.23f;}
            // Continuous grades remain driveable. Reject actual ledges/cliffs, not
            // every uphill step above the former 33-degree hard cutoff.
            if(Type.Value!=4&&!CanTraverse(y-transform.position.y,delta.magnitude)){speed=Mathf.MoveTowards(speed,0,Time.fixedDeltaTime*35);next=transform.position;y=transform.position.y;}
            next.y=Type.Value==4?Mathf.MoveTowards(transform.position.y,y,Time.fixedDeltaTime*5):y;
            var right=rotation*Vector3.right*.85f;var forward=rotation*Vector3.forward*1.3f;
            float roll=(Surface(next+right)-Surface(next-right))/1.7f;
            float pitch=(Surface(next+forward)-Surface(next-forward))/2.6f;
            Vector3 normal=(Vector3.up-rotation*new Vector3(roll,0,pitch)).normalized;
            normal=Vector3.RotateTowards(Vector3.up,normal,45*Mathf.Deg2Rad,0);if(Type.Value==4)normal=Vector3.up;
            normal=Vector3.Slerp(transform.up,normal,1-Mathf.Exp(-10*Time.fixedDeltaTime));rotation=Quaternion.FromToRotation(Vector3.up,normal)*rotation;transform.SetPositionAndRotation(next,rotation);Speed.Value=speed;
            if(Mathf.Abs(speed)>1)Fuel.Value=Mathf.Max(0,Fuel.Value-Time.fixedDeltaTime*(.025f+Mathf.Abs(speed)*.012f)*(turbo?1.5f:1));
        }
        public static bool CanTraverse(float rise,float distance)
        { return Mathf.Abs(rise)<=.32f+distance*1.28f; }
        float Surface(Vector3 point)
        {
            float height=CaveLayout.Underground(point)?CaveLayout.SupportHeight(point):ReserveWorld.Height(point.x,point.z);
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,transform.position.y+4,point.z),Vector3.down,roadHits,10,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=roadHits[i];if(hit.collider.transform.IsChildOf(transform)||hit.normal.y<.60f)continue;
                if(hit.collider.GetComponentInParent<BridgeSurface>()||hit.collider.GetComponentInParent<BuildPiece>())height=Mathf.Max(height,hit.point.y);
            }
            return height;
        }
        public void Damage(int amount){if(IsServer)Condition.Value=Mathf.Max(0,Condition.Value-Mathf.Max(1,amount/9));}
        public static RoadVehicle Near(Vector3 p,float distance=4.7f)
        {RoadVehicle best=null;foreach(var v in All){float d=Vector3.Distance(v.transform.position,p);if(d<distance){distance=d;best=v;}}return best;}
    }
}
