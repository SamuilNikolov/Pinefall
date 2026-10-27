using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public static class RoundArena
    {
        public const int MaximumPlayers = 20;
        public static int TierFor(int participants) { return participants <= 5 ? 0 : participants <= 12 ? 1 : 2; }
        public static float SizeFor(int tier) { return tier == 0 ? 2048 : tier == 1 ? 4096 : ReserveWorld.Size; }
        public static bool Restricted { get { var match = WildfrontMatch.Current; return match && match.Battle.Value; } }
        public static float Size { get { return Restricted ? SizeFor(WildfrontMatch.Current.ArenaTier.Value) : ReserveWorld.Size; } }
        public static float Half { get { return Mathf.Min(Size * .5f - 32,ReserveWorld.Edge); } }
        public static Vector2 Center { get { return Vector2.zero; } }
        public static string Label { get { return Size < 3000 ? "Small / 2 km" : Size < 5000 ? "Medium / 4 km" : "Full reserve / 6.1 km"; } }
        public static bool Contains(Vector3 point, float margin = 0)
        { float half = Restricted ? Half - margin : ReserveWorld.Edge - margin; return Mathf.Abs(point.x) <= half && Mathf.Abs(point.z) <= half; }
        public static bool Includes(District site) { return Contains(new Vector3(site.center.x, 0, site.center.y), 35); }
        public static Vector3 Clamp(Vector3 point, float margin = 0)
        { float half = Restricted ? Half - margin : ReserveWorld.Edge - margin; point.x = Mathf.Clamp(point.x, -half, half); point.z = Mathf.Clamp(point.z, -half, half); return point; }
    }

    public sealed partial class WildfrontMatch
    {
        public NetworkVariable<int> ArenaTier = new NetworkVariable<int>(2);
        public void SelectArena()
        {
            if (!IsServer || Phase.Value != 0) return;
            int count = 0; foreach (var player in Ranger.Players) if (player && player.IsSpawned) count++;
            ArenaTier.Value = RoundArena.TierFor(count);
        }
    }
}
