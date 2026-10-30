#pragma once
#include "CoreMinimal.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "PFMovementComponent.generated.h"

UCLASS()
class PINEFALL_API UPFMovementComponent : public UCharacterMovementComponent
{
    GENERATED_BODY()
  public:
    bool bWantsSprint = false;
    bool bWantsHop = false;
    bool bSliding = false;
    bool bCanChain = false, bPoweredFlight = false, bPreviousCrouch = false;
    float MomentumSpeed = 0, GroundedSeconds = 1;
    uint32 ServerJumpCount = 0; // Authority-only accepted takeoffs, never client input.
    bool IsSliding() const;
    void ResetTraversal();
    virtual void OnTeleported() override;
    virtual bool DoJump(bool bReplayingMoves, float DeltaTime) override;
    virtual void UpdateCharacterStateBeforeMovement(float DeltaSeconds) override;
    virtual void CalcVelocity(float DeltaTime, float Friction, bool bFluid,
                              float BrakingDeceleration) override;
    virtual void ProcessLanded(const FHitResult &Hit, float RemainingTime, int32 Iterations) override;
    virtual void OnMovementModeChanged(EMovementMode PreviousMode, uint8 PreviousCustomMode) override;
    virtual float GetMaxSpeed() const override;
    virtual float ImmersionDepth() const override;
    virtual void UpdateFromCompressedFlags(uint8 Flags) override;
    virtual FNetworkPredictionData_Client *GetPredictionData_Client() const override;
};

class FSavedMove_PF : public FSavedMove_Character
{
  public:
    using Super = FSavedMove_Character;
    bool bSavedSprint = false, bSavedHop = false;
    bool bSavedSliding = false, bSavedChain = false, bSavedFlight = false, bSavedPreviousCrouch = false;
    float SavedMomentum = 0, SavedGroundedSeconds = 1;
    virtual void Clear() override;
    virtual uint8 GetCompressedFlags() const override;
    virtual bool CanCombineWith(const FSavedMovePtr &NewMove, ACharacter *Character,
                                float MaxDelta) const override;
    virtual void SetMoveFor(ACharacter *Character, float DeltaTime, FVector const &NewAccel,
                            FNetworkPredictionData_Client_Character &ClientData) override;
    virtual void PrepMoveFor(ACharacter *Character) override;
};
class FPrediction_PF : public FNetworkPredictionData_Client_Character
{
  public:
    FPrediction_PF(const UCharacterMovementComponent &Component)
        : FNetworkPredictionData_Client_Character(Component)
    {
    }
    virtual FSavedMovePtr AllocateNewMove() override
    {
        return FSavedMovePtr(new FSavedMove_PF());
    }
};
