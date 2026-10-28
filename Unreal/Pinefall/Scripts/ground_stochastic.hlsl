// Three overlapping triangular patches with independently hashed translations
// and quarter turns. Shared corner samples keep boundaries continuous. Color,
// roughness and normals use exactly the same transform; no visible square tiles.
float2 skew = float2(UV.x - UV.y * 0.577350269, UV.y * 1.154700538) * 3.464101615;
float2 cell = floor(skew);
float2 f = frac(skew);
float z = 1.0 - f.x - f.y;
float2 corner[3];
float3 w;
if (z > 0.0)
{
    corner[0] = cell;
    corner[1] = cell + float2(0, 1);
    corner[2] = cell + float2(1, 0);
    w = float3(z, f.y, f.x);
}
else
{
    corner[0] = cell + float2(1, 1);
    corner[1] = cell + float2(1, 0);
    corner[2] = cell + float2(0, 1);
    w = float3(-z, 1.0 - f.y, 1.0 - f.x);
}
w = pow(max(w, 0.0), 3.0);
w /= max(dot(w, float3(1, 1, 1)), 0.0001);
float2 dx = ddx(UV), dy = ddy(UV);
float3 result = 0;
[unroll]
for (int i = 0; i < 3; ++i)
{
    float2 seed = frac(sin(float2(dot(corner[i], float2(127.1,311.7)),
                                 dot(corner[i], float2(269.5,183.3)))) * 43758.5453);
    int turn = (int)floor(seed.x * 4.0);
    float2 p = UV, gx = dx, gy = dy;
    if (turn == 1) { p = float2(-UV.y, UV.x); gx = float2(-dx.y, dx.x); gy = float2(-dy.y, dy.x); }
    if (turn == 2) { p = -UV; gx = -dx; gy = -dy; }
    if (turn == 3) { p = float2(UV.y, -UV.x); gx = float2(dx.y, -dx.x); gy = float2(dy.y, -dy.x); }
    float3 value = Texture2DSampleGrad(Tex, TexSampler, p + seed * 17.0, gx, gy).rgb;
    if (NormalMap > 0.5)
    {
        float2 n = value.rg * 2.0 - 1.0;
        // BC5 normals reconstruct Z. Rotate tangent XY back into the common basis.
        float nz = sqrt(saturate(1.0 - dot(n, n)));
        if (turn == 1) n = float2(n.y, -n.x);
        if (turn == 2) n = -n;
        if (turn == 3) n = float2(-n.y, n.x);
        value = float3(n, nz);
    }
    result += value * w[i];
}
return NormalMap > 0.5 ? normalize(result) : result;
