#pragma once
#include "CoreMinimal.h"
#include "PFBuildTypes.h"
struct FPFBuildMesh
{
    TArray<FVector> Vertices,Normals;
    TArray<int32> Triangles;
    TArray<FVector2D> UV;
    void Quad(FVector A,FVector B,FVector C,FVector D);
    void Box(FVector Center,FVector Size);
    void Triangle(FVector A,FVector B,FVector C,float Thickness=10);
};
namespace PFBuildGeometry
{
    FPFBuildMesh Generate(EPFBuildKind Kind,uint16 Mask,uint8 RampPattern=0);
    int32 TileAtRay(const FPFBuildState &State,const FVector &Start,const FVector &Direction);
    FVector EditPoint(EPFBuildKind Kind,int32 Tile);
}
