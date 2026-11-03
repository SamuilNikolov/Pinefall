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
    UPROPERTY(Config) bool bBalancedLighting = true;
    UPROPERTY(Config) float HipFieldOfView = 85.f;
    UPROPERTY(Config) float AimSensitivityMultiplier = 1.f;
    UPROPERTY(Config) float CrosshairScale = 1.f;
    UPROPERTY(Config) int32 CrosshairColor = 0;
    float GetHipFieldOfView() const;
    float GetAimSensitivity(float AimFOV, float AimAlpha) const;
    FLinearColor GetCrosshairColor() const;
    virtual void ApplyNonResolutionSettings() override;
    virtual void SetToDefaults() override;
    static UPFUserSettings *Get();
};
