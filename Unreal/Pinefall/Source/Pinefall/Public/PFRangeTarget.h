#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PFRangeTarget.generated.h"
class UStaticMeshComponent;
UCLASS()
class PINEFALL_API APFRangeTarget : public AActor
{
    GENERATED_BODY()
  public:
    APFRangeTarget();
    UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> Plate;
    UPROPERTY(Replicated, BlueprintReadOnly) float Health = 100;
    UPROPERTY(Replicated, BlueprintReadOnly) float Armor = 50;
    void ReceiveShot(float Damage);
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
};
