#include "PFUserSettings.h"
#include "Engine/Engine.h"
#include "AudioDevice.h"
UPFUserSettings *UPFUserSettings::Get()
{
    return GEngine ? Cast<UPFUserSettings>(GEngine->GetGameUserSettings()) : nullptr;
}
void UPFUserSettings::SetToDefaults()
{
    Super::SetToDefaults();
    ScalabilityQuality.SetFromSingleQualityLevel(2);
    bBalancedLighting = true;
    MouseSensitivity = .12f;
    MasterVolume = .8f;
    HipFieldOfView = 85.f;
    AimSensitivityMultiplier = CrosshairScale = 1.f;
    CrosshairColor = 0;
    SetFrameRateLimit(90);
    SetVSyncEnabled(false);
}
float UPFUserSettings::GetHipFieldOfView() const
{
    return FMath::IsFinite(HipFieldOfView) ? FMath::Clamp(HipFieldOfView, 75.f, 110.f) : 85.f;
}
float UPFUserSettings::GetAimSensitivity(float AimFOV, float AimAlpha) const
{
    const float Multiplier = FMath::IsFinite(AimSensitivityMultiplier)
        ? FMath::Clamp(AimSensitivityMultiplier, .25f, 2.f) : 1.f;
    // Keep unscoped movement unchanged; blend the independent ADS preference
    // alongside the existing zoom compensation, including during transitions.
    const float Zoom = FMath::Clamp(AimFOV / GetHipFieldOfView(), .25f, 1.f);
    return FMath::Lerp(1.f, Zoom * Multiplier, FMath::Clamp(AimAlpha, 0.f, 1.f));
}
FLinearColor UPFUserSettings::GetCrosshairColor() const
{
    switch (CrosshairColor)
    {
    case 1: return FLinearColor(.2f, 1.f, 1.f);
    case 2: return FLinearColor(.55f, 1.f, .15f);
    case 3: return FLinearColor(1.f, .35f, .8f);
    default: return FLinearColor::White;
    }
}
void UPFUserSettings::ApplyNonResolutionSettings()
{
    if (bBalancedLighting)
    {
        ScalabilityQuality.GlobalIlluminationQuality = 1;
        ScalabilityQuality.ReflectionQuality = 1;
    }
    Super::ApplyNonResolutionSettings();
    if (GEngine)
    {
        auto Device = GEngine->GetMainAudioDevice();
        if (Device)
            Device->SetTransientPrimaryVolume(FMath::Clamp(MasterVolume, 0.f, 1.f));
    }
}
