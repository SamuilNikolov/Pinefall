using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class Ranger
    {
        uint shotSequence, receivedSequence; double lastShotStamp = -100; float shotTokens = 2, tokenAt; int lastShotWeapon = -1;
        bool RejectShot(string reason) { if (ReserveSession.Arg("--smoke") == "fieldcraft") Debug.Log("SHOT_REJECT " + OwnerClientId + " " + reason); return false; }
        [ServerRpc] void FireServerRpc(Vector3 origin, Vector3 direction, int weapon, double stamp, uint sequence, bool aimed, float smoothing)
        { ResolveShot(origin, direction, weapon, stamp, sequence, aimed, smoothing); }
        public bool ResolveShot(Vector3 origin, Vector3 direction, int weapon, double stamp, uint sequence, bool aimed, float smoothing = .08f)
        {
            if (!IsServer || !Alive || !CanAct || weapon < 0 || weapon >= WeaponData.All.Length || weapon != Weapon.Value || Reloading.Value || Ammo.Value <= 0 || !Finite(origin) || !Finite(direction) || direction.sqrMagnitude < .5f || direction.sqrMagnitude > 1.5f || float.IsNaN(smoothing) || float.IsInfinity(smoothing) || double.IsNaN(stamp) || double.IsInfinity(stamp)) return RejectShot("state");
            double now = NetworkManager.ServerTime.Time;smoothing=Mathf.Clamp(smoothing,0,.1f);
            if (stamp < now - ShotHistory.Window || stamp > now + .08 || sequence <= receivedSequence) return RejectShot("clock/replay age=" + (now - stamp).ToString("0.000"));
            var data = WeaponData.Get(weapon);
            if (lastShotWeapon != weapon) { lastShotWeapon = weapon; lastShotStamp = -100; shotTokens = 2; tokenAt = Time.time; }
            shotTokens = Mathf.Min(2, shotTokens + (Time.time - tokenAt) / data.interval); tokenAt = Time.time;
            if (shotTokens < .95f || stamp - lastShotStamp < data.interval * .90f) return RejectShot("cadence");
            var shooterPose = ShotHistory.Current ? ShotHistory.Current.Get(NetworkObjectId, stamp, transform.position, StanceHeight, .33f) : new ShotHistory.Pose { bottom = transform.position };
            if ((origin - shooterPose.bottom - Vector3.up * (Crouched.Value ? 1.1f : 1.5f)).sqrMagnitude > 4 * 4) return RejectShot("origin");
            receivedSequence = sequence; lastShotStamp = stamp; shotTokens -= 1; nextServerShot = Time.time + data.interval; Ammo.Value--; magazines[weapon] = Ammo.Value;
            if(data.category==WeaponClass.Launcher)
            {
                var rocket=Instantiate(Resources.Load<GameObject>("Rocket"),origin,Quaternion.LookRotation(direction.normalized)).GetComponent<RocketProjectile>();
                rocket.Direction.Value=direction.normalized;rocket.Shooter.Value=NetworkObjectId;rocket.Damage.Value=Mathf.RoundToInt(data.playerDamage*WeaponData.RarityPower(Rarity(weapon)));rocket.StructureDamage.Value=data.damage;rocket.GetComponent<NetworkObject>().Spawn();
                ShotClientRpc(origin,origin+direction.normalized*.4f,-direction.normalized,weapon,false,false);return true;
            }
            Physics.SyncTransforms();
            float spread = ShotSpread(data, aimed, Crouched.Value);
            var animalHits = new Dictionary<Wildlife, int>(); var playerHits = new Dictionary<Ranger, int>(); var heads = new HashSet<Ranger>();
            Vector3 impact = origin + direction.normalized * data.range, normal = -direction.normalized; bool confirmed = false, killed = false;
            for (int pellet = 0; pellet < data.pellets; pellet++)
            {
                Vector3 ray = (direction.normalized + UnityEngine.Random.insideUnitSphere * spread).normalized;
                float nearest = data.range; RaycastHit cover = default; Ranger victim = null; Wildlife animal = null; ShotHistory.Pose victimPose = default; bool victimHead = false;
                foreach (var hit in Physics.RaycastAll(origin, ray, data.range, ~(1 << 10), QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<Ranger>() || hit.collider.GetComponentInParent<Wildlife>() || CurrentVehicle && hit.collider.GetComponentInParent<RoadVehicle>() == CurrentVehicle || hit.distance >= nearest) continue;
                    cover = hit; nearest = hit.distance;
                }
                foreach (var target in Players)
                {
                    if (!target || target == this || !target.Alive || (target.transform.position - origin).sqrMagnitude > (data.range + 5) * (data.range + 5)) continue;
                    double renderedAt = System.Math.Max(now-ShotHistory.Window,stamp - (OwnerClientId == NetworkManager.ServerClientId ? target.OwnerClientId == NetworkManager.ServerClientId ? 0 : 1.0 / NetworkManager.NetworkConfig.TickRate+smoothing : 2.0 / NetworkManager.NetworkConfig.TickRate+smoothing));
                    var pose = ShotHistory.Current ? ShotHistory.Current.Get(target.NetworkObjectId, renderedAt, target.transform.position, target.StanceHeight, .33f) : new ShotHistory.Pose { bottom = target.transform.position, height = target.StanceHeight, radius = .33f };
                    if (PlayerHitboxes.Ray(origin, ray, pose, nearest, out float distance,out bool skull)) { nearest = distance; victim = target; victimPose = pose; victimHead = skull; animal = null; }
                }
                foreach (var target in ReserveSession.Instance.Animals)
                {
                    if (!target || target.Dead || (target.transform.position - origin).sqrMagnitude > (data.range + 5) * (data.range + 5)) continue;
                    double animalAt=System.Math.Max(now-ShotHistory.Window,stamp-(OwnerClientId==NetworkManager.ServerClientId?0:1.0/NetworkManager.NetworkConfig.TickRate+smoothing));
                    var fallback=WildlifeHitboxes.Pose(target,animalAt);
                    var pose=ShotHistory.Current?ShotHistory.Current.Get(target.NetworkObjectId,animalAt,fallback.bottom,fallback.height,fallback.radius):fallback;
                    if(pose.extent.sqrMagnitude<.01f)pose=fallback;
                    if(WildlifeHitboxes.Ray(origin,ray,pose,nearest,out float distance)){nearest=distance;animal=target;victim=null;}
                }
                impact = origin + ray * nearest;
                if (victim)
                {
                    if (victim.Team.Value != Team.Value && WildfrontMatch.Current && WildfrontMatch.Current.Active)
                    {
                        var pose = victimPose;
                        bool head = victimHead;
                        int amount = Mathf.RoundToInt(data.playerDamage * data.RangeMultiplier(nearest) * DamageFactor(weapon) * WeaponData.RarityPower(Rarity(weapon)) * (head ? 1.4f : 1));
                        if (!playerHits.ContainsKey(victim)) playerHits[victim] = 0; playerHits[victim] += amount; if (head) heads.Add(victim);
                    }
                }
                else if (animal)
                { if (!animalHits.ContainsKey(animal)) animalHits[animal] = 0; animalHits[animal] += Mathf.RoundToInt(data.damage * data.RangeMultiplier(nearest) * DamageFactor(weapon) * WeaponData.RarityPower(Rarity(weapon))); }
                else if (cover.collider)
                {
                    normal = cover.normal; var piece = cover.collider.GetComponentInParent<BuildPiece>(); var node = HarvestResources.Find(cover.collider);
                    int amount = Mathf.RoundToInt(data.damage * DamageFactor(weapon));
                    if (piece) { int before = piece.Health.Value; piece.Damage(amount); ConfirmDamage(impact, Mathf.Min(before, amount), 0, piece.Health.Value, piece.Maximum, piece.Health.Value == 0, false, piece.NetworkObjectId, false); }
                    else if (node != null && ConstructionWorld.Current) { int dealt = ConstructionWorld.Current.DamageNode(node, amount, null); ConfirmDamage(impact, dealt, 0, node.health, node.maximum, node.health == 0, false, ulong.MaxValue, false); }
                    var pad=cover.collider.GetComponentInParent<LaunchPad>();if(pad)pad.Damage(amount);
                    var car = cover.collider.GetComponentInParent<RoadVehicle>(); if (car) car.Damage(amount);
                    var range = cover.collider.GetComponent<RangeTarget>(); if (range) range.Hit();
                }
            }
            foreach (var hit in animalHits)
            {
                int before = hit.Key.Health.Value; bool dead = hit.Key.Hit(hit.Value, transform.position); if (dead) { killed = true; AnimalReward(hit.Key); }
                ConfirmDamage(hit.Key.transform.position + Vector3.up, before - hit.Key.Health.Value, 0, hit.Key.Health.Value, hit.Key.EffectiveMaxHealth, dead, false, hit.Key.NetworkObjectId, false); confirmed = true;
            }
            foreach (var hit in playerHits)
            {
                int before = hit.Key.Health.Value, armor = hit.Key.Armor.Value; bool dead = hit.Key.TakeDamageServer(hit.Value, (int)NetworkObjectId); killed |= dead;
                int healthDamage = Mathf.Max(0, before - hit.Key.Health.Value), shieldDamage = Mathf.Max(0, armor - hit.Key.Armor.Value);
                ConfirmDamage(hit.Key.transform.position + Vector3.up * 1.2f, healthDamage, shieldDamage, hit.Key.Health.Value, 100, dead, heads.Contains(hit.Key), hit.Key.NetworkObjectId, true); confirmed |= healthDamage + shieldDamage > 0;
            }
            foreach (var target in ReserveSession.Instance.Animals) if (target && (target.transform.position - origin).sqrMagnitude < 180 * 180) target.HearShot(origin);
            ShotClientRpc(origin, impact, normal, weapon, confirmed, killed);
            if (ReserveSession.Arg("--smoke") == "fieldcraft") Debug.Log("SHOT_RESULT " + OwnerClientId + " age=" + (now - stamp).ToString("0.000") + " hit=" + confirmed);
            return true;
        }
        public void ResetCombatCadence() { lastShotStamp = -100; shotTokens = 2; tokenAt = Time.time; }
        public void SubmitTestShot(Vector3 origin, Vector3 direction, double stamp, bool aimed)
        { FireServerRpc(origin, direction, Weapon.Value, stamp, ++shotSequence, aimed,Mathf.Clamp(.1f-Time.unscaledDeltaTime,.03f,.1f)); }
        void ServerBotFire(Vector3 origin, Vector3 direction)
        { if (Time.time < nextServerShot) return; ResolveShot(origin, direction, Weapon.Value, NetworkManager.ServerTime.Time, ++shotSequence, true); }
    }
}
