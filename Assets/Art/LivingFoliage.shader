Shader "Pinefall/Foliage"
{
    Properties { _MainTex("Scan color",2D)="white"{} _AlphaTex("Opacity",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _Color("Tint",Color)=(1,1,1,1) _Wind("Wind",Float)=0.03 _FadeDistance("Ground-cover distance",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" } Cull Off AlphaToMask On
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing
        sampler2D _MainTex,_AlphaTex,_BumpMap; fixed4 _Color; float _Wind,_ReserveWet,_ReserveViewDistance,_FadeDistance;
        struct Input { float2 uv_MainTex; float4 color:COLOR; float3 worldPos; };
        void vert(inout appdata_full v)
        {
            float3 root=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
            float bend=sin(_Time.y*1.4+root.x*.13+root.z*.11)+sin(_Time.y*2.3+v.vertex.y*.5)*.28;
            v.vertex.x+=bend*_Wind*max(0,v.vertex.y-.15);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            if(_FadeDistance>0){float fade=saturate((distance(_WorldSpaceCameraPos,IN.worldPos)-(_FadeDistance-18))/18);float dither=frac(sin(dot(floor(IN.worldPos.xz*8),float2(12.9898,78.233)))*43758.5453);clip(dither-fade);}
            float opacity=tex2D(_AlphaTex,IN.uv_MainTex).r;clip(opacity-.38);
            float3 color=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
            float variation=max(.72,IN.color.r);o.Albedo=color*variation*lerp(1,.84,_ReserveWet);
            o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));o.Normal.xy*=.45;
            o.Smoothness=lerp(.10,.32,_ReserveWet);o.Occlusion=.94;o.Metallic=0;o.Emission=color*.10;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
