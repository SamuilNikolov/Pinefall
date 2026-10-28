Shader "Hidden/Pinefall/Atmosphere"
{
    Properties { _MainTex("Scene",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_TexelSize;float _InStorm,_Hurt;
            fixed4 frag(v2f_img i):SV_Target
            {
                float3 c=tex2D(_MainTex,i.uv).rgb;
                // Edge-aware resolve reduces stair-stepping on foliage and loot;
                // flat regions retain the original image without a blur wash.
                float3 nw=tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(-1,-1)).rgb;
                float3 ne=tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(1,-1)).rgb;
                float3 sw=tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(-1,1)).rgb;
                float3 se=tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(1,1)).rgb;
                const float3 weights=float3(.299,.587,.114);
                float lnw=dot(nw,weights),lne=dot(ne,weights),lsw=dot(sw,weights),lse=dot(se,weights),lm=dot(c,weights);
                float low=min(lm,min(min(lnw,lne),min(lsw,lse))),high=max(lm,max(max(lnw,lne),max(lsw,lse)));
                if(high-low>max(.035,high*.125)){
                    float2 dir=float2(-((lnw+lne)-(lsw+lse)),(lnw+lsw)-(lne+lse));
                    float reduce=max((lnw+lne+lsw+lse)*.03125,.0078125);
                    dir=clamp(dir/(min(abs(dir.x),abs(dir.y))+reduce),-6,6)*_MainTex_TexelSize.xy;
                    c=.5*(tex2D(_MainTex,i.uv+dir*(-.166667)).rgb+tex2D(_MainTex,i.uv+dir*.166667).rgb);
                }
                c=max(0,c-.012);c=c*1.10/(1+c*.13);
                float l=dot(c,float3(.2126,.7152,.0722));c=lerp(float3(l,l,l),c,.90);
                c=lerp(c*float3(.98,1.005,1.02),c*float3(1.025,1.01,.98),saturate(l));
                float2 p=i.uv-.5;float vignette=1-dot(p,p)*.23;c*=vignette;
                float edge=smoothstep(.12,.68,length(i.uv-.5));c=lerp(c,c*float3(.55,.35,.95)+float3(.08,.025,.17),_InStorm*.72);c=lerp(c,float3(.75,.025,.035),_Hurt*(.10+edge*.55));return float4(c,1);
            }
            ENDCG
        }
    }
}
