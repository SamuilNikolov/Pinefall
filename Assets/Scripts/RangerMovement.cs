using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<bool> Sliding = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public float StanceHeight { get { return Downed.Value ? .75f : Sliding.Value ? .98f : Crouched.Value ? 1.25f : 1.8f; } }
        Vector3 slideDirection; float slideSpeed, slideEnds, slideCooldown; bool slideJumpRequested;
        readonly Dictionary<Transform, StanceRig> stanceRigs = new Dictionary<Transform, StanceRig>();
        sealed class StanceRig
        {
            public Vector3 position; public bool posed;
            public readonly List<Transform> bones = new List<Transform>();
            public readonly List<Quaternion> rotations = new List<Quaternion>();
            public readonly List<int> roles = new List<int>();
        }
        void StanceTick()
        {
            float height = StanceHeight;
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule && Mathf.Abs(capsule.height - height) > .01f) { capsule.height = height; capsule.center = Vector3.up * height * .5f; }
            if (controller && Mathf.Abs(controller.height - height) > .01f) { controller.height = height; controller.center = Vector3.up * height * .5f; }
            if (controller) controller.stepOffset = Mathf.Min(.42f, height * .45f);
        }
        public bool StartSlide(Vector3 direction)
        {
            if (!IsOwner || !Alive || !CanAct || CurrentVehicle || Swimming.Value || !CanSprint || stamina < .1f || Time.time < slideCooldown || !controller.isGrounded) return false;
            direction.y = 0; if (direction.sqrMagnitude < .01f) return false;
            slideDirection = direction.normalized; slideSpeed = Mathf.Max(10.8f, Velocity.Value + 1.4f); slideEnds = Time.time + 1.35f; slideCooldown = Time.time + 1.7f;
            Sliding.Value = true; Crouched.Value = true; stamina = Mathf.Max(0, stamina - .065f); return true;
        }
        public void MovementWithoutInput()
        {
            if (!controller || !controller.enabled || !Alive || CurrentVehicle) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (controller.isGrounded) vertical = -2; else vertical -= 18 * dt;
            Vector3 drift = Vector3.zero;
            if (Sliding.Value) { slideSpeed = Mathf.MoveTowards(slideSpeed, 0, dt * 5); drift = slideDirection * slideSpeed; if (Time.time >= slideEnds) Sliding.Value = false; }
            if (!SwimMove(drift, false)) controller.Move((drift + Vector3.up * vertical) * dt);
            float actual = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
            if (Mathf.Abs(Velocity.Value - actual) > .2f) Velocity.Value = actual;
            stamina = Mathf.Clamp01(stamina + dt * .23f);
        }
        Vector3 MovementInput(float x, float z, bool sprint)
        {
            bool crouch = GameBindings.Held("Crouch");
            if (GameBindings.Down("Crouch") && (sprint || Velocity.Value > 6.5f)) StartSlide(transform.forward * z + transform.right * x);
            if (Sliding.Value)
            {
                float slope = ReserveWorld.Height(transform.position.x, transform.position.z) - ReserveWorld.Height(transform.position.x + slideDirection.x, transform.position.z + slideDirection.z);
                slideSpeed = Mathf.Clamp(slideSpeed + (slope * 8 - 4.1f) * Time.deltaTime, 0, 13);
                if (GameBindings.Down("Jump")) slideJumpRequested = true;
                if (Time.time >= slideEnds || slideSpeed < 3 || Swimming.Value || slideJumpRequested) Sliding.Value = false;
                else { slideDirection = Vector3.RotateTowards(slideDirection, (transform.forward + transform.right * x * .3f).normalized, Time.deltaTime * .75f, 0); Crouched.Value = true; return slideDirection * slideSpeed; }
            }
            if (!crouch && Crouched.Value)
                crouch = Physics.CheckCapsule(transform.position + Vector3.up * .55f, transform.position + Vector3.up * 1.45f, .28f, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore);
            Crouched.Value = crouch;
            float speed = (crouch ? 2.5f : sprint ? 9.4f : 4.8f) * (Hunger.Value < 10 ? .7f : 1);
            return Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1) * speed;
        }
        public void ApplyStanceVisual(Transform model)
        {
            if (!model) return;
            if (!stanceRigs.TryGetValue(model, out var rigState))
            {
                rigState = new StanceRig { position = model.localPosition };
                foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                {
                    string name = bone.name.ToLowerInvariant();
                    int role = name.Contains("upleg") || name.Contains("upperleg") || name.Contains("thigh") ? 1 : name.Contains("lowerleg") || name.EndsWith("leg") || name.Contains("calf") ? 2 : name.Contains("spine") ? 3 : 0;
                    if (role == 0) continue; rigState.bones.Add(bone); rigState.rotations.Add(bone.localRotation); rigState.roles.Add(role);
                }
                stanceRigs.Add(model, rigState);
            }
            float drop = Sliding.Value ? .80f : Crouched.Value ? .55f : 0;
            model.localPosition = Vector3.Lerp(model.localPosition, rigState.position - Vector3.up * drop, Time.deltaTime * 16);
            bool pose = Crouched.Value || Sliding.Value;
            if (pose || rigState.posed)
                for (int i = 0; i < rigState.bones.Count; i++)
                {
                    float angle = !pose ? 0 : rigState.roles[i] == 1 ? Sliding.Value ? -70 : -48 : rigState.roles[i] == 2 ? Sliding.Value ? 20 : 85 : Sliding.Value ? -8 : 9;
                    rigState.bones[i].localRotation = rigState.rotations[i] * Quaternion.Euler(angle, 0, 0);
                }
            rigState.posed = pose;
        }
    }
}
