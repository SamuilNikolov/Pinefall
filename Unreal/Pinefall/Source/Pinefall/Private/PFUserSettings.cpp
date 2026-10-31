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
    bBalancedLighting = false;
    SetFrameRateLimit(90);
    SetVSyncEnabled(false);
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
