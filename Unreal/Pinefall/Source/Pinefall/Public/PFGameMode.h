#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/PlayerState.h"
#include "PFGameMode.generated.h"
UCLASS()
class PINEFALL_API APFPlayerState : public APlayerState
{
    GENERATED_BODY()
  public:
    UPROPERTY(Replicated, BlueprintReadOnly) int32 Eliminations = 0;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
};
UCLASS()
class PINEFALL_API APFGameMode : public AGameModeBase
{
    GENERATED_BODY()
  public:
    APFGameMode();
    virtual void PreLogin(const FString &Options, const FString &Address, const FUniqueNetIdRepl &UniqueId,
                          FString &ErrorMessage) override;
    virtual void PostLogin(APlayerController *NewPlayer) override;
    void RespawnAfterDeath(AController *Controller);
};
