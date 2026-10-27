using UnityEngine;

namespace Pinefall
{
    public static class BuildGrid
    {
        public static Vector3 Snap(Vector3 point, Vector3 origin, float yaw)
        {
            var rotation = Quaternion.Euler(0, yaw, 0);
            var local = Quaternion.Inverse(rotation) * (point - origin);
            local.x = Mathf.Round(local.x / 4) * 4;
            local.z = Mathf.Round(local.z / 4) * 4;
            local.y = Mathf.Round(local.y / 3) * 3;
            return origin + rotation * local;
        }

        public static int FacingTurn(float yaw, float frameYaw, int offset = 0)
        { return ((Mathf.RoundToInt(Mathf.DeltaAngle(frameYaw, yaw) / 90) + offset) % 4 + 4) % 4; }

        public static bool Connected(BuildPiece piece, Vector3 point)
        {
            var delta = Quaternion.Euler(0, -piece.GridYaw.Value, 0) * (piece.transform.position - point);
            return Mathf.Abs(delta.y) <= 3.05f && ((Mathf.Abs(delta.x) < .12f && Mathf.Abs(delta.z) < 4.12f) || (Mathf.Abs(delta.z) < .12f && Mathf.Abs(delta.x) < 4.12f));
        }

        public static Vector3 RootOrigin(Vector3 feet, float surface)
        { return new Vector3(feet.x, Mathf.Ceil((surface + .025f) / .75f) * .75f, feet.z); }

        // Walls occupy the boundary of the current cell. Ramps lead into the next cell;
        // floors/cones follow the feet, including a jump onto the next 3 m storey.
        public static Vector3 Target(Vector3 feet, Vector3 forward, int kind, Vector3 origin, float yaw, float level)
        {
            forward.y = 0; forward.Normalize();
            var aimed = feet + forward * (kind == 2 ? 2.2f : kind == 0 ? .2f : .55f);
            aimed.y = level;
            return Snap(aimed, origin, yaw);
        }
    }
}
