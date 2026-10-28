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

        // Keep a small dead band at the 45-degree boundary instead of flickering
        // between two walls when the mouse rests on a diagonal.
        public static int StableTurn(float yaw, float frameYaw, int previous)
        {
            return Mathf.Abs(Mathf.DeltaAngle(frameYaw + previous * 90, yaw)) <= 51
                ? previous : FacingTurn(yaw, frameYaw);
        }

        public static float Storey(float feetHeight, float originHeight)
        { return originHeight + Mathf.Floor((feetHeight - originHeight + .35f) / 3) * 3; }

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
            var aimed = feet + forward * (kind == 0 ? .25f : .8f);
            aimed.y = level;
            return Snap(aimed, origin, yaw);
        }

        public static Vector3 AimTarget(Vector3 feet, Ray aim, int kind, Vector3 origin, float yaw, float level)
        {
            var flat = Vector3.ProjectOnPlane(aim.direction, Vector3.up);
            if (flat.sqrMagnitude < .001f) flat = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            if (kind == 0) return Target(feet, flat, kind, origin, yaw, level);
            // Looking down permits extending a floor over an edge. Looking at the
            // horizon keeps the piece close enough to climb rather than 8 m away.
            float distance = .8f;
            if (aim.direction.y < -.12f)
            {
                float t = (level - aim.origin.y) / aim.direction.y;
                if (t > 0) distance = Mathf.Clamp(Vector3.Dot(aim.GetPoint(t) - feet, flat.normalized), .25f, 4.2f);
            }
            var target = feet + flat.normalized * distance;
            target.y = level;
            return Snap(target, origin, yaw);
        }
    }
}
