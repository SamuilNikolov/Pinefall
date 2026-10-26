Shader "Pinefall/Storm"
{
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct result{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            result vert(appdata_base v){result o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
            fixed4 frag(result i):SV_Target
            {
                float ripple=sin(i.uv.x*17+i.uv.y*7-_Time.y*2)*.5+.5;
                float fine=sin(i.uv.x*73-i.uv.y*21+_Time.y*3)*.5+.5;
                float opacity=(.55+ripple*.22+fine*.12)*saturate((1-i.uv.y)*2);
                return float4(lerp(float3(.27,.10,.58),float3(.66,.37,.95),ripple),opacity);
            }
            ENDCG
        }
    }
}
