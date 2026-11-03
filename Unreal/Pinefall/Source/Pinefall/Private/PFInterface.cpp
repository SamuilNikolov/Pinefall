#include "PFInterface.h"
#include "PFConstructionComponent.h"
#include "PFBuildPiece.h"
#include "PFCombatHUD.h"
#include "PFPlayerController.h"
#include "PFCharacter.h"
#include "PFGameInstance.h"
#include "PFUserSettings.h"
#include "PFInventoryComponent.h"
#include "PFWildlife.h"
#include "PFVehicle.h"
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
#include "ProfilingDebugging/CpuProfilerTrace.h"

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
    Label->SetShadowColorAndOpacity(FLinearColor(0, 0, 0, .65f));
    Label->SetShadowOffset(FVector2D(1, 1));
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
    Text->SetAutoWrapText(false); // Short action labels fit the menu width; keep them on one line.
    if (Handler == TEXT("Quality"))
        QualityReadout = Text;
    if (Handler == TEXT("FrameLimit"))
        FrameLimitReadout = Text;
    if (Handler == TEXT("CrosshairColor"))
        CrosshairColorReadout = Text;
    Text->SetMargin(FMargin(14, 12));
    Button->SetContent(Text);
    FScriptDelegate Delegate;
    Delegate.BindUFunction(this, Handler);
    Button->OnClicked.Add(Delegate);
    Parent->AddChildToVerticalBox(Button)->SetPadding(FMargin(0, 0, 0, 10));
}
TSharedRef<SWidget> UPFInterface::RebuildWidget()
{
    SetIsFocusable(true);
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
    BrandLabel = PFStyle::Text(WidgetTree, TEXT("PINEFALL"), 38, PFStyle::Paper);
    PFStyle::Place(Canvas, BrandLabel, FAnchors(0, 0), FVector2D::ZeroVector, FMargin(28, 22, 300, 58));
    LocationLabel = PFStyle::Text(WidgetTree,
                                GetWorld()->GetMapName().Contains(TEXT("WildfrontReserve"))
                                    ? TEXT("Wildfront Reserve")
                                    : TEXT("Cedar Crossing / Practice"),
                                14, PFStyle::Muted);
    PFStyle::Place(Canvas, LocationLabel, FAnchors(0, 0), FVector2D::ZeroVector, FMargin(30, 76, 400, 35));
    VitalsPanel = WidgetTree->ConstructWidget<UBorder>();
    auto *Vitals = VitalsPanel.Get();
    Vitals->SetVisibility(ESlateVisibility::Collapsed);
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
    PFStyle::Place(Canvas, WeaponText, FAnchors(1, 1), FVector2D(1, 1), FMargin(-30, -112, 440, 64));
    SessionText = PFStyle::Text(WidgetTree, TEXT(""), 13, PFStyle::Muted);
    PFStyle::Place(Canvas, SessionText, FAnchors(0, 0), FVector2D::ZeroVector, FMargin(44, 137, 620, 70));
    CombatLines.Reserve(65);
    HitText = PFStyle::Text(WidgetTree, TEXT(""), 23, PFStyle::Paper);
    HitText->SetRenderOpacity(0);
    bHitTextVisible = false;
    DisplayedHitUntil = -1.f;
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
    MenuScroll = WidgetTree->ConstructWidget<UScrollBox>();
    Menu->SetContent(MenuScroll);
    auto *Pages=WidgetTree->ConstructWidget<UVerticalBox>();
    MenuScroll->AddChild(Pages);
    PlayMenuPage=WidgetTree->ConstructWidget<UVerticalBox>();
    SettingsPage=WidgetTree->ConstructWidget<UVerticalBox>();
    Pages->AddChildToVerticalBox(PlayMenuPage);
    Pages->AddChildToVerticalBox(SettingsPage);
    SettingsPage->SetVisibility(ESlateVisibility::Collapsed);
    auto *Stack = PlayMenuPage.Get();
    Label(Stack, TEXT("Enter the reserve"), 29, PFStyle::Paper);
    Label(Stack, TEXT("Explore the reserve, follow the roads, scavenge settlements and survive the storm."),
          15, PFStyle::Muted);
    Button(Stack, TEXT("Enter the world"), TEXT("Resume"), true);
    Button(Stack, TEXT("Settings"), TEXT("OpenSettings"));
    Button(Stack, TEXT("Host exploration · UDP 7777"), TEXT("Host"));
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
    Button(Stack, TEXT("Leave session"), TEXT("LeaveSession"));
    Button(Stack, TEXT("Quit"), TEXT("Quit"));
    MenuStatus=Label(Stack,TEXT("Esc opens the pause menu. Alt holds inventory; Tab toggles it."),14,PFStyle::Muted);

    Stack=SettingsPage.Get();
    Label(Stack,TEXT("Settings"),29,PFStyle::Paper);
    Button(Stack,TEXT("Back"),TEXT("BackToMenu"));
    auto *Settings=UPFUserSettings::Get();
    Label(Stack,TEXT("Display"),20,PFStyle::Paper);
    const int32 QualityLevel=Settings?Settings->GetOverallScalabilityLevel():2;
    const TCHAR *QualityName=Settings&&Settings->bBalancedLighting?TEXT("Balanced"):
        QualityLevel==1?TEXT("Medium"):QualityLevel==3?TEXT("Epic"):TEXT("High");
    Button(Stack,FString::Printf(TEXT("Graphics: %s  /  Change"),QualityName),TEXT("Quality"));
    const int32 Limit=Settings?FMath::RoundToInt(Settings->GetFrameRateLimit()):90;
    Button(Stack,Limit>0?FString::Printf(TEXT("Frame limit: %d FPS"),Limit):TEXT("Frame limit: Unlimited"),TEXT("FrameLimit"));
    Label(Stack,TEXT("Audio"),20,PFStyle::Paper);
    const float VolumeValue=Settings?Settings->MasterVolume:.8f;
    VolumeReadout=Label(Stack,FString::Printf(TEXT("Master volume: %.0f%%"),VolumeValue*100),16,PFStyle::Muted);
    VolumeSlider=WidgetTree->ConstructWidget<USlider>();
    VolumeSlider->SetValue(VolumeValue);
    VolumeSlider->OnValueChanged.AddDynamic(this,&UPFInterface::Volume);
    Stack->AddChildToVerticalBox(VolumeSlider)->SetPadding(FMargin(0,0,0,20));
    Label(Stack,TEXT("Controls"),20,PFStyle::Paper);
    const float MouseValue=Settings?Settings->MouseSensitivity:.12f;
    SensitivityReadout=Label(Stack,FString::Printf(TEXT("Mouse sensitivity: %.3f"),MouseValue),16,PFStyle::Muted);
    SensitivitySlider=WidgetTree->ConstructWidget<USlider>();
    SensitivitySlider->SetValue(FMath::GetMappedRangeValueClamped(FVector2D(.03,.4),FVector2D(0,1),MouseValue));
    SensitivitySlider->OnValueChanged.AddDynamic(this,&UPFInterface::Sensitivity);
    Stack->AddChildToVerticalBox(SensitivitySlider)->SetPadding(FMargin(0,0,0,20));
    // These controls use the existing scrollable settings page, never gameplay input.
    auto Slider = [&](float Value, FName Handler) {
        auto *Control = WidgetTree->ConstructWidget<USlider>();
        Control->SetValue(Value);
        FScriptDelegate Delegate;
        Delegate.BindUFunction(this, Handler);
        Control->OnValueChanged.Add(Delegate);
        Stack->AddChildToVerticalBox(Control)->SetPadding(FMargin(0,0,0,20));
    };
    const float AimValue = Settings ? Settings->AimSensitivityMultiplier : 1.f;
    AimSensitivityReadout = Label(Stack,FString::Printf(TEXT("Aim sensitivity: %.2fx"),AimValue),16,PFStyle::Muted);
    Slider(FMath::GetMappedRangeValueClamped(FVector2D(.25,2),FVector2D(0,1),AimValue),TEXT("AimSensitivity"));
    Label(Stack,TEXT("Camera and crosshair"),20,PFStyle::Paper);
    const float FOV = Settings ? Settings->GetHipFieldOfView() : 85.f;
    FieldOfViewReadout = Label(Stack,FString::Printf(TEXT("Field of view: %.0f degrees"),FOV),16,PFStyle::Muted);
    Slider((FOV-75.f)/35.f,TEXT("FieldOfView"));
    const float ReticleScale = Settings ? Settings->CrosshairScale : 1.f;
    CrosshairSizeReadout = Label(Stack,FString::Printf(TEXT("Crosshair size: %.0f%%"),ReticleScale*100),16,PFStyle::Muted);
    Slider(FMath::GetMappedRangeValueClamped(FVector2D(.75,2),FVector2D(0,1),ReticleScale),TEXT("CrosshairSize"));
    static const TCHAR *ReticleNames[] = {TEXT("White"),TEXT("Cyan"),TEXT("Lime"),TEXT("Pink")};
    Button(Stack,FString::Printf(TEXT("Crosshair color: %s"),ReticleNames[Settings?FMath::Clamp(Settings->CrosshairColor,0,3):0]),TEXT("CrosshairColor"));
    CrosshairColorReadout->SetColorAndOpacity(Settings?Settings->GetCrosshairColor():FLinearColor::White);
    Label(Stack,TEXT("Aim zoom stays weapon-specific. Crosshair changes are visual and do not change accuracy."),14,PFStyle::Muted);
    Label(Stack,TEXT("WASD move  |  Shift sprint  |  Ctrl crouch/slide\nSpace jump/hop  |  V camera\nLMB fire  |  RMB aim  |  R reload\n1 pickaxe  |  2 rifle  |  3 pistol\n4 food  |  5 medkit  |  6 armor  |  7 repair\nQ build  |  F1-F4 structures  |  F edit\nAlt hold inventory  |  Tab toggle inventory  |  M map\nB survey beacon"),14,PFStyle::Muted);
    SettingsStatus=Label(Stack,TEXT("Changes apply immediately and save when you go back or close the menu."),14,PFStyle::Muted);
    BuildExpeditionUI(Canvas, VitalsStack);
    return Super::RebuildWidget();
}
void UPFInterface::SetMenuVisible(bool Visible)
{
    if(!Visible)BackToMenu();
    if (Menu)
        Menu->SetVisibility(Visible ? ESlateVisibility::Visible : ESlateVisibility::Collapsed);
}
void UPFInterface::NativeTick(const FGeometry &Geometry, float Delta)
{
    TRACE_CPUPROFILER_EVENT_SCOPE(Pinefall_UIRefresh);
    Super::NativeTick(Geometry, Delta);
    auto *Character = Cast<APFCharacter>(GetOwningPlayerPawn());
    // Confirmation text must not wait behind the 10 Hz inventory/lobby refresh.
    // Format only on a new confirmed hit, not every frame or every UI refresh.
    const float Now = GetWorld()->GetTimeSeconds();
    if(Character && HitText)
    {
        if(Character->HitUntil != DisplayedHitUntil)
        {
            DisplayedHitUntil = Character->HitUntil;
            HitText->SetText(FText::FromString(Character->bLastHitKill ? TEXT("Eliminated")
                : FString::Printf(TEXT("%.0f%s"),Character->LastDamageAmount,Character->bLastHitArmor?TEXT(" armor"):TEXT(""))));
            HitText->SetColorAndOpacity(Character->bLastHitKill?PFStyle::Amber:Character->bLastHitArmor?PFStyle::Ice:PFStyle::Paper);
        }
    }
    const auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    const bool ShowHit = Character && Now < Character->HitUntil && PC && !PC->bMenuOpen && !PC->bMapOpen && !PC->bInventoryOpen;
    if(HitText && ShowHit != bHitTextVisible)
    {
        bHitTextVisible = ShowHit;
        HitText->SetRenderOpacity(ShowHit?1.f:0.f);
    }
    RefreshRemaining -= Delta;
    if (RefreshRemaining > 0)
        return;
    RefreshRemaining = .1f;
    UpdateExpeditionUI();
    UpdateHUDData();
    if (!Character)
        return;
    bShowTargetBar = false;
    if (auto *Target = Character->ConfirmedTarget.Get())
        if (Now < Character->TargetBarUntil)
        {
            float Fraction = 0;
            if (auto *Animal = Cast<APFWildlife>(Target))
                Fraction = Animal->Health / FMath::Max(1.f, Animal->MaxHealth);
            else if (auto *Build = Cast<APFBuildPiece>(Target))
                Fraction=Build->Health/Build->Maximum();
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
    if(Now<Character->Construction->HarvestBarUntil&&Character->Construction->HarvestBarFraction>0)
    {
        FVector2D Point;
        if(UWidgetLayoutLibrary::ProjectWorldLocationToWidgetPosition(GetOwningPlayer(),Character->Construction->HarvestBarPoint,Point,false))
        {bShowTargetBar=true;TargetBarPosition=Point;TargetBarFraction=Character->Construction->HarvestBarFraction;}
    }
    if (PC && PC->bMenuOpen)
    {
        int32 Players = GetWorld()->GetGameState() ? GetWorld()->GetGameState()->PlayerArray.Num() : 1;
        SessionText->SetText(
            FText::FromString(FString::Printf(TEXT("%s  /  %d connected"),
                                              GetWorld()->GetNetMode() == NM_Client         ? TEXT("Guest")
                                              : GetWorld()->GetNetMode() == NM_ListenServer ? TEXT("Hosting")
                                                                                            : TEXT("Practice"),
                                              Players)));
    }
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
        // Medium -> Balanced -> High -> Epic. Balanced preserves all High
        // geometry, textures, shadows and foliage, changing only GI/reflections.
        const int32 Current = Settings->GetOverallScalabilityLevel();
        const bool WasBalanced = Settings->bBalancedLighting;
        Settings->bBalancedLighting = !WasBalanced && (Current == 1 || Current < 0);
        const int32 Next = WasBalanced || Settings->bBalancedLighting ? 2 : Current == 2 ? 3 : 1;
        Settings->SetOverallScalabilityLevel(Next);
        Settings->ApplyNonResolutionSettings();
        Settings->SaveSettings();
        const TCHAR *Name = Settings->bBalancedLighting ? TEXT("Balanced") :
            Next == 1 ? TEXT("Medium") : Next == 2 ? TEXT("High") : TEXT("Epic");
        if (QualityReadout)
            QualityReadout->SetText(FText::FromString(FString::Printf(TEXT("Graphics: %s  /  Change"), Name)));
        if(SettingsStatus)SettingsStatus->SetText(FText::FromString(Settings->bBalancedLighting
            ? TEXT("Balanced keeps scenery detail with lighter lighting for smoother play.")
            : TEXT("Graphics preference saved.")));
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
        if(VolumeReadout)VolumeReadout->SetText(FText::FromString(FString::Printf(TEXT("Master volume: %.0f%%"),Value*100)));
    }
}
void UPFInterface::Sensitivity(float Value)
{
    if (auto *Settings = UPFUserSettings::Get())
    {
        Settings->MouseSensitivity = FMath::Lerp(.03f, .4f, Value);
        if(SensitivityReadout)SensitivityReadout->SetText(FText::FromString(FString::Printf(TEXT("Mouse sensitivity: %.3f"),Settings->MouseSensitivity)));
    }
}
void UPFInterface::FieldOfView(float Value)
{
    if(auto *Settings=UPFUserSettings::Get())
    {
        Settings->HipFieldOfView=FMath::Lerp(75.f,110.f,FMath::Clamp(Value,0.f,1.f));
        if(FieldOfViewReadout)FieldOfViewReadout->SetText(FText::FromString(FString::Printf(TEXT("Field of view: %.0f degrees"),Settings->HipFieldOfView)));
    }
}
void UPFInterface::AimSensitivity(float Value)
{
    if(auto *Settings=UPFUserSettings::Get())
    {
        Settings->AimSensitivityMultiplier=FMath::Lerp(.25f,2.f,FMath::Clamp(Value,0.f,1.f));
        if(AimSensitivityReadout)AimSensitivityReadout->SetText(FText::FromString(FString::Printf(TEXT("Aim sensitivity: %.2fx"),Settings->AimSensitivityMultiplier)));
    }
}
void UPFInterface::CrosshairSize(float Value)
{
    if(auto *Settings=UPFUserSettings::Get())
    {
        Settings->CrosshairScale=FMath::Lerp(.75f,2.f,FMath::Clamp(Value,0.f,1.f));
        if(CrosshairSizeReadout)CrosshairSizeReadout->SetText(FText::FromString(FString::Printf(TEXT("Crosshair size: %.0f%%"),Settings->CrosshairScale*100)));
    }
}
void UPFInterface::CrosshairColor()
{
    if(auto *Settings=UPFUserSettings::Get())
    {
        Settings->CrosshairColor=(FMath::Clamp(Settings->CrosshairColor,0,3)+1)%4;
        static const TCHAR *Names[]={TEXT("White"),TEXT("Cyan"),TEXT("Lime"),TEXT("Pink")};
        if(CrosshairColorReadout)
        {
            CrosshairColorReadout->SetText(FText::FromString(FString::Printf(TEXT("Crosshair color: %s"),Names[Settings->CrosshairColor])));
            CrosshairColorReadout->SetColorAndOpacity(Settings->GetCrosshairColor());
        }
    }
}

