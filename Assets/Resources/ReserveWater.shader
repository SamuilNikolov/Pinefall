Shader "Pinefall/Water"
{
    Properties {_Color("Water",Color)=(.065,.24,.30,.90)}
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"} ZWrite Off Cull Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0
        fixed4 _Color;struct Input{float3 worldPos;};
        void vert(inout appdata_full v){v.vertex.y+=sin(v.vertex.x*.07+_Time.y*1.6)*cos(v.vertex.z*.09+_Time.y)*.055;}
        void surf(Input IN,inout SurfaceOutputStandard o){float f=pow(1-saturate(dot(normalize(_WorldSpaceCameraPos-IN.worldPos),float3(0,1,0))),3);o.Albedo=lerp(_Color.rgb,float3(.50,.65,.70),f*.65);float2 p=IN.worldPos.xz;float t=_Time.y;float dx=sin(p.x*.75+p.y*.43+t*1.2)+.4*sin(p.x*2.1-p.y*1.6-t*.8),dz=cos(p.y*.83-p.x*.29+t*.95)+.4*cos(p.y*1.9+p.x*1.3+t);o.Normal=normalize(float3(dx*.045,dz*.045,1));o.Smoothness=.82;o.Metallic=.12;o.Alpha=.90+f*.08;}
        ENDCG
    }
    Fallback "Transparent/Diffuse"
}
