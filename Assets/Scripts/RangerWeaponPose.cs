using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        sealed class ArmChain { public Transform upper, lower, hand; public int side; }
        readonly Dictionary<Transform, List<ArmChain>> weaponArms = new Dictionary<Transform, List<ArmChain>>();
        public void ApplyWeaponPose(Transform model, Transform held)
        {
            if (!model || !held || !Alive || Downed.Value || IsEmoting) return;
            if (!weaponArms.TryGetValue(model, out var arms))
            {
                arms = new List<ArmChain>();
                foreach (var upper in model.GetComponentsInChildren<Transform>(true))
                {
                    string name = upper.name.ToLowerInvariant(); if (name != "upperarm_l" && name != "upperarm_r") continue;
                    int side = name.EndsWith("_l") ? -1 : 1; Transform lower = null, hand = null;
                    foreach (var bone in upper.GetComponentsInChildren<Transform>(true)) { if (bone.name.ToLowerInvariant() == (side < 0 ? "lowerarm_l" : "lowerarm_r")) lower = bone; if (bone.name.ToLowerInvariant() == (side < 0 ? "hand_l" : "hand_r")) hand = bone; }
                    if (lower && hand) arms.Add(new ArmChain { upper = upper, lower = lower, hand = hand, side = side });
                }
                weaponArms.Add(model, arms);
            }
            foreach (var arm in arms)
            {
                Vector3 grip = ToolMode.Value != 0 ? new Vector3(0, arm.side > 0 ? .03f : .23f, 0) : Weapon.Value == 7 ? new Vector3(arm.side > 0 ? 0 : -.04f, arm.side > 0 ? -.12f : .03f, arm.side > 0 ? .20f : .55f) : new Vector3(arm.side > 0 ? 0 : -.03f, arm.side > 0 ? -.08f : -.03f, arm.side > 0 ? .05f : .34f);
                Vector3 target = held.TransformPoint(grip), shoulder = arm.upper.position;
                float upperLength = Vector3.Distance(shoulder, arm.lower.position), lowerLength = Vector3.Distance(arm.lower.position, arm.hand.position);
                Vector3 delta = target - shoulder; float length = Mathf.Clamp(delta.magnitude, .01f, upperLength + lowerLength - .005f);
                if (upperLength < .001f || lowerLength < .001f) continue;
                Vector3 direction = delta.normalized, pole = Vector3.ProjectOnPlane(transform.right * arm.side * .7f - Vector3.up, direction).normalized;
                float cosine = Mathf.Clamp((upperLength * upperLength + length * length - lowerLength * lowerLength) / (2 * upperLength * length), -1, 1);
                Vector3 elbow = shoulder + direction * (cosine * upperLength) + pole * (Mathf.Sqrt(1 - cosine * cosine) * upperLength);
                arm.upper.rotation = Quaternion.FromToRotation(arm.lower.position - shoulder, elbow - shoulder) * arm.upper.rotation;
                arm.lower.rotation = Quaternion.FromToRotation(arm.hand.position - arm.lower.position, target - arm.lower.position) * arm.lower.rotation;
            }
        }
    }
}
