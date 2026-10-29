#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PFReserveWorld.generated.h"
class APFVehicle;
UCLASS()
class PINEFALL_API APFReserveWorld : public AActor
{
    GENERATED_BODY()
  public:
    APFReserveWorld();
    virtual void BeginPlay() override;
    UPROPERTY(EditAnywhere) TArray<FVector> LootPoints;
    UPROPERTY(EditAnywhere) TArray<FTransform> VehicleStarts;
    UPROPERTY(EditAnywhere) TArray<FTransform> BoatStarts;
    UPROPERTY() TArray<TObjectPtr<APFVehicle>> Vehicles;
    void ResetLoot();
    void ResetVehicles();
    void RefreshMapFeed();

  private:
    FTimerHandle MapTimer;
};
