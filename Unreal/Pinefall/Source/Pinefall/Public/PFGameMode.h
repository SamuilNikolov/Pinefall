#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/PlayerState.h"
#include "PFGameMode.generated.h"
class APFMatchState;
class APFCharacter;
class APFWildlife;
class APFVehicle;
UCLASS()
class PINEFALL_API APFPlayerState : public APlayerState
{
    GENERATED_BODY()
  public:
    UPROPERTY(Replicated, BlueprintReadOnly) int32 Eliminations = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 TeamId = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bReady = false;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bEliminated = false;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bInRound = false;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 Bid = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 Kit = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bLandingLocked = false;
    UPROPERTY(Replicated, BlueprintReadOnly) FVector_NetQuantize Landing = FVector::ZeroVector;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
};
UCLASS()
class PINEFALL_API APFGameMode : public AGameModeBase
{
    GENERATED_BODY()
  public:
    APFGameMode();
    virtual void BeginPlay() override;
    virtual void PreLogin(const FString &Options, const FString &Address, const FUniqueNetIdRepl &UniqueId,
                          FString &ErrorMessage) override;
    virtual void PostLogin(APlayerController *NewPlayer) override;
    virtual void Logout(AController *Player) override;
    virtual void HandleStartingNewPlayer_Implementation(APlayerController *Player) override;
    void RespawnAfterDeath(AController *Controller);
    void PlayerEliminated(APFCharacter *Character);
    bool HasLivingTeammate(const APFCharacter *Character) const;
    void SetReady(APlayerController *Player);
    void SetTeamSize(APlayerController *Player, int32 Size);
    void ChooseTeam(APlayerController *Player, int32 Team);
    void RequestStart(APlayerController *Player);
    void SubmitBid(APlayerController *Player, int32 Bid, int32 Kit);
    bool SelectLanding(APlayerController *Player, FVector Point, bool Confirm);
    void ReturnToLobby(APlayerController *Player);
    APFMatchState *Match() const;
    void RunRoundProbe();
    void RunWildlifeProbe();
    void RunCombatProbe();
    void RunWorldProbe();
    void RunReserveProbe();
    void RunMovementProbe();

  private:
    void RoundSecond();
    bool AllReady() const;
    void BeginBidding();
    void BeginLanding();
    void AdvanceTurn();
    void StartBattle();
    void UpdateAlive();
    void BeginStormClose();
    void SpawnFieldLoot();
    bool GroundPoint(FVector &Point) const;
    TWeakObjectPtr<APlayerController> Host;
    TArray<TWeakObjectPtr<APFPlayerState>> DraftOrder;
    int32 TurnIndex = 0, StartingTeams = 0;
    FTimerHandle RoundTimer;
    FTimerHandle ProbeTimer;
    int32 ProbeStep = 0;
    double ProbeAfter = 0;
    int32 ProbeReserve = 0, ProbeFoodSlot = -1;
    float ProbeFoodHunger = 0;
    TArray<FString> ProbePassed, ProbeFailed;
    UPROPERTY() TArray<TObjectPtr<APFWildlife>> ProbeAnimals;
    TArray<FVector> ProbeAnimalOrigins;
    UPROPERTY() TObjectPtr<AActor> ProbeCover;
    FVector ProbeHostOrigin = FVector::ZeroVector;
    FVector ProbeRemoteOrigin = FVector::ZeroVector;
    UPROPERTY() TObjectPtr<APFVehicle> ProbeVehicle;
    FVector ReserveProbeOrigin = FVector::ZeroVector;
    float ReserveProbeYaw = 0;
    int32 MovementProbeJumps = 0;
    int32 MovementPendingCase = 0;
    float MovementPendingWait = 0;
    bool bMovementProbeWasAir = false;
    float MovementProbePeak = 0;
    int32 ReserveProbeFood = 0, ReserveProbePickups = 0;
};
