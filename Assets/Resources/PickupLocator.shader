Shader "Pinefall/PickupLocator"
{
    Properties { _Color("Rarity",Color)=(1,1,1,.55) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION;UNITY_FOG_COORDS(0) };
            v2f vert(appdata input)
            {
                UNITY_SETUP_INSTANCE_ID(input);v2f output;output.vertex=UnityObjectToClipPos(input.vertex);UNITY_TRANSFER_FOG(output,output.vertex);return output;
            }
            fixed4 frag(v2f input):SV_Target { fixed4 color=_Color;UNITY_APPLY_FOG(input.fogCoord,color);return color; }
            ENDCG
        }
    }
}
