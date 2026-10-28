#include "PFMovementComponent.h"
#include "GameFramework/Character.h"
float UPFMovementComponent::GetMaxSpeed() const
{
    return IsCrouching() ? 250.f : bWantsSprint ? 940.f : 480.f;
}
void UPFMovementComponent::UpdateFromCompressedFlags(uint8 Flags)
{
    Super::UpdateFromCompressedFlags(Flags);
    bWantsSprint = (Flags & FSavedMove_Character::FLAG_Custom_0) != 0;
}
FNetworkPredictionData_Client *UPFMovementComponent::GetPredictionData_Client() const
{
    if (!ClientPredictionData)
        const_cast<UPFMovementComponent *>(this)->ClientPredictionData = new FPrediction_PF(*this);
    return ClientPredictionData;
}
void FSavedMove_PF::Clear()
{
    Super::Clear();
    bSavedSprint = false;
}
uint8 FSavedMove_PF::GetCompressedFlags() const
{
    return Super::GetCompressedFlags() | (bSavedSprint ? FLAG_Custom_0 : 0);
}
bool FSavedMove_PF::CanCombineWith(const FSavedMovePtr &Move, ACharacter *Character, float MaxDelta) const
{
    return bSavedSprint == static_cast<const FSavedMove_PF *>(Move.Get())->bSavedSprint &&
           Super::CanCombineWith(Move, Character, MaxDelta);
}
void FSavedMove_PF::SetMoveFor(ACharacter *Character, float Delta, FVector const &Accel,
                               FNetworkPredictionData_Client_Character &Data)
{
    Super::SetMoveFor(Character, Delta, Accel, Data);
    bSavedSprint = CastChecked<UPFMovementComponent>(Character->GetCharacterMovement())->bWantsSprint;
}
void FSavedMove_PF::PrepMoveFor(ACharacter *Character)
{
    Super::PrepMoveFor(Character);
    CastChecked<UPFMovementComponent>(Character->GetCharacterMovement())->bWantsSprint = bSavedSprint;
}
