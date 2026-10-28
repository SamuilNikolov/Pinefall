#include "PFRangeTarget.h"
#include "Components/StaticMeshComponent.h"
#include "UObject/ConstructorHelpers.h"
#include "Net/UnrealNetwork.h"
#include "TimerManager.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInterface.h"
APFRangeTarget::APFRangeTarget()
{
    bReplicates = true;
    auto *Scene = CreateDefaultSubobject<USceneComponent>(TEXT("Target root"));
    SetRootComponent(Scene);
    Plate = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Steel plate"));
    Plate->SetupAttachment(Scene);
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Mesh(TEXT("/Engine/BasicShapes/Cylinder.Cylinder"));
    if (Mesh.Succeeded())
        Plate->SetStaticMesh(Mesh.Object);
    Plate->SetRelativeScale3D(FVector(.65, .65, .07));
    Plate->SetRelativeRotation(FRotator(90, 0, 0));
    Plate->SetCollisionProfileName(TEXT("BlockAll"));
    static ConstructorHelpers::FObjectFinder<UMaterialInterface> Steel(
        TEXT("/Game/StarterContent/Materials/M_Metal_Burnished_Steel.M_Metal_Burnished_Steel"));
    if (Steel.Succeeded())
        Plate->SetMaterial(0, Steel.Object);
    auto *Stand = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Target stand"));
    Stand->SetupAttachment(Scene);
    if (Mesh.Succeeded())
        Stand->SetStaticMesh(Mesh.Object);
    Stand->SetRelativeLocation(FVector(0, 0, -75));
    Stand->SetRelativeScale3D(FVector(.045f, .045f, 1.5f));
    Stand->SetCollisionProfileName(TEXT("BlockAll"));
    if (Steel.Succeeded())
        Stand->SetMaterial(0, Steel.Object);
}
void APFRangeTarget::ReceiveShot(float Damage)
{
    if (!HasAuthority() || Health <= 0)
        return;
    float Absorb = FMath::Min(Armor, FMath::CeilToFloat(Damage * .35f));
    Armor -= Absorb;
    Health = FMath::Max(0.f, Health - (Damage - Absorb));
    if (Health <= 0)
    {
        FTimerHandle Timer;
        GetWorldTimerManager().SetTimer(
            Timer,
            [this]()
            {
                Health = 100;
                Armor = 50;
            },
            3.f, false);
    }
}
void APFRangeTarget::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(APFRangeTarget, Health);
    DOREPLIFETIME(APFRangeTarget, Armor);
}
