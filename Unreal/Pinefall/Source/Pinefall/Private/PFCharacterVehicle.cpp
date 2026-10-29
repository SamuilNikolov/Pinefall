#include "Components/BoxComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "PFCharacter.h"
#include "PFInventoryComponent.h"
#include "PFPlayerController.h"
#include "PFVehicle.h"
void APFCharacter::OnRep_Vehicle()
{
    if (Vehicle)
    {
        if (bVehiclePresentationActive)
            return;
        bVehiclePresentationActive = true;
        StopFire();
        StopAim();
        bViewBeforeDriving = bFirstPerson;
        bFirstPerson = false;
        bUseControllerRotationYaw = false;
        GetCharacterMovement()->StopMovementImmediately();
        GetCharacterMovement()->DisableMovement();
        GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
        AttachToComponent(Vehicle->Collision, FAttachmentTransformRules::KeepWorldTransform);
        SetActorRelativeLocation(FVector(-25, -35, Vehicle->bBoat ? 75 : 25));
        SetActorRelativeRotation(FRotator::ZeroRotator);
    }
    else
    {
        bVehiclePresentationActive = false;
        DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);
        bUseControllerRotationYaw = true;
        GetCapsuleComponent()->SetCollisionEnabled(Health > 0 ? ECollisionEnabled::QueryAndPhysics
                                                              : ECollisionEnabled::NoCollision);
        if (Health > 0)
            GetCharacterMovement()->SetMovementMode(MOVE_Walking);
        bFirstPerson = bViewBeforeDriving;
        DriveAxes = FVector2D::ZeroVector;
        bDriveBoost = bDriveBrake = false;
    }
}
void APFCharacter::ServerDrive_Implementation(FVector2D Axes, bool Boost, bool Brake)
{
    if (Vehicle && Health > 0 && !bDowned)
        Vehicle->Input(this, Axes, Boost, Brake);
}
void APFCharacter::ServerVehicleInteract_Implementation(APFVehicle *Target, bool Service)
{
    if (!IsValid(Target))
        return;
    if (Vehicle)
    {
        if (Vehicle == Target)
            Vehicle->Exit();
        return;
    }
    if (!CanFight())
        return;
    if (Service)
    {
        if (Inventory->CountKind(EPFItemKind::ServiceKit) > 0 && Target->Repair(this))
            Inventory->ConsumeKind(EPFItemKind::ServiceKit);
    }
    else
        Target->Enter(this);
}
