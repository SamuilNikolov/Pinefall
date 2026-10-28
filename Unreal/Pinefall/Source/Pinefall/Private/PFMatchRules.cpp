#include "PFGameMode.h"
#include "PFCharacter.h"
#include "PFPlayerController.h"
#include "PFMatchState.h"
#include "PFInventoryComponent.h"
#include "PFWorldPickup.h"
#include "PFWildlife.h"
#include "Kismet/GameplayStatics.h"
#include "EngineUtils.h"
#include "TimerManager.h"
namespace PFRules
{
const float Waits[] = {90, 60, 55, 45, 40, 30, 20};
const float Closings[] = {100, 140, 125, 110, 85, 60, 45};
const float Radii[] = {3040, 2200, 1520, 938, 500, 200, 0};
} // namespace PFRules
APFMatchState *APFGameMode::Match() const
{
    return GetGameState<APFMatchState>();
}
void APFGameMode::BeginPlay()
{
    Super::BeginPlay();
    if (GetWorld()->URL.HasOption(TEXT("br")))
    {
        auto *State = Match();
        State->Phase = EPFRoundPhase::Lobby;
        State->Announcement = TEXT("Ready up, then choose your landing and loadout.");
        int32 Team = FCString::Atoi(*UGameplayStatics::ParseOption(OptionsString, TEXT("team")));
        State->TeamSize = Team == 2 || Team == 4 ? Team : 1;
    }
    GetWorld()->SpawnActor<APFWildlifeDirector>();
    GetWorldTimerManager().SetTimer(RoundTimer, this, &APFGameMode::RoundSecond, 1, true);
    if (FParse::Param(FCommandLine::Get(), TEXT("PFRoundProbe")))
        GetWorldTimerManager().SetTimer(ProbeTimer, this, &APFGameMode::RunRoundProbe, .5, true);
    else if (FParse::Param(FCommandLine::Get(), TEXT("PFWildlifeProbe")))
        GetWorldTimerManager().SetTimer(ProbeTimer, this, &APFGameMode::RunWildlifeProbe, .5, true);
}
void APFGameMode::HandleStartingNewPlayer_Implementation(APlayerController *Player)
{
    auto *State = Player->GetPlayerState<APFPlayerState>();
    if (Match()->IsBattleRoyale() && State->bEliminated)
    {
        Player->StartSpectatingOnly();
        if (auto *PC = Cast<APFPlayerController>(Player))
            PC->ServerCycleSpectator();
        return;
    }
    Super::HandleStartingNewPlayer_Implementation(Player);
}
void APFGameMode::Logout(AController *Player)
{
    if (auto *State = Player->GetPlayerState<APFPlayerState>())
    {
        State->bEliminated = true;
        State->bInRound = false;
    }
    bool WasHost = Host.Get() == Player;
    Super::Logout(Player);
    if (WasHost)
    {
        Host.Reset();
        for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
            if (It->IsValid() && It->Get() != Player)
            {
                Host = It->Get();
                Match()->HostPlayerId = Host->PlayerState->GetPlayerId();
                break;
            }
    }
    UpdateAlive();
}
bool APFGameMode::AllReady() const
{
    if (Match()->PlayerArray.IsEmpty())
        return false;
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *Player = Cast<APFPlayerState>(Entry))
            if (!Player->bReady || Player->bEliminated)
                return false;
    return true;
}
void APFGameMode::SetReady(APlayerController *Player)
{
    if (Match()->Phase != EPFRoundPhase::Lobby && Match()->Phase != EPFRoundPhase::Countdown)
        return;
    if (auto *State = Player->GetPlayerState<APFPlayerState>())
        State->bReady = !State->bReady;
}
void APFGameMode::SetTeamSize(APlayerController *Player, int32 Size)
{
    if (Player != Host.Get() || Match()->Phase != EPFRoundPhase::Lobby ||
        (Size != 1 && Size != 2 && Size != 4))
        return;
    Match()->TeamSize = Size;
    int32 Index = 0;
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *State = Cast<APFPlayerState>(Entry))
        {
            State->TeamId = Index++ / Size;
            State->bReady = false;
        }
}
void APFGameMode::ChooseTeam(APlayerController *Player, int32 Team)
{
    if (Match()->Phase != EPFRoundPhase::Lobby || Team < 0 || Team >= 20)
        return;
    auto *State = Player->GetPlayerState<APFPlayerState>();
    if (!State)
        return;
    int32 Count = 0;
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *Other = Cast<APFPlayerState>(Entry))
            if (Other != State && Other->TeamId == Team)
                Count++;
    if (Count >= Match()->TeamSize)
        return;
    State->TeamId = Team;
    State->bReady = false;
}
void APFGameMode::RequestStart(APlayerController *Player)
{
    if (Player != Host.Get() || Match()->Phase != EPFRoundPhase::Lobby || !AllReady())
        return;
    Match()->Phase = EPFRoundPhase::Countdown;
    Match()->PhaseEnds = GetWorld()->GetTimeSeconds() + 5;
    Match()->Announcement = TEXT("Expedition starts in five seconds.");
}
void APFGameMode::SubmitBid(APlayerController *Player, int32 Bid, int32 Kit)
{
    UE_LOG(LogTemp, Display, TEXT("PF_BID request=%d kit=%d phase=%d player=%s"), Bid, Kit,
           int32(Match()->Phase), *GetNameSafe(Player));
    if (Match()->Phase != EPFRoundPhase::Bidding || (Kit != 0 && Kit != 5) || Bid < 0 ||
        Bid + (Kit == 5 ? 7 : 0) > 10)
        return;
    if (auto *State = Player->GetPlayerState<APFPlayerState>())
        if (State->bInRound)
        {
            State->Bid = Bid;
            State->Kit = Kit;
            UE_LOG(LogTemp, Display, TEXT("PF_BID accepted=%d kit=%d"), Bid, Kit);
        }
}
void APFGameMode::BeginBidding()
{
    auto *Round = Match();
    Round->Phase = EPFRoundPhase::Bidding;
    Round->PhaseEnds = GetWorld()->GetTimeSeconds() + 25;
    Round->WinningTeam = -1;
    Round->Announcement = TEXT("Ten points. Spend on your loadout or bid for an earlier landing choice.");
    for (APlayerState *Entry : Round->PlayerArray)
        if (auto *State = Cast<APFPlayerState>(Entry))
        {
            State->bInRound = true;
            State->bEliminated = false;
            State->bLandingLocked = false;
            State->Bid = 0;
            State->Kit = 0;
            State->Eliminations = 0;
            State->Landing = FVector::ZeroVector;
        }
    UE_LOG(LogTemp, Display, TEXT("PF_ROUND bidding"));
}
void APFGameMode::BeginLanding()
{
    DraftOrder.Empty();
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *State = Cast<APFPlayerState>(Entry))
            if (State->bInRound)
                DraftOrder.Add(State);
    DraftOrder.Sort([](const TWeakObjectPtr<APFPlayerState> &A, const TWeakObjectPtr<APFPlayerState> &B)
                    { return A->Bid == B->Bid ? A->GetPlayerId() < B->GetPlayerId() : A->Bid > B->Bid; });
    Match()->Phase = EPFRoundPhase::Landing;
    TurnIndex = -1;
    AdvanceTurn();
}
void APFGameMode::AdvanceTurn()
{
    while (++TurnIndex < DraftOrder.Num())
        if (DraftOrder[TurnIndex].IsValid() && DraftOrder[TurnIndex]->bInRound &&
            !DraftOrder[TurnIndex]->bEliminated)
        {
            Match()->TurnPlayerId = DraftOrder[TurnIndex]->GetPlayerId();
            Match()->PhaseEnds = GetWorld()->GetTimeSeconds() + 18;
            Match()->Announcement = DraftOrder[TurnIndex]->GetPlayerName() + TEXT(" is choosing a landing.");
            return;
        }
    StartBattle();
}
bool APFGameMode::GroundPoint(FVector &Point) const
{
    if (Point.ContainsNaN() || FMath::Abs(Point.X) > Match()->ArenaHalfExtent - 400 ||
        FMath::Abs(Point.Y) > Match()->ArenaHalfExtent - 400)
        return false;
    TArray<FHitResult> Hits;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFLandingGround), false);
    GetWorld()->LineTraceMultiByObjectType(Hits, FVector(Point.X, Point.Y, 100000),
                                           FVector(Point.X, Point.Y, -100000),
                                           FCollisionObjectQueryParams(ECC_WorldStatic), Query);
    for (const auto &Hit : Hits)
        if (Hit.GetActor() && Hit.GetActor()->GetClass()->GetName().Contains(TEXT("Landscape")) &&
            Hit.ImpactNormal.Z > .55f)
        {
            Point = Hit.ImpactPoint + FVector(0, 0, 100);
            float Lake = FMath::Square((Point.X - 9000) / 4200) + FMath::Square((Point.Y - 9500) / 6000);
            return !(Lake < 1.1f && Hit.ImpactPoint.Z < -120);
        }
    return false;
}
bool APFGameMode::SelectLanding(APlayerController *Player, FVector Point, bool Confirm)
{
    if (Match()->Phase != EPFRoundPhase::Landing)
        return false;
    auto *State = Player->GetPlayerState<APFPlayerState>();
    if (!State || !State->bInRound || State->bLandingLocked || !GroundPoint(Point))
        return false;
    if (Confirm)
    {
        if (State->GetPlayerId() != Match()->TurnPlayerId)
            return false;
        for (APlayerState *Entry : Match()->PlayerArray)
            if (auto *Other = Cast<APFPlayerState>(Entry))
                if (Other != State && Other->bLandingLocked && Other->TeamId != State->TeamId &&
                    FVector::DistSquared2D(Other->Landing, Point) < 36000000)
                    return false;
    }
    State->Landing = Point;
    if (Confirm)
    {
        State->bLandingLocked = true;
        AdvanceTurn();
    }
    return true;
}
void APFGameMode::StartBattle()
{
    auto *Round = Match();
    Round->Phase = EPFRoundPhase::Active;
    Round->TurnPlayerId = -1;
    Round->StormStage = 0;
    Round->bClosing = false;
    Round->RadiusFrom = Round->RadiusTo = Round->ArenaHalfExtent * 2 * .704f;
    Round->CenterFrom = Round->CenterTo = FVector::ZeroVector;
    Round->PhaseEnds = GetWorld()->GetTimeSeconds() + PFRules::Waits[0];
    Round->StormEnds = Round->PhaseEnds;
    StartingTeams = 0;
    Round->Announcement = TEXT("The reserve is live. Scavenge, stay fed, and watch the circle.");
    for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
        if (auto *PC = Cast<APFPlayerController>(It->Get()))
        {
            auto *State = PC->GetPlayerState<APFPlayerState>();
            if (!State || !State->bInRound)
                continue;
            if (PC->GetPawn())
                PC->GetPawn()->Destroy();
            RestartPlayerAtTransform(PC, FTransform(FRotator::ZeroRotator, State->Landing));
            if (auto *Character = Cast<APFCharacter>(PC->GetPawn()))
                Character->ResetForRound(State->Kit);
            else
                State->bEliminated = true;
            PC->ClientResumeRound();
        }
    SpawnFieldLoot();
    UpdateAlive();
    StartingTeams = Round->AliveTeams;
    UE_LOG(LogTemp, Display, TEXT("PF_ROUND active players=%d teams=%d"), Round->AlivePlayers, StartingTeams);
}
void APFGameMode::BeginStormClose()
{
    auto *Round = Match();
    int32 Stage = Round->StormStage;
    Round->bClosing = true;
    Round->StormStarts = GetWorld()->GetTimeSeconds();
    Round->StormEnds = Round->PhaseEnds = Round->StormStarts + PFRules::Closings[Stage];
    Round->RadiusTo = PFRules::Radii[Stage] * (Round->ArenaHalfExtent * 2 / 614400.f) * 100;
    float Shift = Stage == 0 ? 0 : FMath::Max(0.f, Round->RadiusFrom - Round->RadiusTo) * .35f;
    float Angle = FMath::FRandRange(0, 2 * PI);
    FVector Center =
        FVector(Round->CenterFrom) + FVector(FMath::Cos(Angle) * Shift, FMath::Sin(Angle) * Shift, 0);
    Center.X = FMath::Clamp(Center.X, -Round->ArenaHalfExtent * .55f, Round->ArenaHalfExtent * .55f);
    Center.Y = FMath::Clamp(Center.Y, -Round->ArenaHalfExtent * .55f, Round->ArenaHalfExtent * .55f);
    Round->CenterTo = Center;
    Round->Announcement = TEXT("Storm closing. Move toward the next safe circle.");
}
void APFGameMode::RoundSecond()
{
    auto *Round = Match();
    double Now = GetWorld()->GetTimeSeconds();
    if (Round->Phase == EPFRoundPhase::Range)
        return;
    if (Round->Phase == EPFRoundPhase::Countdown)
    {
        if (!AllReady())
        {
            Round->Phase = EPFRoundPhase::Lobby;
            Round->Announcement = TEXT("Countdown cancelled: a player is not ready.");
        }
        else if (Now >= Round->PhaseEnds)
            BeginBidding();
    }
    else if (Round->Phase == EPFRoundPhase::Bidding && Now >= Round->PhaseEnds)
        BeginLanding();
    else if (Round->Phase == EPFRoundPhase::Landing && Now >= Round->PhaseEnds)
    {
        APFPlayerState *State = DraftOrder.IsValidIndex(TurnIndex) ? DraftOrder[TurnIndex].Get() : nullptr;
        if (State)
        {
            bool Accepted = false;
            for (int32 Try = 0; Try < 80 && !Accepted; Try++)
            {
                FVector Point(FMath::FRandRange(-20000.f, 20000.f), FMath::FRandRange(-20000.f, 20000.f), 0);
                auto *PC = Cast<APlayerController>(State->GetOwner());
                if (PC)
                    Accepted = SelectLanding(PC, Point, true);
            }
            if (!Accepted)
            {
                State->bEliminated = true;
                State->bInRound = false;
                AdvanceTurn();
            }
        }
        else
            AdvanceTurn();
    }
    else if (Round->Phase == EPFRoundPhase::Active)
    {
        for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
            if (auto *Character = Cast<APFCharacter>(It->Get()->GetPawn()))
            {
                Character->SurvivalSecond();
                if (Character->Health > 0 && Round->Outside(Character->GetActorLocation()))
                    Character->EnvironmentalDamage(2 + Round->StormStage * 2);
            }
        UpdateAlive();
        if (Round->Phase != EPFRoundPhase::Active)
            return;
        if (Now >= Round->PhaseEnds)
        {
            if (!Round->bClosing)
                BeginStormClose();
            else
            {
                Round->RadiusFrom = Round->RadiusTo;
                Round->CenterFrom = Round->CenterTo;
                Round->bClosing = false;
                if (Round->StormStage < 6)
                {
                    Round->StormStage++;
                    Round->PhaseEnds = Now + PFRules::Waits[Round->StormStage];
                    Round->StormEnds = Round->PhaseEnds;
                    Round->Announcement = TEXT("Circle settled. Replenish before the next closure.");
                }
                else
                    Round->PhaseEnds = Now + 86400;
            }
        }
    }
}
bool APFGameMode::HasLivingTeammate(const APFCharacter *Character) const
{
    if (!Match()->IsBattleRoyale() || Match()->TeamSize == 1)
        return false;
    auto *State = Character->GetPlayerState<APFPlayerState>();
    if (!State)
        return false;
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *Other = Cast<APFPlayerState>(Entry))
            if (Other != State && Other->bInRound && !Other->bEliminated && Other->TeamId == State->TeamId)
            {
                auto *Pawn = Cast<APFCharacter>(Other->GetPawn());
                if (Pawn && Pawn->Health > 0 && !Pawn->bDowned)
                    return true;
            }
    return false;
}
void APFGameMode::UpdateAlive()
{
    auto *Round = Match();
    Round->AlivePlayers = 0;
    TSet<int32> Teams;
    for (APlayerState *Entry : Round->PlayerArray)
        if (auto *State = Cast<APFPlayerState>(Entry))
            if (State->bInRound && !State->bEliminated)
            {
                Round->AlivePlayers++;
                Teams.Add(State->TeamId);
            }
    Round->AliveTeams = Teams.Num();
    if (Round->Phase == EPFRoundPhase::Active &&
        ((StartingTeams >= 2 && Teams.Num() <= 1) || Teams.IsEmpty()))
    {
        Round->Phase = EPFRoundPhase::Finished;
        Round->WinningTeam = Teams.Num() == 1 ? *Teams.CreateConstIterator() : -1;
        Round->Announcement = Round->WinningTeam >= 0
                                  ? FString::Printf(TEXT("Team %d wins the reserve."), Round->WinningTeam + 1)
                                  : TEXT("The reserve claims everyone. No survivors.");
        UE_LOG(LogTemp, Display, TEXT("PF_ROUND finished winner=%d"), Round->WinningTeam);
    }
}
void APFGameMode::PlayerEliminated(APFCharacter *Character)
{
    if (!Match()->IsBattleRoyale())
    {
        RespawnAfterDeath(Character->GetController());
        return;
    }
    if (auto *State = Character->GetPlayerState<APFPlayerState>())
    {
        State->bEliminated = true;
        Match()->Announcement = State->GetPlayerName() + TEXT(" was eliminated.");
    }
    Character->Inventory->DropEverything();
    if (auto *PC = Cast<APFPlayerController>(Character->GetController()))
        PC->ServerCycleSpectator();
    for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
        if (auto *Other = Cast<APFCharacter>(It->Get()->GetPawn()))
            if (Other != Character && Other->bDowned && !HasLivingTeammate(Other))
                Other->EnvironmentalDamage(1000);
    UpdateAlive();
}
void APFGameMode::ReturnToLobby(APlayerController *Player)
{
    if (Player != Host.Get() || Match()->Phase != EPFRoundPhase::Finished)
        return;
    auto *Round = Match();
    Round->Phase = EPFRoundPhase::Lobby;
    Round->Announcement = TEXT("Back at camp. Ready for another expedition?");
    StartingTeams = 0;
    for (TActorIterator<APFWorldPickup> It(GetWorld()); It; ++It)
        It->Destroy();
    for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
        if (auto *PC = Cast<APFPlayerController>(It->Get()))
        {
            auto *State = PC->GetPlayerState<APFPlayerState>();
            State->bReady = false;
            State->bEliminated = false;
            State->bInRound = false;
            if (PC->GetPawn())
                PC->GetPawn()->Destroy();
            RestartPlayer(PC);
            PC->ClientResumeRound();
        }
}
void APFGameMode::SpawnFieldLoot()
{
    for (TActorIterator<APFWorldPickup> It(GetWorld()); It; ++It)
        It->Destroy();
    for (int32 Index = 0; Index < 90; Index++)
    {
        FVector Point(FMath::FRandRange(-21500.f, 21500.f), FMath::FRandRange(-21500.f, 21500.f), 0);
        if (!GroundPoint(Point))
            continue;
        Point.Z -= 85;
        FPFItemStack Item;
        Item.Count = 1;
        switch (Index % 9)
        {
        case 0:
            Item.Kind = EPFItemKind::Weapon;
            Item.WeaponId = TEXT("Ridgeline");
            Item.Magazine = 5;
            Item.Rarity = FMath::FRand() < .1 ? 3 : FMath::FRand() < .3 ? 2 : 0;
            break;
        case 1:
            Item.Kind = EPFItemKind::Weapon;
            Item.WeaponId = TEXT("ServiceNine");
            Item.Magazine = 12;
            break;
        case 2:
        case 3:
            Item.Kind = EPFItemKind::Ammo;
            Item.WeaponId = Index % 2 ? TEXT("Ridgeline") : TEXT("ServiceNine");
            Item.Count = Item.WeaponId == TEXT("Ridgeline") ? 10 : 24;
            break;
        case 4:
        case 5:
            Item.Kind = EPFItemKind::Food;
            Item.Count = 2;
            break;
        case 6:
            Item.Kind = EPFItemKind::Medkit;
            break;
        case 7:
            Item.Kind = FMath::FRand() < .2 ? EPFItemKind::HeavyVest : EPFItemKind::LightVest;
            break;
        default:
            Item.Kind = EPFItemKind::ArmorPlate;
            Item.Count = 2;
            break;
        }
        FActorSpawnParameters Params;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        if (auto *Loot = GetWorld()->SpawnActor<APFWorldPickup>(Point, FRotator::ZeroRotator, Params))
            Loot->Initialize(Item);
    }
}
