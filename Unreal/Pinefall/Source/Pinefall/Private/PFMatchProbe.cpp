// Opt-in integration harness. Ordinary builds never enter these paths without
// -PFRoundProbe on the authority. Remote inputs use the normal owning-controller RPCs.
#include "PFGameMode.h"
#include "PFPlayerController.h"
#include "PFCharacter.h"
#include "PFInventoryComponent.h"
#include "PFMatchState.h"
#include "PFWorldPickup.h"
#include "EngineUtils.h"
#include "Engine/DamageEvents.h"
#include "TimerManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "Serialization/JsonSerializer.h"
void APFPlayerController::ProbeRoundInput()
{
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    auto *Mine = GetPlayerState<APFPlayerState>();
    if (!Match || !Mine)
        return;
    FString ProbeRole;
    FParse::Value(FCommandLine::Get(), TEXT("PFProbeRole="), ProbeRole);
    // Process startup order is nondeterministic. Select the intended teams via
    // the real lobby RPC before readying, rather than assuming connection order.
    if (Match->Phase == EPFRoundPhase::Lobby && !bProbeReadySent)
    {
        int32 DesiredTeam = ProbeRole == TEXT("enemy") ? 1 : 0;
        if (Mine->TeamId != DesiredTeam)
        {
            ServerTeam(DesiredTeam);
            return;
        }
        ServerReady();
        bProbeReadySent = true;
    }
    int32 Phase = int32(Match->Phase);
    if (Phase != ProbePhase && Match->Phase == EPFRoundPhase::Bidding)
    {
        UE_LOG(LogTemp, Display, TEXT("PF_PROBE sending bids role=%s"), *ProbeRole);
        ServerBid(99, 5);
        ServerBid(ProbeRole == TEXT("enemy")  ? 10
                  : ProbeRole == TEXT("ally") ? 3
                                              : 1,
                  ProbeRole == TEXT("enemy") ? 0 : 5);
    }
    ProbePhase = Phase;
    if (Match->Phase == EPFRoundPhase::Landing && Match->TurnPlayerId == Mine->GetPlayerId() &&
        GetWorld()->GetTimeSeconds() >= LandingRetry)
    {
        LandingRetry = GetWorld()->GetTimeSeconds() + 1;
        FVector Point = ProbeRole == TEXT("enemy")  ? FVector(-2000, 0, 0)
                        : ProbeRole == TEXT("ally") ? FVector(-10000, 6000, 0)
                                                    : FVector(-10000, -6000, 0);
        ServerLanding(Point, true);
    }
}
void APFGameMode::RunRoundProbe()
{
    if (!FParse::Param(FCommandLine::Get(), TEXT("PFRoundProbe")) || !Host.IsValid())
        return;
    auto *Round = Match();
    double Now = GetWorld()->GetTimeSeconds();
    if (Now < ProbeAfter)
        return;
    auto Finish = [&]()
    {
        GetWorldTimerManager().ClearTimer(ProbeTimer);
        TSharedRef<FJsonObject> Result = MakeShared<FJsonObject>();
        TArray<TSharedPtr<FJsonValue>> Passed, Failed;
        for (auto &Text : ProbePassed)
            Passed.Add(MakeShared<FJsonValueString>(Text));
        for (auto &Text : ProbeFailed)
            Failed.Add(MakeShared<FJsonValueString>(Text));
        Result->SetArrayField(TEXT("passed"), Passed);
        Result->SetArrayField(TEXT("failed"), Failed);
        Result->SetStringField(
            TEXT("scope"),
            TEXT("Three real processes, owning-client ready/bid/landing RPCs; authority advances timers and "
                 "arranges damage for deterministic rule checks. No graphics, WAN or microphone claim."));
        FString Json;
        FJsonSerializer::Serialize(Result, TJsonWriterFactory<>::Create(&Json));
        FString Report;
        if (FParse::Value(FCommandLine::Get(), TEXT("PFRoundReport="), Report))
        {
            IFileManager::Get().MakeDirectory(*FPaths::GetPath(Report), true);
            FFileHelper::SaveStringToFile(Json, *Report);
        }
        UE_LOG(LogTemp, Display, TEXT("PF_ROUND_PROBE_%s passed=%d failed=%d"),
               ProbeFailed.IsEmpty() ? TEXT("PASS") : TEXT("FAIL"), ProbePassed.Num(), ProbeFailed.Num());
    };
    if (!ProbeFailed.IsEmpty())
    {
        Finish();
        return;
    }
    auto Check = [&](bool Value, const TCHAR *Text)
    {
        if (Value)
            ProbePassed.Add(Text);
        else
            ProbeFailed.Add(Text);
        UE_LOG(LogTemp, Display, TEXT("PF_ROUND_CHECK %s %s"), Value ? TEXT("PASS") : TEXT("FAIL"), Text);
        return Value;
    };
    auto *HostPC = Cast<APFPlayerController>(Host.Get());
    auto *HostState = HostPC->GetPlayerState<APFPlayerState>();
    APFPlayerState *AllyState = nullptr;
    APFPlayerState *EnemyState = nullptr;
    for (APlayerState *Entry : Round->PlayerArray)
        if (auto *State = Cast<APFPlayerState>(Entry))
            if (State != HostState)
            {
                if (State->TeamId == HostState->TeamId)
                    AllyState = State;
                else
                    EnemyState = State;
            }
    if (!AllyState || !EnemyState)
        return;
    auto *AllyPC = Cast<APFPlayerController>(AllyState->GetOwner());
    auto *EnemyPC = Cast<APFPlayerController>(EnemyState->GetOwner());
    auto *HostPawn = Cast<APFCharacter>(HostPC->GetPawn());
    auto *AllyPawn = Cast<APFCharacter>(AllyPC->GetPawn());
    auto *EnemyPawn = Cast<APFCharacter>(EnemyPC->GetPawn());
    auto Damage = [&](APFCharacter *Pawn, float Amount, APlayerController *Source)
    { return Pawn->TakeDamage(Amount, FDamageEvent(), Source, Source ? Source->GetPawn() : nullptr); };
    if (ProbeStep == 0)
    {
        if (!AllReady())
            return;
        Check(GetNumPlayers() == 3 && Round->TeamSize == 2, TEXT("three participants allocated into duos"));
        RequestStart(HostPC);
        Check(Round->Phase == EPFRoundPhase::Countdown, TEXT("ready lobby accepts host countdown"));
        SetReady(AllyPC);
        RoundSecond();
        Check(Round->Phase == EPFRoundPhase::Lobby, TEXT("withdrawing readiness cancels countdown"));
        SetReady(AllyPC);
        RequestStart(HostPC);
        Round->PhaseEnds = Now;
        RoundSecond();
        ProbeStep = 1;
        ProbeAfter = Now + 3;
    }
    else if (ProbeStep == 1)
    {
        // Initial asset work may delay a remote phase notification. Await the
        // owning-client commands within the real bidding window, not a 3 s guess.
        if ((HostState->Bid != 1 || AllyState->Bid != 3 || EnemyState->Bid != 10) && Now < ProbeAfter + 12)
            return;
        Check(HostState->Bid == 1 && AllyState->Bid == 3 && EnemyState->Bid == 10,
              TEXT("remote bids arrive and oversized bids are rejected"));
        Check(HostState->Kit == 5 && AllyState->Kit == 5 && EnemyState->Kit == 0,
              TEXT("loadout and priority share the ten-point budget"));
        Round->PhaseEnds = Now;
        RoundSecond();
        Check(Round->TurnPlayerId == EnemyState->GetPlayerId(), TEXT("highest bidder chooses first"));
        Check(!SelectLanding(HostPC, FVector(-10000, -6000, 0), true),
              TEXT("out-of-turn landing is rejected"));
        ProbeStep = 2;
    }
    else if (ProbeStep == 2)
    {
        if (Round->Phase != EPFRoundPhase::Active)
            return;
        if (!Check(HostPawn && AllyPawn && EnemyPawn, TEXT("all selected landings spawn playable pawns")))
            return;
        Check(Round->AlivePlayers == 3 && Round->AliveTeams == 2,
              TEXT("active round counts players and teams"));
        Check(FVector::Dist2D(HostPawn->GetActorLocation(), AllyPawn->GetActorLocation()) > 10000,
              TEXT("teammates land at independent locations"));
        Check(HostPawn->Inventory->HasWeapon(TEXT("Ridgeline")) &&
                  !EnemyPawn->Inventory->HasWeapon(TEXT("Ridgeline")),
              TEXT("kit sacrifice changes actual starting weapons"));
        float HP = AllyPawn->Health;
        Damage(AllyPawn, 20, HostPC);
        Check(AllyPawn->Health == HP, TEXT("friendly fire is rejected"));
        FPFItemStack Vest;
        Vest.Kind = EPFItemKind::LightVest;
        Vest.Count = 1;
        HostPawn->Inventory->Add(Vest);
        int32 VestSlot = HostPawn->Inventory->Slots.IndexOfByPredicate(
            [](const FPFItemStack &Item) { return Item.Count > 0 && Item.Kind == EPFItemKind::LightVest; });
        if (!Check(VestSlot >= 0, TEXT("armor can be carried in inventory")))
            return;
        HP = HostPawn->Health;
        HostPawn->Inventory->ServerUseSlot(VestSlot, HostPawn->Inventory->Slots[VestSlot].Revision);
        Check(HostPawn->Armor == 50 && HostPawn->Health == HP,
              TEXT("vest equip grants armor without health damage"));
        Damage(HostPawn, 40, EnemyPC);
        Check(HostPawn->Armor == 40 && HostPawn->Health == 70,
              TEXT("light vest absorbs 25 percent of gun damage"));
        HostPawn->EnvironmentalDamage(10);
        Check(HostPawn->Armor == 40 && HostPawn->Health == 60, TEXT("environmental damage bypasses armor"));
        HostPawn->Hunger = 0;
        HostPawn->SurvivalSecond();
        HostPawn->SurvivalSecond();
        HostPawn->SurvivalSecond();
        Check(HostPawn->Health == 57 && HostPawn->Armor == 40,
              TEXT("starvation damages health without consuming armor"));
        int32 Food = HostPawn->Inventory->Slots.IndexOfByPredicate(
            [](const FPFItemStack &Item) { return Item.Count > 0 && Item.Kind == EPFItemKind::Food; });
        if (!Check(Food >= 0, TEXT("loadout contains usable rations")))
            return;
        ProbeFoodSlot = Food;
        HostPawn->Inventory->ServerUseSlot(Food, HostPawn->Inventory->Slots[Food].Revision);
        Check(HostPawn->Hunger == 35 && HostPawn->Health == 62,
              TEXT("food restores hunger and a small amount of health"));
        HostPawn->Ammo = 2;
        HostPawn->StoreMagazine();
        ProbeReserve = HostPawn->Inventory->Reserve(HostPawn->WeaponId);
        HostPawn->Reload();
        ProbeStep = 3;
        ProbeAfter = Now + 3.5;
    }
    else if (ProbeStep == 3)
    {
        Check(HostPawn->Ammo == 5 && HostPawn->Inventory->Reserve(HostPawn->WeaponId) == ProbeReserve - 3,
              TEXT("reload consumes exactly the missing rounds"));
        // The starting ration was consumed in the hunger check. Arrange a fresh
        // stack for the independent drop/pickup transaction checks.
        FPFItemStack Ration;
        Ration.Kind = EPFItemKind::Food;
        Ration.Count = 2;
        HostPawn->Inventory->Add(Ration);
        ProbeFoodSlot = HostPawn->Inventory->Slots.IndexOfByPredicate(
            [](const FPFItemStack &Item) { return Item.Count > 0 && Item.Kind == EPFItemKind::Food; });
        if (!Check(ProbeFoodSlot >= 0, TEXT("drop test has a fresh ration stack")))
            return;
        auto &Food = HostPawn->Inventory->Slots[ProbeFoodSlot];
        uint32 Old = Food.Revision;
        int32 Count = Food.Count;
        HostPawn->Inventory->ServerDropSlot(ProbeFoodSlot, Count, Old);
        Check(HostPawn->Inventory->Slots[ProbeFoodSlot].Count == 0,
              TEXT("dropping removes the original inventory stack"));
        APFWorldPickup *Dropped = nullptr;
        for (TActorIterator<APFWorldPickup> It(GetWorld()); It; ++It)
            if (It->GetOwner() == HostPawn && It->Item.Kind == EPFItemKind::Food &&
                FVector::DistSquared(It->GetActorLocation(), HostPawn->GetActorLocation()) < 25000)
            {
                Dropped = *It;
                break;
            }
        if (!Check(Dropped != nullptr, TEXT("drop creates a nearby shareable world pickup")))
            return;
        HostPawn->ServerInteract(Dropped);
        Check(HostPawn->Inventory->Slots[ProbeFoodSlot].Count == Count,
              TEXT("picking up returns the stack through validated interaction"));
        float Hunger = HostPawn->Hunger;
        HostPawn->Inventory->ServerUseSlot(ProbeFoodSlot, Old);
        Check(HostPawn->Hunger == Hunger, TEXT("stale inventory click cannot consume a replacement stack"));
        Damage(AllyPawn, 999, EnemyPC);
        Check(AllyPawn->bDowned && AllyPawn->Health == 35 && !AllyState->bEliminated,
              TEXT("lethal team damage downs a revivable player"));
        HostPawn->ServerRevive(true);
        Check(AllyPawn->ReviveProgress == 0, TEXT("revive cannot start outside its interaction range"));
        AllyPawn->SetActorLocation(HostPawn->GetActorLocation() + FVector(200, 0, 0));
        HostPawn->ServerRevive(true);
        ProbeStep = 4;
        ProbeAfter = Now + 4;
    }
    else if (ProbeStep == 4)
    {
        Check(!AllyPawn->bDowned && AllyPawn->Health == 35,
              TEXT("held nearby revive completes after three seconds"));
        // Exercise the real storm clock without waiting an entire match in a test.
        Round->PhaseEnds = Now;
        RoundSecond();
        Check(Round->bClosing && Round->RadiusTo < Round->RadiusFrom,
              TEXT("waiting storm enters a smaller closing phase"));
        Round->StormEnds = Round->PhaseEnds = Now;
        RoundSecond();
        Check(!Round->bClosing && Round->StormStage == 1,
              TEXT("storm closure advances into the next waiting cycle"));
        FVector OldPosition = EnemyPawn->GetActorLocation();
        EnemyPawn->SetActorLocation(FVector(24000, 24000, OldPosition.Z));
        float HP = EnemyPawn->Health;
        RoundSecond();
        Check(EnemyPawn->Health < HP, TEXT("outside-circle players take authoritative storm damage"));
        EnemyPawn->SetActorLocation(OldPosition);
        HostPawn->Knocks = 2;
        Damage(HostPawn, 999, EnemyPC);
        HostPC->ServerCycleSpectator();
        Check(HostState->bEliminated && HostPC->SpectateTarget.Get() == AllyPawn,
              TEXT("eliminated duo member can spectate only a surviving teammate"));
        Damage(AllyPawn, 999, EnemyPC);
        HostPC->ServerCycleSpectator();
        Check(HostPC->SpectateTarget.Get() == EnemyPawn,
              TEXT("after team wipe spectating may follow any survivor"));
        Check(Round->Phase == EPFRoundPhase::Finished && Round->WinningTeam == EnemyState->TeamId,
              TEXT("last surviving team wins without battle respawns"));
        ReturnToLobby(HostPC);
        Check(Round->Phase == EPFRoundPhase::Lobby && !HostState->bEliminated && !HostState->bReady,
              TEXT("host returns everyone to a clean unready lobby"));
        Check(HostPC->GetPawn() && AllyPC->GetPawn() && EnemyPC->GetPawn(),
              TEXT("all participants receive new lobby pawns"));
        Finish();
    }
}
