#include "PFCharacter.h"
#include "PFVehicle.h"
#include "PFGameMode.h"
#include "PFMatchState.h"
#include "PFInventoryComponent.h"
#include "PFWorldPickup.h"
#include "PFMovementComponent.h"
#include "PFWaterBody.h"
#include "Components/CapsuleComponent.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/DamageType.h"
#include "Engine/DamageEvents.h"
#include "TimerManager.h"
bool APFCharacter::CanFight() const
{
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    return Health > 0 && !bDowned && !Vehicle &&
           (!Match || !Match->IsBattleRoyale() || Match->Phase == EPFRoundPhase::Active);
}
void APFCharacter::EndPlay(const EEndPlayReason::Type Reason)
{
    if (HasAuthority() && Vehicle)
        Vehicle->Exit(true);
    GetWorldTimerManager().ClearTimer(ReloadTimer);
    if (ReviveTarget.IsValid())
    {
        ReviveTarget->ReviveProgress = 0;
        ReviveTarget->Reviver.Reset();
    }
    Super::EndPlay(Reason);
}
void APFCharacter::ResetForRound(int32 Kit)
{
    if (!HasAuthority())
        return;
    CastChecked<UPFMovementComponent>(GetCharacterMovement())->ResetTraversal();
    Health = 100;
    Armor = 0;
    ArmorTier = 0;
    Hunger = 85;
    Stamina = 100;
    Oxygen = 100;
    bDowned = false;
    Knocks = 0;
    BleedOutAt = 0;
    ReviveProgress = 0;
    Inventory->InitializeLoadout(false, Kit);
    WeaponId = Kit == 5 ? TEXT("Ridgeline") : TEXT("ServiceNine");
    ApplyWeapon();
    GetCharacterMovement()->SetMovementMode(MOVE_Walking);
    GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
    OnRep_Downed();
}
void APFCharacter::EquipInventoryWeapon(FName Id)
{
    ServerEquip(Id);
}
void APFCharacter::StoreMagazine()
{
    if (HasAuthority())
        Inventory->StoreMagazine(WeaponId, Ammo);
}
void APFCharacter::EnsureEquippedWeapon()
{
    if (!HasAuthority() || Inventory->HasWeapon(WeaponId))
        return;
    WeaponId = NAME_None;
    for (const auto &Slot : Inventory->Slots)
        if (Slot.Count > 0 && Slot.Kind == EPFItemKind::Weapon)
        {
            WeaponId = Slot.WeaponId;
            break;
        }
    GetWorldTimerManager().ClearTimer(ReloadTimer);
    bReloading = false;
    ApplyWeapon();
    ForceNetUpdate();
}
void APFCharacter::EnvironmentalDamage(float Amount)
{
    if (!HasAuthority())
        return;
    TGuardValue<bool> Guard(bEnvironmentalDamage, true);
    TakeDamage(Amount, FDamageEvent(), nullptr, nullptr);
}
void APFCharacter::OnRep_Downed()
{
    if (bDowned)
    {
        Crouch();
        StopFire();
    }
    else if (Health > 0)
        UnCrouch();
}
void APFCharacter::SurvivalSecond()
{
    if (!HasAuthority() || Health <= 0)
        return;
    if (bDowned)
    {
        if (GetWorld()->GetTimeSeconds() >= BleedOutAt)
            EnvironmentalDamage(1000);
        return;
    }
    bool Moving = GetVelocity().Size2D() > 500;
    auto *Water = Cast<APFWaterBody>(GetPhysicsVolume());
    bool HeadUnderwater =
        Water && GetActorLocation().Z + (bIsCrouched ? 55 : 65) < Water->SurfaceHeight() - 5;
    Oxygen = FMath::Clamp(Oxygen + (HeadUnderwater ? -4.f : 24.f), 0.f, 100.f);
    if (HeadUnderwater && Oxygen <= 0)
    {
        EnvironmentalDamage(8);
        if (Health <= 0)
            return;
    }
    Hunger = FMath::Max(0.f, Hunger - (Moving ? .15f : .065f));
    const auto* Movement = CastChecked<UPFMovementComponent>(GetCharacterMovement());
    const bool Sprinting = Movement->bWantsSprint && Movement->IsMovingOnGround() && !Movement->IsSliding() && GetVelocity().Size2D() > 650;
    Stamina = FMath::Clamp(Stamina + (Sprinting ? -4.f : 16.f), 0.f, 100.f);
    if (Hunger <= 0)
    {
        if (++StarvationTimer >= 3)
        {
            StarvationTimer = 0;
            EnvironmentalDamage(3);
        }
    }
    else
        StarvationTimer = 0;
    auto *Target = ReviveTarget.Get();
    if (!Target)
        return;
    auto *Mine = GetPlayerState<APFPlayerState>();
    auto *Their = Target->GetPlayerState<APFPlayerState>();
    FHitResult Hit;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFReviveCover), false, this);
    Query.AddIgnoredActor(Target);
    bool Blocked = GetWorld()->LineTraceSingleByChannel(Hit, GetActorLocation(), Target->GetActorLocation(),
                                                        ECC_Visibility, Query);
    if (!CanFight() || !Target->bDowned || Target->Health <= 0 || !Mine || !Their ||
        Mine->TeamId != Their->TeamId ||
        FVector::DistSquared(GetActorLocation(), Target->GetActorLocation()) > 102400 || Blocked)
    {
        Target->ReviveProgress = 0;
        Target->Reviver.Reset();
        ReviveTarget.Reset();
        return;
    }
    Target->ReviveProgress += 1.f / 3;
    if (Target->ReviveProgress >= .999f)
    {
        Target->Health = 35;
        Target->Hunger = FMath::Max(25.f, Target->Hunger);
        Target->bDowned = false;
        Target->BleedOutAt = 0;
        Target->ReviveProgress = 0;
        Target->Reviver.Reset();
        Target->OnRep_Downed();
        Target->ForceNetUpdate();
        ReviveTarget.Reset();
    }
}
void APFCharacter::BeginInteract()
{
    if (!AcceptsInput())
        return;
    if (Vehicle)
    {
        ServerVehicleInteract(Vehicle, false);
        return;
    }
    if (!CanFight())
        return;
    if (FocusedVehicle.IsValid())
    {
        ServerVehicleInteract(FocusedVehicle.Get(), false);
        return;
    }
    if (FocusedPickup.IsValid())
        ServerInteract(FocusedPickup.Get());
    else
        ServerRevive(true);
}
void APFCharacter::EndInteract()
{
    ServerRevive(false);
}
void APFCharacter::ServerInteract_Implementation(APFWorldPickup *Pickup)
{
    if (!CanFight() || !IsValid(Pickup) || Pickup->IsActorBeingDestroyed() ||
        FVector::DistSquared(GetActorLocation(), Pickup->GetActorLocation()) > 160000)
        return;
    FHitResult Hit;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFPickupCover), false, this);
    Query.AddIgnoredActor(Pickup);
    if (GetWorld()->LineTraceSingleByChannel(Hit, GetActorLocation() + FVector(0, 0, 45),
                                             Pickup->GetActorLocation() + FVector(0, 0, 20), ECC_Visibility,
                                             Query))
        return;
    int32 Accepted = Inventory->Add(Pickup->Item);
    if (Accepted <= 0)
        return;
    Pickup->Item.Count -= Accepted;
    if (Pickup->Item.Count <= 0)
        Pickup->Destroy();
    else
        Pickup->ForceNetUpdate();
    EnsureEquippedWeapon();
}
void APFCharacter::ServerRevive_Implementation(bool Held)
{
    if (ReviveTarget.IsValid())
    {
        ReviveTarget->ReviveProgress = 0;
        ReviveTarget->Reviver.Reset();
    }
    ReviveTarget.Reset();
    if (!Held || !CanFight())
        return;
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    auto *Mine = GetPlayerState<APFPlayerState>();
    if (!Match || !Mine || Match->Phase != EPFRoundPhase::Active)
        return;
    for (APlayerState *Entry : Match->PlayerArray)
        if (auto *Their = Cast<APFPlayerState>(Entry))
            if (Their != Mine && Their->TeamId == Mine->TeamId)
            {
                auto *Target = Cast<APFCharacter>(Their->GetPawn());
                if (Target && Target->bDowned && Target->Health > 0 && !Target->Reviver.IsValid() &&
                    FVector::DistSquared(GetActorLocation(), Target->GetActorLocation()) <= 102400)
                {
                    ReviveTarget = Target;
                    Target->Reviver = this;
                    return;
                }
            }
}
