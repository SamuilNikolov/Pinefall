#include "PFGameMode.h"
#include "PFCharacter.h"
#include "PFPlayerController.h"
#include "Net/UnrealNetwork.h"
#include "TimerManager.h"
APFGameMode::APFGameMode()
{
    DefaultPawnClass = APFCharacter::StaticClass();
    PlayerControllerClass = APFPlayerController::StaticClass();
    PlayerStateClass = APFPlayerState::StaticClass();
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
}
