#pragma once
#include "Blueprint/UserWidget.h"
#include "CoreMinimal.h"
#include "Fonts/SlateFontInfo.h"
#include "Styling/SlateBrush.h"
#include "PFInterface.generated.h"
class UCanvasPanel;
class UBorder;
class UTextBlock;
class UProgressBar;
class UEditableTextBox;
class USlider;
class UVerticalBox;
class UPFTacticalMap;
class UPFItemButton;
class UImage;
class UTexture2D;
struct FPFHUDItem
{
    FSlateBrush Icon;
    FString Count, Key, Caption;
    FLinearColor Rarity = FLinearColor(.35, .4, .45);
    bool bOccupied = false, bEquipped = false;
};
struct FPFHUDAlly
{
    FString Name, Status;
    float Health = 0, Armor = 0, ArmorMax = 100;
    FLinearColor Color = FLinearColor(.3, .65, 1);
    bool bDowned = false, bEliminated = false;
};
UCLASS()
class PINEFALL_API UPFInterface : public UUserWidget
{
    GENERATED_BODY()
  public:
    virtual TSharedRef<SWidget> RebuildWidget() override;
    virtual FReply NativeOnKeyDown(const FGeometry &Geometry,const FKeyEvent &Event) override;
    virtual FReply NativeOnKeyUp(const FGeometry &Geometry,const FKeyEvent &Event) override;
    virtual void NativeTick(const FGeometry &Geometry, float Delta) override;
    int32 PaintCombat(const FGeometry &Geometry, FSlateWindowElementList &Out, int32 Layer) const;
    int32 PaintExpeditionHUD(const FGeometry &Geometry, FSlateWindowElementList &Out, int32 Layer) const;
    void SetMenuVisible(bool bVisible);
    void RefreshInventory(){UpdateExpeditionUI();UpdateHUDData();}

  protected:
    float RefreshRemaining = 0;
    mutable TArray<FVector2D> CombatLines;
    FVector2D TargetBarPosition = FVector2D::ZeroVector;
    float TargetBarFraction = 0;
    bool bShowTargetBar = false;
    void UpdateHUDData();
    UPROPERTY() TObjectPtr<UObject> HUDDisplayFont;
    UPROPERTY() TObjectPtr<UTextBlock> BrandLabel;
    UPROPERTY() TObjectPtr<UTextBlock> LocationLabel;
    UPROPERTY() TObjectPtr<UBorder> VitalsPanel;
    FSlateFontInfo HUDSmallFont, HUDLabelFont, HUDTitleFont, HUDNumberFont;
    TArray<FPFHUDItem> HUDItems;
    TArray<FPFHUDAlly> HUDAllies;
    FString HUDName, HUDHealth, HUDArmor, HUDHunger, HUDStamina, HUDMagazine, HUDReserve;
    FString HUDWeaponName, HUDWeaponType, HUDStormLabel, HUDStormClock, HUDSurvivors, HUDEliminations;
    UFUNCTION() void DropWood();
    UFUNCTION() void DropStone();
    UFUNCTION() void DropMetal();
    UPROPERTY() TObjectPtr<UTextBlock> MaterialReadout;
    FString HUDHeading, HUDNotice, HUDBuildHint;
    FString HUDLootName,HUDLootQuantity,HUDLootDetail,HUDLootAction;
    FLinearColor HUDLootColor;
    uint8 HUDLootRarity=0;
    bool bHUDLootFocus=false,bHUDLootCanTake=false;
    TWeakObjectPtr<class APFWorldPickup> LastLootFocus;
    FString HUDMaterials[3];
    bool bHUDBuild=false,bHUDPickaxe=false;
    int32 HUDShape=0;
    FSlateBrush HUDWeaponIcon;
    float HUDHealthValue = 100, HUDArmorValue = 0, HUDArmorMax = 100;
    float HUDHungerValue = 100, HUDStaminaValue = 100;
    bool bHUDDowned = false, bHUDVehicle = false, bHUDReloading = false;
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
    void BuildExpeditionUI(UCanvasPanel *Canvas, UVerticalBox *Vitals);
    void UpdateExpeditionUI();
    UPROPERTY() TObjectPtr<UBorder> RoundPanel;
    UPROPERTY() TObjectPtr<UBorder> InventoryPanel;
    UPROPERTY() TObjectPtr<UPFTacticalMap> TacticalMap;
    UPROPERTY() TObjectPtr<UPFTacticalMap> Minimap;
    UPROPERTY() TObjectPtr<UTextBlock> RoundTitle;
    UPROPERTY() TObjectPtr<UTextBlock> RoundInfo;
    UPROPERTY() TObjectPtr<UTextBlock> Roster;
    UPROPERTY() TObjectPtr<UTextBlock> BidReadout;
    UPROPERTY() TObjectPtr<UTextBlock> RoundBanner;
    UPROPERTY() TObjectPtr<UTextBlock> PickupPrompt;
    UPROPERTY() TObjectPtr<UTextBlock> TeamText;
    UPROPERTY() TObjectPtr<UTextBlock> SurvivalText;
    UPROPERTY() TObjectPtr<UProgressBar> ArmorBar;
    UPROPERTY() TObjectPtr<UProgressBar> HungerBar;
    UPROPERTY() TObjectPtr<UProgressBar> StaminaBar;
    UPROPERTY() TObjectPtr<UVerticalBox> LobbyControls;
    UPROPERTY() TObjectPtr<UVerticalBox> BidControls;
    UPROPERTY() TObjectPtr<UVerticalBox> FinishedControls;
    UPROPERTY() TObjectPtr<USlider> BidSlider;
    UPROPERTY() TArray<TObjectPtr<UTextBlock>> SlotLabels;
    UPROPERTY() TArray<TObjectPtr<UPFItemButton>> SlotButtons;
    UPROPERTY() TArray<TObjectPtr<UImage>> SlotImages;
    UPROPERTY() TArray<TObjectPtr<UBorder>> SlotRarityBars;
    UPROPERTY() TArray<TObjectPtr<UTexture2D>> ItemIcons;
    int32 ChosenKit = 0, ChosenBid = 0;
    UFUNCTION() void HostSolo();
    UFUNCTION() void HostDuo();
    UFUNCTION() void HostSquad();
    UFUNCTION() void Ready();
    UFUNCTION() void StartRound();
    UFUNCTION() void ChangeMode();
    UFUNCTION() void ChangeTeam();
    UFUNCTION() void Essentials();
    UFUNCTION() void Marksman();
    UFUNCTION() void BidChanged(float Value);
    UFUNCTION() void ReturnLobby();
    UFUNCTION() void LeaveSession();
};
