#include "PFBuildGeometry.h"
void FPFBuildMesh::Quad(FVector A,FVector B,FVector C,FVector D)
{
    int32 I=Vertices.Num();FVector N=FVector::CrossProduct(B-A,C-A).GetSafeNormal();
    for(FVector P:{A,B,C,D}){Vertices.Add(P);Normals.Add(N);int32 Axis=N.GetAbs().GetMax()==FMath::Abs(N.X)?0:N.GetAbs().GetMax()==FMath::Abs(N.Y)?1:2;UV.Add(Axis==0?FVector2D(P.Y,P.Z)/200:Axis==1?FVector2D(P.X,P.Z)/200:FVector2D(P.X,P.Y)/200);}
    // PMC cooks flipped normals: use Unreal clockwise front faces while
    // retaining outward vertex normals for lighting and walkable contact.
    Triangles.Append({I,I+2,I+1,I,I+3,I+2});
}
void FPFBuildMesh::Box(FVector C,FVector S)
{
    FVector L=C-S*.5,H=C+S*.5;
    Quad({H.X,L.Y,L.Z},{H.X,H.Y,L.Z},{H.X,H.Y,H.Z},{H.X,L.Y,H.Z});
    Quad({L.X,H.Y,L.Z},{L.X,L.Y,L.Z},{L.X,L.Y,H.Z},{L.X,H.Y,H.Z});
    Quad({L.X,H.Y,L.Z},{L.X,H.Y,H.Z},{H.X,H.Y,H.Z},{H.X,H.Y,L.Z});
    Quad({H.X,L.Y,L.Z},{H.X,L.Y,H.Z},{L.X,L.Y,H.Z},{L.X,L.Y,L.Z});
    Quad({L.X,L.Y,H.Z},{H.X,L.Y,H.Z},{H.X,H.Y,H.Z},{L.X,H.Y,H.Z});
    Quad({L.X,H.Y,L.Z},{H.X,H.Y,L.Z},{H.X,L.Y,L.Z},{L.X,L.Y,L.Z});
}
void FPFBuildMesh::Triangle(FVector A,FVector B,FVector C,float T)
{
    FVector N=FVector::CrossProduct(B-A,C-A).GetSafeNormal()*T*.5;int32 I=Vertices.Num();
    for(auto P:{A+N,B+N,C+N}){Vertices.Add(P);Normals.Add(N.GetSafeNormal());UV.Add(FVector2D(P.Y,P.Z)/200);}Triangles.Append({I,I+2,I+1});
    I=Vertices.Num();for(auto P:{C-N,B-N,A-N}){Vertices.Add(P);Normals.Add(-N.GetSafeNormal());UV.Add(FVector2D(P.Y,P.Z)/200);}Triangles.Append({I,I+2,I+1});
    Quad(A-N,B-N,B+N,A+N);Quad(B-N,C-N,C+N,B+N);Quad(C-N,A-N,A+N,C+N);
}
FPFBuildMesh PFBuildGeometry::Generate(EPFBuildKind Kind,uint16 Mask,uint8 Pattern)
{
    using namespace PFBuildGrid;FPFBuildMesh M;float H=Tile*.5;
    if(Kind==EPFBuildKind::Wall)
    {
        uint16 UpperRight=511^((1<<5)|(1<<7)|(1<<8)),UpperLeft=511^((1<<3)|(1<<6)|(1<<7));
        if(Mask==UpperRight)M.Triangle({H,-H,0},{H,H,0},{H,-H,Storey});
        else if(Mask==UpperLeft)M.Triangle({H,-H,0},{H,H,Storey},{H,H,0});
        else if(Mask==(511^((1<<0)|(1<<1)|(1<<3))))M.Triangle({H,-H,Storey},{H,H,Storey},{H,H,0});
        else if(Mask==(511^((1<<1)|(1<<2)|(1<<5))))M.Triangle({H,-H,0},{H,-H,Storey},{H,H,Storey});
        else for(int32 I=0;I<9;I++)if(Mask&(1<<I))M.Box({H,-H+(I%3+.5f)*Tile/3,(I/3+.5f)*Storey/3},{12,Tile/3,Storey/3});
        bool Diagonal=Mask==UpperRight||Mask==UpperLeft||Mask==(511^((1<<0)|(1<<1)|(1<<3)))||Mask==(511^((1<<1)|(1<<2)|(1<<5)));
        if(Diagonal)return M;
        // Surface ribs improve silhouettes without opening unintended bullet gaps.
        for(int32 I=0;I<3;I++)for(int32 Row=0;Row<3;Row++)if(Mask&(1<<(Row*3+I)))
            M.Box({H+9,-H+(I+.5f)*Tile/3,(Row+.5f)*Storey/3},{6,8,Storey/3});
        return M;
    }
    if(Kind==EPFBuildKind::Floor)
    {
        for(int32 I=0;I<4;I++)if(Mask&(1<<I))M.Box({I%2?H*.5f:-H*.5f,I/2?H*.5f:-H*.5f,-7},{H,H,14});return M;
    }
    if(Kind==EPFBuildKind::Roof)
    {
        FVector P[]={{-H,-H,0},{-H,H,0},{H,H,0},{H,-H,0},{0,0,Storey}};
        if(Mask){float Sum=0;for(int32 I=0;I<4;I++){P[I].Z=(Mask&(1<<I))?Storey:0;Sum+=P[I].Z;}P[4].Z=Sum*.25f;}
        for(int32 I=0;I<4;I++)M.Triangle(P[I],P[(I+1)%4],P[4],12);return M;
    }
    auto Slope=[&](float X0,float X1,float Y0,float Y1,float Z0,float Z1,bool Across=false)
    {
        FVector A(X0,Y0,Z0),B(X1,Y0,Across?Z0:Z1),C(X1,Y1,Z1),D(X0,Y1,Across?Z1:Z0);
        M.Quad(A,B,C,D);M.Quad(D-FVector(0,0,14),C-FVector(0,0,14),B-FVector(0,0,14),A-FVector(0,0,14));
        M.Quad(A-FVector(0,0,14),B-FVector(0,0,14),B,A);M.Quad(B-FVector(0,0,14),C-FVector(0,0,14),C,B);
        M.Quad(C-FVector(0,0,14),D-FVector(0,0,14),D,C);M.Quad(D-FVector(0,0,14),A-FVector(0,0,14),A,D);
    };
    if(Pattern<=2)Slope(-H,H,Pattern==2?0:-H,Pattern==1?0:H,0,Storey);
    else if(Pattern<=4)
    {
        bool Left=Pattern==3;
        Slope(-H,0,Left?0:-H,Left?H:0,0,Storey*.5f);
        M.Box({H*.5f,Left?H*.5f:-H*.5f,Storey*.5f-7},{H,H,14});
        Slope(0,H,Left?-H:0,Left?0:H,Left?Storey:Storey*.5f,Left?Storey*.5f:Storey,true);
    }
    else
    {
        bool Left=Pattern==5;Slope(-H,H-64,Left?-H:0,Left?0:H,0,Storey*.5f);
        M.Box({H-32,0,Storey*.5f-7},{64,Tile,14});
        Slope(-H,H-64,Left?0:-H,Left?H:0,Storey,Storey*.5f);
    }
    return M;
}
int32 PFBuildGeometry::TileAtRay(const FPFBuildState &S,const FVector &Start,const FVector &Dir)
{
    FTransform T=PFBuildGrid::Transform(S.Request);FVector O=T.InverseTransformPosition(Start),D=T.InverseTransformVectorNoScale(Dir);
    float Time=-1;
    if(S.Request.Kind==EPFBuildKind::Wall){if(FMath::Abs(D.X)>.001)Time=(256-O.X)/D.X;}
    else if(S.Request.Kind==EPFBuildKind::Ramp){float Den=D.Z-.75f*D.X;if(FMath::Abs(Den)>.001)Time=(.75f*(O.X+256)-O.Z)/Den;}
    else if(FMath::Abs(D.Z)>.001)Time=(5.f-O.Z)/D.Z;
    if(Time<0||Time>PFBuildGrid::Reach)return -1;
    FVector P=O+D*Time;
    if(S.Request.Kind==EPFBuildKind::Wall){if(FMath::Abs(P.Y)>256||P.Z<0||P.Z>384)return -1;return FMath::Clamp(int32((P.Y+256)/512*3),0,2)+3*FMath::Clamp(int32(P.Z/384*3),0,2);}
    if(FMath::Abs(P.X)>256||FMath::Abs(P.Y)>256)return -1;
    int32 N=S.Request.Kind==EPFBuildKind::Ramp?3:2;
    return FMath::Clamp(int32((P.Y+256)/512*N),0,N-1)+N*FMath::Clamp(int32((P.X+256)/512*N),0,N-1);
}
FVector PFBuildGeometry::EditPoint(EPFBuildKind K,int32 I)
{
    if(K==EPFBuildKind::Wall)return {264,-256+(I%3+.5f)*512/3,(I/3+.5f)*384/3};
    int32 N=K==EPFBuildKind::Ramp?3:2;float X=-256+(I/N+.5f)*512/N,Y=-256+(I%N+.5f)*512/N;
    return {X,Y,K==EPFBuildKind::Ramp?(X+256)*.75f+8:8.f};
}