FReply UPFInterface::NativeOnKeyDown(const FGeometry &Geometry,const FKeyEvent &Event)
{
    if(auto *PC=Cast<APFPlayerController>(GetOwningPlayer()))
    {
        if(Event.GetKey()==EKeys::Tab&&!Event.IsRepeat()&&!PC->bMenuOpen&&!PC->RoundPanelOpen()){PC->ToggleInventory();return FReply::Handled();}
        if(Event.GetKey()==EKeys::Escape&&!Event.IsRepeat()){PC->ToggleMenu();return FReply::Handled();}
        if((Event.GetKey()==EKeys::LeftAlt||Event.GetKey()==EKeys::RightAlt)&&!Event.IsRepeat()){PC->InventoryPressed();return FReply::Handled();}
    }
    return Super::NativeOnKeyDown(Geometry,Event);
}
FReply UPFInterface::NativeOnKeyUp(const FGeometry &Geometry,const FKeyEvent &Event)
{
    if(Event.GetKey()==EKeys::LeftAlt||Event.GetKey()==EKeys::RightAlt)
        if(auto *PC=Cast<APFPlayerController>(GetOwningPlayer())){PC->AltInventoryReleased();return FReply::Handled();}
    return Super::NativeOnKeyUp(Geometry,Event);
}

