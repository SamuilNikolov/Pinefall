#pragma once
#include "PFWorldPickup.h"
#include "PFSupplyDrop.generated.h"
class APFCharacter;
/** Public world objective. Rewards are released once, on authority, never into a client's inventory. */
UCLASS()
class PINEFALL_API APFSupplyDrop : public APFWorldPickup
{
    GENERATED_BODY()
public:
    APFSupplyDrop();
    UPROPERTY(Replicated) double LandingAt = 0;
    UPROPERTY(ReplicatedUsing=RefreshVisual) bool bOpened = false;
    UPROPERTY() TObjectPtr<UStaticMeshComponent> Canopy;
    bool HasLanded() const;
    bool Open(APFCharacter *Player);
    virtual void RefreshVisual() override;
    virtual void BeginPlay() override;
    virtual void Tick(float Delta) override;
    virtual void EndPlay(const EEndPlayReason::Type Reason) override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& Props) const override;
private:
    FTimerHandle LandingTimer;
    void Land();
    void RemovePin();
};
