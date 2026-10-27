Shader "Pinefall/CaveRock" {
 Properties { _MainTex("Rock scan",2D)="white"{} _Color("Tint",Color)=(.67,.70,.69,1) }
 SubShader { Tags {"RenderType"="Opaque"} LOD 180
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color;struct Input{float3 worldPos;float3 worldNormal;};
 void surf(Input IN,inout SurfaceOutputStandard o){float3 w=pow(abs(IN.worldNormal),4);w/=max(.001,w.x+w.y+w.z);float3 q=IN.worldPos/3.7;float3 c=tex2D(_MainTex,q.zy).rgb*w.x+tex2D(_MainTex,q.xz).rgb*w.y+tex2D(_MainTex,q.xy).rgb*w.z;o.Albedo=c*_Color.rgb;o.Smoothness=.12;o.Occlusion=.88;}
 ENDCG
 } Fallback "Diffuse"
}
