using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Pinefall
{
    public sealed class FieldcraftProbe : MonoBehaviour
    {
        [Serializable] public class Report { public List<string> passed = new List<string>(), failed = new List<string>(), errors = new List<string>(); }
        readonly Report report = new Report();
        void Awake() { Application.logMessageReceived += Log; }
        void Log(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) report.errors.Add(message + "\n" + stack); }
        void Check(bool value, string name) { (value ? report.passed : report.failed).Add(name); Debug.Log("FIELDCRAFT_CHECK " + name + " " + value); Write(); }
        void Write() { File.WriteAllText(ReserveSession.Arg("--report"), JsonUtility.ToJson(report, true)); }
        void Finish() { Write(); Application.Quit(); }
        IEnumerator Start()
        {
            float until = Time.realtimeSinceStartup + 80;
            while ((!Ranger.Local || !ConstructionWorld.Current) && Time.realtimeSinceStartup < until) yield return null;
            if (!Ranger.Local || !ConstructionWorld.Current) { Check(false, "Player and construction manager initialize"); Finish(); yield break; }
            string role = ReserveSession.Arg("--field-role");
            if (role == "late")
            {
                yield return new WaitForSecondsRealtime(4);
                Check(HarvestResources.Nodes.Any(n => n.health == 0 && !n.collider.enabled), "Late join receives harvested-node state");
                Check(BuildPiece.All.Any(p => p.Mask.Value != BuildPiece.FullMask(p.Kind.Value)), "Late join receives edited structures");
                Check(ReserveSession.Instance.Animals.Any(a => a && a.Boss.Value && a.transform.localScale.x > 1.5f), "Late join receives boss scale and health state");
                Check(ProximityVoice.Current.MicrophoneStarts == 0, "Late joining never starts microphone capture"); Finish(); yield break;
            }
            if (!ReserveSession.Instance.IsHost) { yield return Client(); yield break; }
            until = Time.realtimeSinceStartup + 70; while (Ranger.Players.Count < 3 && Time.realtimeSinceStartup < until) yield return null;
            if (Ranger.Players.Count < 3) { Check(false, "Three-player test group connects"); Finish(); yield break; }
            until = Time.realtimeSinceStartup + 8;
            while ((Ranger.Players.Any(q => q.Team.Value < 0) || Ranger.Players.Select(q => q.Team.Value).Distinct().Count() < 2) && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            var p = Ranger.Local; var enemy = Ranger.Players.First(q => q.Team.Value != p.Team.Value); var friend = Ranger.Players.First(q => q != p && q.Team.Value == p.Team.Value);
            var match = WildfrontMatch.Current; match.Battle.Value = true; match.Phase.Value = 1; match.ShrinkAt.Value = ReserveSession.Instance.Network.ServerTime.Time + 10000; match.StageEnds.Value = match.ShrinkAt.Value + 100;
            foreach (var player in Ranger.Players) { player.ResetRound(ReserveWorld.OnGround(player == p ? 0 : player == enemy ? 0 : -9, player == enemy ? -144 : -166, .2f)); player.InitializeEquipment(true); player.Health.Value = 100; player.Owned.Value |= 1 << 1; player.Weapon.Value = 1; player.Ammo.Value = 15; }
            yield return new WaitForSecondsRealtime(2);
            bool remoteCrouch=false;float baseModelY=friend.rig?friend.rig.transform.localPosition.y:0;int observations = 0, before = p.Health.Value; float shooting = Time.realtimeSinceStartup + 10;
            while (Time.realtimeSinceStartup < shooting)
            {
                p.SmokeMoveTo(ReserveWorld.OnGround(Mathf.Sin(Time.time * 2) * 2.6f, -166, .2f));
                if(friend.Crouched.Value&&friend.GetComponent<CapsuleCollider>().height<1.4f&&friend.rig&&friend.rig.transform.localPosition.y<baseModelY-.25f)remoteCrouch=true;
                if (p.Health.Value < before) observations++; p.Health.Value = 100; before = 100; yield return null;
            }
            Check(ProximityVoice.Current.FramesFrom(enemy.NetworkObjectId)>=70,"Remote packet bursts preserve sustained proximity speech frames");Check(observations >= 9, "Remote client damages a moving host with delayed shots");Check(remoteCrouch,"Remote crouch lowers both the hitbox and visible character model");
            p.SmokeMoveTo(ReserveWorld.OnGround(0, -175, .2f));
            var world = ConstructionWorld.Current; p.Wood.Value = 300; p.Stone.Value = 200; p.Metal.Value = 100;
            Vector3 point = Vector3.zero; bool found = false;
            for (int z = -184; z <= -152 && !found; z += 4) for (int x = -8; x <= 8 && !found; x += 4)
            {
                var candidate = new Vector3(x, Mathf.Ceil((ReserveWorld.Height(x, z) + .03f) / .75f) * .75f, z);
                if (ConstructionWorld.PlacementError(p, 1, 0, candidate, 0) == "") { point = candidate; found = true; }
            }
            Check(found, "A clear grounded snapped build cell is found");
            if (found)
            {
                int wood = p.Wood.Value; Check(world.TryPlace(p, 1, 0, point, 0) && p.Wood.Value == wood - 10, "Floor placement consumes exactly ten wood");
                Check(!world.TryPlace(p, 1, 0, point, 0) && p.Wood.Value == wood - 10, "Duplicate placement neither duplicates nor charges");
                Check(!world.TryPlace(p, 1, 0, point + Vector3.up * 30, 0), "Unsupported distant construction is rejected");
                Check(world.TryPlace(p, 0, 1, point, 0), "Stone wall can attach to floor");
                var wall = BuildPiece.All.Last(b => b.Kind.Value == 0);
                Check(world.TryEdit(p, wall, 511 ^ (1 << 4), 0, false), "Wall accepts a center-window edit");
                Physics.SyncTransforms();
                bool through = Physics.Raycast(wall.transform.TransformPoint(new Vector3(0, 1.5f, 0)), wall.transform.forward, out var hit, 3, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<BuildPiece>() == wall;
                Check(!through, "Window edit removes actual projectile collision");
                Check(!world.TryEdit(enemy, wall, 511, 0, false), "Enemy cannot edit another team's wall");
                friend.ResetRound(point + Vector3.left * 2); yield return new WaitForSecondsRealtime(.6f);
                Check(world.TryEdit(friend, wall, 511 ^ 18, 0, false), "Teammate can create a shared door");
                p.SmokeMoveTo(wall.transform.TransformPoint(new Vector3(0, .2f, .5f)));
                p.DoorServerRpc(wall.NetworkObjectId); Check(wall.DoorOpen.Value, "Edited door opens with synchronized collision");
                Check(!BuildPiece.ValidMask(0, 1 | 256), "Disconnected wall edit is rejected");
                Check(BuildPiece.ValidMask(1, 7) && BuildPiece.ValidMask(2, 3) && BuildPiece.ValidMask(3, 6), "Floor, stair and pyramid edit masks validate");
                int hp = wall.Health.Value; world.TryEdit(p, wall, 511 ^ 16, 0, false); Check(wall.Health.Value == hp, "Editing does not heal a damaged structure");
                Vector3 other = point + Vector3.left * 4;
                Check(world.TryPlace(p, 2, 2, other, 1), "Metal stairs attach with rotation");
                Check(world.TryPlace(p, 3, 0, point + Vector3.right * 4, 0), "Pyramid attaches to adjacent grid cell");
            }
            var stairs = BuildPiece.All.FirstOrDefault(b=>b.Kind.Value==2);
            if(stairs)
            {
                p.SmokeMoveTo(stairs.transform.TransformPoint(new Vector3(0,.65f,-1.65f)));float highest=p.transform.position.y;float walkUntil=Time.realtimeSinceStartup+1;
                while(Time.realtimeSinceStartup<walkUntil){p.GetComponent<CharacterController>().Move((stairs.transform.forward*4-Vector3.up*3)*Mathf.Min(Time.deltaTime,.04f));highest=Mathf.Max(highest,p.transform.position.y);yield return null;}
                Check(highest-stairs.transform.position.y>2.2f,"Player collision walks up the constructed stairs");
            }
            p.SmokeMoveTo(ReserveWorld.OnGround(32,-176,.2f)); bool stacked=false;BuildPiece foundation=null,upper=null;
            for(int z=-184;z<=-168&&!stacked;z+=4)for(int x=24;x<=40&&!stacked;x+=4)
            {
                Vector3 basePoint=new Vector3(x,Mathf.Ceil((ReserveWorld.Height(x,z)+.03f)/.75f)*.75f,z);
                if((basePoint+Vector3.up*6-p.transform.position).sqrMagnitude>144||ConstructionWorld.PlacementError(p,1,0,basePoint,0)!=""||ConstructionWorld.Grounded(basePoint+Vector3.up*3)||ConstructionWorld.Grounded(basePoint+Vector3.up*6))continue;
                int existing=BuildPiece.All.Count;if(!world.TryPlace(p,1,0,basePoint,0))continue;foundation=BuildPiece.All.Last();
                stacked=world.TryPlace(p,1,0,basePoint+Vector3.up*3,0)&&world.TryPlace(p,1,0,basePoint+Vector3.up*6,0);upper=BuildPiece.All.Last();if(!stacked)foreach(var partial in BuildPiece.All.Skip(existing).ToArray())partial.NetworkObject.Despawn();
            }
            Check(stacked,"Stacked floors attach vertically above a grounded foundation");
            if(stacked){Debug.Log("STACK_FOUNDATION "+foundation.transform.position+" upper="+upper.transform.position+" upperGrounded="+ConstructionWorld.Grounded(upper.transform.position));foundation.Damage(1000);yield return new WaitForSecondsRealtime(.4f);Check(!upper||!upper.IsSpawned,"Destroying a foundation collapses unsupported upper floors");}
            p.SmokeMoveTo(ReserveWorld.OnGround(0,-175,.2f));
            var node = HarvestResources.Nodes.FirstOrDefault(n => n.material == 0 && n.health > 0 && !ReserveWater.HasSurface(n.Position.x, n.Position.z));
            Check(node != null && node.handles.Count >= 1, "Instanced foliage has harvest handles without per-tree network objects");
            if (node != null)
            {
                int wood = p.Wood.Value; world.DamageNode(node, node.maximum, p);
                Check(node.health == 0 && !node.collider.enabled && p.Wood.Value > wood, "Harvest removes collision and grants wood once");
                int now = p.Wood.Value; world.DamageNode(node, 45, p); Check(p.Wood.Value == now, "Destroyed resources cannot pay twice");
                Check(node.handles.All(h => h.matrices[h.index].lossyScale.sqrMagnitude < .001f), "Harvest removes every instanced visual handle");
            }
            int stash = p.Stone.Value; var materialLoot = ExpeditionLoot.Spawn(ReserveWorld.OnGround(p.transform.position.x + 1, p.transform.position.z, .3f), 13, 1, 0, 25); p.ScavengeServerRpc(materialLoot.NetworkObjectId);
            Check(p.Stone.Value == stash + 25, "Building materials can be discovered and collected");
            int drops = WorldLoot.All.Count(l => l.Kind.Value == 13); p.DropItemServerRpc(13, 2, 10);
            Check(WorldLoot.All.Count(l => l.Kind.Value == 13) == drops + 1, "Construction materials can be dropped and shared");
            var beast = Instantiate(Resources.Load<GameObject>("Boar"), ReserveWorld.OnGround(8, -171), Quaternion.identity).GetComponent<Wildlife>(); beast.GetComponent<Unity.Netcode.NetworkObject>().Spawn();
            Check(beast.MutateBoss() && beast.Health.Value == beast.maxHealth * 3 && beast.transform.localScale.x > 1.5f, "Storm boss mutation increases size and health");
            int beforeLoot = WorldLoot.All.Count; p.AnimalReward(beast); Check(WorldLoot.All.Skip(beforeLoot).All(l => l.Kind.Value == 5 || l.Kind.Value == 9), "Boss rewards remain organic survival items");
            p.SmokeMoveTo(ReserveWorld.OnGround(0, -175, 9)); float heightBefore = p.transform.position.y; ReserveUI.Instance.VerificationPanel("settings"); yield return new WaitForSecondsRealtime(.6f);
            Check(p.transform.position.y < heightBefore - 1, "Gravity continues while settings are open"); ReserveUI.Instance.VerificationPanel("");
            var pose = new ShotHistory.Pose { bottom = Vector3.zero, height = 1.8f, radius = .33f };
            Check(ShotHistory.RayCapsule(new Vector3(0, 1, -10), Vector3.forward, pose, 20, out float d) && d > 9 && d < 10, "Analytic body hit recognizes the trigger-only player capsule");
            Check(!ShotHistory.RayCapsule(new Vector3(2, 1, -10), Vector3.forward, pose, 20, out d), "Analytic shots do not invent off-target hits");
            Check(PlayerHitboxes.Ray(new Vector3(0,1.68f,-10),Vector3.forward,pose,20,out d,out bool skull)&&skull,"Player headshots use a head-sized region");
            Check(!PlayerHitboxes.Ray(new Vector3(.34f,1.78f,-10),Vector3.forward,pose,20,out d,out skull),"Empty space beside the head is not a hit");
            Check(!PlayerHitboxes.Ray(new Vector3(0,.13f,-10),Vector3.forward,pose,20,out d,out skull),"Separated feet do not fill the entire stance rectangle");
            double serverNow = ReserveSession.Instance.Network.ServerTime.Time;
            Check(!p.ResolveShot(p.transform.position + Vector3.up, Vector3.forward, p.Weapon.Value, serverNow - 1, 5000, true), "Out-of-window shot timestamps are rejected");
            Check(!p.ResolveShot(p.transform.position + Vector3.up, Vector3.forward, p.Weapon.Value, serverNow + 2, 5001, true), "Future shot timestamps are rejected");
            p.SmokeMoveTo(ReserveWorld.OnGround(0, -174, .1f)); yield return new WaitForSecondsRealtime(.3f);
            Check(p.StartSlide(Vector3.forward), "A grounded player can initiate a slide");
            Vector3 slideStart = p.transform.position; yield return new WaitForSecondsRealtime(.3f);
            Check(p.Sliding.Value && (p.transform.position - slideStart).sqrMagnitude > .5f && p.GetComponent<CapsuleCollider>().height < 1, "Slide momentum and shortened hitbox continue without menu input");
            p.Sliding.Value = false;
            Vector3 waterStart = new Vector3(ReserveWater.RiverX(190), ReserveWater.Level(ReserveWater.RiverX(190), 190) + .23f, 190);
            var boat = ExpeditionLoot.SpawnVehicle(waterStart, 4, 100, 100); boat.transform.rotation = Quaternion.Euler(0, 138, 0);
            // Heading is initialized at spawn; use the existing rotation's forward then face the seam through steering.
            boat.NetworkObject.Despawn();
            var boatObject = Instantiate(Resources.Load<GameObject>("RoadVehicle"), waterStart, Quaternion.Euler(0, 138, 0)); boat = boatObject.GetComponent<RoadVehicle>(); boat.Type.Value = 4; boat.Condition.Value = 100; boat.Fuel.Value = 100; boatObject.GetComponent<Unity.Netcode.NetworkObject>().Spawn();
            p.SmokeMoveTo(waterStart + Vector3.right); yield return null; Check(boat.Enter(p), "Player can board a launch at a lake-river seam");
            float driveEnd = Time.realtimeSinceStartup + 2.5f;
            while (Time.realtimeSinceStartup < driveEnd) { boat.DriveServerRpc(1, 0, false, false); yield return new WaitForSecondsRealtime(.05f); }
            Check(boat.transform.position.z < waterStart.z - 12 && ReserveWater.HasSurface(boat.transform.position.x, boat.transform.position.z), "Boat travels across changing lake-river water levels"); p.ExitVehicleServerRpc();
            Check(ProximityVoice.Current.MicrophoneStarts == 0, "Fieldcraft automation never starts the microphone");
            var voiceBudget = new VoicePacketBudget(); bool bursts = true;
            for (int group = 0; group < 80; group++) for (int frame = 0; frame < 3; frame++) bursts &= voiceBudget.Accept(group * .06);
            Check(bursts, "Voice accepts ordinary batched 20ms frames instead of dropping adjacent callbacks");
            var samples = new byte[ProximityVoice.FrameSamples]; for (int i = 0; i < samples.Length; i++) samples[i] = ProximityVoice.Encode(Mathf.Sin(i * .07f) * .12f);
            var jitter = new VoiceJitterBuffer(); jitter.Push(10, samples); jitter.Push(12, samples); jitter.Push(11, samples); jitter.Push(13, samples);
            var audio = new float[ProximityVoice.FrameSamples * 4]; jitter.Render(audio);
            Check(jitter.PlayedSamples == audio.Length && jitter.ConcealedSamples == 0 && audio.Any(v => Mathf.Abs(v) > .05f), "Voice jitter buffer plays reordered frames continuously");
            jitter.Reset(); jitter.Push(20, samples); jitter.Push(21, samples); jitter.Push(23, samples); jitter.Push(24, samples); jitter.Render(new float[ProximityVoice.FrameSamples * 5]);
            Check(jitter.ConcealedSamples == ProximityVoice.FrameSamples, "Voice conceals a missing frame without clearing the remaining speech");
            jitter=new VoiceJitterBuffer(); for(uint seq=100;seq<106;seq++)jitter.Push(seq,samples);
            var deviceAudio=new float[1920];jitter.RenderDevice(deviceAudio,2,48000);
            Check(jitter.PlayedSamples>=320&&jitter.PlayedSamples<=323&&deviceAudio.Any(v=>Mathf.Abs(v)>.05f)&&deviceAudio.Where((v,i)=>i%2==0).SequenceEqual(deviceAudio.Where((v,i)=>i%2==1)),"Voice device playback resamples continuously into a stereo 48kHz audio block");
            p.Health.Value = 0; friend.Health.Value = 100; enemy.Health.Value = 100; match.TeamSize.Value = 2;
            Check(Ranger.CanSpectate(p, friend) && !Ranger.CanSpectate(p, enemy), "Team spectators are restricted to surviving teammates");
            p.SpectateServerRpc(friend.NetworkObjectId); Check(p.Spectating == friend && p.ObserverPosition == friend.transform.position, "Spectator target drives the network-interest viewpoint");
            p.SpectateServerRpc(enemy.NetworkObjectId); Check(p.Spectating == friend, "Server rejects spying on rivals while a teammate survives");
            friend.Health.Value = 0; Check(Ranger.CanSpectate(p, enemy), "Eliminated teams can spectate all surviving players");
            match.TeamSize.Value = 1; friend.Health.Value = 100; Check(Ranger.CanSpectate(p, enemy) && Ranger.CanSpectate(p, friend), "Solo spectators may select any surviving player");
            match.TeamSize.Value = 2; p.Health.Value = friend.Health.Value = 100; p.SpectatingId.Value = ulong.MaxValue;
            p.SmokeMoveTo(ReserveWorld.OnGround(0, -170, .2f)); float hurtBeforeVest = p.DamageFlash;
            p.ArmorTier.Value = 2; p.Armor.Value = 70; var vest = ExpeditionLoot.Spawn(p.transform.position + Vector3.right, 6, 1, 0, 50); p.ScavengeServerRpc(vest.NetworkObjectId);
            Check(p.DamageFlash == hurtBeforeVest && p.ArmorEquipPulse > Time.time, "Swapping a vest gives blue equip feedback without a hurt event");
            p.TakeDamageServer(5, -1); Check(p.DamageFlash > Time.time, "Real damage still produces a hurt event");
            Check(WeaponData.All.Select(w => w.category).Distinct().Count() == 8 && !WeaponData.Get(3).automatic && WeaponData.Get(4).automatic && WeaponData.Get(0).mode == WeaponFireMode.BoltAction && WeaponData.Get(2).pellets >= 6, "Eight firearm classes enforce distinct fire-mode and projectile specifications");
            Check(WeaponData.Get(4).RangeMultiplier(100) < WeaponData.Get(1).RangeMultiplier(100), "SMGs lose damage sooner than assault rifles");
            p.Owned.Value |= 1 << 7; p.Weapon.Value = 7; p.Ammo.Value = 1; p.ResetCombatCadence();
            bool launched = p.ResolveShot(p.transform.position + Vector3.up * 1.65f, new Vector3(0, 1, .1f).normalized, 7, ReserveSession.Instance.Network.ServerTime.Time, 6000, true);
            var rocket = FindObjectsOfType<RocketProjectile>().FirstOrDefault(); Vector3 initialRocket = rocket ? rocket.transform.position : Vector3.zero;
            yield return new WaitForSecondsRealtime(.2f);
            Check(launched && p.Ammo.Value == 0 && rocket && (rocket.transform.position - initialRocket).sqrMagnitude > 25, "RPG consumes a rocket and launches a traveling server projectile");
            if (rocket)
            {
                Vector3 blast = ReserveWorld.OnGround(p.transform.position.x + 5, p.transform.position.z, .8f);
                var exposed = Instantiate(Resources.Load<GameObject>("Boar"), ReserveWorld.OnGround(blast.x + 1, blast.z), Quaternion.identity).GetComponent<Wildlife>(); exposed.GetComponent<Unity.Netcode.NetworkObject>().Spawn();
                var covered = Instantiate(Resources.Load<GameObject>("Boar"), ReserveWorld.OnGround(blast.x, blast.z + 3.5f), Quaternion.identity).GetComponent<Wildlife>(); covered.GetComponent<Unity.Netcode.NetworkObject>().Spawn();
                var barricade = Instantiate(Resources.Load<GameObject>("BuildPiece"), ReserveWorld.OnGround(blast.x, blast.z), Quaternion.identity).GetComponent<BuildPiece>(); barricade.Kind.Value = 0; barricade.Mask.Value = 511; barricade.Health.Value = 80; barricade.GetComponent<Unity.Netcode.NetworkObject>().Spawn();
                int openHealth = exposed.Health.Value, coveredHealth = covered.Health.Value; Physics.SyncTransforms(); rocket.Detonate(blast);
                Check(exposed.Health.Value < openHealth && covered.Health.Value == coveredHealth, "RPG blast damages exposed targets while solid cover protects others");
                Check(!barricade.IsSpawned || barricade.Health.Value == 0, "RPG blast damages and destroys a freshly built wall");
            }
            p.SetModCountServer(7, 1); p.AttachModServerRpc(7, 7); Check(p.ReloadFactor(7) == .8f && !WeaponMods.Compatible(7, 3), "Launcher attachments use the eighth packed weapon slot safely");
            Debug.Log("FIELDCRAFT_LATE_READY");
            yield return new WaitForSecondsRealtime(42); Finish();
        }
        IEnumerator Client()
        {
            yield return new WaitForSecondsRealtime(3); var p = Ranger.Local;
            StartCoroutine(VoiceBursts());
            if (ReserveSession.Instance.RequestedTeam == 1)
            {
                float deadline = Time.realtimeSinceStartup + 40; while ((!WildfrontMatch.Current || !WildfrontMatch.Current.Active) && Time.realtimeSinceStartup < deadline) yield return null;
                var target = Ranger.Players.First(q => q.Team.Value != p.Team.Value && q.OwnerClientId == 0);
                for (int n = 0; n < 14; n++)
                {
                    Vector3 origin = p.transform.position + Vector3.up * 1.65f, direction = (target.transform.position + Vector3.up * 1.05f - origin).normalized;
                    double stamp = ReserveSession.Instance.Network.ServerTime.Time;
                    yield return new WaitForSecondsRealtime(.13f); p.SubmitTestShot(origin, direction, stamp, true); yield return new WaitForSecondsRealtime(.24f);
                }
                yield return new WaitForSecondsRealtime(.5f); Debug.Log("REMOTE_HITS " + p.ConfirmedHits); Check(p.ConfirmedHits >= 9, "Remote delayed shots receive server-confirmed hit feedback");
            }
            else
            {
                yield return new WaitForSecondsRealtime(5); p.Crouched.Value = true; yield return new WaitForSecondsRealtime(.7f);
                Check(p.GetComponent<CapsuleCollider>().height < 1.4f, "Crouch changes the actual local hitbox height"); p.Crouched.Value = false;
            }
            yield return new WaitForSecondsRealtime(25);
            Check(BuildPiece.All.Count >= 3, "Remote peers receive multiple player-built shapes");
            Check(BuildPiece.All.Any(b => b.Mask.Value != BuildPiece.FullMask(b.Kind.Value)), "Remote peers receive edited geometry state");
            Check(ReserveSession.Instance.Animals.Any(a => a && a.Boss.Value && a.transform.localScale.x > 1.5f), "Remote peers display the boss's larger model");
            Check(ProximityVoice.Current.MicrophoneStarts == 0, "Remote fieldcraft checks do not record voice"); Finish();
        }
        IEnumerator VoiceBursts()
        {
            var tone = new byte[ProximityVoice.FrameSamples]; for(int i=0;i<tone.Length;i++)tone[i]=ProximityVoice.Encode(Mathf.Sin(i*.06f)*.1f);
            for(int group=0;group<30;group++){for(int packet=0;packet<3;packet++)ProximityVoice.Current.Transmit(tone);yield return new WaitForSecondsRealtime(.06f);}
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
