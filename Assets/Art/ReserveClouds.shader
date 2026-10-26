Shader "Pinefall/Clouds"
{
    Properties { _CloudTex("Cloud noise",2D)="white"{} _CloudColor("Cloud color",Color)=(1,1,1,1) _Coverage("Coverage",Float)=.3 _Storm("Storm",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent-50" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _CloudTex;float4 _CloudColor;float _Coverage,_Storm;
            struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float horizon:TEXCOORD1; };
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;o.horizon=saturate((p.y-_WorldSpaceCameraPos.y)/300);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv+float2(_Time.y*.0017,_Time.y*.0006);
                float n=tex2D(_CloudTex,uv).r*.65+tex2D(_CloudTex,uv*2.4+float2(.27,.16)).r*.25+tex2D(_CloudTex,uv*6.7).r*.1;
                float mask=smoothstep(.76-_Coverage*.45,.94-_Coverage*.5,n)*i.horizon;
                float shade=lerp(.74,1,n);return float4(_CloudColor.rgb*shade,mask*lerp(.7,.98,_Coverage));
            }
            ENDCG
        }
    }
}
