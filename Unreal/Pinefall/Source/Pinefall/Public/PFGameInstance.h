#pragma once
#include "CoreMinimal.h"
#include "Engine/GameInstance.h"
#include "PFGameInstance.generated.h"
UCLASS()
class PINEFALL_API UPFGameInstance : public UGameInstance
{
    GENERATED_BODY()
  public:
    UPROPERTY(BlueprintReadOnly) FString ConnectionMessage;
    UFUNCTION(BlueprintCallable) void HostRange();
    UFUNCTION(BlueprintCallable) void HostBattleRoyale(int32 TeamSize);
    UFUNCTION(BlueprintCallable) bool JoinRange(const FString &Address);
    UFUNCTION(BlueprintCallable) FString LocalAddress() const;

  private:
    mutable FString CachedLanAddress;
};
