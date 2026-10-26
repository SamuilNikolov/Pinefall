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
                float3 bloom=0;
                bloom+=max(0,tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(3,3)).rgb-.9);
                bloom+=max(0,tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(-3,3)).rgb-.9);
                bloom+=max(0,tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(3,-3)).rgb-.9);
                bloom+=max(0,tex2D(_MainTex,i.uv+_MainTex_TexelSize.xy*float2(-3,-3)).rgb-.9);
                c+=bloom*.045;c=c*1.06/(1+c*.08);
                float l=dot(c,float3(.2126,.7152,.0722));c=lerp(float3(l,l,l),c,.96);
                c=lerp(c*float3(.98,1.005,1.02),c*float3(1.025,1.01,.98),saturate(l));
                float2 p=i.uv-.5;float vignette=1-dot(p,p)*.23;c*=vignette;
                float edge=smoothstep(.12,.68,length(i.uv-.5));c=lerp(c,c*float3(.55,.35,.95)+float3(.08,.025,.17),_InStorm*.72);c=lerp(c,float3(.75,.025,.035),_Hurt*(.10+edge*.55));return float4(c,1);
            }
            ENDCG
        }
    }
}
