using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinefall
{
    public sealed partial class ReserveWorld
    {
        sealed class FirBatch
        {
            public Matrix4x4[] source, near, middle, far; public int nearCount, middleCount, farCount, version;
            public Vector3 center; public Bounds bounds; public float refresh;
        }
        readonly List<FirBatch> firBatches = new List<FirBatch>();
        Mesh[] firMeshes; Material firMaterial,firFarMaterial;
        void AddLivingFirs(List<Matrix4x4> matrices, List<HarvestNode> nodes)
        {
            if (firMeshes == null)
            {
                firMeshes = new[] { FirMesh(12, 9, 3), FirMesh(9, 7, 3), FirMesh(7, 5, 2) };
                foreach (var mesh in firMeshes) fineAssets.Add(mesh);
                firMaterial = Resources.Load<Material>("LivingFir");firFarMaterial=Resources.Load<Material>("LivingFirFar");
            }
            for (int start = 0; start < matrices.Count; start += 1023)
            {
                int count = Mathf.Min(1023, matrices.Count - start); var source = matrices.GetRange(start, count).ToArray();
                HarvestResources.Bind(nodes, start, count, source);
                var bounds = new Bounds((Vector3)source[0].GetColumn(3), Vector3.zero); foreach (var matrix in source) bounds.Encapsulate((Vector3)matrix.GetColumn(3)); bounds.Expand(32);
                firBatches.Add(new FirBatch { source = source, near = new Matrix4x4[count], middle = new Matrix4x4[count], far = new Matrix4x4[count], center = bounds.center, bounds = bounds });
            }
        }
        void DrawLivingFirs(Camera camera, Camera target = null, bool atlas = false)
        {
            if (!firMaterial || firMeshes == null) return;
            float nearRange = Mathf.Lerp(38, 80, RuntimePerformance.Detail), middleRange = Mathf.Lerp(145, 300, RuntimePerformance.Detail);
            float farRange = Mathf.Min(RuntimePerformance.ViewDistance, 900) * (.75f + RuntimePerformance.Detail * .25f);
            if (Ranger.Local && Ranger.Local.Aiming) { nearRange *= 1.4f; middleRange *= 1.5f; }
            foreach (var batch in firBatches)
            {
                if (atlas) { Graphics.DrawMeshInstanced(firMeshes[1], 0, firMaterial, batch.source, batch.source.Length, null, ShadowCastingMode.On, true, 0, camera); continue; }
                if ((batch.center - camera.transform.position).sqrMagnitude > (farRange + 120) * (farRange + 120) || !GeometryUtility.TestPlanesAABB(drawPlanes, batch.bounds)) continue;
                if (Time.unscaledTime >= batch.refresh || batch.version != HarvestResources.Version)
                {
                    batch.refresh = Time.unscaledTime + .18f; batch.version = HarvestResources.Version; batch.nearCount = batch.middleCount = batch.farCount = 0;
                    foreach (var matrix in batch.source)
                    {
                        if (matrix.m11 < .01f) continue;
                        float distance = ((Vector3)matrix.GetColumn(3) - camera.transform.position).sqrMagnitude;
                        if (distance < nearRange * nearRange) batch.near[batch.nearCount++] = matrix;
                        else if (distance < middleRange * middleRange) batch.middle[batch.middleCount++] = matrix;
                        else if (distance < farRange * farRange) batch.far[batch.farCount++] = matrix;
                    }
                }
                if (batch.nearCount > 0) Graphics.DrawMeshInstanced(firMeshes[0], 0, firMaterial, batch.near, batch.nearCount, null, ShadowCastingMode.On, true, 0, target);
                if (batch.middleCount > 0) Graphics.DrawMeshInstanced(firMeshes[1], 0, firMaterial, batch.middle, batch.middleCount, null, ShadowCastingMode.Off, true, 0, target);
                if (batch.farCount > 0) Graphics.DrawMeshInstanced(firMeshes[2], 0, firFarMaterial?firFarMaterial:firMaterial, batch.far, batch.farCount, null, ShadowCastingMode.Off, true, 0, target);
            }
        }
        static Mesh FirMesh(int tiers, int branches, int twigs)
        {
            var vertices = new List<Vector3>(); var indices = new List<int>(); var uv = new List<Vector2>(); var colors = new List<Color>();
            var random = new System.Random(721);
            Rect[] patches = { new Rect(.17f,.67f,.29f,.32f), new Rect(.65f,.62f,.34f,.37f), new Rect(.28f,.23f,.35f,.36f), new Rect(.64f,.22f,.35f,.35f) };
            for (int tier = 0; tier < tiers; tier++)
                for (int branch = 0; branch < branches; branch++)
                    for (int twig = 0; twig < twigs; twig++)
                    {
                        float height = 1.4f + tier / (float)(tiers - 1) * 8.7f;
                        float angle = branch * Mathf.PI * 2 / branches + tier * 2.39996f + twig * .34f;
                        Vector3 outward = new Vector3(Mathf.Cos(angle), .12f + (float)random.NextDouble() * .24f, Mathf.Sin(angle)).normalized;
                        Vector3 side = Quaternion.AngleAxis((twig % 3 - 1) * 62 + (float)random.NextDouble() * 24 - 12, outward) * new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
                        float reach = (3.6f * (1 - tier / (float)tiers) + .45f) * (.65f + (float)random.NextDouble() * .35f);
                        Vector3 root = new Vector3(0, height, 0) + outward * (twig * .16f);
                        Vector3 tip = root + outward * reach;
                        float width = reach * (.40f + (float)random.NextDouble() * .14f); int n = vertices.Count;
                        vertices.Add(root - side * width * .15f); vertices.Add(root + side * width * .15f); vertices.Add(tip + side * width); vertices.Add(tip - side * width);
                        var patch = patches[(tier + twig + branch) % patches.Length];
                        uv.Add(new Vector2(patch.xMin, patch.yMin)); uv.Add(new Vector2(patch.xMax, patch.yMin)); uv.Add(new Vector2(patch.xMax, patch.yMax)); uv.Add(new Vector2(patch.xMin, patch.yMax));
                        indices.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
                        for (int i = 0; i < 4; i++) colors.Add(Color.white * (.82f + (float)random.NextDouble() * .18f));
                    }
            var mesh = new Mesh { name = "Scanned fir branch LOD " + tiers }; mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
