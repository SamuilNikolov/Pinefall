using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    // Explicit standalone integration probe. Never captures a microphone.
    public sealed class LivingFrontierProbe : MonoBehaviour
    {
        [Serializable] public sealed class Report { public List<string> passed=new List<string>(),failed=new List<string>(),errors=new List<string>(); }
        readonly Report report=new Report();
        void Awake(){Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)report.errors.Add(text+"\n"+stack);}
        void Check(bool value,string label){(value?report.passed:report.failed).Add(label);Debug.Log("FRONTIER_CHECK "+label+" "+value);File.WriteAllText(ReserveSession.Arg("--report"),JsonUtility.ToJson(report,true));}
        IEnumerator Start()
        {
            float deadline=Time.realtimeSinceStartup+80;
            while((!Ranger.Local||!ConstructionWorld.Current)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!Ranger.Local){Check(false,"Host initializes");Application.Quit();yield break;}
            var p=Ranger.Local;var match=WildfrontMatch.Current;var world=ConstructionWorld.Current;
            yield return new WaitForSecondsRealtime(1);
            Check(new[]{1,5,6,12,13,20}.Select(RoundArena.TierFor).SequenceEqual(new[]{0,0,1,1,2,2}),"Lobby boundary counts select small, medium and full arenas");
            match.Battle.Value=true;match.Phase.Value=1;match.ShrinkAt.Value=ReserveSession.Instance.Network.ServerTime.Time+10000;match.StageEnds.Value=match.ShrinkAt.Value+100;
            foreach(int tier in new[]{0,1,2}){match.ArenaTier.Value=tier;Check(Mathf.Abs(RoundArena.Size-new[]{2048,4096,6144}[tier])<1,"Arena tier "+tier+" has its advertised span");}
            match.ArenaTier.Value=0;Vector3 landing;
            Check(!FreeSpawn.Resolve(new Vector3(1500,0,0),out landing),"Small arena rejects a landing outside its bounds");
            match.SelectArena();Check(match.ArenaTier.Value==0,"Arena cannot change after deployment begins");
            ExpeditionLoot.Populate();yield return null;
            Check(RoadVehicle.All.All(v=>!v||RoundArena.Contains(v.transform.position, -40)),"Small round populates transport within its playable area");
            Check(LaunchPad.All.Any(pad=>pad.Fixed.Value)&&WorldLoot.All.Any(l=>l.Kind.Value==14),"Small arena has fixed and discoverable launchpads");
            Check(WorldLoot.All.Any(l=>l.Kind.Value==15),"Small arena contains a usable supply relay");
            match.Battle.Value=false;
            var ui=ReserveUI.Instance;ui.VerificationPanel("");ui.TickInventoryHold(true);Check(ui.InventoryVisible,"Holding inventory opens its panel");ui.TickInventoryHold(false);Check(!ui.InventoryVisible,"Releasing inventory closes its panel");
            Check(GameBindings.DefaultKey("Inventory")==KeyCode.LeftAlt,"Inventory defaults to holding Left Alt");
            Check(Emotes.Sector(Vector2.zero)==-1&&Emotes.Sector(new Vector2(0,180))==0&&Emotes.Sector(new Vector2(160,100))==1,"Emote wheel uses directional sectors and a center cancel zone");
            ui.VerificationPanel("");ui.TickEmoteHold(true);ui.SetEmotePointer(new Vector2(0,180));ui.TickEmoteHold(false);yield return new WaitForSecondsRealtime(.4f);
            Check(p.IsEmoting&&p.EmoteId.Value==0,"Releasing the wheel key starts the selected wave");
            var hand=p.body.GetComponentsInChildren<Transform>(true).First(t=>t.name.ToLowerInvariant()=="hand_r");Check(hand.position.y-p.transform.position.y>1.65f,"Wave visibly raises the right hand above the shoulder");
            p.CancelEmoteServer();ui.TickEmoteHold(true);ui.SetEmotePointer(Vector2.zero);ui.TickEmoteHold(false);Check(!p.IsEmoting,"Releasing in the wheel center cancels without an emote");
            yield return new WaitForSecondsRealtime(.5f);ui.TickEmoteHold(true);ui.SetEmotePointer(new Vector2(160,100));ui.FinishEmoteWheel(true);ui.TickEmoteHold(false);yield return null;
            Check(p.IsEmoting&&p.EmoteId.Value==1,"Click-selection path commits its directional emote");
            p.TakeDamageServer(1,-3);Check(!p.IsEmoting,"Real damage interrupts an emote");p.Health.Value=100;p.EmoteServerRpc(999);Check(!p.IsEmoting,"Invalid emote requests are rejected");
            yield return new WaitForSecondsRealtime(.5f);p.Velocity.Value=3;p.EmoteServerRpc(0);Check(!p.IsEmoting,"Moving players cannot start an emote");p.Velocity.Value=0;
            p.EmoteServerRpc(0);p.EmoteUntil.Value=ReserveSession.Instance.Network.ServerTime.Time+.1;yield return new WaitForSecondsRealtime(.2f);Check(p.EmoteId.Value==-1,"Emotes expire on the shared server timeline");
            p.CancelEmoteServer();p.SmokeMoveTo(ReserveWorld.OnGround(0,-205,.04f));yield return new WaitForSecondsRealtime(.3f);p.Velocity.Value=0;
            Check(!p.StartSlide(Vector3.forward),"Standing crouch cannot trigger a slide");p.Velocity.Value=9.4f;Check(p.StartSlide(Vector3.forward),"Fast movement can enter a crouch slide");p.Sliding.Value=p.Crouched.Value=false;
            p.ResetHopMomentum();p.SmokeBunnyHop=true;int hops=0;bool previousGround=true;float highest=0,maximumSpeed=0,hopUntil=Time.realtimeSinceStartup+3.1f;
            while(Time.realtimeSinceStartup<hopUntil){bool ground=p.GetComponent<CharacterController>().isGrounded;if(previousGround&&!ground)hops++;previousGround=ground;highest=Mathf.Max(highest,p.transform.position.y-ReserveWorld.Height(p.transform.position.x,p.transform.position.z));maximumSpeed=Mathf.Max(maximumSpeed,p.Velocity.Value);yield return null;}
            p.SmokeBunnyHop=false;Check(hops>=2&&highest>1,"Holding jump chains repeated physical bunny hops");Check(maximumSpeed>=11&&maximumSpeed<=24.2f,"Chained bunny hops gain substantial speed within the high-speed cap");p.SmokeMoveTo(ReserveWorld.OnGround(0,-175,.1f));p.ResetHopMomentum();yield return new WaitForSecondsRealtime(.3f);
            var rotation=Quaternion.Euler(0,37,0);var origin=BuildGrid.RootOrigin(ReserveWorld.OnGround(0,-176),ReserveWorld.Height(0,-176));
            var snap=BuildGrid.Snap(origin+rotation*new Vector3(4.1f,2.9f,-3.9f),origin,37);
            Check((snap-origin-rotation*new Vector3(4,3,-4)).sqrMagnitude<.001f,"Rotated grid snaps in local horizontal and vertical axes");
            Check(Enumerable.Range(0,4).All(turn=>BuildGrid.FacingTurn(37+turn*90,37)==turn),"Quarter turns follow the player in a rotated build frame");
            p.Wood.Value=p.Stone.Value=p.Metal.Value=999;BuildPiece floor=null;
            for(int z=-200;z<=-160&&!floor;z+=4)for(int x=-20;x<=20&&!floor;x+=4)
            {var feet=ReserveWorld.OnGround(x,z,.2f);var point=BuildGrid.RootOrigin(feet,ReserveWorld.Height(x,z));p.SmokeMoveTo(feet+rotation*Vector3.back*2.5f);if(world.TryPlace(p,1,0,point,0,37,point)){floor=BuildPiece.All.Last();origin=point;}}
            Check(floor,"Grounded floor accepts a player-relative 37 degree frame");
            if(floor)
            {
                p.SmokeMoveTo(origin+rotation*Vector3.back*3+Vector3.up*.3f);
                bool turns=true;for(int turn=0;turn<4;turn++)turns&=world.TryPlace(p,0,0,origin,turn,37,origin);
                Check(turns,"Four rotated walls share the same floor edges without duplicates");
                Check(BuildPiece.All.Where(b=>b.Kind.Value==0).All(b=>Mathf.Abs(Mathf.DeltaAngle(b.transform.eulerAngles.y,37+90*Mathf.Round((b.transform.eulerAngles.y-37)/90)))<.1f),"Spawned geometry retains relative rotations");
                Vector3 ramp=origin+rotation*Vector3.forward*4;p.SmokeMoveTo(ramp+rotation*Vector3.back*3+Vector3.up*.5f);
                Check(world.TryPlace(p,2,0,ramp,0,37,origin),"Ramp connects to the rotated floor's next cell");
                p.SmokeMoveTo(origin+rotation*Vector3.back*3+Vector3.up*.3f);p.LaunchPads.Value=2;
                p.DeployLaunchPadServerRpc(floor.NetworkObjectId);yield return null;var pad=LaunchPad.All.FirstOrDefault(a=>a.Floor.Value==floor.NetworkObjectId);
                Check(pad&&p.LaunchPads.Value==1,"Deployable launchpad attaches to a full allied floor and spends one item");
                p.DeployLaunchPadServerRpc(floor.NetworkObjectId);Check(p.LaunchPads.Value==1,"Duplicate pad does not spend another item");
                world.TryEdit(p,floor,7,0,false);yield return new WaitForSecondsRealtime(.2f);Check(!pad||!pad.IsSpawned,"Editing away the full floor removes its launchpad");
                world.TryEdit(p,floor,15,0,false);
            }
            var launchPoint=ReserveWorld.OnGround(60,-175,.08f);var fixedPad=LaunchPad.Spawn(launchPoint);p.SmokeMoveTo(launchPoint+Vector3.up*.3f);float y=p.transform.position.y;
            yield return new WaitForSecondsRealtime(1);Check(p.transform.position.y>y+8,"Stepping onto a fixed pad launches the owning player");
            yield return new WaitForSecondsRealtime(2);Check(p.Gliding,"Launchpad opens a networked glider on descent");
            p.SmokeMoveTo(ReserveWorld.OnGround(100,-175,.05f));yield return new WaitForSecondsRealtime(.5f);Check(!p.Gliding,"Landing closes the glider");
            world.ResetWorld();
            bool floors=true,ceilings=true,stable=true;
            foreach(var cave in CaveLayout.Sites)
            {
                var inside=cave.World(0,0);inside.y=cave.Floor(0);
                floors&=Physics.Raycast(inside+Vector3.up*2,Vector3.down,out var hit,4,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y-inside.y)<.5f;
                ceilings&=Physics.Raycast(inside+Vector3.up*1.8f,Vector3.up,12,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
                Debug.Log("CAVE_PROBE "+cave.name+" floor="+inside.y+" actual="+(hit.collider?hit.point.y:-999));p.SmokeMoveTo(inside+Vector3.up*.15f);yield return new WaitForSecondsRealtime(.3f);stable&=CaveLayout.Underground(p.transform.position)&&Mathf.Abs(p.transform.position.y-inside.y)<.7f;
            }
            Check(floors,"All six caves have solid traversable floors");Check(ceilings,"All six caves have overhead rock collision");Check(stable,"Below-ground movement stays underground instead of being rescued to the surface");
            var drop=CaveLayout.LootPosition(p.transform.position,Vector3.right);Check(CaveLayout.Underground(drop),"Dropped items stay on the cave floor");
            p.SmokeMoveTo(ReserveWorld.OnGround(0,-175,.2f));
            var animal=Instantiate(Resources.Load<GameObject>("Boar"),ReserveWorld.OnGround(6,-175),Quaternion.Euler(0,37,0)).GetComponent<Wildlife>();animal.NetworkObject.Spawn();yield return null;
            var pose=WildlifeHitboxes.Pose(animal,0);float distance;var center=pose.bottom+Vector3.up*pose.extent.y;
            bool body=true;foreach(var direction in new[]{animal.transform.forward,animal.transform.right,-animal.transform.forward})body&=WildlifeHitboxes.Ray(center-direction*8,direction,pose,12,out distance);
            Check(body,"Animal hit bounds accept front, flank and rear shots");Check(!WildlifeHitboxes.Ray(center+Vector3.up*(pose.extent.y+1)-animal.transform.forward*8,animal.transform.forward,pose,12,out distance),"Animal bounds reject shots above the body");
            Check(AnimalLoot.ExtraRoll(false,false,.34f)&&!AnimalLoot.ExtraRoll(false,false,.35f)&&AnimalLoot.ExtraRoll(true,false,.79f)&&!AnimalLoot.ExtraRoll(true,false,.8f)&&AnimalLoot.ExtraRoll(false,true,.999f),"Equipment chances distinguish ordinary, enraged and boss kills");
            animal.MutateBoss();int before=WorldLoot.All.Count;bool killed=animal.Hit(100000,p.transform.position);if(killed)p.AnimalReward(animal);yield return null;
            var rewards=WorldLoot.All.Skip(before).ToArray();Check(killed&&animal.Dead,"Boss animal can be killed through authoritative damage");Check(rewards.Count(l=>l.Kind.Value!=5&&l.Kind.Value!=9)>=2,"Boss kill awards at least two usable equipment items");
            Check(rewards.All(l=>new[]{0,1,2,5,6,7,8,9,14}.Contains(l.Kind.Value)),"Animal rewards contain usable supplies with no trophy economy");
            match.Battle.Value=true;match.Phase.Value=0;
            var bots=new List<Ranger>();
            for(int i=1;i<20;i++){var go=Instantiate(Resources.Load<GameObject>("Ranger"),ReserveWorld.OnGround(200+i*2,-175,.3f),Quaternion.identity);var bot=go.GetComponent<Ranger>();bot.IsBot.Value=true;bot.Team.Value=i;bot.DisplayName.Value=new Unity.Collections.FixedString64Bytes("Capacity "+i);bot.NetworkObject.Spawn();bot.GetComponent<HunterBot>().enabled=false;bots.Add(bot);}
            match.SelectArena();Check(Ranger.Players.Count==20&&match.ArenaTier.Value==2,"Twenty authoritative participants select the full reserve");
            match.TeamSize.Value=2;foreach(var bot in bots)bot.Team.Value=match.AssignTeam(bot,-1);Check(Ranger.Players.GroupBy(q=>q.Team.Value).All(g=>g.Count()<=2),"Twenty participants fit ten duo teams");
            foreach(var bot in bots)bot.NetworkObject.Despawn();match.Battle.Value=false;
            var approve=typeof(ReserveSession).GetMethod("Approve",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);bool reservations=true;
            for(ulong i=1;i<=19;i++){var request=new NetworkManager.ConnectionApprovalRequest{ClientNetworkId=100+i,Payload=System.Text.Encoding.UTF8.GetBytes("PinefallReserve/9")};var response=new NetworkManager.ConnectionApprovalResponse();approve.Invoke(ReserveSession.Instance,new object[]{request,response});reservations&=response.Approved;}
            var extra=new NetworkManager.ConnectionApprovalResponse();approve.Invoke(ReserveSession.Instance,new object[]{new NetworkManager.ConnectionApprovalRequest{ClientNetworkId=999,Payload=System.Text.Encoding.UTF8.GetBytes("PinefallReserve/9")},extra});
            Check(reservations&&!extra.Approved,"Connection approval reserves nineteen remote slots and rejects participant twenty-one");
            ((HashSet<ulong>)typeof(ReserveSession).GetField("pendingApprovals",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(ReserveSession.Instance)).Clear();
            Check(ProximityVoice.Current.MicrophoneStarts==0,"Frontier verification never records microphone audio");
            Debug.Log("FRONTIER_OK");Application.Quit();
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
