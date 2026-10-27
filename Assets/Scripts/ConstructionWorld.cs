using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed class ConstructionWorld : NetworkBehaviour
    {
        public static ConstructionWorld Current;
        readonly Dictionary<int, int> changedNodes = new Dictionary<int, int>();
        readonly HashSet<BuildPiece> supported = new HashSet<BuildPiece>();
        readonly Queue<BuildPiece> frontier = new Queue<BuildPiece>();
        readonly Dictionary<Vector3Int, List<BuildPiece>> cells = new Dictionary<Vector3Int, List<BuildPiece>>();
        readonly Dictionary<ulong, float> snapshotAt = new Dictionary<ulong, float>();
        float supportAt;
        void Awake() { NetworkSeed.Initialize(this); }
        public override void OnNetworkSpawn()
        {
            Current = this; HarvestResources.Restore(); changedNodes.Clear();
            if (IsServer) NetworkManager.OnClientConnectedCallback += Joined;
            else SnapshotServerRpc();
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer) NetworkManager.OnClientConnectedCallback -= Joined;
            HarvestResources.Restore(); changedNodes.Clear(); if (Current == this) Current = null;
        }
        void Joined(ulong client) { SendSnapshot(client); }
        [ServerRpc(RequireOwnership = false)] void SnapshotServerRpc(ServerRpcParams rpc = default) { SendSnapshot(rpc.Receive.SenderClientId); }
        void SendSnapshot(ulong client)
        {
            if (snapshotAt.TryGetValue(client, out float previous) && Time.unscaledTime - previous < 1) return;
            snapshotAt[client] = Time.unscaledTime;
            // Chunk the sparse changes: no 20k-node initial network population.
            var target = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { client } } };
            var ids = new List<int>(64); var hp = new List<int>(64);
            foreach (var node in changedNodes)
            {
                ids.Add(node.Key); hp.Add(node.Value);
                if (ids.Count == 64) { NodeSnapshotClientRpc(ids.ToArray(), hp.ToArray(), target); ids.Clear(); hp.Clear(); }
            }
            if (ids.Count > 0) NodeSnapshotClientRpc(ids.ToArray(), hp.ToArray(), target);
        }
        [ClientRpc] void NodeSnapshotClientRpc(int[] ids, int[] hp, ClientRpcParams target = default)
        { for (int i = 0; i < ids.Length && i < hp.Length; i++) if (ids[i] >= 0 && ids[i] < HarvestResources.Nodes.Count) HarvestResources.Nodes[ids[i]].Apply(hp[i]); }
        [ClientRpc] void NodeHealthClientRpc(int id, int health)
        { if (id >= 0 && id < HarvestResources.Nodes.Count) HarvestResources.Nodes[id].Apply(health); }
        public int DamageNode(HarvestNode node, int damage, Ranger harvester)
        {
            if (!IsServer || node == null || node.health <= 0 || damage <= 0) return 0;
            int lost = Mathf.Min(node.health, damage); node.Apply(node.health - lost); changedNodes[node.id] = node.health;
            if (harvester) harvester.AddBuildingMaterial(node.material, Mathf.Max(1, lost / 3));
            NodeHealthClientRpc(node.id, node.health); return lost;
        }
        public void ResetWorld()
        {
            if (!IsServer) return;
            foreach (var piece in BuildPiece.All.ToArray()) if (piece && piece.IsSpawned) piece.NetworkObject.Despawn();
            changedNodes.Clear(); RestoreNodesClientRpc();
        }
        [ClientRpc] void RestoreNodesClientRpc() { HarvestResources.Restore(); }
        public static Vector3 Snap(Vector3 requested)
        { return new Vector3(Mathf.Round(requested.x / 4) * 4, Mathf.Round(requested.y / .75f) * .75f, Mathf.Round(requested.z / 4) * 4); }
        public static bool Grounded(Vector3 point)
        {
            float ground = ReserveWorld.Height(point.x, point.z);
            if (point.y >= ground - .4f && point.y - ground <= .85f && !ReserveWater.HasSurface(point.x, point.z)) return true;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * .1f, Vector3.down, 1, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore))
                if (!(hit.collider is TerrainCollider) && !hit.collider.GetComponentInParent<BuildPiece>() && HarvestResources.Find(hit.collider) == null && (hit.collider.gameObject.isStatic || hit.collider.GetComponentInParent<BridgeSurface>())) return true;
            return false;
        }
        public static string PlacementError(Ranger player, int kind, int material, Vector3 requested, int turn, float gridYaw = 0, Vector3 gridOrigin = default)
        {
            if (!player || !player.Alive || !player.CanAct || player.CurrentVehicle) return "Build on foot while alive.";
            if (kind < 0 || kind > 3 || material < 0 || material > 2 || turn < 0 || turn > 3 || !Ranger.FiniteVector(requested) || !Ranger.FiniteVector(gridOrigin) || float.IsNaN(gridYaw) || float.IsInfinity(gridYaw) || gridYaw < 0 || gridYaw >= 360) return "Invalid blueprint.";
            Vector3 point = gridOrigin == Vector3.zero && gridYaw == 0 ? Snap(requested) : BuildGrid.Snap(requested, gridOrigin, gridYaw);
            if ((point - requested).sqrMagnitude > .02f || (point - player.transform.position).sqrMagnitude > 12 * 12) return "Move closer to the blueprint.";
            if (Mathf.Abs(point.x) > ReserveWorld.Edge || Mathf.Abs(point.z) > ReserveWorld.Edge || point.y < -30 || point.y > 650) return "Outside the reserve.";
            if (BuildPiece.All.Count >= 512 || BuildPiece.All.Count(p => p && p.Builder.Value == player.NetworkObjectId) >= 80) return "Structure limit reached.";
            if (player.BuildingMaterial(material) < 10) return "Need 10 " + BuildPiece.Materials[material].ToLowerInvariant() + ".";
            Quaternion rotation = Quaternion.Euler(0, gridYaw + turn * 90, 0);
            Vector3 center = point + rotation * new Vector3(0, kind == 0 ? 1.5f : .08f, kind == 0 ? 2 : 0);
            foreach (var piece in BuildPiece.All)
            {
                if (!piece || piece.Kind.Value != kind) continue;
                if (kind == 0)
                { if ((piece.Center - center).sqrMagnitude < .05f && Mathf.Abs(Vector3.Dot(piece.transform.forward, rotation * Vector3.forward)) > .95f) return "Already built here."; }
                else if ((piece.transform.position - point).sqrMagnitude < .05f) return "Already built here.";
            }
            bool support = Grounded(point) || BuildPiece.All.Any(p => p && BuildGrid.Connected(p, point));
            if (!support) return "Attach to ground or an existing structure.";
            // Conservative obstruction test against existing world, excluding ground and actors.
            Vector3 half = kind == 0 ? new Vector3(1.87f, 1.36f, .08f) : kind == 1 ? new Vector3(1.85f, .045f, 1.85f) : new Vector3(1.75f, 1.15f, 1.75f);
            if (kind >= 2) center = point + Vector3.up * 1.5f;
            foreach (var collider in Physics.OverlapBox(center, half, rotation, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore))
            {
                if (collider is TerrainCollider || collider.GetComponentInParent<BuildPiece>()) continue;
                if (collider.gameObject.name == "Blueprint") continue;
                return "Clear the obstruction first.";
            }
            foreach (var other in Ranger.Players)
            {
                if (!other || !other.Alive) continue; Vector3 local = Quaternion.Inverse(rotation) * (other.transform.position - point);
                if (kind == 0 && Mathf.Abs(local.x) < 2.3f && Mathf.Abs(local.z - 2) < .4f && local.y < 3 && local.y + other.StanceHeight > 0) return "A player is in the way.";
                if (kind == 1 && Mathf.Abs(local.x) < 2.2f && Mathf.Abs(local.z) < 2.2f && local.y < -.1f && local.y + other.StanceHeight > -.1f) return "A player is in the way.";
            }
            return "";
        }
        public bool TryPlace(Ranger player, int kind, int material, Vector3 point, int turn, float gridYaw = 0, Vector3 gridOrigin = default)
        {
            if (!IsServer) return false;
            string reason = PlacementError(player, kind, material, point, turn, gridYaw, gridOrigin);
            if (reason != "") { player.InformServer(reason); return false; }
            var go = Instantiate(Resources.Load<GameObject>("BuildPiece"), point, Quaternion.Euler(0, gridYaw + turn * 90, 0));
            var piece = go.GetComponent<BuildPiece>(); piece.Kind.Value = kind; piece.Material.Value = material; piece.Mask.Value = BuildPiece.FullMask(kind);
            piece.GridYaw.Value = gridYaw; piece.GridOrigin.Value = gridOrigin;
            piece.Builder.Value = player.NetworkObjectId; piece.Team.Value = player.Team.Value; piece.Health.Value = 80; piece.BuiltAt.Value = NetworkManager.ServerTime.Time;
            player.AddBuildingMaterial(material, -10); go.GetComponent<NetworkObject>().Spawn(); return true;
        }
        public bool TryEdit(Ranger player, BuildPiece piece, int mask, int quarterTurn, bool reset)
        {
            if (!IsServer || !CanModify(player, piece) || quarterTurn < 0 || quarterTurn > 3) return false;
            if (reset) mask = BuildPiece.FullMask(piece.Kind.Value);
            if (!BuildPiece.ValidMask(piece.Kind.Value, mask)) { player.InformServer("Keep a connected wall section. Use reset to restore the blueprint."); return false; }
            if (piece.Kind.Value == 0)
                foreach (var actor in Ranger.Players)
                {
                    if (!actor || !actor.Alive) continue;
                    Vector3 local = piece.transform.InverseTransformPoint(actor.transform.position);
                    if (Mathf.Abs(local.z - 2) > .4f || Mathf.Abs(local.x) > 2.3f) continue;
                    for (int cell = 0; cell < 9; cell++)
                    {
                        if ((mask & ~piece.Mask.Value & (1 << cell)) == 0) continue;
                        float left = -2 + cell % 3 * 4f / 3, low = cell / 3;
                        if (local.x + .3f > left && local.x - .3f < left + 4f / 3 && local.y < low + 1 && local.y + actor.StanceHeight > low)
                        { player.InformServer("A player is inside that edit. Keep the opening clear."); return false; }
                    }
                }
            // Rotation of wall/floor edits would move collision through opponents; only ramps/cones rotate in place.
            if (piece.Kind.Value >= 2) piece.transform.rotation = Quaternion.Euler(0, piece.GridYaw.Value + quarterTurn * 90, 0);
            piece.Mask.Value = mask; piece.DoorOpen.Value = false; return true;
        }
        public static bool CanModify(Ranger player, BuildPiece piece)
        { return player && player.Alive && player.CanAct && !player.CurrentVehicle && piece && piece.IsSpawned && piece.Team.Value == player.Team.Value && (piece.Center - player.transform.position).sqrMagnitude < 9 * 9; }
        public void StructureRemoved() { if (supportAt == 0) supportAt = Time.time + .15f; }
        static Vector3Int CellKey(Vector3 position) { return new Vector3Int(Mathf.RoundToInt(position.x / 4), Mathf.RoundToInt(position.y / .75f), Mathf.RoundToInt(position.z / 4)); }
        void Update()
        {
            if (!IsServer || supportAt == 0 || Time.time < supportAt) return; supportAt = 0;
            supported.Clear(); frontier.Clear(); cells.Clear();
            foreach (var piece in BuildPiece.All)
            {
                if (!piece) continue; var key = CellKey(piece.transform.position);
                if (!cells.TryGetValue(key, out var group)) { group = new List<BuildPiece>(); cells.Add(key, group); } group.Add(piece);
                if (Grounded(piece.transform.position)) { supported.Add(piece); frontier.Enqueue(piece); }
            }
            while (frontier.Count > 0)
            {
                var next = frontier.Dequeue();
                var center = CellKey(next.transform.position);
                for (int y = -4; y <= 4; y++) for (int x = -2; x <= 2; x++) for (int z = -2; z <= 2; z++)
                {
                    if (!cells.TryGetValue(center + new Vector3Int(x, y, z), out var group)) continue;
                    foreach (var piece in group)
                        if (piece && !supported.Contains(piece) && BuildGrid.Connected(next, piece.transform.position)) { supported.Add(piece); frontier.Enqueue(piece); }
                }
            }
            foreach (var piece in BuildPiece.All.ToArray()) if (piece && !supported.Contains(piece) && piece.IsSpawned) piece.NetworkObject.Despawn();
        }
    }
}
