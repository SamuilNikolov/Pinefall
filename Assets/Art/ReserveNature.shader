Shader "Pinefall/Nature"
{
    Properties { _Color("Color",Color)=(.25,.35,.22,1) _Wind("Wind",Float)=0.03 _MainTex("Needle detail",2D)="white"{} }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" } LOD 200 Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing
        struct Input { float4 color:COLOR; float3 worldPos;float2 uv_MainTex; };
        fixed4 _Color;float _Wind,_ReserveWet;sampler2D _MainTex;
        void vert(inout appdata_full v)
        {
            float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;
            v.vertex.x+=sin(_Time.y*1.6+w.x*.14+w.z*.1)*_Wind*max(0,v.vertex.y-1);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float noise=sin(IN.worldPos.x*2.1+sin(IN.worldPos.z*3.4))*sin(IN.worldPos.y*6.3)*.035;
            fixed4 needle=tex2D(_MainTex,IN.uv_MainTex);clip(needle.a-.3);
            o.Albedo=(_Color.rgb*max(IN.color.rgb,float3(.72,.72,.72))*needle.rgb+noise)*lerp(1,.80,_ReserveWet);
            o.Metallic=0;o.Smoothness=lerp(.12,.48,_ReserveWet);o.Occlusion=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
