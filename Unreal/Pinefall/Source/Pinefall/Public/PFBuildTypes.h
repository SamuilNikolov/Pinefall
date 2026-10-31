#pragma once
#include "CoreMinimal.h"
#include "Engine/NetSerialization.h"
#include "PFBuildTypes.generated.h"
UENUM(BlueprintType)
enum class EPFBuildKind : uint8 { Wall, Floor, Ramp, Roof };
UENUM(BlueprintType)
enum class EPFBuildMaterial : uint8 { Wood, Stone, Metal };
UENUM(BlueprintType)
enum class EPFConstructionMode : uint8 { Combat, Pickaxe, Build };
USTRUCT()
struct FPFBuildRequest
{
    GENERATED_BODY()
    UPROPERTY() FVector_NetQuantize Origin = FVector::ZeroVector;
    UPROPERTY() FIntVector Cell = FIntVector::ZeroValue;
    UPROPERTY() float Yaw = 0;
    UPROPERTY() int32 Frame = 0;
    UPROPERTY() uint32 FrameNonce = 1;
    UPROPERTY() uint32 RequestId = 0;
    UPROPERTY() EPFBuildKind Kind = EPFBuildKind::Wall;
    UPROPERTY() EPFBuildMaterial Material = EPFBuildMaterial::Wood;
    UPROPERTY() uint8 Turn = 0;
};
USTRUCT()
struct FPFBuildState
{
    GENERATED_BODY()
    UPROPERTY() FPFBuildRequest Request;
    UPROPERTY() int32 CreatorId = -1;
    UPROPERTY() int32 Team = -1;
    UPROPERTY() uint16 Mask = 511;
    UPROPERTY() uint8 RampPattern = 0;
    UPROPERTY() uint32 Revision = 1;
    UPROPERTY() int32 EditorId = -1;
    UPROPERTY() bool bDoorOpen = false;
    UPROPERTY() double StartedAt = 0;
};
USTRUCT()
struct FPFBuildResources
{
    GENERATED_BODY()
    UPROPERTY() int32 Wood = 100;
    UPROPERTY() int32 Stone = 50;
    UPROPERTY() int32 Metal = 20;
    UPROPERTY() uint32 Revision = 1;
    int32 Get(int32 Kind) const { return Kind==0?Wood:Kind==1?Stone:Metal; }
    int32 &At(int32 Kind) { return Kind==0?Wood:Kind==1?Stone:Metal; }
};
struct FPFBuildKey
{
    int32 Frame=0, X=0,Y=0,Z=0;
    uint8 Slot=0;
    bool operator==(const FPFBuildKey &B) const { return Frame==B.Frame&&X==B.X&&Y==B.Y&&Z==B.Z&&Slot==B.Slot; }
    friend uint32 GetTypeHash(const FPFBuildKey &K) { return HashCombine(HashCombine(GetTypeHash(K.Frame),GetTypeHash(FIntVector(K.X,K.Y,K.Z))),K.Slot); }
};
namespace PFBuildGrid
{
    constexpr float Tile=512, Storey=384, Reach=1536;
    constexpr int32 Cost=10, ResourceCap=999, PieceCap=6000;
    PINEFALL_API FPFBuildKey Key(const FPFBuildRequest &Request);
    PINEFALL_API FTransform Transform(const FPFBuildRequest &Request);
    PINEFALL_API int32 Facing(float Yaw,float FrameYaw,int32 Previous);
    PINEFALL_API bool Valid(const FPFBuildRequest &Request);
    PINEFALL_API uint16 FullMask(EPFBuildKind Kind);
    PINEFALL_API bool ValidEdit(EPFBuildKind Kind,uint16 Mask,uint8 RampPattern=0);
    PINEFALL_API bool IsDoor(uint16 Mask,int32 &Column);
    using FAnchors=TArray<FIntVector,TInlineAllocator<128>>;
    PINEFALL_API void Anchors(const FPFBuildState &State,FAnchors &Out);
    PINEFALL_API bool Connected(const FPFBuildState &A,const FPFBuildState &B);
    PINEFALL_API const TCHAR *ShapeName(EPFBuildKind Kind);
    PINEFALL_API const TCHAR *MaterialName(EPFBuildMaterial Material);
}
