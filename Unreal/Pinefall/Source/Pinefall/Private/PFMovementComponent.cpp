#include "PFMovementComponent.h"
#include "GameFramework/Character.h"
#include "PFCharacter.h"
#include "PFMatchState.h"
#include "PFWaterBody.h"
float UPFMovementComponent::ImmersionDepth() const
{
    if (auto *Water = Cast<APFWaterBody>(GetPhysicsVolume()))
    {
        float Half = CharacterOwner->GetSimpleCollisionHalfHeight();
        return FMath::Clamp(
            (Water->SurfaceHeight() - CharacterOwner->GetActorLocation().Z + Half) / (2 * Half), 0.f, 1.f);
    }
    return Super::ImmersionDepth();
}
float UPFMovementComponent::GetMaxSpeed() const
{
    auto *Player = Cast<APFCharacter>(GetOwner());
    if (!Player)
        return Super::GetMaxSpeed();
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    if (Player->Health <= 0 || (Match && Match->IsBattleRoyale() && Match->Phase != EPFRoundPhase::Active))
        return 0;
    if (Player->bDowned)
        return 100;
    if (IsSwimming())
        return MaxSwimSpeed;
    if (Player->Hunger <= 0)
        return 250;
    return IsCrouching() ? 250 : bWantsSprint && Player->Stamina > 5 && Player->Hunger > 20 ? 940 : 480;
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
