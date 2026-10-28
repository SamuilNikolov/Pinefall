#pragma once
#include "CoreMinimal.h"
#include "Engine/DataTable.h"
#include "PFWeaponDefinition.generated.h"
class USoundBase;
class UStaticMesh;
UENUM(BlueprintType)
enum class EPFWeaponClass : uint8
{
    Pistol,
    Shotgun,
    SMG,
    AssaultRifle,
    Sniper,
    MarksmanRifle,
    SupportGun,
    Launcher
};
USTRUCT(BlueprintType)
struct PINEFALL_API FPFWeaponDefinition : public FTableRowBase
{
    GENERATED_BODY()
    UPROPERTY(EditAnywhere, BlueprintReadOnly) FText DisplayName = FText::FromString(TEXT("Ridgeline"));
    UPROPERTY(EditAnywhere, BlueprintReadOnly) EPFWeaponClass Category = EPFWeaponClass::Sniper;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) bool bAutomatic = false;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Magazine = 5;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float Damage = 80.f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float Interval = 1.15f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float ReloadSeconds = 2.5f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float RangeCm = 65000.f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) float AimFOV = 32.f;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TObjectPtr<UStaticMesh> Model;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) TObjectPtr<USoundBase> ShotSound;
};