void UPFInterface::OpenSettings()
{
    bSettingsOpen=true;
    if(PlayMenuPage)PlayMenuPage->SetVisibility(ESlateVisibility::Collapsed);
    if(SettingsPage)SettingsPage->SetVisibility(ESlateVisibility::Visible);
    if(MenuScroll)
    {
        FString View;
        if(FParse::Value(FCommandLine::Get(),TEXT("PFCombatView="),View)&&View==TEXT("SettingsControls"))
            MenuScroll->ScrollToEnd(); // Explicit presentation fixture for lower-page readability.
        else MenuScroll->ScrollToStart();
    }
}
void UPFInterface::BackToMenu()
{
    bSettingsOpen=false;
    if(SettingsPage)SettingsPage->SetVisibility(ESlateVisibility::Collapsed);
    if(PlayMenuPage)PlayMenuPage->SetVisibility(ESlateVisibility::Visible);
    if(MenuScroll)MenuScroll->ScrollToStart();
    if(auto *Settings=UPFUserSettings::Get())Settings->SaveSettings();
}
void UPFInterface::FrameLimit()
{
    if(auto *Settings=UPFUserSettings::Get())
    {
        const int32 Current=FMath::RoundToInt(Settings->GetFrameRateLimit());
        const int32 Next=Current==0?60:Current<60?60:Current<90?90:Current<120?120:Current<144?144:0;
        Settings->SetFrameRateLimit(Next);
        Settings->ApplyNonResolutionSettings();
        Settings->SaveSettings();
        if(FrameLimitReadout)FrameLimitReadout->SetText(FText::FromString(Next>0?FString::Printf(TEXT("Frame limit: %d FPS"),Next):TEXT("Frame limit: Unlimited")));
    }
}
