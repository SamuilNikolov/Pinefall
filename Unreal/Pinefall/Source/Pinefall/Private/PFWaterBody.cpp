#include "PFWaterBody.h"
#include "Components/BoxComponent.h"
#include "Components/BrushComponent.h"
#include "Components/StaticMeshComponent.h"

APFWaterBody::APFWaterBody()
{
    PrimaryActorTick.bCanEverTick = false;
    bWaterVolume = true;
    bPhysicsOnContact = false;
    Priority = 5;
    FluidFriction = .3f;
    GetBrushComponent()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    WaterBounds = CreateDefaultSubobject<UBoxComponent>(TEXT("WaterBounds"));
    SetRootComponent(WaterBounds);
    WaterBounds->SetBoxExtent(FVector(4200, 6000, 1000));
    WaterBounds->SetCollisionProfileName(TEXT("OverlapAllDynamic"));
    WaterBounds->SetCollisionResponseToChannel(ECC_Visibility, ECR_Ignore);
    WaterBounds->SetCanEverAffectNavigation(false);
    Surface = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Surface"));
    Surface->SetupAttachment(WaterBounds);
    Surface->SetRelativeLocation(FVector(0, 0, 1000));
    Surface->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Surface->CastShadow = false;
    Surface->SetCanEverAffectNavigation(false);
}
bool APFWaterBody::ContainsWater(const FVector &Point) const
{
    FVector Delta = Point - GetActorLocation();
    FVector Extent = WaterBounds->GetScaledBoxExtent();
    return FMath::Square(Delta.X / Extent.X) + FMath::Square(Delta.Y / Extent.Y) <= 1 &&
           FMath::Abs(Delta.Z) <= Extent.Z;
}
bool APFWaterBody::IsOverlapInVolume(const USceneComponent &Test) const
{
    return ContainsWater(Test.GetComponentLocation());
}
