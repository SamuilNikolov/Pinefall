#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PFInventoryComponent.h"
#include "PFWorldPickup.generated.h"
class USphereComponent;
class UStaticMeshComponent;
UCLASS()
class PINEFALL_API APFWorldPickup : public AActor
{
    GENERATED_BODY()
  public:
    APFWorldPickup();
    UPROPERTY(ReplicatedUsing = RefreshVisual, BlueprintReadOnly) FPFItemStack Item;
    UPROPERTY(VisibleAnywhere) TObjectPtr<USphereComponent> Interaction;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> Body;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> Detail;
    void Initialize(const FPFItemStack &Stack);
    UFUNCTION() void RefreshVisual();
    virtual void BeginPlay() override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
};
