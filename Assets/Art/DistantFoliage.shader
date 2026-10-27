Shader "Pinefall/DistantFoliage" {
 Properties { _MainTex("Scan color",2D)="white"{} _AlphaTex("Opacity",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Wind("Wind",Float)=.03 _FadeDistance("Fade",Float)=0 }
 SubShader {Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off AlphaToMask On
 Pass {Tags {"LightMode"="ForwardBase"}
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 #include "Lighting.cginc"
 sampler2D _MainTex,_AlphaTex;float4 _MainTex_ST;fixed4 _Color;float _Wind,_FadeDistance,_ReserveWet;
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed3 light:TEXCOORD1;float3 world:TEXCOORD2;UNITY_FOG_COORDS(3)};
 v2f vert(appdata v){UNITY_SETUP_INSTANCE_ID(v);v2f o;float3 root=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;v.vertex.x+=sin(_Time.y*1.4+root.x*.13+root.z*.11)*_Wind*max(0,v.vertex.y-.15);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);float3 normal=UnityObjectToWorldNormal(v.normal);o.light=clamp(ShadeSH9(float4(normal,1))+_LightColor0.rgb*(.2+.65*abs(dot(normal,_WorldSpaceLightPos0.xyz))),.28,1.35)*max(.78,v.color.r);UNITY_TRANSFER_FOG(o,o.vertex);return o;}
 fixed4 frag(v2f i):SV_Target {clip(tex2D(_AlphaTex,i.uv).r-.34);if(_FadeDistance>0){float fade=saturate((distance(_WorldSpaceCameraPos,i.world)-(_FadeDistance-18))/18);clip(frac(sin(dot(floor(i.world.xz*8),float2(12.9898,78.233)))*43758.5453)-fade);}fixed4 c=fixed4(tex2D(_MainTex,i.uv).rgb*_Color.rgb*i.light*lerp(1,.84,_ReserveWet),1);UNITY_APPLY_FOG(i.fogCoord,c);return c;}
 ENDCG
 }}Fallback "Transparent/Cutout/Diffuse"
}
