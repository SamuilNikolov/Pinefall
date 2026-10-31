// Coherent stochastic PBR sampling. Only region layers with nonzero weight run;
// color and packed normal/roughness share all offsets and quarter-turns.
struct PFStochasticGround
{
    void Sample(Texture2D C, SamplerState CS, Texture2D S, SamplerState SS,
                float2 uv, float2 dx, float2 dy, out float3 color, out float3 normal, out float rough)
    {
        float2 skew=float2(uv.x-uv.y*.577350269,uv.y*1.154700538)*3.464101615;
        float2 cell=floor(skew),f=frac(skew);float z=1-f.x-f.y;
        float2 corner[3];float3 w;
        if(z>0){corner[0]=cell;corner[1]=cell+float2(0,1);corner[2]=cell+float2(1,0);w=float3(z,f.y,f.x);}
        else{corner[0]=cell+1;corner[1]=cell+float2(1,0);corner[2]=cell+float2(0,1);w=float3(-z,1-f.y,1-f.x);}
        w=pow(max(w,0),3);w/=max(dot(w,float3(1,1,1)),.0001);
        color=0;normal=0;rough=0;
        [unroll] for(int i=0;i<3;i++)
        {
            float2 seed=frac(sin(float2(dot(corner[i],float2(127.1,311.7)),dot(corner[i],float2(269.5,183.3))))*43758.5453);
            int turn=(int)floor(seed.x*4);float2 p=uv,gx=dx,gy=dy;
            if(turn==1){p=float2(-uv.y,uv.x);gx=float2(-dx.y,dx.x);gy=float2(-dy.y,dy.x);}
            if(turn==2){p=-uv;gx=-dx;gy=-dy;}
            if(turn==3){p=float2(uv.y,-uv.x);gx=float2(dx.y,-dx.x);gy=float2(dy.y,-dy.x);}
            p+=seed*17;
            float3 c=Texture2DSampleGrad(C,CS,p,gx,gy).rgb;
            float3 s=Texture2DSampleGrad(S,SS,p,gx,gy).rgb;
            float2 n=s.rg*2-1;float nz=sqrt(saturate(1-dot(n,n)));
            if(turn==1)n=float2(n.y,-n.x);if(turn==2)n=-n;if(turn==3)n=float2(-n.y,n.x);
            color+=c*w[i];normal+=float3(n,nz)*w[i];rough+=s.b*w[i];
        }
        normal=normalize(normal);
    }
};
PFStochasticGround sampleGround;
float woods=1-smoothstep(-23000,-7000,P.x);
float sand=(1-smoothstep(-30000,-14000,P.x))*(1-smoothstep(-44000,-28000,P.y));
float snow=smoothstep(18000,32000,P.x)*smoothstep(10000,22000,P.y);
float alpine=smoothstep(12000,26000,P.x)*(1-smoothstep(-6000,10000,P.y));
float cliff=max(saturate((1-N.z)*3.2),alpine*.55);
float wet=smoothstep(15000,30000,P.y)*(1-smoothstep(26000,43000,P.x))*smoothstep(-22000,-12000,P.x);
float forestCover=max(woods*.82,wet*.75);
float wf=forestCover*(1-sand)*(1-snow)*(1-cliff);
float wm=(1-forestCover)*(1-sand)*(1-snow)*(1-cliff);
float wa=sand*(1-snow)*(1-cliff);
float ws=snow*(1-cliff*.65);
float wr=cliff*(1-snow*.35);
float total=max(.001,wf+wm+wa+ws+wr);
float3 worldDx=ddx(P),worldDy=ddy(P);
float2 dx=worldDx.xy,dy=worldDy.xy;float3 color=0,n=0,c,nn;float rough=0,r;
[branch] if(wf>.001){sampleGround.Sample(ForestC,ForestCSampler,ForestS,ForestSSampler,P.xy/300,dx/300,dy/300,c,nn,r);color+=c*float3(.6,.72,.50)*wf;n+=nn*wf;rough+=r*wf;}
[branch] if(wm>.001){sampleGround.Sample(MeadowC,MeadowCSampler,MeadowS,MeadowSSampler,P.xy/500,dx/500,dy/500,c,nn,r);color+=c*float3(.45,.95,.42)*wm;n+=nn*wm;rough+=r*wm;}
[branch] if(wa>.001){sampleGround.Sample(SandC,SandCSampler,SandS,SandSSampler,P.xy/800,dx/800,dy/800,c,nn,r);color+=c*wa;n+=nn*wa;rough+=r*wa;}
[branch] if(ws>.001){sampleGround.Sample(SnowC,SnowCSampler,SnowS,SnowSSampler,P.xy/300,dx/300,dy/300,c,nn,r);color+=c*ws;n+=nn*ws;rough+=r*ws;}
[branch] if(wr>.001)
{
    // Project cliff scans along their dominant planes, rather than stretching
    // the horizontal terrain UVs into vertical stripes on steep landforms.
    float3 axes=pow(abs(N),4);axes/=max(dot(axes,float3(1,1,1)),.001);
    float3 rc=0,gradient=0;float rr=0;
    [branch] if(axes.z>.001)
    {
        sampleGround.Sample(RockC,RockCSampler,RockS,RockSSampler,P.xy/400,dx/400,dy/400,c,nn,r);
        rc+=c*axes.z;rr+=r*axes.z;gradient+=float3(nn.xy,0)*axes.z;
    }
    [branch] if(axes.y>.001)
    {
        sampleGround.Sample(RockC,RockCSampler,RockS,RockSSampler,P.xz/400,worldDx.xz/400,worldDy.xz/400,c,nn,r);
        rc+=c*axes.y;rr+=r*axes.y;gradient+=float3(nn.x,0,nn.y)*axes.y;
    }
    [branch] if(axes.x>.001)
    {
        sampleGround.Sample(RockC,RockCSampler,RockS,RockSSampler,P.yz/400,worldDx.yz/400,worldDy.yz/400,c,nn,r);
        rc+=c*axes.x;rr+=r*axes.x;gradient+=float3(0,nn.xy)*axes.x;
    }
    float3 tangent=normalize(float3(N.z,0,-N.x));
    float3 bitangent=normalize(cross(N,tangent));
    float3 rockNormal=normalize(N+gradient*.65);
    float3 localNormal=float3(dot(rockNormal,tangent),dot(rockNormal,bitangent),dot(rockNormal,N));
    color+=rc*wr;n+=localNormal*wr;rough+=rr*wr;
}
OutNormal=normalize(n);OutRoughness=saturate(rough/total);
float macro=.95+.045*sin(P.x/1900+sin(P.y/3300))+.025*sin(P.y/7100);
// Subtle wetland response remains in the material, rather than a global filter.
float moisture=wet*(1-snow)*(1-sand)*(1-cliff);
OutRoughness=clamp(OutRoughness-moisture*.1,.35,1);
return color/total*macro*(1-moisture*.12);
