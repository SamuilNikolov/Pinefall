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
        return FMath::Clamp((Water->SurfaceHeight() - CharacterOwner->GetActorLocation().Z + Half) /
                                (2 * Half),
                            0.f, 1.f);
    }
    return Super::ImmersionDepth();
}
bool UPFMovementComponent::IsSliding() const
{
    const auto *Player = Cast<APFCharacter>(GetOwner());
    return Player && Player->GetLocalRole() == ROLE_SimulatedProxy ? Player->bSliding : bSliding;
}
void UPFMovementComponent::ResetTraversal()
{
    bSliding = bCanChain = bPoweredFlight = bPreviousCrouch = false;
    bWantsHop = bWantsSprint = false;
    MomentumSpeed = 0;
    GroundedSeconds = 1;
    if (auto *Player = Cast<APFCharacter>(GetOwner()))
        Player->bSliding = false;
}
void UPFMovementComponent::OnTeleported()
{
    ResetTraversal();
    Super::OnTeleported();
}
float UPFMovementComponent::GetMaxSpeed() const
{
    const auto *Player = Cast<APFCharacter>(GetOwner());
    if (!Player)
        return Super::GetMaxSpeed();
    const auto *Match = GetWorld()->GetGameState<APFMatchState>();
    if (Player->Health <= 0 || Player->Vehicle ||
        (Match && Match->IsBattleRoyale() && Match->Phase != EPFRoundPhase::Active))
        return 0;
    if (Player->bDowned)
        return 100;
    if (IsSwimming())
        return MaxSwimSpeed;
    if (Player->Hunger <= 0)
        return IsCrouching() ? 200 : 250;
    if (bSliding)
        return 2600;
    if (IsCrouching() && !IsFalling())
        return 200;
    const float Base = bWantsSprint && Player->Stamina > 5 && Player->Hunger > 20 ? 940 : 480;
    return FMath::Max(Base, Player->Hunger > 20 && Player->Stamina > 5 ? MomentumSpeed : 0.f);
}
void UPFMovementComponent::UpdateCharacterStateBeforeMovement(float Delta)
{
    Super::UpdateCharacterStateBeforeMovement(Delta);
    auto *Player = Cast<APFCharacter>(GetOwner());
    if (!Player || Player->GetLocalRole() == ROLE_SimulatedProxy)
        return;
    const bool Eligible = Player->CanFight() && Player->Hunger > 20 && Player->Stamina > 5;
    if (IsMovingOnGround())
        GroundedSeconds += Delta;
    const float Speed = Velocity.Size2D();
    if (Eligible && IsMovingOnGround() && bWantsToCrouch && !bPreviousCrouch && Speed >= 650)
    {
        bSliding = true;
        MomentumSpeed = FMath::Min(Speed, 2600.f);
    }
    if (bSliding && (!Eligible || !IsMovingOnGround() || !bWantsToCrouch || Speed < 300))
        bSliding = false;
    if (!Eligible || (IsMovingOnGround() && !bSliding && GroundedSeconds > .18f))
    {
        MomentumSpeed = 0;
        bCanChain = false;
    }
    bPreviousCrouch = bWantsToCrouch;
    Player->bSliding = bSliding;
}
bool UPFMovementComponent::DoJump(bool Replaying, float Delta)
{
    const float Before = Velocity.Size2D();
    if (!Super::DoJump(Replaying, Delta))
        return false;
    const auto *Player = Cast<APFCharacter>(GetOwner());
    if (Player && Player->HasAuthority() && !Replaying)
        ++ServerJumpCount;
    float Speed = Before;
    if (Player && Player->Hunger > 20 && Player->Stamina > 5 && bCanChain && GroundedSeconds <= .15f &&
        Before >= 450)
        Speed = FMath::Min(2400.f, Before + 170.f);
    if (Before > 1)
    {
        Velocity.X *= Speed / Before;
        Velocity.Y *= Speed / Before;
    }
    MomentumSpeed = FMath::Min(Speed, 2600.f);
    bPoweredFlight = true;
    bCanChain = bSliding = false;
    if (auto *Character = Cast<APFCharacter>(GetOwner()))
        Character->bSliding = false;
    return true;
}
void UPFMovementComponent::CalcVelocity(float Delta, float Friction, bool Fluid, float Deceleration)
{
    if (bSliding && IsMovingOnGround())
    {
        // Use normal walking sweeps/step-up/floor collision, with low-drag lateral motion.
        FVector Direction = Velocity.GetSafeNormal2D();
        const FVector Input = Acceleration.GetSafeNormal2D();
        if (!Input.IsNearlyZero())
            Direction = FMath::Lerp(Direction, Input, FMath::Min(Delta * 1.6f, .15f)).GetSafeNormal2D();
        FVector Planar = Direction * FMath::Max(0.f, Velocity.Size2D() - 160.f * Delta);
        const FVector Normal = CurrentFloor.HitResult.ImpactNormal;
        Planar += FVector(Normal.X, Normal.Y, 0) * FMath::Max(0.f, Normal.Z) *
                  FMath::Abs(GetGravityZ()) * Delta * .7f;
        Planar = Planar.GetClampedToMaxSize(2600);
        Velocity.X = Planar.X;
        Velocity.Y = Planar.Y;
        MomentumSpeed = Planar.Size2D();
        return;
    }
    if (IsMovingOnGround() && bCanChain && GroundedSeconds <= .15f && bWantsHop)
        return; // Preserve landing momentum through the next buffered jump.
    Super::CalcVelocity(Delta, Friction, Fluid, Deceleration);
}
void UPFMovementComponent::ProcessLanded(const FHitResult &Hit, float Remaining, int32 Iterations)
{
    // Before Super: it can immediately perform remaining walking physics.
    bCanChain = bPoweredFlight;
    bPoweredFlight = false;
    GroundedSeconds = 0;
    auto *Player = Cast<APFCharacter>(GetOwner());
    if (Player && Player->CanFight() && Player->Hunger > 20 && Player->Stamina > 5 && bWantsToCrouch &&
        Velocity.Size2D() >= 650)
    {
        // Buffered air-crouch must enter sliding before remaining landing physics
        // can clamp the fast arrival to ordinary crouch-walk speed.
        bSliding = true;
        MomentumSpeed = FMath::Min(float(Velocity.Size2D()), 2600.f);
        Player->bSliding = true;
    }
    Super::ProcessLanded(Hit, Remaining, Iterations);
}
void UPFMovementComponent::OnMovementModeChanged(EMovementMode Previous, uint8 Custom)
{
    Super::OnMovementModeChanged(Previous, Custom);
    if (MovementMode != MOVE_Walking && MovementMode != MOVE_Falling)
        ResetTraversal();
}
void UPFMovementComponent::UpdateFromCompressedFlags(uint8 Flags)
{
    Super::UpdateFromCompressedFlags(Flags);
    bWantsSprint = (Flags & FSavedMove_Character::FLAG_Custom_0) != 0;
    bWantsHop = (Flags & FSavedMove_Character::FLAG_Custom_1) != 0;
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
    bSavedSprint = bSavedHop = bSavedSliding = bSavedChain = bSavedFlight = bSavedPreviousCrouch = false;
    SavedMomentum = 0;
    SavedGroundedSeconds = 1;
}
uint8 FSavedMove_PF::GetCompressedFlags() const
{
    return Super::GetCompressedFlags() | (bSavedSprint ? FLAG_Custom_0 : 0) |
           (bSavedHop ? FLAG_Custom_1 : 0);
}
bool FSavedMove_PF::CanCombineWith(const FSavedMovePtr &Move, ACharacter *Character,
                                   float MaxDelta) const
{
    const auto *Other = static_cast<const FSavedMove_PF *>(Move.Get());
    if (bSavedHop || Other->bSavedHop || bSavedSliding || Other->bSavedSliding || SavedMomentum > 0 ||
        Other->SavedMomentum > 0)
        return false;
    return bSavedSprint == Other->bSavedSprint && Super::CanCombineWith(Move, Character, MaxDelta);
}
void FSavedMove_PF::SetMoveFor(ACharacter *Character, float Delta, FVector const &Accel,
                               FNetworkPredictionData_Client_Character &Data)
{
    Super::SetMoveFor(Character, Delta, Accel, Data);
    const auto *Movement = CastChecked<UPFMovementComponent>(Character->GetCharacterMovement());
    bSavedSprint = Movement->bWantsSprint;
    bSavedHop = Movement->bWantsHop;
    bSavedSliding = Movement->bSliding;
    bSavedChain = Movement->bCanChain;
    bSavedFlight = Movement->bPoweredFlight;
    bSavedPreviousCrouch = Movement->bPreviousCrouch;
    SavedMomentum = Movement->MomentumSpeed;
    SavedGroundedSeconds = Movement->GroundedSeconds;
}
void FSavedMove_PF::PrepMoveFor(ACharacter *Character)
{
    Super::PrepMoveFor(Character);
    auto *Movement = CastChecked<UPFMovementComponent>(Character->GetCharacterMovement());
    Movement->bWantsSprint = bSavedSprint;
    Movement->bWantsHop = bSavedHop;
    Movement->bSliding = bSavedSliding;
    Movement->bCanChain = bSavedChain;
    Movement->bPoweredFlight = bSavedFlight;
    Movement->bPreviousCrouch = bSavedPreviousCrouch;
    Movement->MomentumSpeed = SavedMomentum;
    Movement->GroundedSeconds = SavedGroundedSeconds;
}
