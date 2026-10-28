#include "PFInterface.h"
#include "PFCombatHUD.h"
#include "PFPlayerController.h"
#include "PFCharacter.h"
#include "PFGameInstance.h"
#include "PFUserSettings.h"
#include "PFInventoryComponent.h"
#include "PFWildlife.h"
#include "Camera/CameraComponent.h"
#include "Blueprint/WidgetLayoutLibrary.h"
#include "Blueprint/WidgetTree.h"
#include "Components/CanvasPanel.h"
#include "Components/CanvasPanelSlot.h"
#include "Components/Border.h"
#include "Components/VerticalBox.h"
#include "Components/VerticalBoxSlot.h"
#include "Components/TextBlock.h"
#include "Components/Button.h"
#include "Components/ProgressBar.h"
#include "Components/EditableTextBox.h"
#include "Components/Slider.h"
#include "Components/ScrollBox.h"
#include "GameFramework/GameStateBase.h"
#include "Kismet/KismetSystemLibrary.h"
#include "Styling/CoreStyle.h"

namespace PFStyle
{
const FLinearColor Ink(.025f, .04f, .045f, .93f), Paper(.89f, .92f, .89f, 1), Muted(.55f, .65f, .64f, 1),
    Ice(.34f, .69f, .85f, 1), Amber(.77f, .57f, .28f, 1);
void Place(UCanvasPanel *Canvas, UWidget *Widget, FAnchors Anchors, FVector2D Alignment, FMargin Offsets)
{
    auto *Slot = Canvas->AddChildToCanvas(Widget);
    Slot->SetAnchors(Anchors);
    Slot->SetAlignment(Alignment);
    Slot->SetOffsets(Offsets);
}
UTextBlock *Text(UWidgetTree *Tree, const FString &Value, int32 Size, FLinearColor Color)
{
    auto *Label = Tree->ConstructWidget<UTextBlock>();
    Label->SetText(FText::FromString(Value));
    Label->SetFont(FCoreStyle::GetDefaultFontStyle(Size >= 24 ? TEXT("Bold") : TEXT("Regular"), Size));
    Label->SetColorAndOpacity(Color);
    Label->SetAutoWrapText(true);
    return Label;
}
} // namespace PFStyle
UTextBlock *UPFInterface::Label(UVerticalBox *Parent, const FString &Value, int32 Size, FLinearColor Color)
{
    auto *Text = PFStyle::Text(WidgetTree, Value, Size, Color);
    auto *LayoutSlot = Parent->AddChildToVerticalBox(Text);
    LayoutSlot->SetPadding(FMargin(0, 0, 0, 12));
    return Text;
}
void UPFInterface::Button(UVerticalBox *Parent, const FString &Value, FName Handler, bool Primary)
{
    auto *Button = WidgetTree->ConstructWidget<UButton>();
    Button->SetBackgroundColor(Primary ? PFStyle::Amber : FLinearColor(.10f, .16f, .17f, 1));
    auto *Text = PFStyle::Text(WidgetTree, Value, 18, Primary ? PFStyle::Ink : PFStyle::Paper);
    Text->SetMargin(FMargin(14, 12));
    Button->SetContent(Text);
    FScriptDelegate Delegate;
    Delegate.BindUFunction(this, Handler);
    Button->OnClicked.Add(Delegate);
    Parent->AddChildToVerticalBox(Button)->SetPadding(FMargin(0, 0, 0, 10));
}
TSharedRef<SWidget> UPFInterface::RebuildWidget()
{
    if (!WidgetTree)
        WidgetTree = NewObject<UWidgetTree>(this);
    auto *Canvas = WidgetTree->ConstructWidget<UCanvasPanel>();
    WidgetTree->RootWidget = Canvas;
    auto *Combat = WidgetTree->ConstructWidget<UPFCombatHUD>();
    Combat->SetOwningPlayer(GetOwningPlayer());
    Combat->Interface = this;
    Combat->SetVisibility(ESlateVisibility::HitTestInvisible);
    auto *CombatSlot = Canvas->AddChildToCanvas(Combat);
    CombatSlot->SetAnchors(FAnchors(0, 0, 1, 1));
    CombatSlot->SetOffsets(FMargin(0));
    CombatSlot->SetZOrder(-100);
    auto *Brand = PFStyle::Text(WidgetTree, TEXT("PINEFALL"), 38, PFStyle::Paper);
    PFStyle::Place(Canvas, Brand, FAnchors(0, 0), FVector2D::ZeroVector, FMargin(42, 28, 420, 65));
    auto *Place = PFStyle::Text(WidgetTree, TEXT("Cedar Crossing  /  Field trial"), 14, PFStyle::Muted);
    PFStyle::Place(Canvas, Place, FAnchors(0, 0), FVector2D::ZeroVector, FMargin(44, 86, 520, 45));
    auto *Vitals = WidgetTree->ConstructWidget<UBorder>();
    Vitals->SetBrushColor(FLinearColor(.02f, .035f, .04f, .76f));
    Vitals->SetPadding(FMargin(18));
    auto *VitalsStack = WidgetTree->ConstructWidget<UVerticalBox>();
    Vitals->SetContent(VitalsStack);
    HealthText = Label(VitalsStack, TEXT("Health 100"), 20, PFStyle::Paper);
    HealthBar = WidgetTree->ConstructWidget<UProgressBar>();
    HealthBar->SetFillColorAndOpacity(FLinearColor(.36f, .66f, .54f, 1));
    HealthBar->SetPercent(1);
    VitalsStack->AddChildToVerticalBox(HealthBar);
    PFStyle::Place(Canvas, Vitals, FAnchors(0, 1), FVector2D(0, 1), FMargin(42, -34, 300, 180));
    WeaponText = PFStyle::Text(WidgetTree, TEXT("Ridgeline"), 20, PFStyle::Paper);
    WeaponText->SetJustification(ETextJustify::Right);
    PFStyle::Place(Canvas, WeaponText, FAnchors(1, 1), FVector2D(1, 1), FMargin(-42, -35, 460, 106));
    SessionText = PFStyle::Text(WidgetTree, TEXT(""), 13, PFStyle::Muted);
    PFStyle::Place(Canvas, SessionText, FAnchors(0, 0), FVector2D::ZeroVector, FMargin(44, 137, 620, 70));
    CombatLines.Reserve(65);
    HitText = PFStyle::Text(WidgetTree, TEXT(""), 23, PFStyle::Paper);
    HitText->SetJustification(ETextJustify::Center);
    PFStyle::Place(Canvas, HitText, FAnchors(.5f, .5f), FVector2D(.5, .5), FMargin(0, -55, 350, 46));
    DamageText = PFStyle::Text(WidgetTree, TEXT("Taking fire"), 20, FLinearColor(.9f, .35f, .24f, 1));
    DamageText->SetJustification(ETextJustify::Center);
    PFStyle::Place(Canvas, DamageText, FAnchors(.5f, .5f), FVector2D(.5, .5), FMargin(0, 70, 350, 42));
    DamageText->SetRenderOpacity(0);
    Menu = WidgetTree->ConstructWidget<UBorder>();
    Menu->SetBrushColor(PFStyle::Ink);
    Menu->SetPadding(FMargin(26));
    PFStyle::Place(Canvas, Menu, FAnchors(1, 0, 1, 1), FVector2D::ZeroVector, FMargin(-462, 28, 420, 28));
    auto *Scroll = WidgetTree->ConstructWidget<UScrollBox>();
    Menu->SetContent(Scroll);
    auto *Stack = WidgetTree->ConstructWidget<UVerticalBox>();
    Scroll->AddChild(Stack);
    Label(Stack, TEXT("Enter the reserve"), 29, PFStyle::Paper);
    Label(Stack, TEXT("Explore Cedar Crossing, test your aim, or invite friends to your range."), 15,
          PFStyle::Muted);
    Button(Stack, TEXT("Enter practice range"), TEXT("Resume"), true);
    Button(Stack, TEXT("Host range · UDP 7777"), TEXT("Host"));
    Button(Stack, TEXT("Host solo battle royale"), TEXT("HostSolo"));
    Button(Stack, TEXT("Host duos"), TEXT("HostDuo"));
    Button(Stack, TEXT("Host squads"), TEXT("HostSquad"));
    AddressBox = WidgetTree->ConstructWidget<UEditableTextBox>();
    AddressBox->WidgetStyle.SetFont(FCoreStyle::GetDefaultFontStyle(TEXT("Regular"), 18));
    AddressBox->SetForegroundColor(PFStyle::Ink);
    AddressBox->SetHintText(FText::FromString(TEXT("Friend's IPv4 address:port")));
    AddressBox->SetText(FText::FromString(TEXT("127.0.0.1:7777")));
    Stack->AddChildToVerticalBox(AddressBox)->SetPadding(FMargin(0, 2, 0, 10));
    Button(Stack, TEXT("Join by address"), TEXT("Join"));
    if (auto *Game = Cast<UPFGameInstance>(GetGameInstance()))
        Label(Stack, TEXT("LAN addresses: ") + Game->LocalAddress(), 13, PFStyle::Muted);
    Label(Stack,
          TEXT("Internet hosting requires a reachable UDP port. The LAN address shown below does not "
               "establish public reachability."),
          13, PFStyle::Muted);
    Label(Stack, TEXT("Sound"), 18, PFStyle::Paper);
    VolumeSlider = WidgetTree->ConstructWidget<USlider>();
    auto *Settings = UPFUserSettings::Get();
    VolumeSlider->SetValue(Settings ? Settings->MasterVolume : .8f);
    VolumeSlider->OnValueChanged.AddDynamic(this, &UPFInterface::Volume);
    Stack->AddChildToVerticalBox(VolumeSlider)->SetPadding(FMargin(0, 0, 0, 17));
    Label(Stack, TEXT("Mouse sensitivity"), 18, PFStyle::Paper);
    SensitivitySlider = WidgetTree->ConstructWidget<USlider>();
    SensitivitySlider->SetValue(Settings
                                    ? FMath::GetMappedRangeValueClamped(FVector2D(.03, .4), FVector2D(0, 1),
                                                                        Settings->MouseSensitivity)
                                    : .24f);
    SensitivitySlider->OnValueChanged.AddDynamic(this, &UPFInterface::Sensitivity);
    Stack->AddChildToVerticalBox(SensitivitySlider)->SetPadding(FMargin(0, 0, 0, 17));
    Button(Stack, TEXT("Change graphics quality"), TEXT("Quality"));
    Button(Stack, TEXT("Leave session"), TEXT("LeaveSession"));
    Button(Stack, TEXT("Quit"), TEXT("Quit"));
    MenuStatus = Label(Stack,
                       TEXT("WASD move · mouse look\nLMB fire · RMB aim · R reload\nShift sprint · Ctrl "
                            "crouch · Space jump\n1 rifle · 2 pistol · V camera · Esc menu"),
                       14, PFStyle::Muted);
    BuildExpeditionUI(Canvas, VitalsStack);
    return Super::RebuildWidget();
}
void UPFInterface::SetMenuVisible(bool Visible)
{
    if (Menu)
        Menu->SetVisibility(Visible ? ESlateVisibility::Visible : ESlateVisibility::Collapsed);
}
void UPFInterface::NativeTick(const FGeometry &Geometry, float Delta)
{
    Super::NativeTick(Geometry, Delta);
    RefreshRemaining -= Delta;
    if (RefreshRemaining > 0)
        return;
    RefreshRemaining = .05f;
    UpdateExpeditionUI();
    auto *Character = Cast<APFCharacter>(GetOwningPlayerPawn());
    if (!Character)
        return;
    float Now = GetWorld()->GetTimeSeconds();
    bShowTargetBar = false;
    if (auto *Target = Character->ConfirmedTarget.Get())
        if (Now < Character->TargetBarUntil)
        {
            float Fraction = 0;
            if (auto *Animal = Cast<APFWildlife>(Target))
                Fraction = Animal->Health / FMath::Max(1.f, Animal->MaxHealth);
            else if (auto *Opponent = Cast<APFCharacter>(Target))
                Fraction = Opponent->Health / 100.f;
            FHitResult Cover;
            FCollisionQueryParams Query(SCENE_QUERY_STAT(PFHealthBarCover), true, Character);
            Query.AddIgnoredActor(Target);
            FVector Center, Extent;
            Target->GetActorBounds(true, Center, Extent);
            if (Fraction > 0 &&
                !GetWorld()->LineTraceSingleByChannel(Cover, Character->Camera->GetComponentLocation(),
                                                      Center, ECC_Visibility, Query))
            {
                FVector2D Point;
                if (UWidgetLayoutLibrary::ProjectWorldLocationToWidgetPosition(
                        GetOwningPlayer(), Center + FVector(0, 0, Extent.Z + 25), Point, false))
                {
                    TargetBarPosition = Point;
                    TargetBarFraction = Fraction;
                    bShowTargetBar = true;
                }
            }
        }
    HealthBar->SetPercent(Character->Health / 100.f);
    HealthText->SetText(FText::FromString(
        FString::Printf(TEXT("Health %.0f   Armor %.0f"), Character->Health, Character->Armor)));
    WeaponText->SetText(FText::FromString(FString::Printf(
        TEXT("%s    %02d\n%s\n1 Rifle  2 Pistol  ·  R Reload  ·  V View"),
        *Character->Weapon.DisplayName.ToString(), Character->Ammo,
        Character->bReloading ? TEXT("Reloading…")
                              : *FString::Printf(TEXT("Reserve %d   ·   Alt inventory"),
                                                 Character->Inventory->Reserve(Character->WeaponId)))));
    HitText->SetRenderOpacity(Now < Character->HitUntil ? 1 : 0);
    HitText->SetText(FText::FromString(
        Character->bLastHitKill ? TEXT("Eliminated")
                                : FString::Printf(TEXT("%.0f%s"), Character->LastDamageAmount,
                                                  Character->bLastHitArmor ? TEXT(" armor") : TEXT(""))));
    HitText->SetColorAndOpacity(Character->bLastHitKill    ? PFStyle::Amber
                                : Character->bLastHitArmor ? PFStyle::Ice
                                                           : PFStyle::Paper);
    DamageText->SetRenderOpacity(FMath::Clamp((Character->DamageUntil - Now) / .6f, 0.f, 1.f));
    DamageText->SetText(FText::FromString(Character->bDamageWasEnvironmental ? TEXT("Storm damage")
                                          : Character->bDamageWasArmor       ? TEXT("Armor hit")
                                                                             : TEXT("Taking damage")));
    DamageText->SetColorAndOpacity(Character->bDamageWasArmor ? PFStyle::Ice
                                                              : FLinearColor(.95, .35, .25, 1));
    int32 Players = GetWorld()->GetGameState() ? GetWorld()->GetGameState()->PlayerArray.Num() : 1;
    SessionText->SetText(
        FText::FromString(FString::Printf(TEXT("%s  /  %d connected"),
                                          GetWorld()->GetNetMode() == NM_Client         ? TEXT("Guest")
                                          : GetWorld()->GetNetMode() == NM_ListenServer ? TEXT("Hosting")
                                                                                        : TEXT("Practice"),
                                          Players)));
}
void UPFInterface::Resume()
{
    if (auto *PC = Cast<APFPlayerController>(GetOwningPlayer()))
        PC->SetMenu(false);
    if (auto *Settings = UPFUserSettings::Get())
        Settings->SaveSettings();
}
void UPFInterface::Host()
{
    if (auto *Settings = UPFUserSettings::Get())
        Settings->SaveSettings();
    if (auto *Game = Cast<UPFGameInstance>(GetGameInstance()))
        Game->HostRange();
}
void UPFInterface::Join()
{
    if (auto *Game = Cast<UPFGameInstance>(GetGameInstance()))
    {
        Game->JoinRange(AddressBox->GetText().ToString());
        MenuStatus->SetText(FText::FromString(Game->ConnectionMessage));
    }
}
void UPFInterface::Quality()
{
    if (auto *Settings = UPFUserSettings::Get())
    {
        int32 Next = Settings->GetOverallScalabilityLevel() + 1;
        if (Next > 3 || Next < 1)
            Next = 1;
        Settings->SetOverallScalabilityLevel(Next);
        Settings->ApplySettings(false);
        MenuStatus->SetText(
            FText::FromString(FString::Printf(TEXT("Graphics: %s"), Next == 1   ? TEXT("Medium")
                                                                    : Next == 2 ? TEXT("High")
                                                                                : TEXT("Epic"))));
    }
}
void UPFInterface::Quit()
{
    if (auto *Settings = UPFUserSettings::Get())
        Settings->SaveSettings();
    UKismetSystemLibrary::QuitGame(this, GetOwningPlayer(), EQuitPreference::Quit, false);
}
void UPFInterface::Volume(float Value)
{
    if (auto *Settings = UPFUserSettings::Get())
    {
        Settings->MasterVolume = Value;
        Settings->ApplyNonResolutionSettings();
    }
}
void UPFInterface::Sensitivity(float Value)
{
    if (auto *Settings = UPFUserSettings::Get())
        Settings->MouseSensitivity = FMath::Lerp(.03f, .4f, Value);
}
