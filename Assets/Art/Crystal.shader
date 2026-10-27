Shader "Pinefall/Crystal" {
 Properties { _Color("Crystal body",Color)=(.12,.25,.6,1) _EmissionColor("Crystal light",Color)=(.1,.2,.4,1) _Glossiness("Polish",Range(0,1))=.72 }
 SubShader {Tags {"RenderType"="Opaque"} LOD 180
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _Color,_EmissionColor;half _Glossiness;struct Input{float3 worldNormal;};
 void surf(Input IN,inout SurfaceOutputStandard o){float facet=saturate(dot(IN.worldNormal,normalize(float3(.4,.7,.3))));o.Albedo=_Color.rgb*(.48+facet*.52);o.Metallic=.18;o.Smoothness=_Glossiness;o.Emission=_EmissionColor.rgb*(.15+facet*.28);o.Occlusion=.85;}
 ENDCG
 }Fallback "Standard"
}
