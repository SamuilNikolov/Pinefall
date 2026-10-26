Shader "Pinefall/Rain"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            struct output {float4 pos:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            output vert(input v){output o;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
            fixed4 frag(output i):SV_Target{float a=saturate(1-abs(i.uv.x-.5)*2)*sin(i.uv.y*3.14159);return float4(i.color.rgb,i.color.a*a*.65);}
            ENDCG
        }
    }
}
