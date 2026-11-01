#include "PFWaterBody.h"
#include "Components/BoxComponent.h"
#include "Components/BrushComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInterface.h"

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
void APFWaterBody::FitSurface()
{
    // Interchange imports the mesh inside a StaticMeshes folder, not at the
    // import folder itself. Repair maps saved with the old, empty mesh path.
    if (!Surface->GetStaticMesh())
        Surface->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,
            TEXT("/Game/Pinefall/Art/World/SM_Lake/StaticMeshes/SM_Lake.SM_Lake")));
    if (UStaticMesh *Mesh = Surface->GetStaticMesh())
    {
        const FVector Extent = WaterBounds->GetUnscaledBoxExtent();
        const FVector MeshSize = Mesh->GetBoundingBox().GetSize();
        if (MeshSize.X > 0 && MeshSize.Y > 0)
            Surface->SetRelativeScale3D(FVector(2 * Extent.X / MeshSize.X, 2 * Extent.Y / MeshSize.Y, 1));
        Surface->SetRelativeLocation(FVector(0, 0, Extent.Z));
    }
    Surface->SetCachedMaxDrawDistance(0);
}
void APFWaterBody::OnConstruction(const FTransform &Transform)
{
    Super::OnConstruction(Transform);
    FitSurface();
}
void APFWaterBody::BeginPlay()
{
    Super::BeginPlay();
    FitSurface();
}
float APFWaterBody::SurfaceHeight() const
{
    return Surface->GetComponentLocation().Z;
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
