#include "PFInterface.h"
#include "PFCharacter.h"
#include "PFMatchState.h"
#include "PFGameMode.h"
#include "PFGameInstance.h"
#include "PFPlayerController.h"
#include "PFInventoryComponent.h"
#include "PFWorldPickup.h"
#include "PFVehicle.h"
#include "PFItemButton.h"
#include "PFTacticalMap.h"
#include "Blueprint/WidgetTree.h"
#include "Components/CanvasPanel.h"
#include "Components/CanvasPanelSlot.h"
#include "Components/Border.h"
#include "Components/VerticalBox.h"
#include "Components/VerticalBoxSlot.h"
#include "Components/UniformGridPanel.h"
#include "Components/UniformGridSlot.h"
#include "Components/ScrollBox.h"
#include "Components/TextBlock.h"
#include "Components/ProgressBar.h"
#include "Components/Slider.h"
#include "Components/Image.h"
#include "Components/SizeBox.h"
#include "Engine/Texture2D.h"
#include "Kismet/GameplayStatics.h"
#include "Styling/CoreStyle.h"
namespace PFExpeditionStyle
{
const FLinearColor Paper(.89, .92, .89, 1), Muted(.55, .65, .64, 1), Ink(.025, .04, .045, .95),
    Ice(.34, .69, .85, 1), Amber(.77, .57, .28, 1);
void Place(UCanvasPanel *Canvas, UWidget *Widget, FAnchors Anchors, FVector2D Alignment, FMargin Offsets)
{
    auto *Slot = Canvas->AddChildToCanvas(Widget);
    Slot->SetAnchors(Anchors);
    Slot->SetAlignment(Alignment);
    Slot->SetOffsets(Offsets);
}
void Visible(UWidget *Widget, bool Show)
{
    Widget->SetVisibility(Show ? ESlateVisibility::Visible : ESlateVisibility::Collapsed);
}
} // namespace PFExpeditionStyle
void UPFInterface::BuildExpeditionUI(UCanvasPanel *Canvas, UVerticalBox *Vitals)
{
    using namespace PFExpeditionStyle;
    auto Bar = [&](FLinearColor Color)
    {
        auto *Result = WidgetTree->ConstructWidget<UProgressBar>();
        auto Style = Result->GetWidgetStyle();
        Style.BackgroundImage.TintColor = FSlateColor(FLinearColor(.055, .08, .09, 1));
        Style.FillImage.TintColor = FSlateColor(FLinearColor::White);
        Result->SetWidgetStyle(Style);
        Result->SetFillColorAndOpacity(Color);
        Vitals->AddChildToVerticalBox(Result)->SetPadding(FMargin(0, 3));
        return Result;
    };
    auto HealthStyle = HealthBar->GetWidgetStyle();
    HealthStyle.BackgroundImage.TintColor = FSlateColor(FLinearColor(.055, .08, .09, 1));
    HealthBar->SetWidgetStyle(HealthStyle);
    ArmorBar = Bar(Ice);
    SurvivalText = Label(Vitals, TEXT("Hunger 85   Stamina 100"), 13, Muted);
    HungerBar = Bar(Amber);
    StaminaBar = Bar(Paper);
    RoundBanner = WidgetTree->ConstructWidget<UTextBlock>();
    RoundBanner->SetFont(FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), 20));
    RoundBanner->SetColorAndOpacity(Paper);
    RoundBanner->SetJustification(ETextJustify::Center);
    RoundBanner->SetAutoWrapText(true);
    Place(Canvas, RoundBanner, FAnchors(.5, 0), FVector2D(.5, 0), FMargin(0, 28, 640, 95));
    PickupPrompt = WidgetTree->ConstructWidget<UTextBlock>();
    PickupPrompt->SetFont(FCoreStyle::GetDefaultFontStyle(TEXT("Regular"), 17));
    PickupPrompt->SetColorAndOpacity(Paper);
    PickupPrompt->SetJustification(ETextJustify::Center);
    PickupPrompt->SetAutoWrapText(true);
    Place(Canvas, PickupPrompt, FAnchors(.5, .5), FVector2D(.5, .5), FMargin(0, 90, 450, 80));
    TeamText = WidgetTree->ConstructWidget<UTextBlock>();
    TeamText->SetFont(FCoreStyle::GetDefaultFontStyle(TEXT("Regular"), 14));
    TeamText->SetColorAndOpacity(Ice);
    TeamText->SetAutoWrapText(true);
    Place(Canvas, TeamText, FAnchors(0, 1), FVector2D(0, 1), FMargin(42, -230, 320, 110));
    Minimap = WidgetTree->ConstructWidget<UPFTacticalMap>();
    Minimap->bMini = true;
    Minimap->SetOwningPlayer(GetOwningPlayer());
    Place(Canvas, Minimap, FAnchors(1, 0), FVector2D(1, 0), FMargin(-28, 28, 226, 226));
    TacticalMap = WidgetTree->ConstructWidget<UPFTacticalMap>();
    TacticalMap->SetOwningPlayer(GetOwningPlayer());
    Place(Canvas, TacticalMap, FAnchors(0, .5), FVector2D(0, .5), FMargin(28, 0, 700, 590));
    Visible(TacticalMap, false);
    RoundPanel = WidgetTree->ConstructWidget<UBorder>();
    RoundPanel->SetBrushColor(Ink);
    RoundPanel->SetPadding(FMargin(24));
    Place(Canvas, RoundPanel, FAnchors(1, 0, 1, 1), FVector2D::ZeroVector, FMargin(-462, 28, 420, 28));
    auto *RoundScroll = WidgetTree->ConstructWidget<UScrollBox>();
    RoundPanel->SetContent(RoundScroll);
    auto *Stack = WidgetTree->ConstructWidget<UVerticalBox>();
    RoundScroll->AddChild(Stack);
    RoundTitle = Label(Stack, TEXT("Your expedition"), 29, Paper);
    RoundInfo = Label(Stack, TEXT(""), 15, Muted);
    Roster = Label(Stack, TEXT(""), 14, Paper);
    LobbyControls = WidgetTree->ConstructWidget<UVerticalBox>();
    Stack->AddChildToVerticalBox(LobbyControls);
    Button(LobbyControls, TEXT("Ready / unready"), TEXT("Ready"), true);
    Button(LobbyControls, TEXT("Start expedition"), TEXT("StartRound"));
    Button(LobbyControls, TEXT("Solo / duo / squad"), TEXT("ChangeMode"));
    Button(LobbyControls, TEXT("Choose next team"), TEXT("ChangeTeam"));
    BidControls = WidgetTree->ConstructWidget<UVerticalBox>();
    Stack->AddChildToVerticalBox(BidControls);
    Button(BidControls, TEXT("Bare essentials · 0 points"), TEXT("Essentials"));
    Label(BidControls, TEXT("Pistol and one spare magazine. Spend up to ten points on landing priority."), 14,
          Muted);
    Button(BidControls, TEXT("Ridgeline marksman · 7 points"), TEXT("Marksman"));
    Label(
        BidControls,
        TEXT("Scoped rifle, pistol, three rifle magazines and a medkit. Up to three priority points remain."),
        14, Muted);
    BidReadout = Label(BidControls, TEXT("Landing bid: 0"), 18, Amber);
    BidSlider = WidgetTree->ConstructWidget<USlider>();
    BidSlider->SetValue(0);
    BidSlider->OnValueChanged.AddDynamic(this, &UPFInterface::BidChanged);
    BidControls->AddChildToVerticalBox(BidSlider);
    FinishedControls = WidgetTree->ConstructWidget<UVerticalBox>();
    Stack->AddChildToVerticalBox(FinishedControls);
    Button(FinishedControls, TEXT("Return everyone to camp"), TEXT("ReturnLobby"), true);
    Label(Stack,
          TEXT("M opens the map. During landing, click anywhere safe to preview; click on your turn to claim "
               "it. Teammates may choose separate locations. Opposing claims need 60 m separation."),
          14, Muted);
    InventoryPanel = WidgetTree->ConstructWidget<UBorder>();
    InventoryPanel->SetBrushColor(Ink);
    InventoryPanel->SetPadding(FMargin(22));
    Place(Canvas, InventoryPanel, FAnchors(1, 0, 1, 1), FVector2D::ZeroVector, FMargin(-542, 28, 500, 28));
    auto *Scroll = WidgetTree->ConstructWidget<UScrollBox>();
    InventoryPanel->SetContent(Scroll);
    auto *InventoryStack = WidgetTree->ConstructWidget<UVerticalBox>();
    Scroll->AddChild(InventoryStack);
    Label(InventoryStack, TEXT("Field inventory"), 28, Paper);
    Label(InventoryStack,
          TEXT("Hold Alt to browse. Click to equip or use. Drop shares one item; Shift + drop shares the "
               "stack."),
          14, Muted);
    auto *Grid = WidgetTree->ConstructWidget<UUniformGridPanel>();
    Grid->SetSlotPadding(FMargin(5));
    InventoryStack->AddChildToVerticalBox(Grid);
    for (const TCHAR *Name : {TEXT("T_RifleIcon"), TEXT("T_PistolIcon"), TEXT("T_Ammo"), TEXT("T_Food"),
                              TEXT("T_Medkit"), TEXT("T_Plate"), TEXT("T_Vest")})
        ItemIcons.Add(LoadObject<UTexture2D>(
            nullptr, *FString::Printf(TEXT("/Game/Pinefall/Art/UI/%s.%s"), Name, Name)));
    for (int32 Index = 0; Index < 12; Index++)
    {
        auto *Cell = WidgetTree->ConstructWidget<UVerticalBox>();
        Grid->AddChildToUniformGrid(Cell, Index / 3, Index % 3);
        auto *Use = WidgetTree->ConstructWidget<UPFItemButton>();
        Use->Setup(GetOwningPlayer(), Index, false);
        Use->SetBackgroundColor(FLinearColor(.1, .16, .17, 1));
        auto *Tile = WidgetTree->ConstructWidget<UVerticalBox>();
        Use->SetContent(Tile);
        auto *ImageSize = WidgetTree->ConstructWidget<USizeBox>();
        ImageSize->SetHeightOverride(40);
        ImageSize->SetWidthOverride(110);
        auto *Image = WidgetTree->ConstructWidget<UImage>();
        ImageSize->SetContent(Image);
        Tile->AddChildToVerticalBox(ImageSize)->SetHorizontalAlignment(HAlign_Center);
        SlotImages.Add(Image);
        auto *Text = WidgetTree->ConstructWidget<UTextBlock>();
        Text->SetFont(FCoreStyle::GetDefaultFontStyle(TEXT("Regular"), 14));
        Text->SetColorAndOpacity(Paper);
        Text->SetAutoWrapText(true);
        Text->SetMargin(FMargin(8, 5));
        Text->SetMinDesiredWidth(100);
        Tile->AddChildToVerticalBox(Text);
        auto *Fixed = WidgetTree->ConstructWidget<USizeBox>();
        Fixed->SetMinDesiredHeight(132);
        Fixed->SetContent(Use);
        Cell->AddChildToVerticalBox(Fixed);
        SlotLabels.Add(Text);
        SlotButtons.Add(Use);
        auto *Drop = WidgetTree->ConstructWidget<UPFItemButton>();
        Drop->Setup(GetOwningPlayer(), Index, true);
        Drop->SetBackgroundColor(FLinearColor(.07, .1, .11, 1));
        auto *DropText = WidgetTree->ConstructWidget<UTextBlock>();
        DropText->SetText(FText::FromString(TEXT("Drop")));
        DropText->SetFont(FCoreStyle::GetDefaultFontStyle(TEXT("Regular"), 13));
        DropText->SetColorAndOpacity(Muted);
        DropText->SetMargin(FMargin(8, 5));
        Drop->SetContent(DropText);
        Cell->AddChildToVerticalBox(Drop);
        SlotButtons.Add(Drop);
    }
    Visible(RoundPanel, false);
    Visible(InventoryPanel, false);
}
void UPFInterface::UpdateExpeditionUI()
{
    using namespace PFExpeditionStyle;
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    if (!PC || !Match)
        return;
    auto *Mine = PC->GetPlayerState<APFPlayerState>();
    auto *Player = Cast<APFCharacter>(PC->GetPawn());
    bool Round = PC->RoundPanelOpen() && !PC->bMenuOpen;
    Visible(Minimap, !PC->bMenuOpen && !PC->bMapOpen && !PC->bInventoryOpen && !Round);
    Visible(RoundPanel, Round);
    Visible(InventoryPanel, PC->bInventoryOpen && !PC->bMenuOpen);
    Visible(TacticalMap, !PC->bMenuOpen && (PC->bMapOpen || Match->Phase == EPFRoundPhase::Landing));
    Visible(WeaponText, !PC->bMenuOpen && !PC->bInventoryOpen && !Round);
    Visible(LobbyControls, Match->Phase == EPFRoundPhase::Lobby || Match->Phase == EPFRoundPhase::Countdown);
    Visible(BidControls, Match->Phase == EPFRoundPhase::Bidding);
    Visible(FinishedControls, Match->Phase == EPFRoundPhase::Finished);
    FString Mode = Match->TeamSize == 1 ? TEXT("Solo") : Match->TeamSize == 2 ? TEXT("Duos") : TEXT("Squads");
    FString Title = Match->Phase == EPFRoundPhase::Bidding    ? TEXT("Priority or supplies?")
                    : Match->Phase == EPFRoundPhase::Landing  ? TEXT("Choose your landing")
                    : Match->Phase == EPFRoundPhase::Finished ? TEXT("Expedition complete")
                                                              : TEXT("Expedition lobby");
    FString Clock = Match->Phase == EPFRoundPhase::Lobby ? TEXT("Waiting for everyone to ready up.")
                    : Match->Phase == EPFRoundPhase::Finished
                        ? TEXT("")
                        : FString::Printf(TEXT("%.0f seconds"), Match->SecondsLeft());
    RoundTitle->SetText(FText::FromString(Title));
    RoundInfo->SetText(FText::FromString(FString::Printf(
        TEXT("%s  ·  Team %d\n%s\n%s"), *Mode, Mine ? Mine->TeamId + 1 : 1, *Match->Announcement, *Clock)));
    FString Players, Team;
    for (APlayerState *Entry : Match->PlayerArray)
        if (auto *State = Cast<APFPlayerState>(Entry))
        {
            Players += FString::Printf(TEXT("%s  ·  T%d  ·  %s\n"), *State->GetPlayerName().Left(24),
                                       State->TeamId + 1,
                                       State->bEliminated      ? TEXT("Spectating")
                                       : State->bLandingLocked ? TEXT("Landing locked")
                                       : State->bReady         ? TEXT("Ready")
                                                               : TEXT("Not ready"));
            if (Mine && State != Mine && Match->TeamSize > 1 && State->TeamId == Mine->TeamId)
            {
                auto *Pawn = Cast<APFCharacter>(State->GetPawn());
                Team += FString::Printf(
                    TEXT("%s  %s\n"), *State->GetPlayerName().Left(20),
                    State->bEliminated      ? TEXT("Eliminated")
                    : Pawn && Pawn->bDowned ? TEXT("Downed")
                    : Pawn ? *FString::Printf(TEXT("%.0f HP / %.0f armor"), Pawn->Health, Pawn->Armor)
                           : TEXT("Joining"));
            }
        }
    Roster->SetText(FText::FromString(Players));
    TeamText->SetText(FText::FromString(Team));
    BidReadout->SetText(
        FText::FromString(FString::Printf(TEXT("Bid %d  +  kit %d  =  %d / 10"), ChosenBid,
                                          ChosenKit == 5 ? 7 : 0, ChosenBid + (ChosenKit == 5 ? 7 : 0))));
    FString Banner;
    if (Match->Phase == EPFRoundPhase::Active)
    {
        int32 Left = FMath::CeilToInt(Match->SecondsLeft());
        Banner = FString::Printf(TEXT("%d survivors  ·  %d teams\nStorm %d %s  %02d:%02d"),
                                 Match->AlivePlayers, Match->AliveTeams, Match->StormStage + 1,
                                 Match->bClosing ? TEXT("closing") : TEXT("waiting"), Left / 60, Left % 60);
    }
    else if (Match->IsBattleRoyale())
        Banner = Match->Announcement;
    RoundBanner->SetText(FText::FromString(Banner));
    if (!Player)
    {
        PickupPrompt->SetText(FText::FromString(TEXT("] Next spectator target")));
        return;
    }
    ArmorBar->SetPercent(Player->Armor / (Player->ArmorTier == 1 ? 50 : Player->ArmorTier == 2 ? 75 : 100));
    HungerBar->SetPercent(Player->Hunger / 100);
    StaminaBar->SetPercent(Player->Stamina / 100);
    FString Survival = FString::Printf(TEXT("Hunger %.0f · Stamina %.0f"), Player->Hunger, Player->Stamina);
    if (Player->Oxygen < 100)
        Survival += FString::Printf(TEXT("\nAir %.0f · Space rise / Ctrl dive"), Player->Oxygen);
    SurvivalText->SetText(FText::FromString(Survival));
    FString Prompt =
        Player->Health <= 0 ? TEXT("] Next spectator target")
        : Player->bDowned
            ? FString::Printf(TEXT("Downed · %.0f seconds to revive\nRevive %.0f%%"),
                              FMath::Max(0., Player->BleedOutAt - Match->GetServerWorldTimeSeconds()),
                              Player->ReviveProgress * 100)
            : TEXT("");
    if (!Player->bDowned)
        for (APlayerState *Entry : Match->PlayerArray)
            if (auto *Ally = Cast<APFPlayerState>(Entry))
                if (Mine && Ally->TeamId == Mine->TeamId)
                    if (auto *Pawn = Cast<APFCharacter>(Ally->GetPawn()))
                        if (Pawn->bDowned && FVector::DistSquared(Pawn->GetActorLocation(),
                                                                  Player->GetActorLocation()) < 102400)
                        {
                            Prompt = TEXT("Hold E to revive teammate");
                            break;
                        }
    if (Player->Vehicle)
        Prompt = TEXT("E Exit · WASD Drive · Shift Boost · Ctrl Brake · Mouse Look");
    else if (auto *Transport = Player->FocusedVehicle.Get())
        Prompt = FString::Printf(TEXT("E Enter %s · R Service with kit\nCondition %.0f%% · Fuel %.0f%%"),
                                 *Transport->Label(), Transport->Health / 5, Transport->Fuel);
    else if (auto *Pickup = Player->FocusedPickup.Get())
        Prompt = FString::Printf(TEXT("E  %s  ×%d"), *Pickup->Item.Label(), Pickup->Item.Count);
    PickupPrompt->SetText(
        FText::FromString(PC->bMenuOpen || Round || PC->bInventoryOpen ? TEXT("") : Prompt));
    if (PC->bInventoryOpen)
        for (int32 Index = 0; Index < 12; Index++)
        {
            bool Has =
                Player->Inventory->Slots.IsValidIndex(Index) && Player->Inventory->Slots[Index].Count > 0;
            FPFItemStack Item = Has ? Player->Inventory->Slots[Index] : FPFItemStack();
            int32 Icon = Item.Kind == EPFItemKind::Weapon       ? (Item.WeaponId == TEXT("Ridgeline") ? 0 : 1)
                         : Item.Kind == EPFItemKind::Ammo       ? 2
                         : Item.Kind == EPFItemKind::Food       ? 3
                         : Item.Kind == EPFItemKind::Medkit     ? 4
                         : Item.Kind == EPFItemKind::ArmorPlate ? 5
                                                                : 6;
            SlotImages[Index]->SetVisibility(Has ? ESlateVisibility::HitTestInvisible
                                                 : ESlateVisibility::Hidden);
            SlotImages[Index]->SetBrushFromTexture(ItemIcons[Icon], false);
            SlotLabels[Index]->SetText(FText::FromString(
                Has ? FString::Printf(TEXT("%s\n%d%s"), *Item.Label(),
                                      Item.Kind == EPFItemKind::Weapon
                                          ? (Player->WeaponId == Item.WeaponId ? Player->Ammo : Item.Magazine)
                                          : Item.Count,
                                      Item.Kind == EPFItemKind::Weapon ? TEXT(" loaded") : TEXT(" carried"))
                    : TEXT("Empty\n—")));
            for (int32 ButtonIndex = Index * 2; ButtonIndex <= Index * 2 + 1; ButtonIndex++)
            {
                SlotButtons[ButtonIndex]->Revision = Item.Revision;
                SlotButtons[ButtonIndex]->SetIsEnabled(Has&&(ButtonIndex%2!=0||Item.Kind!=EPFItemKind::Ammo));
                if(ButtonIndex%2==0)if(auto* Action=Cast<UTextBlock>(SlotButtons[ButtonIndex]->GetContent()))Action->SetText(FText::FromString(Item.Kind==EPFItemKind::Ammo?TEXT("Reserve ammo"):Item.Kind==EPFItemKind::Weapon?TEXT("Equip"):Item.Kind==EPFItemKind::ServiceKit?TEXT("Service nearby"):TEXT("Use")));
            }
        }
}
void UPFInterface::HostSolo()
{
    if (auto *Game = Cast<UPFGameInstance>(GetGameInstance()))
        Game->HostBattleRoyale(1);
}
void UPFInterface::HostDuo()
{
    if (auto *Game = Cast<UPFGameInstance>(GetGameInstance()))
        Game->HostBattleRoyale(2);
}
void UPFInterface::HostSquad()
{
    if (auto *Game = Cast<UPFGameInstance>(GetGameInstance()))
        Game->HostBattleRoyale(4);
}
void UPFInterface::Ready()
{
    if (auto *PC = Cast<APFPlayerController>(GetOwningPlayer()))
        PC->ServerReady();
}
void UPFInterface::StartRound()
{
    if (auto *PC = Cast<APFPlayerController>(GetOwningPlayer()))
        PC->ServerStartRound();
}
void UPFInterface::ChangeMode()
{
    if (auto *PC = Cast<APFPlayerController>(GetOwningPlayer()))
        if (auto *Match = GetWorld()->GetGameState<APFMatchState>())
            PC->ServerTeamSize(Match->TeamSize == 1 ? 2 : Match->TeamSize == 2 ? 4 : 1);
}
void UPFInterface::ChangeTeam()
{
    if (auto *PC = Cast<APFPlayerController>(GetOwningPlayer()))
        if (auto *State = PC->GetPlayerState<APFPlayerState>())
            PC->ServerTeam((State->TeamId + 1) % 20);
}
void UPFInterface::Essentials()
{
    ChosenKit = 0;
    BidChanged(BidSlider->GetValue());
}
void UPFInterface::Marksman()
{
    ChosenKit = 5;
    BidChanged(BidSlider->GetValue());
}
void UPFInterface::BidChanged(float Value)
{
    ChosenBid = FMath::RoundToInt(Value * (ChosenKit == 5 ? 3 : 10));
    if (auto *PC = Cast<APFPlayerController>(GetOwningPlayer()))
        PC->ServerBid(ChosenBid, ChosenKit);
}
void UPFInterface::ReturnLobby()
{
    if (auto *PC = Cast<APFPlayerController>(GetOwningPlayer()))
        PC->ServerReturnLobby();
}
void UPFInterface::LeaveSession()
{
    UGameplayStatics::OpenLevel(this, TEXT("/Game/Pinefall/Maps/WildfrontReserve"), true);
}
