using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed class LaunchPad : NetworkBehaviour
    {
        public static readonly List<LaunchPad> All = new List<LaunchPad>();
        public NetworkVariable<ulong> Floor = new NetworkVariable<ulong>(ulong.MaxValue);
        public NetworkVariable<int> Health = new NetworkVariable<int>(250);
        public NetworkVariable<bool> Fixed = new NetworkVariable<bool>();
        readonly Dictionary<ulong, float> launched = new Dictionary<ulong, float>();
        float nextCheck; GameObject visual; Material surface;
        void Awake() { NetworkSeed.Initialize(this); }
        public override void OnNetworkSpawn()
        {
            All.Add(this); visual = new GameObject("Launch platform"); visual.transform.SetParent(transform, false);
            surface = new Material(Resources.Load<Material>("BuildMetal")); surface.color = new Color(.18f, .24f, .31f);
            var deck = WeaponView.Part(visual.transform, PrimitiveType.Cylinder, Vector3.up * .08f, new Vector3(2.7f, .08f, 2.7f), surface);
            for (int side = 0; side < 4; side++)
            {
                var beam = WeaponView.Part(visual.transform, PrimitiveType.Cube, Quaternion.Euler(0, side * 90, 0) * new Vector3(0, .18f, 1), new Vector3(.5f, .14f, .22f), BuildGeometry.Surface(2)); beam.transform.localRotation = Quaternion.Euler(0, side * 90, 0);
            }
            var collider = gameObject.AddComponent<BoxCollider>(); collider.size = new Vector3(2.65f, .22f, 2.65f); collider.center = Vector3.up * .06f;
            foreach (var part in visual.GetComponentsInChildren<Collider>()) { part.enabled = false; Destroy(part); }
            WorldInterest.Install(this);
        }
        public override void OnNetworkDespawn() { All.Remove(this); WorldInterest.Forget(NetworkObject); }
        void Update()
        {
            if (!IsServer || Time.time < nextCheck) return; nextCheck = Time.time + .06f;
            if (Floor.Value != ulong.MaxValue)
            {
                if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(Floor.Value, out var owner) || !owner || !owner.TryGetComponent<BuildPiece>(out var piece) || piece.Mask.Value != 15)
                { NetworkObject.Despawn(); return; }
            }
            foreach (var player in Ranger.Players)
            {
                if (!player || player.IsBot.Value || !player.Alive || !player.CanAct || player.CurrentVehicle) continue;
                var delta = player.transform.position - transform.position;
                if (Mathf.Abs(delta.x) > 1.45f || Mathf.Abs(delta.z) > 1.45f || delta.y < -.25f || delta.y > .65f) continue;
                if (launched.TryGetValue(player.NetworkObjectId, out float previous) && Time.time - previous < 2.5f) continue;
                launched[player.NetworkObjectId] = Time.time;
                player.LaunchFromPad(player.transform.forward * 21 + Vector3.up * 25);
            }
        }
        public void Damage(int damage) { if (!IsServer || damage < 1) return; Health.Value = Mathf.Max(0, Health.Value - damage); if (Health.Value == 0) NetworkObject.Despawn(); }
        public static LaunchPad Spawn(Vector3 point, BuildPiece floor = null)
        {
            var go = Instantiate(Resources.Load<GameObject>("LaunchPad"), point, Quaternion.identity); var pad = go.GetComponent<LaunchPad>();
            pad.Fixed.Value = !floor; pad.Health.Value = floor ? 250 : 500; pad.Floor.Value = floor ? floor.NetworkObjectId : ulong.MaxValue; go.GetComponent<NetworkObject>().Spawn(); return pad;
        }
        public override void OnDestroy() { All.Remove(this); if (surface) Destroy(surface); base.OnDestroy(); }
    }

    public sealed partial class Ranger
    {
        public NetworkVariable<int> LaunchPads = new NetworkVariable<int>();
        Vector3 launchDrift; bool launched, gliding; float launchAt;
        public NetworkVariable<bool> GlideState = new NetworkVariable<bool>(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        public bool Gliding { get { return GlideState.Value; } }
        public void LaunchFromPad(Vector3 impulse)
        {
            if (!IsServer || !Alive || !CanAct) return;
            LaunchClientRpc(impulse, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
        }
        [ClientRpc] void LaunchClientRpc(Vector3 impulse, ClientRpcParams target = default)
        {
            if (!LocalHuman) return; ResetHopMomentum();launched = true; gliding = false;GlideState.Value=false; launchAt = Time.time;
            launchDrift = new Vector3(impulse.x, 0, impulse.z); vertical = impulse.y; Crouched.Value = Sliding.Value = false;
            ShowNotice("Launch! Steer in the air. Your glider opens on descent.", 3);
        }
        bool LaunchMotion(ref Vector3 move)
        {
            if (!launched) return false;
            if (!Alive || Swimming.Value || Time.time - launchAt > .5f && controller.isGrounded)
            { launched = gliding = false;GlideState.Value=false; launchDrift = Vector3.zero; return false; }
            float dt = Mathf.Min(.05f, Time.deltaTime);
            vertical -= (gliding ? 6 : 12) * dt;
            if (vertical < 0) gliding = true;
            if (gliding) vertical = Mathf.Max(vertical, -4.8f);if(GlideState.Value!=gliding)GlideState.Value=gliding;
            Vector3 steering = move.sqrMagnitude > .1f ? move.normalized * 18 : launchDrift.normalized * 18;
            launchDrift = Vector3.Lerp(launchDrift, steering, dt * 2.4f); move = launchDrift; return true;
        }
        [ServerRpc] public void DeployLaunchPadServerRpc(ulong floorId)
        {
            if (!Alive || !CanAct || CurrentVehicle || LaunchPads.Value < 1 || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(floorId, out var obj)) return;
            var floor = obj.GetComponent<BuildPiece>();
            if (!floor || floor.Kind.Value != 1 || floor.Mask.Value != 15 || !ConstructionWorld.CanModify(this, floor)) return;
            foreach (var pad in LaunchPad.All) if (pad && (pad.transform.position - floor.transform.position).sqrMagnitude < 4) return;
            LaunchPads.Value--; LaunchPad.Spawn(floor.transform.position + Vector3.up * .07f, floor);
        }
        public void TryDeployLaunchPad()
        {
            var floor = AimedBuild();
            if (!floor && Physics.Raycast(transform.position + Vector3.up * .5f, Vector3.down, out var hit, 3, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore)) floor = hit.collider.GetComponentInParent<BuildPiece>();
            if (!floor || floor.Kind.Value != 1) { ShowNotice("Aim at a complete allied floor to place a launchpad.", 3); return; }
            DeployLaunchPadServerRpc(floor.NetworkObjectId);
        }
    }
}
