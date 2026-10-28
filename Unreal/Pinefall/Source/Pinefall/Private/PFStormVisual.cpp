#include "PFStormVisual.h"
#include "PFMatchState.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInterface.h"
#include "UObject/ConstructorHelpers.h"
APFStormVisual::APFStormVisual()
{
    PrimaryActorTick.bCanEverTick = true;
    PrimaryActorTick.TickInterval = .05;
    Wall = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Storm boundary"));
    SetRootComponent(Wall);
    static ConstructorHelpers::FObjectFinder<UStaticMesh> Mesh(TEXT("/Engine/BasicShapes/Cylinder.Cylinder"));
    if (Mesh.Succeeded())
        Wall->SetStaticMesh(Mesh.Object);
    Wall->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Wall->CastShadow = false;
    SetActorHiddenInGame(true);
}
void APFStormVisual::BeginPlay()
{
    Super::BeginPlay();
    if (auto *Material = LoadObject<UMaterialInterface>(
            nullptr, TEXT("/Game/Pinefall/Materials/M_StormSurface.M_StormSurface")))
        Wall->SetMaterial(0, Material);
}
void APFStormVisual::Tick(float Delta)
{
    Super::Tick(Delta);
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    bool Visible = Match && Match->Phase == EPFRoundPhase::Active;
    SetActorHiddenInGame(!Visible);
    if (!Visible)
        return;
    float Radius = FMath::Max(20.f, Match->SafeRadius());
    SetActorLocation(Match->SafeCenter() + FVector(0, 0, 6000));
    SetActorScale3D(FVector(Radius / 50, Radius / 50, 200));
}
