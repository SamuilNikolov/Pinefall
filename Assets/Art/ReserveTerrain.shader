Shader "Pinefall/BiomeTerrain"
{
 Properties {
  _Meadow("Meadow scan",2D)="white"{} _Forest("Forest floor scan",2D)="white"{}
  _Sand("Sand",2D)="white"{} _Snow("Snow",2D)="white"{} _Rock("Rock",2D)="white"{}
  _MeadowNormal("Meadow normal",2D)="bump"{} _ForestNormal("Forest normal",2D)="bump"{}
  _SandNormal("Sand normal",2D)="bump"{} _SnowNormal("Snow normal",2D)="bump"{} _RockNormal("Rock normal",2D)="bump"{}
  _MeadowArm("Meadow occlusion roughness",2D)="white"{} _ForestArm("Forest occlusion roughness",2D)="white"{}
  [HideInInspector] _CavePatch("Cave surface replacement",Float)=0
  [HideInInspector] _TerrainHolesTexture("Terrain holes",2D)="white"{}
 }
 SubShader {
  Tags {"RenderType"="Opaque" "TerrainCompatible"="True"} LOD 250
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.5
  #include "UnityCG.cginc"
  UNITY_DECLARE_TEX2D(_Meadow);
  UNITY_DECLARE_TEX2D_NOSAMPLER(_Forest);UNITY_DECLARE_TEX2D_NOSAMPLER(_Sand);UNITY_DECLARE_TEX2D_NOSAMPLER(_Snow);UNITY_DECLARE_TEX2D_NOSAMPLER(_Rock);UNITY_DECLARE_TEX2D_NOSAMPLER(_MeadowNormal);UNITY_DECLARE_TEX2D_NOSAMPLER(_ForestNormal);UNITY_DECLARE_TEX2D_NOSAMPLER(_SandNormal);UNITY_DECLARE_TEX2D_NOSAMPLER(_SnowNormal);UNITY_DECLARE_TEX2D_NOSAMPLER(_RockNormal);UNITY_DECLARE_TEX2D_NOSAMPLER(_MeadowArm);UNITY_DECLARE_TEX2D_NOSAMPLER(_ForestArm);
  sampler2D _RoadCoverage,_TerrainHolesTexture;
  float _ReserveWet,_CavePatch;
  struct Input {float3 worldPos;float3 worldNormal;INTERNAL_DATA};
  float weight(float2 p,float2 anchor){float2 d=p-anchor;return exp(-dot(d,d)/65000.0);}
  void surf(Input IN,inout SurfaceOutputStandard o){
   float2 q=IN.worldPos.xz,p=q/4;
   clip(lerp(tex2D(_TerrainHolesTexture,q/6144+.5).r,1,_CavePatch)-.5);
   float a=weight(p,float2(-20,-120)),b=weight(p,float2(-530,-270)),c=weight(p,float2(-330,540)),d=weight(p,float2(430,470)),e=weight(p,float2(530,-260)),f=weight(p,float2(260,-610));
   float sum=max(.00001,a+b+c+d+e+f);a/=sum;b/=sum;c/=sum;d/=sum;e/=sum;f/=sum;
   float slope=1-abs(WorldNormalVector(IN,float3(0,0,1)).y),rockWeight=saturate((slope-.22)*2.8);
   float snowline=smoothstep(185,265,IN.worldPos.y);float snowWeight=c+d*snowline;
   float grassWeight=a*(1-rockWeight),forestWeight=(e+f)*(1-rockWeight),sandWeight=b*(1-rockWeight),stoneWeight=d*(1-snowline)+rockWeight*(1-c);
   float normalization=max(.01,grassWeight+forestWeight+sandWeight+snowWeight+stoneWeight);
   float3 color=0,normal=float3(0,0,.001);float rough=.92,occlusion=1;
   float distance=length(_WorldSpaceCameraPos-IN.worldPos),micro=1-smoothstep(55,160,distance);
   if(grassWeight>.025){float3 fine=UNITY_SAMPLE_TEX2D(_Meadow,q/3.2).rgb;float3 macro=UNITY_SAMPLE_TEX2D(_Meadow,q/27+float2(.31,.57)).rgb;color+=lerp(macro,fine,micro*.70+.30 )*float3(.94,.97,.89)*grassWeight;if(micro>.05){normal+=UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_MeadowNormal,_Meadow,q/3.2))*grassWeight;float3 arm=UNITY_SAMPLE_TEX2D_SAMPLER(_MeadowArm,_Meadow,q/3.2).rgb;occlusion=lerp(occlusion,arm.r,grassWeight*.65);rough=lerp(rough,arm.g,grassWeight);}}
   if(forestWeight>.025){float3 floor=UNITY_SAMPLE_TEX2D_SAMPLER(_Forest,_Meadow,q/3.8).rgb;color+=floor*forestWeight;if(micro>.05){normal+=UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_ForestNormal,_Meadow,q/3.8))*forestWeight;float3 arm=UNITY_SAMPLE_TEX2D_SAMPLER(_ForestArm,_Meadow,q/3.8).rgb;occlusion=lerp(occlusion,arm.r,forestWeight*.75);rough=lerp(rough,arm.g,forestWeight);}}
   if(sandWeight>.025){color+=UNITY_SAMPLE_TEX2D_SAMPLER(_Sand,_Meadow,q/4.5).rgb*sandWeight;if(micro>.05)normal+=UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_SandNormal,_Meadow,q/4.5))*sandWeight;}
   if(snowWeight>.025){color+=UNITY_SAMPLE_TEX2D_SAMPLER(_Snow,_Meadow,q/5).rgb*float3(.94,.98,1)*snowWeight;if(micro>.05)normal+=UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_SnowNormal,_Meadow,q/5))*snowWeight;}
   if(stoneWeight>.025){float3 rock=UNITY_SAMPLE_TEX2D_SAMPLER(_Rock,_Meadow,q/5).rgb;if(slope>.30){float3 w=pow(abs(IN.worldNormal),4);w/=max(.001,w.x+w.y+w.z);rock=UNITY_SAMPLE_TEX2D_SAMPLER(_Rock,_Meadow,IN.worldPos.zy/5).rgb*w.x+rock*w.y+UNITY_SAMPLE_TEX2D_SAMPLER(_Rock,_Meadow,IN.worldPos.xy/5).rgb*w.z;}color+=rock*stoneWeight;if(micro>.05)normal+=UnpackNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_RockNormal,_Meadow,q/5))*stoneWeight;}
   color/=normalization;
   float autumn=1-smoothstep(540,760,length(q-float2(1000,850)));color=lerp(color,color*float3(1.08,.86,.63),autumn*.48);
   float road=tex2D(_RoadCoverage,q/6144+.5).r;color=lerp(color,float3(.23,.21,.18),road*.80);
   o.Albedo=color*lerp(1,.78,_ReserveWet);normal.xy*=micro*.80+.15;o.Normal=normalize(normal);
   o.Metallic=0;o.Smoothness=lerp(saturate(1-rough)*.15,.28,_ReserveWet);o.Occlusion=occlusion;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
