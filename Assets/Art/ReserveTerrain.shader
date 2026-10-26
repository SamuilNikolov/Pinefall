Shader "Pinefall/BiomeTerrain"
{
    Properties
    {
        _Meadow("Meadow",2D)="white"{}
        _Forest("Forest floor",2D)="white"{}
        _Sand("Sand",2D)="white"{} _Snow("Snow",2D)="white"{} _Rock("Rock",2D)="white"{}
        _SandNormal("Sand normal",2D)="bump"{} _SnowNormal("Snow normal",2D)="bump"{} _RockNormal("Rock normal",2D)="bump"{}
        _MeadowNormal("Meadow normal",2D)="bump"{}
        _ForestNormal("Forest normal",2D)="bump"{}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "TerrainCompatible"="True" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #include "UnityCG.cginc"
        sampler2D _Meadow,_Forest,_MeadowNormal,_ForestNormal,_Sand,_Snow,_Rock,_SandNormal,_SnowNormal,_RockNormal;
        float _ReserveWet;
        sampler2D _RoadCoverage;
        struct Input {float3 worldPos;float3 worldNormal;INTERNAL_DATA};
        float weight(float2 p,float2 anchor){float2 d=p-anchor;return exp(-dot(d,d)/65000.0);}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 q=IN.worldPos.xz,p=q/4.0;
            float valley=weight(p,float2(-20,-120));
            float desert=weight(p,float2(-530,-270));
            float ice=weight(p,float2(-330,540));
            float mountain=weight(p,float2(430,470));
            float jungle=weight(p,float2(530,-260));
            float marsh=weight(p,float2(260,-610));float total=max(.000001,valley+desert+ice+mountain+jungle+marsh);
            valley/=total;desert/=total;ice/=total;mountain/=total;jungle/=total;marsh/=total;
            float3 meadow=tex2D(_Meadow,q/8).rgb*float3(.68,1.32,.64);
            float3 floor=tex2D(_Forest,q/4).rgb;
            float detail=tex2D(_Meadow,q/3).g;
            float3 sand=tex2D(_Sand,q/4).rgb*float3(1.10,1.02,.85);
            float3 snow=tex2D(_Snow,q/5).rgb*float3(.93,.98,1);
            float3 rock=tex2D(_Rock,q/7).rgb*.87;
            float snowline=smoothstep(112,148,IN.worldPos.y);
            float3 rocky=lerp(rock,snow,snowline);
            float shade=saturate((sin(p.x*.033+sin(p.y*.03))*sin(p.y*.027)+.3)*.25);
            float3 field=lerp(meadow,floor*.8,shade);
            float3 forest=lerp(floor*float3(.51,.88,.48),meadow*.55,.25);
            float3 albedo=field*valley+sand*desert+snow*ice+rocky*mountain+forest*jungle+lerp(forest,sand*.65,.38)*marsh;
            float autumn=1-smoothstep(540,760,length(q-float2(1000,850)));albedo=lerp(albedo,lerp(floor,float3(.44,.31,.13),.45),autumn*.6);
            float slope=1-abs(WorldNormalVector(IN,float3(0,0,1)).y);
            albedo=lerp(albedo,rock,saturate((slope-.25)*2.6)*(1-ice*.75));
            float coverage=tex2D(_RoadCoverage,q/6144.0+.5).r;
            albedo=lerp(albedo,lerp(float3(.22,.20,.16),sand,desert),coverage);
            o.Albedo=albedo*lerp(1,.83,_ReserveWet);
            float3 normal=UnpackNormal(tex2D(_MeadowNormal,q/8));
            normal=lerp(normal,UnpackNormal(tex2D(_ForestNormal,q/4)),jungle);
            normal=lerp(normal,UnpackNormal(tex2D(_SandNormal,q/4)),desert);normal=lerp(normal,UnpackNormal(tex2D(_SnowNormal,q/5)),ice+mountain*snowline);normal=lerp(normal,UnpackNormal(tex2D(_RockNormal,q/7)),mountain*(1-snowline));normal.xy*=.65;o.Normal=normalize(normal);
            o.Metallic=0;o.Smoothness=lerp(.04,.27,_ReserveWet)*(1-ice*.5);o.Occlusion=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
