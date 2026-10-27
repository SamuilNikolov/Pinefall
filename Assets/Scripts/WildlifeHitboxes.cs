using UnityEngine;

namespace Pinefall
{
    public static class WildlifeHitboxes
    {
        public static void Measure(Wildlife animal)
        {
            var body = animal.transform.Find("Body");
            if (!body) return;
            Bounds bounds = default; bool found = false;
            foreach (var renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                Bounds local;
                if (renderer is SkinnedMeshRenderer skin) local = skin.localBounds;
                else { var filter = renderer.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh) continue; local = filter.sharedMesh.bounds; }
                Matrix4x4 matrix = animal.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                }
            }
            if (!found || bounds.size.sqrMagnitude < .01f) return;
            animal.HitCenter = bounds.center;
            animal.HitSize = Vector3.Max(bounds.size * .94f, Vector3.one * .22f);
            var capsule = animal.GetComponent<CapsuleCollider>();
            if (capsule && !animal.Aquatic)
            {
                capsule.direction = 2; capsule.center = bounds.center;
                capsule.radius = Mathf.Max(.10f, Mathf.Min(bounds.extents.x, bounds.extents.y) * .84f);
                capsule.height = Mathf.Max(capsule.radius * 2, bounds.size.z * .95f);
            }
        }

        public static ShotHistory.Pose Pose(Wildlife animal, double time)
        {
            Vector3 size = Vector3.Scale(animal.HitSize, animal.transform.lossyScale);
            Vector3 center = animal.transform.TransformPoint(animal.HitCenter);
            return new ShotHistory.Pose { time = time, bottom = center - Vector3.up * size.y * .5f, height = size.y, radius = Mathf.Max(size.x, size.z) * .5f, yaw = animal.transform.eulerAngles.y, extent = size * .5f };
        }

        public static bool Ray(Vector3 origin, Vector3 direction, ShotHistory.Pose pose, float maximum, out float distance)
        {
            if (pose.extent.sqrMagnitude < .01f) return ShotHistory.RayCapsule(origin, direction, pose, maximum, out distance);
            var inverse = Quaternion.Euler(0, -pose.yaw, 0);
            Vector3 local = inverse * (origin - pose.bottom - Vector3.up * pose.extent.y), ray = inverse * direction;
            float near = 0, far = maximum;
            for (int axis = 0; axis < 3; axis++)
            {
                float extent = pose.extent[axis] + .045f;
                if (Mathf.Abs(ray[axis]) < .00001f) { if (Mathf.Abs(local[axis]) > extent) { distance = 0; return false; } continue; }
                float a = (-extent - local[axis]) / ray[axis], b = (extent - local[axis]) / ray[axis];
                near = Mathf.Max(near, Mathf.Min(a, b)); far = Mathf.Min(far, Mathf.Max(a, b));
                if (near > far) { distance = 0; return false; }
            }
            distance = near; return distance < maximum;
        }
    }
}
