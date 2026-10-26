using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed class BuildPiece : NetworkBehaviour
    {
        public const float Width = 4, Height = 3;
        public static readonly List<BuildPiece> All = new List<BuildPiece>();
        public static readonly string[] Shapes = { "Wall", "Floor", "Stairs", "Pyramid" };
        public static readonly string[] Materials = { "Wood", "Stone", "Metal" };
        public NetworkVariable<int> Kind = new NetworkVariable<int>(), Material = new NetworkVariable<int>(), Mask = new NetworkVariable<int>(511);
        public NetworkVariable<int> Health = new NetworkVariable<int>(80), Team = new NetworkVariable<int>();
        public NetworkVariable<ulong> Builder = new NetworkVariable<ulong>();
        public NetworkVariable<bool> DoorOpen = new NetworkVariable<bool>();
        public NetworkVariable<double> BuiltAt = new NetworkVariable<double>();
        public int Maximum { get { return MaxHealth(Material.Value); } }
        public static int MaxHealth(int material) { return material == 0 ? 300 : material == 1 ? 450 : 600; }
        public static int FullMask(int kind) { return kind == 0 ? 511 : kind == 3 ? 0 : 15; }
        GameObject model; Transform door; float nextGrowth;
        void Awake() { NetworkSeed.Initialize(this); }
        public override void OnNetworkSpawn()
        {
            All.Add(this); Kind.OnValueChanged += Changed; Material.OnValueChanged += Changed; Mask.OnValueChanged += Changed;
            DoorOpen.OnValueChanged += DoorChanged; Rebuild();
        }
        public override void OnNetworkDespawn()
        {
            All.Remove(this); Kind.OnValueChanged -= Changed; Material.OnValueChanged -= Changed; Mask.OnValueChanged -= Changed; DoorOpen.OnValueChanged -= DoorChanged;
        }
        void Changed(int previous, int next) { Rebuild(); }
        void DoorChanged(bool previous, bool next) { if (door) door.localRotation = Quaternion.Euler(0, next ? -100 : 0, 0); }
        public void SetPreviewHidden(bool hidden)
        { if (model) foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) if (renderer.name == "Surface render" || renderer.transform.parent && renderer.transform.parent.name == "Door hinge") renderer.enabled = !hidden; }
        void Rebuild()
        {
            if (model) { model.SetActive(false); Destroy(model); }
            model = new GameObject("Built " + Shapes[Mathf.Clamp(Kind.Value, 0, 3)]); model.transform.SetParent(transform, false);
            BuildGeometry.Create(model.transform, Kind.Value, Mask.Value, BuildGeometry.Surface(Material.Value), true);
            door = model.transform.Find("Door hinge"); DoorChanged(false, DoorOpen.Value);
        }
        void Update()
        {
            if (!IsServer || !IsSpawned || Time.time < nextGrowth) return; nextGrowth = Time.time + .25f;
            if (NetworkManager.ServerTime.Time - BuiltAt.Value < 6) Health.Value = Mathf.Min(Maximum, Health.Value + Mathf.CeilToInt(Maximum / 24f));
        }
        public void Damage(int amount)
        {
            if (!IsServer || !IsSpawned || amount <= 0) return;
            Health.Value = Mathf.Max(0, Health.Value - amount);
            if (Health.Value == 0) { ConstructionWorld.Current.StructureRemoved(); NetworkObject.Despawn(); }
        }
        public static bool ValidMask(int kind, int mask)
        {
            if (kind == 3) return mask >= 0 && mask <= 15;
            int full = FullMask(kind); if (mask < 1 || mask > full) return false;
            if (kind != 0) return true;
            // Connected remaining wall cells prevent unsupported floating edit fragments.
            int visited = mask & -mask;
            for (int n = 0; n < 9; n++)
                for (int i = 0; i < 9; i++) if ((visited & (1 << i)) != 0)
                {
                    int x = i % 3, y = i / 3;
                    if (x > 0) visited |= mask & (1 << (i - 1)); if (x < 2) visited |= mask & (1 << (i + 1));
                    if (y > 0) visited |= mask & (1 << (i - 3)); if (y < 2) visited |= mask & (1 << (i + 3));
                }
            return visited == mask;
        }
        public Vector3 Center { get { return transform.TransformPoint(new Vector3(0, Kind.Value == 0 ? 1.5f : .8f, Kind.Value == 0 ? 2 : 0)); } }
    }
    public static class BuildGeometry
    {
        static readonly Material[] surfaces = new Material[3];
        public static Material Surface(int material)
        {
            material = Mathf.Clamp(material, 0, 2);
            if (!surfaces[material])
            {
                surfaces[material] = Resources.Load<Material>("Build" + BuildPiece.Materials[material]);
                if (!surfaces[material]) { surfaces[material] = new Material(Shader.Find("Standard")); surfaces[material].color = material == 0 ? new Color(.45f, .30f, .17f) : material == 1 ? new Color(.44f, .45f, .43f) : new Color(.30f, .39f, .44f); }
            }
            return surfaces[material];
        }
        static GameObject Box(Transform root, Vector3 position, Vector3 size, Material material, bool collision, string name = "Panel")
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(root, false); obj.transform.localPosition = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material; if (!collision) Object.Destroy(obj.GetComponent<Collider>()); return obj;
        }
        public static void Create(Transform root, int kind, int mask, Material surface, bool collision)
        {
            CreateParts(root, kind, mask, surface, collision);
            // One render mesh per piece; cut-cell physics stays separate and doors retain a hinge.
            var parts = new List<CombineInstance>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.transform.parent && filter.transform.parent.name == "Door hinge") continue;
                var renderer = filter.GetComponent<MeshRenderer>(); if (!renderer) continue;
                parts.Add(new CombineInstance { mesh = filter.sharedMesh, transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix }); renderer.enabled = false;
            }
            if (parts.Count == 0) return;
            var mesh = new Mesh { name = "Combined build surface" }; mesh.CombineMeshes(parts.ToArray(), true, true);
            var output = new GameObject("Surface render"); output.transform.SetParent(root, false); output.AddComponent<MeshFilter>().sharedMesh = mesh;
            output.AddComponent<MeshRenderer>().sharedMaterial = surface; output.AddComponent<OwnedBuildMesh>().mesh = mesh;
        }
        static void CreateParts(Transform root, int kind, int mask, Material surface, bool collision)
        {
            if (kind == 0)
            {
                if (mask == 95 || mask == 311 || mask == 416 || mask == 200)
                {
                    Vector3[] triangle = mask == 95 ? new[] { new Vector3(-2, 0, 2), new Vector3(2, 0, 2), new Vector3(-2, 3, 2) } :
                        mask == 311 ? new[] { new Vector3(-2, 0, 2), new Vector3(2, 0, 2), new Vector3(2, 3, 2) } :
                        mask == 416 ? new[] { new Vector3(2, 0, 2), new Vector3(2, 3, 2), new Vector3(-2, 3, 2) } :
                        new[] { new Vector3(-2, 0, 2), new Vector3(-2, 3, 2), new Vector3(2, 3, 2) };
                    MeshSurface(root, triangle, new[] { 0, 1, 2 }, surface, collision, true); return;
                }
                for (int i = 0; i < 9; i++) if ((mask & (1 << i)) != 0)
                {
                    Vector3 p = new Vector3((i % 3 - 1) * 4f / 3, i / 3 + .5f, 2);
                    Box(root, p, new Vector3(4f / 3, 1, .16f), surface, collision);
                    Box(root, p + new Vector3(0, -.42f, -.10f), new Vector3(1.25f, .08f, .08f), surface, false, "Wall framing");
                }
                if ((511 ^ mask) == ((1 << 1) | (1 << 4)))
                {
                    var hinge = new GameObject("Door hinge"); hinge.transform.SetParent(root, false); hinge.transform.localPosition = new Vector3(-2f / 3, 0, 2);
                    Box(hinge.transform, new Vector3(2f / 3, 1, 0), new Vector3(1.27f, 1.96f, .12f), surface, collision, "Door");
                }
                return;
            }
            if (kind == 1)
            {
                for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0)
                    Box(root, new Vector3(i % 2 == 0 ? -1 : 1, -.08f, i / 2 == 0 ? -1 : 1), new Vector3(2, .16f, 2), surface, collision);
                if (collision && ConstructionWorld.Grounded(root.position))
                    for (int i = 0; i < 4; i++)
                    {
                        var corner = new Vector3(i % 2 == 0 ? -1.8f : 1.8f, 0, i / 2 == 0 ? -1.8f : 1.8f); Vector3 world = root.TransformPoint(corner);
                        float gap = world.y - ReserveWorld.Height(world.x, world.z);
                        if (gap > .12f && gap < 1) Box(root, corner - Vector3.up * gap * .5f, new Vector3(.14f, gap, .14f), surface, false, "Foundation foot");
                    }
                return;
            }
            if (kind == 2)
            {
                for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0)
                    for (int step = 0; step < 4; step++)
                    {
                        float z = -2 + (i / 2) * 2 + (step + .5f) * .5f, top = (z + 2 + .25f) * .75f;
                        Box(root, new Vector3(i % 2 == 0 ? -1 : 1, top - .12f, z), new Vector3(2, .24f, .5f), surface, collision, "Stair tread");
                        Box(root, new Vector3(i % 2 == 0 ? -1 : 1, top - .19f, z - .23f), new Vector3(2, .37f, .045f), surface, collision, "Stair riser");
                    }
                for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float x = (i % 2 == 0 ? -1 : 1) + side * .87f, z = i / 2 == 0 ? -1 : 1;
                        var beam = Box(root, new Vector3(x, (z + 2) * .75f - .17f, z), new Vector3(.12f, .14f, 2.5f), surface, false, "Stair stringer"); beam.transform.localRotation = Quaternion.Euler(-36.87f, 0, 0);
                    }
                // Smooth invisible walk surface avoids high-speed step catches.
                if (collision)
                    for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0)
                    {
                        float x = i % 2 == 0 ? -2 : 0, z = i / 2 == 0 ? -2 : 0;
                        MeshSurface(root, new[] { new Vector3(x, (z + 2) * .75f, z), new Vector3(x + 2, (z + 2) * .75f, z), new Vector3(x + 2, (z + 4) * .75f, z + 2), new Vector3(x, (z + 4) * .75f, z + 2) }, new[] { 0, 2, 1, 0, 3, 2 }, surface, true, false);
                    }
                return;
            }
            Vector3[] corners = { new Vector3(-2, 0, -2), new Vector3(2, 0, -2), new Vector3(2, 0, 2), new Vector3(-2, 0, 2), new Vector3(0, 3, 0) };
            if (mask != 0) { for (int i = 0; i < 4; i++) corners[i].y = (mask & (1 << i)) != 0 ? 3 : 0; corners[4].y = (corners[0].y + corners[1].y + corners[2].y + corners[3].y) * .25f; }
            MeshSurface(root, corners, new[] { 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 }, surface, collision, true);
        }
        static void MeshSurface(Transform root, Vector3[] points, int[] triangles, Material surface, bool collision, bool render)
        {
            var mesh = new Mesh { name = "Construction surface" }; var vertices = new Vector3[points.Length * 2]; points.CopyTo(vertices, 0); points.CopyTo(vertices, points.Length); mesh.vertices = vertices;
            var both = new int[triangles.Length * 2]; triangles.CopyTo(both, 0);
            for (int i = 0; i < triangles.Length; i += 3) { both[triangles.Length + i] = triangles[i] + points.Length; both[triangles.Length + i + 1] = triangles[i + 2] + points.Length; both[triangles.Length + i + 2] = triangles[i + 1] + points.Length; }
            bool vertical = true; foreach (var p in points) if (Mathf.Abs(p.z - points[0].z) > .01f) vertical = false;
            mesh.triangles = both; var uv = new Vector2[vertices.Length]; for (int i = 0; i < vertices.Length; i++) uv[i] = new Vector2(vertices[i].x, vertical ? vertices[i].y : vertices[i].z); mesh.uv = uv; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject("Construction surface"); obj.transform.SetParent(root, false);
            if (render) { obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = surface; }
            if (collision) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            obj.AddComponent<OwnedBuildMesh>().mesh = mesh;
        }
    }
    public sealed class OwnedBuildMesh : MonoBehaviour { public Mesh mesh; void OnDestroy() { if (mesh) Destroy(mesh); } }
}
