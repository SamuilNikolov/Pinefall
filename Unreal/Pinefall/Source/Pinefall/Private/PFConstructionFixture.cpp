#include "PFConstructionFixture.h"
#include "PFConstructionWorld.h"
#include "PFConstructionSubsystem.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "EngineUtils.h"
void PFConstructionFixture::Create(UWorld *World,FVector Origin)
{
    for(TActorIterator<AActor> It(World);It;++It)if(It->GetFName()==TEXT("PFHarvestProbeFixture"))return;
    FActorSpawnParameters Params;Params.Name=TEXT("PFHarvestProbeFixture");auto *Actor=World->SpawnActor<AActor>(Params);
    auto Group=[&](const TCHAR *Name,const TCHAR *Path,bool Collision)
    {
        auto *C=NewObject<UHierarchicalInstancedStaticMeshComponent>(Actor,Name);Actor->AddInstanceComponent(C);
        if(!Actor->GetRootComponent())Actor->SetRootComponent(C);else C->SetupAttachment(Actor->GetRootComponent());
        C->SetMobility(EComponentMobility::Static);C->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,Path));C->SetCollisionProfileName(Collision?TEXT("BlockAll"):TEXT("NoCollision"));C->SetVisibility(!Collision);C->RegisterComponent();return C;
    };
    auto *Visual=Group(TEXT("ProbeCanopy"),TEXT("/Game/Pinefall/Art/Scenery/SM_ForestFir.SM_ForestFir"),false);
    auto *Collider=Group(TEXT("ProbeTrunk"),TEXT("/Game/Pinefall/Art/World/SM_TrunkCollision.SM_TrunkCollision"),true);
    auto *Floor=Group(TEXT("ProbeFloor"),TEXT("/Engine/BasicShapes/Cube.Cube"),true);
    Floor->AddInstance(FTransform(FRotator::ZeroRotator,Origin-FVector(0,0,50),FVector(24,18,1)),true);
    FVector At=Origin+FVector(-400,300,0);Visual->AddInstance(FTransform(FRotator::ZeroRotator,At,FVector(.18)),true);
    Collider->AddInstance(FTransform(FRotator::ZeroRotator,At+FVector(0,0,170),FVector(.35,.35,3.4)),true);
    if(auto *Registry=World->GetSubsystem<UPFConstructionSubsystem>();Registry&&Registry->Manager)Registry->Manager->IndexHarvestables();
}
bool PFConstructionFixture::TreeBlocks(UWorld *World,FVector Origin)
{
    FHitResult Hit;FVector At=Origin+FVector(-400,300,170);
    return World->LineTraceSingleByObjectType(Hit,At-FVector(100,0,0),At+FVector(100,0,0),FCollisionObjectQueryParams(ECC_WorldStatic));
}
