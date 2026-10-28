#pragma once
#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"
#include "PFInterface.generated.h"
class UCanvasPanel;
class UBorder;
class UTextBlock;
class UProgressBar;
class UEditableTextBox;
class USlider;
class UVerticalBox;
UCLASS()
class PINEFALL_API UPFInterface : public UUserWidget
{
    GENERATED_BODY()
  public:
    virtual TSharedRef<SWidget> RebuildWidget() override;
    virtual void NativeTick(const FGeometry &Geometry, float Delta) override;
    void SetMenuVisible(bool bVisible);

  protected:
    float RefreshRemaining = 0;
    UPROPERTY() TObjectPtr<UBorder> Menu;
    UPROPERTY() TObjectPtr<UTextBlock> HealthText;
    UPROPERTY() TObjectPtr<UTextBlock> WeaponText;
    UPROPERTY() TObjectPtr<UTextBlock> SessionText;
    UPROPERTY() TObjectPtr<UTextBlock> HitText;
    UPROPERTY() TObjectPtr<UTextBlock> DamageText;
    UPROPERTY() TObjectPtr<UTextBlock> MenuStatus;
    UPROPERTY() TObjectPtr<UProgressBar> HealthBar;
    UPROPERTY() TObjectPtr<UEditableTextBox> AddressBox;
    UPROPERTY() TObjectPtr<USlider> VolumeSlider;
    UPROPERTY() TObjectPtr<USlider> SensitivitySlider;
    UTextBlock *Label(UVerticalBox *Parent, const FString &Text, int32 Size, FLinearColor Color);
    void Button(UVerticalBox *Parent, const FString &Text, FName Handler, bool bPrimary = false);
    UFUNCTION() void Resume();
    UFUNCTION() void Host();
    UFUNCTION() void Join();
    UFUNCTION() void Quality();
    UFUNCTION() void Quit();
    UFUNCTION() void Volume(float Value);
    UFUNCTION() void Sensitivity(float Value);
};
