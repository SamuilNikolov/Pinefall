using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinefall
{
    public static class ReserveEffects
    {
        sealed class Burst { public ParticleSystem system; public Transform root; }
        static readonly List<Burst> bursts = new List<Burst>();
        static readonly List<Tracer> tracers = new List<Tracer>();
        static Material particle, trail; static Texture2D soft;
        static void Materials()
        {
            if (particle) return;
            soft = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            { float radius = new Vector2(x - 15.5f, y - 15.5f).magnitude / 15.5f; soft.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - radius), 1.5f))); }
            soft.Apply(false, true); particle = new Material(Shader.Find("Particles/Standard Unlit")); particle.mainTexture = soft;
            particle.SetFloat("_Mode", 2); particle.SetInt("_SrcBlend", 5); particle.SetInt("_DstBlend", 10); particle.SetInt("_ZWrite", 0); particle.EnableKeyword("_ALPHABLEND_ON"); particle.renderQueue = 3000;
            trail = new Material(Shader.Find("Sprites/Default"));
        }
        static Burst Available()
        {
            Materials(); foreach (var burst in bursts) if (burst.system && !burst.system.IsAlive()) return burst;
            if (bursts.Count >= 24) return null;
            var go = new GameObject("Pooled impact burst"); var system = go.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.playOnAwake = false; main.loop = false; main.maxParticles = 20; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission; emission.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = particle; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var value = new Burst { root = go.transform, system = system }; bursts.Add(value); return value;
        }
        public static void Impact(Vector3 position, Vector3 normal, bool flesh)
        { BurstAt(position, normal, flesh ? new Color(.48f, .075f, .05f, .75f) : new Color(.57f, .52f, .40f, .65f), flesh ? .055f : .11f, flesh ? 10 : 13, false); }
        public static void Shield(Vector3 position) { BurstAt(position, Vector3.up, new Color(.32f, .72f, 1, .85f), .065f, 12, true); }
        static void BurstAt(Vector3 position, Vector3 normal, Color color, float size, int count, bool spark)
        {
            var burst = Available(); if (burst == null) return; burst.root.position = position + normal * .025f; burst.root.rotation = Quaternion.LookRotation(normal.sqrMagnitude > .01f ? normal : Vector3.up);
            var main = burst.system.main; main.duration = .45f; main.startLifetime = new ParticleSystem.MinMaxCurve(.15f, spark ? .32f : .50f); main.startSpeed = new ParticleSystem.MinMaxCurve(.8f, spark ? 3.5f : 2.8f); main.startSize = new ParticleSystem.MinMaxCurve(size * .45f, size); main.startColor = color; main.gravityModifier = spark ? .4f : .18f;
            var shape = burst.system.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = spark ? 70 : 43; shape.radius = .015f;
            var fade = burst.system.colorOverLifetime; fade.enabled = true; var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); fade.color = gradient;
            burst.system.Play(); burst.system.Emit(count);
        }
        public static void Trace(Vector3 start, Vector3 end, int weapon)
        {
            if ((end - start).sqrMagnitude < 4) return; Materials(); Tracer tracer = null;
            foreach (var candidate in tracers) if (candidate && !candidate.Active) { tracer = candidate; break; }
            if (!tracer && tracers.Count < 32) { var go = new GameObject("Pooled bullet trail"); tracer = go.AddComponent<Tracer>(); tracer.Initialize(trail); tracers.Add(tracer); }
            if (tracer) tracer.Fire(start, end, weapon == 2 ? new Color(1, .70f, .38f, .65f) : new Color(1, .88f, .62f, .65f));
        }
        public static void Reset()
        { foreach (var burst in bursts) if (burst.system) burst.system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); foreach (var tracer in tracers) if (tracer) tracer.Cancel(); }
    }
    public sealed class Tracer : MonoBehaviour
    {
        LineRenderer line; Vector3 origin, direction; float distance, began;
        public bool Active { get; private set; }
        public void Initialize(Material material)
        { line = gameObject.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.positionCount = 2; line.startWidth = .018f; line.endWidth = .008f; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; line.enabled = false; }
        public void Fire(Vector3 start, Vector3 end, Color color)
        { origin = start; direction = (end - start).normalized; distance = Vector3.Distance(start, end); began = Time.time; line.startColor = line.endColor = color; Active = true; line.enabled = true; }
        public void Cancel() { Active = false; if (line) line.enabled = false; }
        void Update()
        {
            if (!Active) return; float head = (Time.time - began) * 900;
            if (head > distance + 6) { Cancel(); return; }
            line.SetPosition(0, origin + direction * Mathf.Clamp(head - 5, .5f, distance)); line.SetPosition(1, origin + direction * Mathf.Clamp(head, .6f, distance));
        }
    }
}
