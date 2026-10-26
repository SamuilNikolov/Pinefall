using UnityEngine;
namespace Pinefall
{
    public static class PlayerHitboxes
    {
        // Anatomical capsules share the exact stance and orientation sampled in shot history.
        public static bool Ray(Vector3 origin, Vector3 direction, ShotHistory.Pose pose, float maximum, out float distance, out bool head)
        {
            distance = maximum; head = false; bool hit = false;
            float height = pose.height;
            Vector3 right = Quaternion.Euler(0, pose.yaw, 0) * Vector3.right;
            if (height < .9f)
            {
                var downedHead = new ShotHistory.Pose { bottom = pose.bottom - right * 1.52f + Vector3.up * .39f, height = .38f, radius = .19f };
                if (ShotHistory.RayCapsule(origin, direction, downedHead, distance, out float headDistance)) { distance = headDistance; head = true; hit = true; }
                for (int part = 0; part < 5; part++)
                {
                    var lowBody = new ShotHistory.Pose { bottom = pose.bottom - right * (.28f * part) + Vector3.up * .04f, height = .60f, radius = .22f };
                    if (ShotHistory.RayCapsule(origin, direction, lowBody, distance, out float bodyDistance)) { distance = bodyDistance; head = false; hit = true; }
                }
                return hit;
            }
            var skull = new ShotHistory.Pose { bottom = pose.bottom + Vector3.up * (height - .39f), height = .39f, radius = .195f };
            if (ShotHistory.RayCapsule(origin, direction, skull, distance, out float d)) { distance = d; head = true; hit = true; }
            // Torso/hips and separated legs avoid the empty rectangular space around the model.
            float hip = height >= 1.7f ? .70f : height >= 1.2f ? .37f : .18f;
            var torso = new ShotHistory.Pose { bottom = pose.bottom + Vector3.up * hip, height = Mathf.Max(.44f, height - hip - .24f), radius = .285f };
            if (ShotHistory.RayCapsule(origin, direction, torso, distance, out d)) { distance = d; head = false; hit = true; }
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = new ShotHistory.Pose { bottom = pose.bottom + right * (.15f * side), height = Mathf.Max(.28f, hip + .19f), radius = .10f };
                if (ShotHistory.RayCapsule(origin, direction, leg, distance, out d)) { distance = d; head = false; hit = true; }
                var arm = new ShotHistory.Pose { bottom = pose.bottom + right * (.34f * side) + Vector3.up * (height * .47f), height = height * .33f, radius = .115f };
                if (ShotHistory.RayCapsule(origin, direction, arm, distance, out d)) { distance = d; head = false; hit = true; }
            }
            if (height < 1.15f)
            {
                Vector3 forward = Quaternion.Euler(0, pose.yaw, 0) * Vector3.forward;
                for (int side = -1; side <= 1; side += 2) for (int segment = 1; segment <= 3; segment++)
                {
                    var slideLeg = new ShotHistory.Pose { bottom = pose.bottom + right * (.15f * side) + forward * (.24f * segment) + Vector3.up * .04f, height = .3f, radius = .14f };
                    if (ShotHistory.RayCapsule(origin, direction, slideLeg, distance, out d)) { distance = d; head = false; hit = true; }
                }
            }
            return hit;
        }
    }
}
