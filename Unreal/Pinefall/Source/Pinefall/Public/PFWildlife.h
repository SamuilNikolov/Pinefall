#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "PFWildlife.generated.h"
class APFCharacter;
class UAnimSequence;
class USoundBase;
class USoundAttenuation;
UENUM()
enum class EPFWildlifeMotion : uint8
{
    Idle,
    Walk,
    Run,
    Attack,
    Dead
};

UCLASS()
class PINEFALL_API APFWildlife : public ACharacter
{
    GENERATED_BODY()
  public:
    APFWildlife();
    virtual void BeginPlay() override;
    virtual void Tick(float DeltaSeconds) override;
    virtual float TakeDamage(float Amount, const FDamageEvent &Event, AController *Instigator,
                             AActor *Causer) override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &Out) const override;
    UPROPERTY(ReplicatedUsing = ApplySpecies) FName Species = TEXT("Deer");
    UPROPERTY(ReplicatedUsing = ApplyMotion) EPFWildlifeMotion Motion = EPFWildlifeMotion::Idle;
    UPROPERTY(ReplicatedUsing = ApplyRage) bool bEnraged = false;
    UPROPERTY(ReplicatedUsing = ApplyRage) bool bAlpha = false;
    UPROPERTY(Replicated) float Health = 65;
    UPROPERTY(Replicated) float MaxHealth = 65;
    bool IsPredator() const
    {
        return Species == TEXT("Wolf");
    }
    void Think(const TArray<TWeakObjectPtr<APFCharacter>> &Players, bool Closing, int32 Stage,
               float ArenaHalf);
    UFUNCTION() void ApplySpecies();
    UFUNCTION() void ApplyMotion();
    UFUNCTION() void ApplyRage();

  private:
    void SetMotion(EPFWildlifeMotion Value);
    void DropRewards();
    bool ClearBite(APFCharacter *Player) const;
    FVector Goal = FVector::ZeroVector;
    TWeakObjectPtr<APFCharacter> Aggressor;
    TWeakObjectPtr<APFCharacter> BiteTarget;
    double AlertUntil = 0, NextWander = 0, NextAttack = 0, BiteAt = 0;
    int32 LastMutationStage = -1;
    float ModelScale = .32f;
    float BaseHalfHeight = 70, BaseRadius = 40, BaseMeshZ = -70;
    double NextVoice = 0;
    UPROPERTY() TObjectPtr<USoundBase> Voice;
    UPROPERTY() TObjectPtr<USoundAttenuation> VoiceAttenuation;
    UFUNCTION(NetMulticast, Unreliable) void MulticastCall();
    bool bLocomoting = false;
    FVector MoveDirection = FVector::ZeroVector;
    UPROPERTY() TArray<TObjectPtr<UAnimSequence>> Clips;
};

/** One player snapshot and bounded thinking work, rather than one world scan per animal. */
UCLASS()
class PINEFALL_API APFWildlifeDirector : public AActor
{
    GENERATED_BODY()
  public:
    APFWildlifeDirector();
    virtual void BeginPlay() override;
    virtual void EndPlay(EEndPlayReason::Type Reason) override;
    UPROPERTY() TArray<TObjectPtr<APFWildlife>> Population;

  private:
    void Step();
    bool SpawnAnimal(float HalfExtent);
    FTimerHandle BrainTimer;
    TArray<TWeakObjectPtr<APFCharacter>> Players;
    int32 Cursor = 0;
    bool bRoundRunning = false;
    double NextReplenish = 0;
};
