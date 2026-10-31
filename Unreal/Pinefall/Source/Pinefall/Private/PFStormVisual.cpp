#include "PFStormVisual.h"
#include "PFMatchState.h"
#include "Components/StaticMeshComponent.h"
#include "Components/PostProcessComponent.h"
#include "GameFramework/PlayerController.h"
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
    Atmosphere=CreateDefaultSubobject<UPostProcessComponent>(TEXT("Storm atmosphere"));Atmosphere->SetupAttachment(Wall);
    Atmosphere->bUnbound=true;Atmosphere->Priority=50;Atmosphere->BlendWeight=0;
    Atmosphere->Settings.bOverride_SceneColorTint=true;Atmosphere->Settings.SceneColorTint=FLinearColor(.62,.37,.95);
    Atmosphere->Settings.bOverride_ColorSaturation=true;Atmosphere->Settings.ColorSaturation=FVector4(.72,.70,.85,1);
    Atmosphere->Settings.bOverride_VignetteIntensity=true;Atmosphere->Settings.VignetteIntensity=.4;
    SetActorHiddenInGame(true);
}
void APFStormVisual::BeginPlay()
{
    Super::BeginPlay();
    if(auto *Mesh=LoadObject<UStaticMesh>(nullptr,TEXT("/Game/Pinefall/Art/Construction/SM_StormWall.SM_StormWall")))Wall->SetStaticMesh(Mesh);
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
    FVector View;FRotator Rotation;
    if(auto *PC=GetWorld()->GetFirstPlayerController())PC->GetPlayerViewPoint(View,Rotation);
    float Weight=Visible&&Match->Outside(View)?.82f:0.f;
    Atmosphere->BlendWeight=FMath::FInterpTo(Atmosphere->BlendWeight,Weight,Delta,4);
    if (!Visible)
        return;
    float Radius = FMath::Max(20.f, Match->SafeRadius());
    SetActorLocation(Match->SafeCenter() + FVector(0, 0, -4000));
    SetActorScale3D(FVector(Radius / 100, Radius / 100, 1000));
}
