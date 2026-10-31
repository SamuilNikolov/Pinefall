#include "PFBuildTypes.h"
FPFBuildKey PFBuildGrid::Key(const FPFBuildRequest &R)
{
    FPFBuildKey K;K.Frame=R.Frame;K.X=R.Cell.X;K.Y=R.Cell.Y;K.Z=R.Cell.Z;
    if(R.Kind==EPFBuildKind::Wall)
    {
        K.Slot=R.Turn%2;
        if(R.Turn==0)K.X++;if(R.Turn==1)K.Y++;
    }
    else K.Slot=R.Kind==EPFBuildKind::Floor?2:3;
    return K;
}
FTransform PFBuildGrid::Transform(const FPFBuildRequest &R)
{
    FRotator Frame(0,R.Yaw,0);
    return FTransform(FRotator(0,R.Yaw+R.Turn*90,0),FVector(R.Origin)+Frame.RotateVector(FVector(R.Cell.X*Tile,R.Cell.Y*Tile,R.Cell.Z*Storey)));
}
int32 PFBuildGrid::Facing(float Yaw,float FrameYaw,int32 Previous)
{
    return FMath::Abs(FRotator::NormalizeAxis(Yaw-FrameYaw-Previous*90))<=51?Previous:(FMath::RoundToInt(FRotator::NormalizeAxis(Yaw-FrameYaw)/90)%4+4)%4;
}
bool PFBuildGrid::Valid(const FPFBuildRequest &R)
{
    return !FVector(R.Origin).ContainsNaN()&&FMath::IsFinite(R.Yaw)&&R.Yaw>=0&&R.Yaw<360&&
        uint8(R.Kind)<4&&uint8(R.Material)<3&&R.Turn<4&&FMath::Abs(int64(R.Cell.X))<1000&&FMath::Abs(int64(R.Cell.Y))<1000&&FMath::Abs(int64(R.Cell.Z))<128;
}
uint16 PFBuildGrid::FullMask(EPFBuildKind Kind) { return Kind==EPFBuildKind::Wall?511:Kind==EPFBuildKind::Roof?0:15; }
bool PFBuildGrid::IsDoor(uint16 Mask,int32 &Column)
{
    for(int32 C=0;C<3;C++)if(Mask==(511^((1<<C)|(1<<(C+3))))){Column=C;return true;}
    return false;
}
bool PFBuildGrid::ValidEdit(EPFBuildKind Kind,uint16 Mask,uint8 RampPattern)
{
    if(Kind==EPFBuildKind::Roof)return Mask<15;
    if(Kind==EPFBuildKind::Floor)return Mask>0&&Mask<16&&Mask!=6&&Mask!=9;
    if(Kind==EPFBuildKind::Ramp)return Mask==15&&RampPattern<7;
    if(Mask==511||Mask==(511^(1<<4)))return true;
    int32 Column;if(IsDoor(Mask,Column))return true;
    static const uint16 Patterns[]={63,7,73,146,292,219,438,27,54,216,432,
        uint16(511^((1<<5)|(1<<7)|(1<<8))),uint16(511^((1<<3)|(1<<6)|(1<<7))),
        uint16(511^((1<<0)|(1<<1)|(1<<3))),uint16(511^((1<<1)|(1<<2)|(1<<5))),
        uint16(511^((1<<0)|(1<<1)|(1<<2)|(1<<4)))};
    for(uint16 P:Patterns)if(Mask==P)return true;
    return false;
}
void PFBuildGrid::Anchors(const FPFBuildState &S,FAnchors &Out)
{
    auto Add=[&](FVector P)
    {
        const int32 Turn=S.Request.Turn;
        if(Turn==1)P=FVector(-P.Y,P.X,P.Z);else if(Turn==2)P=FVector(-P.X,-P.Y,P.Z);else if(Turn==3)P=FVector(P.Y,-P.X,P.Z);
        P+=FVector(S.Request.Cell.X*Tile,S.Request.Cell.Y*Tile,S.Request.Cell.Z*Storey);
        Out.AddUnique(FIntVector(int32(FMath::RoundToInt(P.X)),int32(FMath::RoundToInt(P.Y)),int32(FMath::RoundToInt(P.Z))));
    };
    auto Quad=[&](FVector A,FVector B,FVector C,FVector D)
    {for(int32 Y=0;Y<3;Y++)for(int32 X=0;X<3;X++)Add(FMath::Lerp(FMath::Lerp(A,B,X*.5f),FMath::Lerp(D,C,X*.5f),Y*.5f));};
    auto Tri=[&](FVector A,FVector B,FVector C)
    {for(int32 Y=0;Y<=4;Y++)for(int32 X=0;X<=4-Y;X++)Add(A+(B-A)*(X*.25f)+(C-A)*(Y*.25f));};
    float H=Tile*.5f;
    if(S.Request.Kind==EPFBuildKind::Wall)
    {
        uint16 M=S.Mask;
        if(M==(511^((1<<5)|(1<<7)|(1<<8))))Tri({H,-H,0},{H,H,0},{H,-H,Storey});
        else if(M==(511^((1<<3)|(1<<6)|(1<<7))))Tri({H,-H,0},{H,H,Storey},{H,H,0});
        else if(M==(511^((1<<0)|(1<<1)|(1<<3))))Tri({H,-H,Storey},{H,H,Storey},{H,H,0});
        else if(M==(511^((1<<1)|(1<<2)|(1<<5))))Tri({H,-H,0},{H,-H,Storey},{H,H,Storey});
        else for(int32 I=0;I<9;I++)if(M&(1<<I))
        {float Y=-H+(I%3)*Tile/3,Z=(I/3)*Storey/3;Quad({H,Y,Z},{H,Y+Tile/3,Z},{H,Y+Tile/3,Z+Storey/3},{H,Y,Z+Storey/3});}
    }
    else if(S.Request.Kind==EPFBuildKind::Floor)
    {
        for(int32 I=0;I<4;I++)if(S.Mask&(1<<I))
        {float X=(I%2)*H-H,Y=(I/2)*H-H;Quad({X,Y,0},{X+H,Y,0},{X+H,Y+H,0},{X,Y+H,0});}
    }
    else if(S.Request.Kind==EPFBuildKind::Roof)
    {
        FVector P[]={{-H,-H,0},{-H,H,0},{H,H,0},{H,-H,0},{0,0,Storey}};
        if(S.Mask){float Sum=0;for(int32 I=0;I<4;I++){P[I].Z=(S.Mask&(1<<I))?Storey:0;Sum+=P[I].Z;}P[4].Z=Sum*.25f;}
        for(int32 I=0;I<4;I++)Tri(P[I],P[(I+1)%4],P[4]);
    }
    else
    {
        auto Slope=[&](float X0,float X1,float Y0,float Y1,float Z0,float Z1,bool Across=false)
        {Quad({X0,Y0,Z0},{X1,Y0,Across?Z0:Z1},{X1,Y1,Z1},{X0,Y1,Across?Z1:Z0});};
        int32 Pattern=S.RampPattern;
        if(Pattern<=2)Slope(-H,H,Pattern==2?0:-H,Pattern==1?0:H,0,Storey);
        else if(Pattern<=4)
        {
            bool Left=Pattern==3;Slope(-H,0,Left?0:-H,Left?H:0,0,Storey*.5f);
            float Y0=Left?0:-H,Y1=Left?H:0;Quad({0,Y0,Storey*.5f},{H,Y0,Storey*.5f},{H,Y1,Storey*.5f},{0,Y1,Storey*.5f});
            Slope(0,H,Left?-H:0,Left?0:H,Left?Storey:Storey*.5f,Left?Storey*.5f:Storey,true);
        }
        else
        {
            bool Left=Pattern==5;Slope(-H,H-64,Left?-H:0,Left?0:H,0,Storey*.5f);
            Quad({H-64,-H,Storey*.5f},{H,-H,Storey*.5f},{H,H,Storey*.5f},{H-64,H,Storey*.5f});
            Slope(-H,H-64,Left?0:-H,Left?H:0,Storey,Storey*.5f);
        }
    }
}
bool PFBuildGrid::Connected(const FPFBuildState &A,const FPFBuildState &B)
{
    if(A.Request.Frame!=B.Request.Frame)return false;
    FAnchors PA,PB;Anchors(A,PA);Anchors(B,PB);
    for(const auto &P:PA)if(PB.Contains(P))return true;return false;
}
const TCHAR *PFBuildGrid::ShapeName(EPFBuildKind K) { static const TCHAR *N[]={TEXT("WALL"),TEXT("FLOOR"),TEXT("RAMP"),TEXT("ROOF")};return N[FMath::Clamp(int32(K),0,3)]; }
const TCHAR *PFBuildGrid::MaterialName(EPFBuildMaterial K) {static const TCHAR *N[]={TEXT("WOOD"),TEXT("STONE"),TEXT("METAL")};return N[FMath::Clamp(int32(K),0,2)];}
