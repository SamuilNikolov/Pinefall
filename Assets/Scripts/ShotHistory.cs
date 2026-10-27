using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    // Samples are bounded and queried analytically: rewinding never moves live colliders.
    public sealed class ShotHistory : MonoBehaviour
    {
        public const double Window = .5;
        public static ShotHistory Current;
        public struct Pose { public double time; public Vector3 bottom, extent; public float height, radius, yaw; }
        sealed class Track { public readonly Pose[] poses = new Pose[32]; public int count, head; }
        readonly Dictionary<ulong, Track> tracks = new Dictionary<ulong, Track>();
        readonly List<ulong> stale = new List<ulong>();
        float nextPrune;
        void Awake() { Current = this; }
        void FixedUpdate()
        {
            var session = ReserveSession.Instance;
            if (!session || !session.Network || !session.Network.IsServer) return;
            double now = session.Network.ServerTime.Time;
            foreach (var player in Ranger.Players)
                if (player && player.IsSpawned && (player.OwnerClientId == NetworkManager.ServerClientId || player.IsBot.Value)) Record(player.NetworkObjectId, player.transform.position, player.StanceHeight, .33f, now, player.transform.eulerAngles.y);
            foreach (var animal in session.Animals)
            {
                if (!animal || !animal.IsSpawned || animal.Dead) continue;
                var pose=WildlifeHitboxes.Pose(animal,now);
                Record(animal.NetworkObjectId,pose.bottom,pose.height,pose.radius,now,pose.yaw,pose.extent);
            }
            if (Time.time < nextPrune) return;
            nextPrune = Time.time + 2; stale.Clear();
            foreach (var pair in tracks)
                if (pair.Value.count > 0 && now - pair.Value.poses[(pair.Value.head + 31) % 32].time > 1) stale.Add(pair.Key);
            foreach (ulong id in stale) tracks.Remove(id);
        }
        public void Record(ulong id, Vector3 bottom, float height, float radius, double now, float yaw = 0, Vector3 extent = default)
        {
            if (!tracks.TryGetValue(id, out var track)) { track = new Track(); tracks.Add(id, track); }
            track.poses[track.head] = new Pose { time = now, bottom = bottom, height = height, radius = radius, yaw = yaw, extent = extent };
            track.head = (track.head + 1) % 32; track.count = Math.Min(32, track.count + 1);
        }
        public Pose Get(ulong id, double time, Vector3 fallback, float height, float radius)
        {
            var result = new Pose { time = time, bottom = fallback, height = height, radius = radius };
            if (!tracks.TryGetValue(id, out var track) || track.count == 0) return result;
            var newer = track.poses[(track.head + 31) % 32];
            if (time >= newer.time) return newer;
            for (int n = 1; n < track.count; n++)
            {
                var older = track.poses[(track.head + 31 - n) % 32];
                if (time >= older.time)
                {
                    float t = Mathf.Clamp01((float)((time - older.time) / Math.Max(.0001, newer.time - older.time)));
                    // Teleports are discontinuities, not a sweep through intervening cover.
                    if ((newer.bottom - older.bottom).sqrMagnitude > 100) return t < .5f ? older : newer;
                    return new Pose { time = time, bottom = Vector3.Lerp(older.bottom, newer.bottom, t), height = Mathf.Lerp(older.height, newer.height, t), radius = Mathf.Lerp(older.radius, newer.radius, t), yaw = Mathf.LerpAngle(older.yaw, newer.yaw, t), extent=Vector3.Lerp(older.extent,newer.extent,t) };
                }
                newer = older;
            }
            return newer;
        }
        public static bool RayCapsule(Vector3 origin, Vector3 direction, Pose pose, float maximum, out float distance)
        {
            // Closest approach to the capsule's vertical axis, then the ray entry point.
            float radius = pose.radius + .035f, height = Mathf.Max(radius * 2, pose.height);
            Vector3 a = pose.bottom + Vector3.up * radius, b = pose.bottom + Vector3.up * (height - radius);
            Vector3 axis = b - a, offset = origin - a;
            float length = axis.magnitude;
            if (length < .001f) { distance = SphereEntry(origin, direction, a, radius); return distance >= 0 && distance < maximum; }
            Vector3 unit = axis / length;
            float dd = Vector3.Dot(direction, unit), od = Vector3.Dot(offset, unit);
            Vector3 perpendicular = direction - unit * dd, projected = offset - unit * od;
            float aa = perpendicular.sqrMagnitude, bb = Vector3.Dot(perpendicular, projected), cc = projected.sqrMagnitude - radius * radius;
            distance = float.PositiveInfinity;
            float discriminant = bb * bb - aa * cc;
            if (aa > .000001f && discriminant >= 0)
            {
                float t = (-bb - Mathf.Sqrt(discriminant)) / aa;
                float y = od + t * dd;
                if (t >= 0 && y >= 0 && y <= length) distance = t;
            }
            float first = SphereEntry(origin, direction, a, radius), second = SphereEntry(origin, direction, b, radius);
            if (first >= 0) distance = Mathf.Min(distance, first);
            if (second >= 0) distance = Mathf.Min(distance, second);
            return distance >= 0 && distance < maximum;
        }
        static float SphereEntry(Vector3 origin, Vector3 direction, Vector3 center, float radius)
        {
            Vector3 offset = origin - center; float b = Vector3.Dot(offset, direction), c = offset.sqrMagnitude - radius * radius;
            float d = b * b - c; if (d < 0) return -1;
            float t = -b - Mathf.Sqrt(d); return t >= 0 ? t : c <= 0 ? 0 : -1;
        }
        public void ResetSession() { tracks.Clear(); }
        public void Teleport(ulong id, Vector3 point, float height, double now, float yaw)
        { tracks.Remove(id); Record(id, point, height, .33f, now, yaw); }
        void OnDestroy() { if (Current == this) Current = null; }
    }
}
