#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameStateBase.h"
#include "PFMatchState.generated.h"

UENUM(BlueprintType)
enum class EPFRoundPhase : uint8
{
    Range,
    Lobby,
    Countdown,
    Bidding,
    Landing,
    Active,
    Finished
};

/** Public round decisions only. Inventory stays owner-only; map code filters live enemy positions. */
UCLASS()
class PINEFALL_API APFMatchState : public AGameStateBase
{
    GENERATED_BODY()
  public:
    UPROPERTY(Replicated, BlueprintReadOnly) EPFRoundPhase Phase = EPFRoundPhase::Range;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 TeamSize = 1;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 HostPlayerId = -1;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 TurnPlayerId = -1;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 AlivePlayers = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 AliveTeams = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 WinningTeam = -1;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 StormStage = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bClosing = false;
    UPROPERTY(Replicated, BlueprintReadOnly) double PhaseEnds = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) double StormStarts = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) double StormEnds = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) float ArenaHalfExtent = 24000;
    UPROPERTY(Replicated, BlueprintReadOnly) float RadiusFrom = 35000;
    UPROPERTY(Replicated, BlueprintReadOnly) float RadiusTo = 35000;
    UPROPERTY(Replicated, BlueprintReadOnly) FVector_NetQuantize CenterFrom = FVector::ZeroVector;
    UPROPERTY(Replicated, BlueprintReadOnly) FVector_NetQuantize CenterTo = FVector::ZeroVector;
    UPROPERTY(Replicated, BlueprintReadOnly) FString Announcement;
    bool IsBattleRoyale() const
    {
        return Phase != EPFRoundPhase::Range;
    }
    float SafeRadius() const;
    FVector SafeCenter() const;
    float SecondsLeft() const;
    bool Outside(const FVector &Point) const;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
};
