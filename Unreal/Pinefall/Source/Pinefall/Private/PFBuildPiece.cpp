#include "PFBuildPiece.h"
#include "PFBuildGeometry.h"
#include "PFConstructionSubsystem.h"
#include "PFConstructionWorld.h"
#include "PFConstructionComponent.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "ProceduralMeshComponent.h"
#include "Net/UnrealNetwork.h"
#include "Materials/MaterialInterface.h"
#include "Components/CapsuleComponent.h"
namespace
{
void Fill(UProceduralMeshComponent *Mesh,const FPFBuildMesh &Data,bool Collision)
{
    TArray<FLinearColor> Colors;TArray<FProcMeshTangent> Tangents;
    Mesh->ClearAllMeshSections();
    Mesh->CreateMeshSection_LinearColor(0,Data.Vertices,Data.Triangles,Data.Normals,Data.UV,Colors,Tangents,Collision);
}
}
APFBuildPiece::APFBuildPiece()
{
    bReplicates=true;SetReplicateMovement(false);SetNetUpdateFrequency(15);
    SetNetCullDistanceSquared(FMath::Square(150000.f));
    PrimaryActorTick.bCanEverTick=true;PrimaryActorTick.TickInterval=.1f;PrimaryActorTick.bStartWithTickEnabled=false;
    Geometry=CreateDefaultSubobject<UProceduralMeshComponent>(TEXT("Structural geometry"));SetRootComponent(Geometry);
    Geometry->bUseComplexAsSimpleCollision=true;Geometry->bUseAsyncCooking=false;
    Geometry->SetCollisionProfileName(TEXT("BlockAll"));Geometry->SetCachedMaxDrawDistance(60000);
    Door=CreateDefaultSubobject<UProceduralMeshComponent>(TEXT("Edited door"));Door->SetupAttachment(Geometry);
    Door->bUseComplexAsSimpleCollision=true;Door->bUseAsyncCooking=false;Door->SetCollisionProfileName(TEXT("BlockAll"));
}
float APFBuildPiece::Maximum() const { return Build.Request.Material==EPFBuildMaterial::Wood?150:Build.Request.Material==EPFBuildMaterial::Stone?300:500; }
void APFBuildPiece::Initialize(const FPFBuildState &State,bool Prediction,bool PreviewOnly)
{
    Build=State;bPrediction=Prediction;bPreviewOnly=PreviewOnly;Health=Maximum()*.2f;
    if(PreviewOnly)SetActorEnableCollision(false);
    if(Prediction){SetReplicates(false);Geometry->SetCastShadow(false);Door->SetCastShadow(false);}
    ApplyGeometry();
    if(HasAuthority()&&!Prediction)SetActorTickEnabled(true);
}
void APFBuildPiece::BeginPlay()
{
    Super::BeginPlay();ApplyGeometry();if(!bPrediction)OnRep_Build();
    if(!bPrediction)
        if(auto *World=GetWorld()->GetSubsystem<UPFConstructionSubsystem>();World&&World->Manager)World->Manager->RegisterPiece(this);
}
void APFBuildPiece::OnRep_Build()
{
    ApplyGeometry();
    if(auto *Registry=GetWorld()->GetSubsystem<UPFConstructionSubsystem>();Registry&&Registry->Manager)Registry->Manager->RegisterPiece(this);
    if(auto *PC=GetWorld()->GetFirstPlayerController())
        if(auto *Pawn=PC->GetPawn().Get())
            if(auto *Tools=Pawn->FindComponentByClass<UPFConstructionComponent>())Tools->ResolvePrediction(this);
}
void APFBuildPiece::OnRep_Condition()
{
    Geometry->SetCustomPrimitiveDataFloat(3,1-Health/FMath::Max(1.f,Maximum()));
}
void APFBuildPiece::ApplyGeometry()
{
    FTransform Target=PFBuildGrid::Transform(Build.Request);
    if(!GetActorTransform().Equals(Target,.01f))
    {Geometry->SetMobility(EComponentMobility::Movable);SetActorTransform(Target);}
    uint32 Hash=HashCombine(HashCombine(uint8(Build.Request.Kind),Build.Mask),Build.RampPattern);
    if(Hash!=GeometryHash)
    {
        GeometryHash=Hash;
        Fill(Geometry,PFBuildGeometry::Generate(Build.Request.Kind,Build.Mask,Build.RampPattern),!bPreviewOnly);
    }
    if(SurfaceMaterial!=uint8(Build.Request.Material))
    {
        SurfaceMaterial=uint8(Build.Request.Material);
        const TCHAR *Names[]={TEXT("Wood"),TEXT("Stone"),TEXT("Metal")};
        auto *Surface=LoadObject<UMaterialInterface>(nullptr,*FString::Printf(TEXT("/Game/Pinefall/Materials/Reserve/M_Reserve_%s.M_Reserve_%s"),Names[uint8(Build.Request.Material)],Names[uint8(Build.Request.Material)]));
        Geometry->SetMaterial(0,Surface);Door->SetMaterial(0,Surface);
    }
    int32 Column=0;bool HasDoor=Build.Request.Kind==EPFBuildKind::Wall&&PFBuildGrid::IsDoor(Build.Mask,Column);
    if(HasDoor)
    {
        if(Door->GetNumSections()==0)
        {FPFBuildMesh Slab;Slab.Box({0,PFBuildGrid::Tile/6,PFBuildGrid::Storey/3},{12,PFBuildGrid::Tile/3,PFBuildGrid::Storey*2/3});Fill(Door,Slab,!bPreviewOnly);}
        Door->SetRelativeLocation({256,-256+Column*PFBuildGrid::Tile/3,0});
        Door->SetRelativeRotation(FRotator(0,Build.bDoorOpen?95:0,0));
    }
    else if(Door->GetNumSections()>0)Door->ClearAllMeshSections();
    Door->SetVisibility(HasDoor);Door->SetCollisionEnabled(HasDoor?ECollisionEnabled::QueryAndPhysics:ECollisionEnabled::NoCollision);
    Geometry->CanCharacterStepUpOn=ECB_Yes;
    Geometry->SetWalkableSlopeOverride(FWalkableSlopeOverride(Build.Request.Kind==EPFBuildKind::Ramp?WalkableSlope_Increase:WalkableSlope_Default,50.f));
    // A moving preview must stay movable: switching back to Static each frame
    // repeatedly re-registers its render/physics state. Placed predictions stay
    // static so movement bases never send unknown local NetGUIDs to the server.
    Geometry->SetMobility(bPreviewOnly?EComponentMobility::Movable:EComponentMobility::Static);
    OnRep_Condition();
}
bool APFBuildPiece::SetEdit(uint16 Mask,uint8 RampPattern,bool DoorOpen)
{
    if(!HasAuthority()||Health<=0||!PFBuildGrid::ValidEdit(Build.Request.Kind,Mask,RampPattern))return false;
    FlushNetDormancy();Build.Mask=Mask;Build.RampPattern=RampPattern;Build.bDoorOpen=DoorOpen;Build.Revision++;
    OnRep_Build();ForceNetUpdate();return true;
}
bool APFBuildPiece::ToggleDoor(APawn *User)
{
    int32 Column;
    if(!HasAuthority()||!User||FVector::DistSquared(User->GetActorLocation(),Center())>FMath::Square(350.f)||!PFBuildGrid::IsDoor(Build.Mask,Column))return false;
    if(Build.bDoorOpen)
    {
        FVector CloseCenter=GetActorTransform().TransformPosition({256,-256+(Column+.5f)*PFBuildGrid::Tile/3,128});
        FCollisionQueryParams Q(SCENE_QUERY_STAT(PFDoorClose),false,this);
        if(GetWorld()->OverlapBlockingTestByChannel(CloseCenter,GetActorQuat(),ECC_Pawn,FCollisionShape::MakeBox(FVector(12,PFBuildGrid::Tile/6,120)),Q))return false;
    }
    FlushNetDormancy();Build.bDoorOpen=!Build.bDoorOpen;ApplyGeometry();ForceNetUpdate();return true;
}
FVector APFBuildPiece::Center() const { return GetActorTransform().TransformPosition(Build.Request.Kind==EPFBuildKind::Wall?FVector(256,0,192):FVector(0,0,Build.Request.Kind==EPFBuildKind::Floor?0:128)); }
void APFBuildPiece::Tick(float Delta)
{
    Super::Tick(Delta);if(!HasAuthority()||bPrediction)return;
    Age+=Delta;float Duration=Build.Request.Material==EPFBuildMaterial::Wood?8:Build.Request.Material==EPFBuildMaterial::Stone?12:16;
    Health=FMath::Min(Maximum(),Health+Maximum()*.8f/Duration*Delta);
    if(Age>=Duration){bComplete=true;SetActorTickEnabled(false);SetNetDormancy(DORM_DormantAll);}
}
float APFBuildPiece::TakeDamage(float Amount,FDamageEvent const &Event,AController *DamageInstigator,AActor *Causer)
{
    if(!HasAuthority()||bPrediction||!FMath::IsFinite(Amount)||Amount<=0||Health<=0)return 0;
    FlushNetDormancy();float Applied=FMath::Min(Amount,Health);Health-=Applied;OnRep_Condition();ForceNetUpdate();
    if(Health<=0)Destroy();return Applied;
}
void APFBuildPiece::EndPlay(EEndPlayReason::Type Reason)
{
    if(!bPrediction||GetNetMode()==NM_Client)if(auto *World=GetWorld()->GetSubsystem<UPFConstructionSubsystem>();World&&World->Manager)World->Manager->UnregisterPiece(this);
    Super::EndPlay(Reason);
}
void APFBuildPiece::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);DOREPLIFETIME(APFBuildPiece,Build);DOREPLIFETIME(APFBuildPiece,Health);DOREPLIFETIME(APFBuildPiece,bComplete);
}
