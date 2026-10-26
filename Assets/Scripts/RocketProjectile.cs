using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class RocketProjectile : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Direction = new NetworkVariable<Vector3>(Vector3.forward);
        public NetworkVariable<ulong> Shooter = new NetworkVariable<ulong>();
        public NetworkVariable<int> Damage = new NetworkVariable<int>(110), StructureDamage = new NetworkVariable<int>(220);
        float life; bool detonated;
        void Awake() { NetworkSeed.Initialize(this); }
        public override void OnNetworkSpawn()
        {
            life = Time.time + 6.7f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.transform.SetParent(transform, false); body.transform.localScale = new Vector3(.11f, .30f, .11f); body.transform.localRotation = Quaternion.Euler(90, 0, 0); body.GetComponent<Collider>().enabled = false; Destroy(body.GetComponent<Collider>()); body.GetComponent<Renderer>().sharedMaterial = BuildGeometry.Surface(2);
            transform.rotation = Quaternion.LookRotation(Direction.Value.normalized);
            var trail = gameObject.AddComponent<TrailRenderer>(); trail.time = .16f; trail.startWidth = .09f; trail.endWidth = .01f; trail.sharedMaterial = Resources.Load<Material>("Dust"); trail.startColor = new Color(1, .73f, .35f, .55f); trail.endColor = new Color(.55f, .5f, .45f, 0);
        }
        void FixedUpdate()
        {
            if (!IsServer || !IsSpawned || detonated) return;
            Vector3 delta = Direction.Value.normalized * 44 * Time.fixedDeltaTime; float closest = delta.magnitude; RaycastHit selected = default;
            foreach (var hit in Physics.SphereCastAll(transform.position, .09f, Direction.Value.normalized, delta.magnitude, ~(1 << 10), QueryTriggerInteraction.Collide))
            {
                var player = hit.collider.GetComponentInParent<Ranger>();
                if (hit.collider.transform.IsChildOf(transform) || player && player.NetworkObjectId == Shooter.Value || hit.collider.isTrigger && !player && !hit.collider.GetComponentInParent<Wildlife>()) continue;
                if (hit.distance <= closest) { closest = hit.distance; selected = hit; }
            }
            if (selected.collider) { Detonate(selected.point - Direction.Value.normalized * .08f); return; }
            transform.position += delta; if (Time.time > life || Mathf.Abs(transform.position.x) > ReserveWorld.Edge || Mathf.Abs(transform.position.z) > ReserveWorld.Edge) Detonate(transform.position);
        }
        public void Detonate(Vector3 point)
        {
            if (!IsServer || !IsSpawned || detonated) return; detonated = true;
            var shooter = Ranger.Players.FirstOrDefault(p => p && p.NetworkObjectId == Shooter.Value);
            const float radius = 6;
            foreach (var player in Ranger.Players)
            {
                if (!player || !player.Alive || player != shooter && shooter && player.Team.Value == shooter.Team.Value) continue;
                float distance = Vector3.Distance(point, player.transform.position + Vector3.up * .9f); if (distance >= radius || !ClearBlast(point, player.transform.position + Vector3.up, null)) continue;
                int before = player.Health.Value, armor = player.Armor.Value;
                int amount = Mathf.RoundToInt(Damage.Value * Mathf.Lerp(1, .12f, distance / radius) * (player == shooter ? .6f : 1));
                bool killed = player.TakeDamageServer(amount, (int)Shooter.Value);
                if (shooter) shooter.ConfirmDamage(player.transform.position + Vector3.up, Mathf.Max(0, before - player.Health.Value), Mathf.Max(0, armor - player.Armor.Value), player.Health.Value, 100, killed, false, player.NetworkObjectId, player != shooter);
            }
            foreach (var animal in ReserveSession.Instance.Animals)
            {
                if (!animal || animal.Dead) continue; float distance = Vector3.Distance(point, animal.transform.position + Vector3.up * .5f); if (distance >= radius || !ClearBlast(point, animal.transform.position + Vector3.up * .6f, null)) continue;
                int before = animal.Health.Value; bool killed = animal.Hit(Mathf.RoundToInt(Damage.Value * 1.6f * (1 - distance / radius)), point);
                if (shooter) { shooter.ConfirmDamage(animal.transform.position + Vector3.up, before - animal.Health.Value, 0, animal.Health.Value, animal.EffectiveMaxHealth, killed, false, animal.NetworkObjectId, false); if (killed) shooter.AnimalReward(animal); }
            }
            foreach (var piece in BuildPiece.All.ToArray())
            {
                if (!piece) continue; float distance = Vector3.Distance(point, piece.Center); if (distance >= radius || !ClearBlast(point, piece.Center, piece)) continue;
                piece.Damage(Mathf.RoundToInt(StructureDamage.Value * Mathf.Lerp(1, .25f, distance / radius)));
            }
            var nodes = new HashSet<HarvestNode>(); var vehicles = new HashSet<RoadVehicle>();
            foreach (var collider in Physics.OverlapSphere(point, radius, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore))
            {
                var node = HarvestResources.Find(collider); if (node != null) nodes.Add(node); var car = collider.GetComponentInParent<RoadVehicle>(); if (car) vehicles.Add(car);
            }
            if (ConstructionWorld.Current) foreach (var node in nodes) ConstructionWorld.Current.DamageNode(node, StructureDamage.Value, null);
            foreach (var car in vehicles) car.Damage(StructureDamage.Value * 2);
            ExplosionClientRpc(point); NetworkObject.Despawn();
        }
        static bool ClearBlast(Vector3 origin, Vector3 target, BuildPiece allowed)
        {
            Vector3 delta = target - origin; float length = delta.magnitude;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, Mathf.Max(0, length - .12f), ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore))
                if (!allowed || hit.collider.GetComponentInParent<BuildPiece>() != allowed) return false;
            return true;
        }
        [ClientRpc] void ExplosionClientRpc(Vector3 point)
        {
            ReserveEffects.Impact(point, Vector3.up, false); ReserveAudio.Blast(point);
            var obj = new GameObject("Blast pressure"); obj.transform.position = point;
            var particles = obj.AddComponent<ParticleSystem>(); var main = particles.main; main.duration = .3f; main.loop = false; main.startLifetime = .55f; main.startSpeed = 8; main.startSize = .35f; main.startColor = new Color(.85f, .59f, .29f, .75f); main.maxParticles = 38;
            var emission = particles.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 30) }); obj.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Dust"); particles.Play(); Destroy(obj, 1.2f);
        }
    }
    public static class LauncherModel
    {
        public static void Create(Transform parent)
        {
            Material metal = BuildGeometry.Surface(2), grip = BuildGeometry.Surface(0);
            WeaponView.Part(parent, PrimitiveType.Cylinder, new Vector3(0, .04f, .18f), new Vector3(.14f, .56f, .14f), metal, new Vector3(90, 0, 0));
            WeaponView.Part(parent, PrimitiveType.Cylinder, new Vector3(0, .04f, .73f), new Vector3(.20f, .05f, .20f), metal, new Vector3(90, 0, 0));
            WeaponView.Part(parent, PrimitiveType.Cube, new Vector3(0, -.12f, .20f), new Vector3(.065f, .23f, .09f), grip);
            WeaponView.Part(parent, PrimitiveType.Cube, new Vector3(0, .16f, .31f), new Vector3(.035f, .16f, .10f), metal);
        }
    }
}
