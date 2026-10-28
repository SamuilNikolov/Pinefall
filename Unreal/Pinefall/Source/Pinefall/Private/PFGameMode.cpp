#include "PFGameMode.h"
#include "PFCharacter.h"
#include "PFPlayerController.h"
#include "PFMatchState.h"
#include "Net/UnrealNetwork.h"
#include "TimerManager.h"
APFGameMode::APFGameMode()
{
    DefaultPawnClass = APFCharacter::StaticClass();
    PlayerControllerClass = APFPlayerController::StaticClass();
    PlayerStateClass = APFPlayerState::StaticClass();
    GameStateClass = APFMatchState::StaticClass();
}
void APFGameMode::PreLogin(const FString &Options, const FString &Address, const FUniqueNetIdRepl &Id,
                           FString &Error)
{
    Super::PreLogin(Options, Address, Id, Error);
    if (GetNumPlayers() >= 20)
        Error = TEXT("This reserve already has twenty players.");
}
void APFGameMode::PostLogin(APlayerController *Player)
{
    Super::PostLogin(Player);
    auto *State = Player->GetPlayerState<APFPlayerState>();
    auto *Round = Match();
    if (!Host.IsValid())
    {
        Host = Player;
        Round->HostPlayerId = State->GetPlayerId();
    }
    if (Round->Phase == EPFRoundPhase::Active || Round->Phase == EPFRoundPhase::Finished ||
        Round->Phase == EPFRoundPhase::Bidding || Round->Phase == EPFRoundPhase::Landing)
    {
        State->bEliminated = true;
        State->bInRound = false;
    }
    else
        for (int32 Team = 0; Team < 20; Team++)
        {
            int32 Count = 0;
            for (APlayerState *Entry : Round->PlayerArray)
                if (auto *Other = Cast<APFPlayerState>(Entry))
                    if (Other != State && Other->TeamId == Team)
                        Count++;
            if (Count < Round->TeamSize)
            {
                State->TeamId = Team;
                break;
            }
        }
    UE_LOG(LogTemp, Display, TEXT("PF_JOIN players=%d"), GetNumPlayers());
}
void APFGameMode::RespawnAfterDeath(AController *Controller)
{
    TWeakObjectPtr<AController> Weak = Controller;
    FTimerHandle Timer;
    GetWorldTimerManager().SetTimer(
        Timer,
        [this, Weak]()
        {
            if (Weak.IsValid())
            {
                if (APawn *PreviousPawn = Weak->GetPawn())
                    PreviousPawn->Destroy();
                RestartPlayer(Weak.Get());
            }
        },
        6.f, false);
}
void APFPlayerState::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(APFPlayerState, Eliminations);
    DOREPLIFETIME(APFPlayerState, TeamId);
    DOREPLIFETIME(APFPlayerState, bReady);
    DOREPLIFETIME(APFPlayerState, bEliminated);
    DOREPLIFETIME(APFPlayerState, bInRound);
    DOREPLIFETIME(APFPlayerState, Bid);
    DOREPLIFETIME(APFPlayerState, Kit);
    DOREPLIFETIME(APFPlayerState, bLandingLocked);
    DOREPLIFETIME(APFPlayerState, Landing);
}
