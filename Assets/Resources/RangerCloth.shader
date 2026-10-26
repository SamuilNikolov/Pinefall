Shader "Pinefall/RangerCloth"
{
    Properties { _MainTex("Outfit texture",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _Tint("Clothing tint",Color)=(.3,.4,.27,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex,_BumpMap;fixed4 _Tint;
        struct Input {float2 uv_MainTex;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            fixed3 c=tex2D(_MainTex,IN.uv_MainTex).rgb;
            float cloth=saturate((c.g-c.r*.93)*16)*saturate((c.g-c.b*1.07)*16);
            float light=dot(c,float3(.25,.6,.15));o.Albedo=lerp(c,_Tint.rgb*light*2.4,cloth);
            o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));o.Metallic=.02;o.Smoothness=.22;
        }
        ENDCG
    }
    Fallback "Standard"
}
