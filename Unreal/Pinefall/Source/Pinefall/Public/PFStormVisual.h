#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PFStormVisual.generated.h"
class UStaticMeshComponent;
class UPostProcessComponent;
UCLASS()
class PINEFALL_API APFStormVisual : public AActor
{
    GENERATED_BODY()
  public:
    APFStormVisual();
    virtual void BeginPlay() override;
    virtual void Tick(float Delta) override;

  private:
    UPROPERTY() TObjectPtr<UStaticMeshComponent> Wall;
    UPROPERTY() TObjectPtr<UPostProcessComponent> Atmosphere;
};
