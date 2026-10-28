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
    virtual float GetMaxSpeed() const override;
    virtual void UpdateFromCompressedFlags(uint8 Flags) override;
    virtual FNetworkPredictionData_Client *GetPredictionData_Client() const override;
};

class FSavedMove_PF : public FSavedMove_Character
{
  public:
    using Super = FSavedMove_Character;
    bool bSavedSprint = false;
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
