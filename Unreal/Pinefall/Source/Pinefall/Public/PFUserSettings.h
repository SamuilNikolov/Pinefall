#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameUserSettings.h"
#include "PFUserSettings.generated.h"
UCLASS(Config = GameUserSettings)
class PINEFALL_API UPFUserSettings : public UGameUserSettings
{
    GENERATED_BODY()
  public:
    UPROPERTY(Config) float MouseSensitivity = .12f;
    UPROPERTY(Config) float MasterVolume = .8f;
    virtual void ApplyNonResolutionSettings() override;
    virtual void SetToDefaults() override;
    static UPFUserSettings *Get();
};
