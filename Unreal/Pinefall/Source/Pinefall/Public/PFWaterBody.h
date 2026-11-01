#pragma once
#include "CoreMinimal.h"
#include "GameFramework/PhysicsVolume.h"
#include "PFWaterBody.generated.h"
class UBoxComponent;
class UStaticMeshComponent;
UCLASS()
class PINEFALL_API APFWaterBody : public APhysicsVolume
{
    GENERATED_BODY()
  public:
    APFWaterBody();
    virtual void BeginPlay() override;
    virtual void OnConstruction(const FTransform &Transform) override;
    virtual bool IsOverlapInVolume(const USceneComponent &Test) const override;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UBoxComponent> WaterBounds;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<UStaticMeshComponent> Surface;
    bool ContainsWater(const FVector &Point) const;
    float SurfaceHeight() const;
  private:
    void FitSurface();
};
