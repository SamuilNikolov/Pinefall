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
        Vector3 airMomentum;float jumpBufferedUntil=-10,lastGroundedAt=-10,lastJumpAt=-10;
        public bool SmokeBunnyHop;
        public void ResetHopMomentum(){airMomentum=Vector3.zero;jumpBufferedUntil=lastGroundedAt=lastJumpAt=-10;}
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
            if (!IsOwner || !Alive || !CanAct || CurrentVehicle || Swimming.Value || !CanSprint || stamina < .1f || Time.time < slideCooldown || Velocity.Value < 6.5f || !controller.isGrounded) return false;
            direction.y = 0; if (direction.sqrMagnitude < .01f) return false;
            slideDirection = direction.normalized; slideSpeed = Mathf.Min(26,Mathf.Max(10.8f, Velocity.Value + 1.4f)); slideEnds = Time.time + 1.35f; slideCooldown = Time.time + 1.7f;
            Sliding.Value = true; Crouched.Value = true; stamina = Mathf.Max(0, stamina - .065f); return true;
        }
        public void MovementWithoutInput()
        {
            if (!controller || !controller.enabled || !Alive || CurrentVehicle) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if(SmokeBunnyHop&&ReserveSession.Arg("--smoke")!="")
            {
                Vector3 synthetic=ResolveJumpMovement(transform.forward*9.4f,true,false,dt);controller.Move((synthetic+Vector3.up*vertical)*dt);
                Velocity.Value=new Vector2(controller.velocity.x,controller.velocity.z).magnitude;return;
            }
            Vector3 drift=controller.isGrounded?Vector3.zero:airMomentum;bool air=LaunchMotion(ref drift);
            if(!air){if(controller.isGrounded){vertical=-2;airMomentum=Vector3.zero;}else vertical-=18*dt;}
            if (Sliding.Value) { slideSpeed = Mathf.MoveTowards(slideSpeed, 0, dt * 5); drift = slideDirection * slideSpeed; if (Time.time >= slideEnds) Sliding.Value = false; }
            if (!SwimMove(drift, false)) controller.Move((drift + Vector3.up * vertical) * dt);
            float actual = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
            if (Mathf.Abs(Velocity.Value - actual) > .2f) Velocity.Value = actual;
            stamina = Mathf.Clamp01(stamina + dt * .23f);
        }
        Vector3 MovementInput(float x, float z, bool sprint)
        {
            bool crouch = GameBindings.Held("Crouch");
            if (GameBindings.Down("Crouch") && Velocity.Value >= 6.5f) StartSlide(transform.forward * z + transform.right * x);
            if (Sliding.Value)
            {
                float slope = ReserveWorld.Height(transform.position.x, transform.position.z) - ReserveWorld.Height(transform.position.x + slideDirection.x, transform.position.z + slideDirection.z);
                slideSpeed = Mathf.Clamp(slideSpeed + (slope * 8 - 4.1f) * Time.deltaTime, 0, 26);
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
        Vector3 ResolveJumpMovement(Vector3 desired,bool held,bool pressed,float dt)
        {
            dt=Mathf.Min(dt,.05f);bool grounded=controller.isGrounded;
            if(pressed)jumpBufferedUntil=Time.time+.15f;
            if(grounded)lastGroundedAt=Time.time;
            bool wants=Time.time<=jumpBufferedUntil||held&&grounded;
            bool jumped=wants&&(grounded||Time.time-lastGroundedAt<=.10f)&&Time.time-lastJumpAt>.18f&&(!Crouched.Value||Sliding.Value||slideJumpRequested)&&!Swimming.Value;
            if(jumped)
            {
                bool chained=Time.time-lastJumpAt<1.18f&&airMomentum.magnitude>6.5f;
                jumpBufferedUntil=-10;lastJumpAt=Time.time;Sliding.Value=false;Crouched.Value=false;vertical=8.3f;
                if(airMomentum.sqrMagnitude<desired.sqrMagnitude)airMomentum=desired;
                if(airMomentum.sqrMagnitude<.01f)airMomentum=desired;
                if(chained)airMomentum=airMomentum.normalized*(airMomentum.magnitude+1.7f);
            }
            else if(grounded)vertical=-2;else vertical-=18*dt;
            if(Sliding.Value){airMomentum=desired;return desired;}
            if(grounded&&!jumped)airMomentum=desired;
            else if(desired.sqrMagnitude>.01f)
            {
                float retained=Mathf.Max(airMomentum.magnitude,desired.magnitude);
                airMomentum=Vector3.MoveTowards(airMomentum,desired.normalized*retained,6*dt);
            }
            float cap=CanSprint?24f:4.8f*(Hunger.Value<10?.7f:1);
            airMomentum=Vector3.ClampMagnitude(airMomentum,cap);return airMomentum;
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
