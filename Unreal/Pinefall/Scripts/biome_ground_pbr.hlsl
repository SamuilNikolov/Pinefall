// Seven physically scaled scan surfaces. Material normals are world-space.
struct PFBiomeGround
{
    void Sample(Texture2D C, SamplerState CS, Texture2D S, SamplerState SS,
                float2 uv, float2 dx, float2 dy, out float3 color,
                out float3 normal, out float rough, out float ao, out float height)
    {
        float2 skew=float2(uv.x-uv.y*.577350269,uv.y*1.154700538)*3.464101615;
        float2 cell=floor(skew),f=frac(skew);float z=1-f.x-f.y;
        float2 corner[3];float3 w;
        if(z>0){corner[0]=cell;corner[1]=cell+float2(0,1);corner[2]=cell+float2(1,0);w=float3(z,f.y,f.x);}
        else{corner[0]=cell+1;corner[1]=cell+float2(1,0);corner[2]=cell+float2(0,1);w=float3(-z,1-f.y,1-f.x);}
        // Narrow, continuous bands keep scanned leaves sharp during patch blending.
        w=pow(max(w,0),6);
        float4 colors[3],surfaces[3];float3 normals[3];
        [unroll] for(int i=0;i<3;i++)
        {
            float2 seed=frac(sin(float2(dot(corner[i],float2(127.1,311.7)),dot(corner[i],float2(269.5,183.3))))*43758.5453);
            int turn=(int)floor(seed.x*4);float2 p=uv,gx=dx,gy=dy;
            if(turn==1){p=float2(-uv.y,uv.x);gx=float2(-dx.y,dx.x);gy=float2(-dy.y,dy.x);}
            if(turn==2){p=-uv;gx=-dx;gy=-dy;}
            if(turn==3){p=float2(uv.y,-uv.x);gx=float2(dx.y,-dx.x);gy=float2(dy.y,-dy.x);}
            p+=seed*17;
            colors[i]=Texture2DSampleGrad(C,CS,p,gx,gy);
            surfaces[i]=Texture2DSampleGrad(S,SS,p,gx,gy);
            float2 n=surfaces[i].rg*2-1;float nz=sqrt(saturate(1-dot(n,n)));
            if(turn==1)n=float2(n.y,-n.x);if(turn==2)n=-n;if(turn==3)n=float2(-n.y,n.x);
            normals[i]=float3(n,nz);
            w[i]*=lerp(.75,1.25,surfaces[i].a);
        }
        w/=max(dot(w,float3(1,1,1)),.000001);
        color=0;normal=0;rough=0;ao=0;height=0;
        [unroll] for(int j=0;j<3;j++)
        {
            color+=colors[j].rgb*w[j];ao+=colors[j].a*w[j];
            normal+=normals[j]*w[j];rough+=surfaces[j].b*w[j];height+=surfaces[j].a*w[j];
        }
        normal=normalize(normal);
    }
    void Project(Texture2D C, SamplerState CS, Texture2D S, SamplerState SS,
                 float3 p, float3 dx, float3 dy, float3 baseN,
                 out float3 color, out float3 normal, out float rough, out float ao, out float height)
    {
        float3 axes=pow(abs(baseN),4);axes/=max(dot(axes,float3(1,1,1)),.001);
        float3 c,nn,gradient=0;float r,a,h;
        color=0;rough=0;ao=0;height=0;
        [branch] if(axes.z>.001)
        {
            Sample(C,CS,S,SS,p.xy,dx.xy,dy.xy,c,nn,r,a,h);
            color+=c*axes.z;rough+=r*axes.z;ao+=a*axes.z;height+=h*axes.z;gradient+=float3(nn.xy,0)*axes.z;
        }
        [branch] if(axes.y>.001)
        {
            Sample(C,CS,S,SS,p.xz,dx.xz,dy.xz,c,nn,r,a,h);
            color+=c*axes.y;rough+=r*axes.y;ao+=a*axes.y;height+=h*axes.y;gradient+=float3(nn.x,0,nn.y)*axes.y;
        }
        [branch] if(axes.x>.001)
        {
            Sample(C,CS,S,SS,p.yz,dx.yz,dy.yz,c,nn,r,a,h);
            color+=c*axes.x;rough+=r*axes.x;ao+=a*axes.x;height+=h*axes.x;gradient+=float3(0,nn.xy)*axes.x;
        }
        normal=normalize(baseN+gradient*.65);
    }
};
PFBiomeGround ground;
float3 baseN=normalize(N),dx=ddx(P),dy=ddy(P);
float3 tangent=normalize(float3(baseN.z,0,-baseN.x)),bitangent=normalize(cross(baseN,tangent));
float sand=(1-smoothstep(-25000,-19000,P.x))*(1-smoothstep(-38000,-32000,P.y));
float snow=smoothstep(22000,28000,P.x)*smoothstep(12000,18000,P.y);
float alpine=smoothstep(15000,22000,P.x)*(1-smoothstep(-3000,3000,P.y));
float wet=smoothstep(-18000,-12000,P.x)*(1-smoothstep(37000,43000,P.x))*smoothstep(15000,21000,P.y);
float woods=1-smoothstep(-18000,-12000,P.x);
float broad=.5+.25*sin(P.x/1730+sin(P.y/2900))+.25*sin(P.y/4270+P.x/6170);
float cliff=max(1-smoothstep(.65,.94,baseN.z),alpine*lerp(.35,.7,broad));
cliff=lerp(cliff,1,saturate(RockMode));
float snowCover=snow*(1-cliff*.88);
if(RockMode>.5)snowCover=snow*smoothstep(.55,.94,baseN.z)*lerp(.55,.95,broad);
float lake=length((P.xy-float2(30000,30000))/float2(22000,17000));
float shore=(1-smoothstep(.1,.28,abs(lake-1)))*(1-snow)*(1-alpine);
float mud=saturate(max(wet*lerp(.5,.9,broad),shore*.9));
float remaining=(1-cliff)*(1-snowCover);
float wf=woods*(1-sand)*(1-mud)*remaining;
float wm=(1-woods)*(1-sand)*(1-mud)*remaining;
float wa=sand*remaining,wu=(1-sand)*mud*remaining,ws=snowCover;
float wr=cliff*(1-sand)*(1-snowCover),wd=cliff*sand*(1-snowCover);
float3 color=0,normal=0,c,nn;float rough=0,ao=0,total=0,r,a,h,w;
// Measured source tile sizes in centimeters; height guides transitions, not collision.
[branch] if(wf>.001){ground.Sample(ForestC,ForestCSampler,ForestS,ForestSSampler,P.xy/300,dx.xy/300,dy.xy/300,c,nn,r,a,h);w=wf*lerp(.75,1.25,h);color+=c*w;normal+=(tangent*nn.x+bitangent*nn.y+baseN*nn.z)*w;rough+=r*w;ao+=a*w;total+=w;}
[branch] if(wm>.001){ground.Sample(MeadowC,MeadowCSampler,MeadowS,MeadowSSampler,P.xy/200,dx.xy/200,dy.xy/200,c,nn,r,a,h);w=wm*lerp(.75,1.25,h);color+=c*w;normal+=(tangent*nn.x+bitangent*nn.y+baseN*nn.z)*w;rough+=r*w;ao+=a*w;total+=w;}
[branch] if(wa>.001){ground.Sample(SandC,SandCSampler,SandS,SandSSampler,P.xy/150,dx.xy/150,dy.xy/150,c,nn,r,a,h);w=wa*lerp(.75,1.25,h);color+=c*w;normal+=(tangent*nn.x+bitangent*nn.y+baseN*nn.z)*w;rough+=r*w;ao+=a*w;total+=w;}
[branch] if(wu>.001){ground.Sample(MudC,MudCSampler,MudS,MudSSampler,P.xy/130,dx.xy/130,dy.xy/130,c,nn,r,a,h);w=wu*lerp(.75,1.25,h);float damp=(1-h)*.65;color+=c*(1-damp*.12)*w;normal+=(tangent*nn.x+bitangent*nn.y+baseN*nn.z)*w;rough+=lerp(r,.28,damp)*w;ao+=a*w;total+=w;}
[branch] if(ws>.001){ground.Sample(SnowC,SnowCSampler,SnowS,SnowSSampler,P.xy/200,dx.xy/200,dy.xy/200,c,nn,r,a,h);w=ws*lerp(.75,1.25,h);color+=c*w;normal+=(tangent*nn.x+bitangent*nn.y+baseN*nn.z)*w;rough+=max(r,.55)*w;ao+=a*w;total+=w;}
[branch] if(wr>.001){ground.Project(RockC,RockCSampler,RockS,RockSSampler,P/180,dx/180,dy/180,baseN,c,nn,r,a,h);w=wr*lerp(.75,1.25,h);color+=c*w;normal+=nn*w;rough+=r*w;ao+=a*w;total+=w;}
[branch] if(wd>.001){ground.Project(DesertRockC,DesertRockCSampler,DesertRockS,DesertRockSSampler,P/200,dx/200,dy/200,baseN,c,nn,r,a,h);w=wd*lerp(.75,1.25,h);color+=c*w;normal+=nn*w;rough+=r*w;ao+=a*w;total+=w;}
total=max(total,.001);
OutNormal=normalize(normal);OutRoughness=clamp(rough/total,.2,1);OutAO=clamp(ao/total,.25,1);
return color/total*lerp(.94,1.02,broad);
