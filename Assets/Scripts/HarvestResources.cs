using System.Collections.Generic;
using UnityEngine;

namespace Pinefall
{
    public sealed class HarvestNode
    {
        public int id, material, health, maximum;
        public Collider collider;
        public readonly List<Handle> handles = new List<Handle>(2);
        public struct Handle { public Matrix4x4[] matrices; public int index; public Matrix4x4 original; }
        public Vector3 Position { get { return collider ? collider.transform.position : Vector3.zero; } }
        public void Apply(int value)
        {
            HarvestResources.Version++;
            health = Mathf.Clamp(value, 0, maximum);
            if (collider) collider.enabled = health > 0;
            foreach (var handle in handles)
                handle.matrices[handle.index] = health > 0 ? handle.original : Matrix4x4.TRS(Position, Quaternion.identity, Vector3.zero);
        }
    }
    public static class HarvestResources
    {
        public static int Version;
        public static readonly List<HarvestNode> Nodes = new List<HarvestNode>();
        static readonly Dictionary<Collider, HarvestNode> lookup = new Dictionary<Collider, HarvestNode>();
        public static HarvestNode Register(Collider collider, int material, int health)
        {
            var node = new HarvestNode { id = Nodes.Count, collider = collider, material = material, health = health, maximum = health };
            Nodes.Add(node); lookup.Add(collider, node); return node;
        }
        public static HarvestNode Find(Collider collider) { return collider && lookup.TryGetValue(collider, out var node) ? node : null; }
        public static void Restore() { foreach (var node in Nodes) if (node.health != node.maximum) node.Apply(node.maximum); }
        public static void Clear() { Nodes.Clear(); lookup.Clear(); }
        public static void Bind(List<HarvestNode> nodes, int start, int count, Matrix4x4[] matrices)
        {
            if (nodes == null) return;
            for (int i = 0; i < count; i++) nodes[start + i].handles.Add(new HarvestNode.Handle { matrices = matrices, index = i, original = matrices[i] });
        }
    }
}
