#include "PFInventoryLayout.h"
#include "PFBuildPiece.h"
#include "PFConstructionComponent.h"
#include "Blueprint/WidgetTree.h"
#include "Components/Border.h"
#include "Components/HorizontalBox.h"
#include "Components/HorizontalBoxSlot.h"
#include "Components/CanvasPanel.h"
#include "Components/CanvasPanelSlot.h"
#include "Components/Image.h"
#include "Components/ProgressBar.h"
#include "Components/ScrollBox.h"
#include "Components/SizeBox.h"
#include "Components/Slider.h"
#include "Components/TextBlock.h"
#include "Components/UniformGridPanel.h"
#include "Components/UniformGridSlot.h"
#include "Components/VerticalBox.h"
#include "Components/VerticalBoxSlot.h"
#include "Engine/Texture2D.h"
#include "Kismet/GameplayStatics.h"
#include "PFCharacter.h"
#include "PFGameInstance.h"
#include "PFGameMode.h"
#include "PFInterface.h"
#include "PFHUDStyle.h"
#include "PFInventoryComponent.h"
#include "PFItemButton.h"
#include "PFMatchState.h"
#include "PFPlayerController.h"
#include "PFTacticalMap.h"
#include "PFVehicle.h"
#include "PFWorldPickup.h"
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
    HUDDisplayFont =
        LoadObject<UObject>(nullptr, TEXT("/Game/Pinefall/Art/UI/F_PinefallDisplay.F_PinefallDisplay"));
    HUDSmallFont = FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), 11);
    HUDLabelFont = FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), 18);
    HUDTitleFont = HUDDisplayFont ? FSlateFontInfo(HUDDisplayFont, 24)
                                  : FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), 22);
    HUDNumberFont = HUDDisplayFont ? FSlateFontInfo(HUDDisplayFont, 58)
                                   : FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), 50);
    HUDItems.SetNum(6);
    CombatLines.Reserve(128);
    auto Bar = [&](FLinearColor Color) {
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
    Place(Canvas, Minimap, FAnchors(0, 0), FVector2D(0, 0), FMargin(28, 28, 226, 226));
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
    Label(BidControls,
          TEXT("Pistol and one spare magazine. Spend up to ten points on landing priority."), 14, Muted);
    Button(BidControls, TEXT("Ridgeline marksman · 7 points"), TEXT("Marksman"));
    Label(BidControls,
          TEXT("Scoped rifle, pistol, three rifle magazines and a medkit. Up to three priority points "
               "remain."),
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
          TEXT("M opens the map. During landing, click anywhere safe to preview; click on your turn to "
               "claim "
               "it. Teammates may choose separate locations. Opposing claims need 60 m separation."),
          14, Muted);
    InventoryPanel = WidgetTree->ConstructWidget<UBorder>();
    InventoryPanel->SetBrushColor(FLinearColor(.018, .032, .055, .85));
    InventoryPanel->SetPadding(FMargin(22));
    Place(Canvas, InventoryPanel, FAnchors(1, 0, 1, 1), FVector2D::ZeroVector,
          FMargin(-662, 28, 620, 28));
    auto *Scroll = WidgetTree->ConstructWidget<UScrollBox>();
    InventoryPanel->SetContent(Scroll);
    auto *InventoryStack = WidgetTree->ConstructWidget<UVerticalBox>();
    Scroll->AddChild(InventoryStack);
    Label(InventoryStack, TEXT("FIELD INVENTORY"), 30, Paper);
    Label(
        InventoryStack,
        TEXT("Hold Alt or toggle Tab. Click to equip or use. Drop shares one item; Shift + drop shares the "
             "stack."),
        14, Muted);
    MaterialReadout=Label(InventoryStack,TEXT(""),16,Paper);
    auto *SupplyRow=WidgetTree->ConstructWidget<UHorizontalBox>();InventoryStack->AddChildToVerticalBox(SupplyRow);
    const TCHAR *DropNames[]={TEXT("Drop 50 wood"),TEXT("Drop 50 stone"),TEXT("Drop 50 metal")};
    const FName DropActions[]={TEXT("DropWood"),TEXT("DropStone"),TEXT("DropMetal")};
    for(int32 I=0;I<3;I++){auto *Column=WidgetTree->ConstructWidget<UVerticalBox>();auto *SupplySlot=SupplyRow->AddChildToHorizontalBox(Column);SupplySlot->SetSize(FSlateChildSize(ESlateSizeRule::Fill));SupplySlot->SetPadding(FMargin(2));Button(Column,DropNames[I],DropActions[I]);}
    auto *Grid = WidgetTree->ConstructWidget<UUniformGridPanel>();
    Grid->SetSlotPadding(FMargin(5));
    InventoryStack->AddChildToVerticalBox(Grid);
    for (const TCHAR *Name : {TEXT("T_RifleIcon"), TEXT("T_PistolIcon"), TEXT("T_Ammo"), TEXT("T_Food"),
                              TEXT("T_Medkit"), TEXT("T_Plate"), TEXT("T_Vest"), TEXT("T_RepairIcon")})
        ItemIcons.Add(LoadObject<UTexture2D>(
            nullptr, *FString::Printf(TEXT("/Game/Pinefall/Art/UI/%s.%s"), Name, Name)));
    for (int32 Index = 0; Index < 12; Index++)
    {
        auto *Cell = WidgetTree->ConstructWidget<UVerticalBox>();
        Grid->AddChildToUniformGrid(Cell, Index / 4, Index % 4);
        auto *Use = WidgetTree->ConstructWidget<UPFItemButton>();
        Use->Setup(GetOwningPlayer(), Index, false);
        Use->SetBackgroundColor(FLinearColor(.035, .065, .11, .94));
        auto *Tile = WidgetTree->ConstructWidget<UVerticalBox>();
        Use->SetContent(Tile);
        auto *Rarity = WidgetTree->ConstructWidget<UBorder>();
        Rarity->SetPadding(FMargin(0));
        auto *RaritySize = WidgetTree->ConstructWidget<USizeBox>();
        RaritySize->SetHeightOverride(3);
        RaritySize->SetContent(Rarity);
        Tile->AddChildToVerticalBox(RaritySize);
        SlotRarityBars.Add(Rarity);
        auto *ImageSize = WidgetTree->ConstructWidget<USizeBox>();
        ImageSize->SetHeightOverride(60);
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
        Fixed->SetHeightOverride(160);
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
    Visible(Minimap, !PC->bMenuOpen && !PC->bMapOpen && !Round);
    Visible(RoundPanel, Round);
    Visible(InventoryPanel, PC->bInventoryOpen && !PC->bMenuOpen);
    Visible(TacticalMap, !PC->bMenuOpen && (PC->bMapOpen || Match->Phase == EPFRoundPhase::Landing));
    Visible(WeaponText, false);
    Visible(TeamText, false);
    Visible(SessionText, PC->bMenuOpen);
    Visible(BrandLabel, PC->bMenuOpen);
    Visible(LocationLabel, PC->bMenuOpen);
    Visible(RoundBanner, false);
    Visible(LobbyControls,
            Match->Phase == EPFRoundPhase::Lobby || Match->Phase == EPFRoundPhase::Countdown);
    Visible(BidControls, Match->Phase == EPFRoundPhase::Bidding);
    Visible(FinishedControls, Match->Phase == EPFRoundPhase::Finished);
    FString Mode = Match->TeamSize == 1   ? TEXT("Solo")
                   : Match->TeamSize == 2 ? TEXT("Duos")
                                          : TEXT("Squads");
    FString Title = Match->Phase == EPFRoundPhase::Bidding    ? TEXT("Priority or supplies?")
                    : Match->Phase == EPFRoundPhase::Landing  ? TEXT("Choose your landing")
                    : Match->Phase == EPFRoundPhase::Finished ? TEXT("Expedition complete")
                                                              : TEXT("Expedition lobby");
    FString Clock = Match->Phase == EPFRoundPhase::Lobby ? TEXT("Waiting for everyone to ready up.")
                    : Match->Phase == EPFRoundPhase::Finished
                        ? TEXT("")
                        : FString::Printf(TEXT("%.0f seconds"), Match->SecondsLeft());
    RoundTitle->SetText(FText::FromString(Title));
    RoundInfo->SetText(
        FText::FromString(FString::Printf(TEXT("%s  ·  Team %d\n%s\n%s"), *Mode,
                                          Mine ? Mine->TeamId + 1 : 1, *Match->Announcement, *Clock)));
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
    BidReadout->SetText(FText::FromString(FString::Printf(TEXT("Bid %d  +  kit %d  =  %d / 10"),
                                                          ChosenBid, ChosenKit == 5 ? 7 : 0,
                                                          ChosenBid + (ChosenKit == 5 ? 7 : 0))));
    FString Banner;
    if (Match->Phase == EPFRoundPhase::Active)
    {
        int32 Left = FMath::CeilToInt(Match->SecondsLeft());
        Banner =
            FString::Printf(TEXT("%d survivors  ·  %d teams\nStorm %d %s  %02d:%02d"),
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
    ArmorBar->SetPercent(Player->Armor / (Player->ArmorTier == 1   ? 50
                                          : Player->ArmorTier == 2 ? 75
                                                                   : 100));
    HungerBar->SetPercent(Player->Hunger / 100);
    StaminaBar->SetPercent(Player->Stamina / 100);
    FString Survival =
        FString::Printf(TEXT("Hunger %.0f · Stamina %.0f"), Player->Hunger, Player->Stamina);
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
    else if(Player->FocusedBuild.IsValid())
    {int32 Column;if(PFBuildGrid::IsDoor(Player->FocusedBuild->Build.Mask,Column))Prompt=Player->FocusedBuild->Build.bDoorOpen?TEXT("E  CLOSE DOOR"):TEXT("E  OPEN DOOR");}
    else if (Player->FocusedPickup.IsValid())
        Prompt.Empty();
    if (Prompt.IsEmpty() && Player->bSliding)
        Prompt = TEXT("SLIDE · release Ctrl · Space jump");
    PickupPrompt->SetText(
        FText::FromString(PC->bMenuOpen || Round || PC->bInventoryOpen ? TEXT("") : Prompt));
    if(PC->bInventoryOpen&&MaterialReadout)MaterialReadout->SetText(FText::FromString(FString::Printf(TEXT("SUPPLIES   WOOD %d    STONE %d    METAL %d"),Player->Construction->Resources.Wood,Player->Construction->Resources.Stone,Player->Construction->Resources.Metal)));
    TArray<int32,TInlineAllocator<12>> OrderedSlots;
    if(PC->bInventoryOpen)PFInventoryLayout::DisplayOrder(Player->Inventory->Slots,OrderedSlots);
    if (PC->bInventoryOpen)
        for (int32 Index = 0; Index < 12; Index++)
        {
            int32 Actual=OrderedSlots.IsValidIndex(Index)?OrderedSlots[Index]:INDEX_NONE;
            bool Has=Player->Inventory->Slots.IsValidIndex(Actual)&&Player->Inventory->Slots[Actual].Count>0;
            FPFItemStack Item=Has?Player->Inventory->Slots[Actual]:FPFItemStack();
            int32 Icon = Item.Kind == EPFItemKind::Weapon ? (Item.WeaponId == TEXT("Ridgeline") ? 0 : 1)
                         : Item.Kind == EPFItemKind::Ammo ? 2
                         : Item.Kind == EPFItemKind::Food ? 3
                         : Item.Kind == EPFItemKind::Medkit     ? 4
                         : Item.Kind == EPFItemKind::ArmorPlate ? 5
                         : Item.Kind == EPFItemKind::ServiceKit ? 7 : 6;
            static const FLinearColor Tiers[] = {
                FLinearColor(.43, .49, .55), FLinearColor(.25, .72, .43), FLinearColor(.25, .55, .98),
                FLinearColor(.68, .39, .94), FLinearColor(.98, .66, .23)};
            SlotRarityBars[Index]->SetBrushColor(Has ? Tiers[FMath::Min<int32>(Item.Rarity, 4)]
                                                     : FLinearColor(.09, .14, .2));
            bool Equipped = Has && Item.Kind == EPFItemKind::Weapon && Player->WeaponId == Item.WeaponId;
            SlotButtons[Index * 2]->SetBackgroundColor(Equipped ? FLinearColor(.12, .25, .37, .96)
                                                                : FLinearColor(.035, .065, .11, .94));
            SlotImages[Index]->SetVisibility(Has ? ESlateVisibility::HitTestInvisible
                                                 : ESlateVisibility::Hidden);
            SlotImages[Index]->SetBrushFromTexture(ItemIcons[Icon], false);
            SlotLabels[Index]->SetText(FText::FromString(
                Has ? FString::Printf(
                          TEXT("%s\n%d%s"), *Item.Label(),
                          Item.Kind == EPFItemKind::Weapon
                              ? (Player->WeaponId == Item.WeaponId ? Player->Ammo : Item.Magazine)
                              : Item.Count,
                          Item.Kind == EPFItemKind::Weapon ? TEXT(" loaded") : TEXT(" carried"))
                    : TEXT("Empty\n—")));
            for (int32 ButtonIndex = Index * 2; ButtonIndex <= Index * 2 + 1; ButtonIndex++)
            {
                SlotButtons[ButtonIndex]->ShowSlot(Actual,Item.Revision);
                SlotButtons[ButtonIndex]->SetIsEnabled(
                    Has);
                if (ButtonIndex % 2 == 0)
                    if (auto *Action = Cast<UTextBlock>(SlotButtons[ButtonIndex]->GetContent()))
                        Action->SetText(FText::FromString(
                            Item.Kind == EPFItemKind::Ammo         ? TEXT("Reserve ammo")
                            : Item.Kind == EPFItemKind::Weapon     ? TEXT("Equip")
                            : Item.Kind == EPFItemKind::ServiceKit ? TEXT("Service nearby")
                                                                   : TEXT("Use")));
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
void UPFInterface::UpdateHUDData()
{
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    auto *Mine = PC ? PC->GetPlayerState<APFPlayerState>() : nullptr;
    auto *Player = PC ? Cast<APFCharacter>(PC->GetPawn()) : nullptr;
    if (!PC || !Match)
        return;
    HUDName = Mine ? Mine->GetPlayerName().Left(24) : TEXT("RANGER");
    FString CaptureView;if(FParse::Value(FCommandLine::Get(),TEXT("PFCombatView="),CaptureView))HUDName=TEXT("RANGER");
    HUDHealthValue = Player ? Player->Health : 0;
    HUDArmorValue = Player ? Player->Armor : 0;
    HUDArmorMax = Player && Player->ArmorTier == 1 ? 50 : Player && Player->ArmorTier == 2 ? 75 : 100;
    HUDHungerValue = Player ? Player->Hunger : 0;
    HUDStaminaValue = Player ? Player->Stamina : 0;
    HUDHealth = FString::FromInt(FMath::CeilToInt(HUDHealthValue));
    HUDArmor = FString::FromInt(FMath::CeilToInt(HUDArmorValue));
    HUDHunger = FString::FromInt(FMath::CeilToInt(HUDHungerValue));
    HUDStamina = FString::FromInt(FMath::CeilToInt(HUDStaminaValue));
    bHUDDowned = Player && Player->bDowned;
    bHUDVehicle = Player && Player->Vehicle;
    bHUDReloading = Player && Player->bReloading;
    auto *Focused=Player?Player->FocusedPickup.Get():nullptr;
    if(LastLootFocus.Get()!=Focused)
    {
        if(LastLootFocus.IsValid())LastLootFocus->SetFocused(false);LastLootFocus=Focused;if(Focused)Focused->SetFocused(true);
    }
    bHUDLootFocus=Focused&&!PC->bInventoryOpen&&!PC->bMenuOpen&&!PC->bMapOpen&&!PC->RoundPanelOpen();
    if(bHUDLootFocus)
    {
        static const FLinearColor Colors[]={FLinearColor(.46,.53,.6),FLinearColor(.2,.8,.35),FLinearColor(.15,.5,1),FLinearColor(.65,.2,1),FLinearColor(1,.57,.08)};
        static const TCHAR *Rarities[]={TEXT("COMMON"),TEXT("UNCOMMON"),TEXT("RARE"),TEXT("EPIC"),TEXT("LEGENDARY")};
        HUDLootRarity=FMath::Min<uint8>(Focused->Item.Rarity,4);HUDLootColor=Colors[HUDLootRarity];HUDLootName=Focused->Item.Label().ToUpper();HUDLootQuantity=FString::Printf(TEXT("×%d"),Focused->Item.Count);
        bHUDLootCanTake=Player->Inventory->CapacityFor(Focused->Item)>0;
        HUDLootAction=bHUDLootCanTake?TEXT("PICK UP"):Focused->Item.Kind==EPFItemKind::Weapon&&Player->Inventory->HasWeapon(Focused->Item.WeaponId)?TEXT("ALREADY CARRIED"):TEXT("NO SPACE");
        HUDLootDetail=FString(Rarities[HUDLootRarity])+TEXT("  ·  ")+(Focused->Item.Kind==EPFItemKind::Weapon?TEXT("WEAPON"):Focused->Item.Kind==EPFItemKind::Ammo?TEXT("AMMUNITION"):Focused->Item.Kind>=EPFItemKind::Wood?TEXT("BUILDING MATERIAL"):TEXT("SUPPLIES"));
    }
    bHUDBuild=Player&&Player->Construction->Mode==EPFConstructionMode::Build;
    bHUDPickaxe=Player&&Player->Construction->Mode==EPFConstructionMode::Pickaxe;
    if(Player)
    {
        HUDShape=int32(Player->Construction->Selected);HUDBuildHint=Player->Construction->Hint;
        for(int32 I=0;I<3;I++)HUDMaterials[I]=FString::FromInt(Player->Construction->Resources.Get(I));
    }
    HUDItems.SetNum(6);
    for (auto &Item : HUDItems)
        Item.bOccupied = false;
    if (Player && Player->Inventory)
    {
        HUDMagazine = FString::Printf(TEXT("%02d"), Player->Ammo);
        HUDReserve = FString::FromInt(Player->Inventory->Reserve(Player->WeaponId));
        HUDWeaponName = Player->Weapon.DisplayName.ToString().ToUpper();
        static const TCHAR *Classes[] = {
            TEXT("PISTOL"),       TEXT("SHOTGUN"),        TEXT("SUBMACHINE GUN"), TEXT("ASSAULT RIFLE"),
            TEXT("SNIPER RIFLE"), TEXT("MARKSMAN RIFLE"), TEXT("SUPPORT GUN"),    TEXT("LAUNCHER")};
        const int32 Category = int32(Player->Weapon.Category);
        HUDWeaponType =
            Category >= 0 && Category < UE_ARRAY_COUNT(Classes) ? Classes[Category] : TEXT("WEAPON");
        HUDWeaponIcon.SetResourceObject(
            ItemIcons.IsValidIndex(Player->WeaponId == TEXT("Ridgeline") ? 0 : 1)
                ? ItemIcons[Player->WeaponId == TEXT("Ridgeline") ? 0 : 1].Get()
                : nullptr);
        HUDNotice = Player->bReloading  ? TEXT("RELOADING")
                    : Player->Ammo == 0 ? TEXT("R  RELOAD")
                                        : TEXT("");
        if (Player->Vehicle)
        {
            HUDMagazine =
                FString::FromInt(FMath::RoundToInt(FMath::Abs(Player->Vehicle->Speed) * .036f));
            HUDReserve = TEXT("KM/H");
            HUDWeaponName = Player->Vehicle->Label().ToUpper();
            HUDWeaponType = FString::Printf(TEXT("FUEL %.0f%%   CONDITION %.0f%%"),
                                            Player->Vehicle->Fuel, Player->Vehicle->Health / 5);
        }
        static const FLinearColor Tiers[] = {FLinearColor(.43, .49, .55), FLinearColor(.25, .72, .43),
                                             FLinearColor(.25, .55, .98), FLinearColor(.68, .39, .94),
                                             FLinearColor(.98, .66, .23)};
        for(int32 Quick=0;Quick<PFInventoryLayout::QuickCount;Quick++)
        {
            auto &Item=HUDItems[Quick];Item.Key=PFInventoryLayout::Key(Quick);Item.Caption=PFInventoryLayout::Caption(Quick);Item.Count=TEXT("—");Item.bOccupied=false;Item.bEquipped=false;Item.Icon.SetResourceObject(nullptr);
            int32 Index=PFInventoryLayout::FindQuick(Player->Inventory->Slots,Quick);if(Index==INDEX_NONE)continue;
            const auto &Stack=Player->Inventory->Slots[Index];int32 Icon=Quick<2?Quick:Quick==2?3:Quick==3?4:Quick==4?5:7;
            Item.bOccupied=true;Item.bEquipped=Stack.Kind==EPFItemKind::Weapon&&Stack.WeaponId==Player->WeaponId&&Player->Construction->Mode==EPFConstructionMode::Combat;
            Item.Icon.SetResourceObject(ItemIcons.IsValidIndex(Icon)?ItemIcons[Icon].Get():nullptr);
            Item.Rarity=Tiers[FMath::Min<int32>(Stack.Rarity,4)];
            Item.Count=FString::FromInt(Stack.Kind==EPFItemKind::Weapon?(Item.bEquipped?Player->Ammo:Stack.Magazine):Stack.Count);
        }
    }
    else
        HUDWeaponName.Empty();
    int32 Left = FMath::Max(0, FMath::CeilToInt(Match->SecondsLeft()));
    HUDStormClock = Match->IsBattleRoyale() ? FString::Printf(TEXT("%02d:%02d"), Left / 60, Left % 60)
                                            : TEXT("SAFE");
    HUDStormLabel = Match->Phase == EPFRoundPhase::Active
                        ? FString::Printf(TEXT("STORM %d  %s"), Match->StormStage + 1,
                                          Match->bClosing ? TEXT("CLOSING") : TEXT("HOLDING"))
                    : Match->IsBattleRoyale() ? TEXT("EXPEDITION")
                                              : TEXT("FREE ROAM");
    HUDSurvivors = FString::FromInt(Match->Phase == EPFRoundPhase::Active ? Match->AlivePlayers
                                                                          : Match->PlayerArray.Num());
    HUDEliminations = FString::FromInt(Mine ? Mine->Eliminations : 0);
    HUDHeading = FString::Printf(
        TEXT("%03d"), FMath::RoundToInt(FMath::Fmod(PC->GetControlRotation().Yaw + 360, 360)) % 360);
    HUDAllies.Reset();
    if (Mine && Match->TeamSize > 1)
        for (APlayerState *Entry : Match->PlayerArray)
            if (auto *Ally = Cast<APFPlayerState>(Entry))
                if (Ally != Mine && Ally->TeamId == Mine->TeamId && HUDAllies.Num() < 3)
                {
                    auto &Row = HUDAllies.AddDefaulted_GetRef();
                    Row.Name = Ally->GetPlayerName().Left(24);
                    Row.Color = PFHUDStyle::TeamColor(Ally->GetPlayerId());
                    Row.bEliminated = Ally->bEliminated;
                    Row.Status = Row.bEliminated ? TEXT("ELIMINATED") : TEXT("CONNECTING");
                    // The owner-only feed works beyond pawn relevancy; no enemy information is
                    // requested.
                    for (const auto &Pin : PC->MapMarkers)
                        if (Pin.Kind == 0 && Pin.PlayerId == Ally->GetPlayerId())
                        {
                            Row.Health = Pin.Condition;
                            Row.Armor = Pin.Armor;
                            Row.ArmorMax = Pin.ArmorMax;
                            Row.bDowned = Pin.bDowned;
                            Row.Status =
                                Pin.bDowned ? TEXT("DOWNED")
                                : Player
                                    ? FString::Printf(
                                          TEXT("%.0f m"),
                                          FVector::Dist(Player->GetActorLocation(), Pin.Position) / 100)
                                    : TEXT("ALIVE");
                            break;
                        }
                }
}

void UPFInterface::DropWood(){if(auto *P=Cast<APFCharacter>(GetOwningPlayerPawn()))P->Construction->ServerDropMaterial(0,50,P->Construction->Resources.Revision);}
void UPFInterface::DropStone(){if(auto *P=Cast<APFCharacter>(GetOwningPlayerPawn()))P->Construction->ServerDropMaterial(1,50,P->Construction->Resources.Revision);}
void UPFInterface::DropMetal(){if(auto *P=Cast<APFCharacter>(GetOwningPlayerPawn()))P->Construction->ServerDropMaterial(2,50,P->Construction->Resources.Revision);}
