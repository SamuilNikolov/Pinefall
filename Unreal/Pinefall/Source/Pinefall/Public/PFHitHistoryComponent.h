#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "PFHitHistoryComponent.generated.h"

// Server-only copies of the actual physics-asset primitives. No live body is
// teleported for rewind, and no historical transforms are sent to other players.
struct FPFHitShape
{
    FTransform Pose;
    FVector Extent = FVector::ZeroVector;
    FName Bone;
    int32 BoneIndex = INDEX_NONE;
    float Radius = 0, HalfLength = 0;
    uint8 Kind = 0; // sphere, capsule, box
};
struct FPFHitFrame
{
    double Time = 0;
    FVector Origin = FVector::ZeroVector;
    TArray<FPFHitShape, TInlineAllocator<32>> Shapes;
};
UCLASS()
class PINEFALL_API UPFHitHistoryComponent : public UActorComponent
{
    GENERATED_BODY()
  public:
    UPFHitHistoryComponent();
    virtual void BeginPlay() override;
    virtual void TickComponent(float Delta, ELevelTick Type, FActorComponentTickFunction *Tick) override;
    bool SupportsRewind() const
    {
        return !Templates.IsEmpty();
    }
    bool TraceAt(double Time, const FVector &Start, const FVector &End, FHitResult &Hit) const;
    static constexpr double MaxRewindSeconds = .35;

  private:
    void Capture(FPFHitFrame &Frame, double Time) const;
    TArray<FPFHitShape, TInlineAllocator<32>> Templates;
    static constexpr int32 Capacity = 16;
    FPFHitFrame Frames[Capacity];
    int32 NextFrame = 0, FrameCount = 0;
};
