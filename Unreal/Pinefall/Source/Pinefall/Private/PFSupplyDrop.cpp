#include "PFSupplyDrop.h"
#include "PFCharacter.h"
#include "PFMatchState.h"
#include "Components/StaticMeshComponent.h"
#include "Components/SphereComponent.h"
#include "Engine/StaticMesh.h"
#include "Net/UnrealNetwork.h"
#include "TimerManager.h"
#include "Materials/MaterialInstanceDynamic.h"

APFSupplyDrop::APFSupplyDrop()
{
    bAlwaysRelevant = true; // At most six per round; late joiners need the same descent clock.
    SetNetUpdateFrequency(2);
    PrimaryActorTick.bCanEverTick = true;
    PrimaryActorTick.TickInterval = .033f;
    Canopy = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Supply canopy"));
    Canopy->SetupAttachment(RootComponent);
    Canopy->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Canopy->SetCastShadow(false);
    Interaction->SetSphereRadius(100);
}
bool APFSupplyDrop::HasLanded() const
{
    const auto *State = GetWorld()->GetGameState<APFMatchState>();
    return State && State->GetServerWorldTimeSeconds() >= LandingAt;
}
void APFSupplyDrop::BeginPlay()
{
    Super::BeginPlay();
    RefreshVisual();
    if(HasAuthority())
        GetWorldTimerManager().SetTimer(LandingTimer,this,&APFSupplyDrop::Land,
            FMath::Max(.01f,float(LandingAt-GetWorld()->GetTimeSeconds())),false);
}
void APFSupplyDrop::RefreshVisual()
{
    Body->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Game/Pinefall/Art/Reserve/SM_ServiceCase.SM_ServiceCase")));
    Body->SetRelativeScale3D(FVector(3));
    Body->SetCullDistance(0);
    Body->SetVisibility(!bOpened);
    Interaction->SetCollisionEnabled(bOpened ? ECollisionEnabled::NoCollision : ECollisionEnabled::QueryOnly);
    Detail->SetVisibility(false);
    Beacon->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Cylinder.Cylinder")));
    Beacon->SetRelativeScale3D(FVector(.025,.025,2));
    if(!BeaconMaterial)
        if(auto *Base=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/M_LootBeacon.M_LootBeacon")))
            BeaconMaterial=UMaterialInstanceDynamic::Create(Base,this);
    if(BeaconMaterial)
    {
        BeaconMaterial->SetVectorParameterValue(TEXT("Tint"),FLinearColor(1,.65,.15));
        BeaconMaterial->SetScalarParameterValue(TEXT("Strength"),1.f);
        Beacon->SetMaterial(0,BeaconMaterial);
    }
    Beacon->SetVisibility(!bOpened);
    Canopy->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Sphere.Sphere")));
    Canopy->SetRelativeScale3D(FVector(5,5,.8));
    Canopy->SetVisibility(!bOpened&&!HasLanded());
    if(bOpened)SetActorTickEnabled(false);
}
void APFSupplyDrop::Tick(float Delta)
{
    Super::Tick(Delta);
    const auto *State=GetWorld()->GetGameState<APFMatchState>();
    if(!State||bOpened)return;
    const float Remaining=FMath::Max(0.f,float(LandingAt-State->GetServerWorldTimeSeconds()));
    const float Height=FMath::Min(22000.f,Remaining/16.f*22000.f);
    Body->SetRelativeLocation(FVector(0,0,Height+2));
    Canopy->SetRelativeLocation(FVector(0,0,Height+400));
    Canopy->SetVisibility(Remaining>0);
    Beacon->SetRelativeLocation(FVector(0,0,Height+100));
    if(Remaining<=0)SetActorTickEnabled(false);
}
void APFSupplyDrop::Land()
{
    if(auto *State=GetWorld()->GetGameState<APFMatchState>();State&&State->Phase==EPFRoundPhase::Active&&!bOpened)
    {
        State->Announcement=TEXT("Supply crate landed. Rare gear is up for grabs.");
        State->SupplyAnnouncement=State->Announcement;State->SupplyAnnouncementUntil=GetWorld()->GetTimeSeconds()+6;
        State->ForceNetUpdate();
    }
}
void APFSupplyDrop::RemovePin()
{
    if(HasAuthority())if(auto *State=GetWorld()->GetGameState<APFMatchState>())
    {
        State->SupplyDropPoints.RemoveAll([&](const FVector_NetQuantize &Point){return FVector::DistSquared(Point,GetActorLocation())<100;});
        State->ForceNetUpdate();
    }
}
bool APFSupplyDrop::Open(APFCharacter *Player)
{
    auto *State=GetWorld()->GetGameState<APFMatchState>();
    if(!HasAuthority()||bOpened||!HasLanded()||!IsValid(Player)||!Player->CanFight()||Player->Vehicle||
        !State||State->Phase!=EPFRoundPhase::Active||FVector::DistSquared(Player->GetActorLocation(),GetActorLocation())>160000)return false;
    FHitResult Cover;FCollisionQueryParams Query(SCENE_QUERY_STAT(PFSupplyOpen),false,Player);Query.AddIgnoredActor(this);
    if(GetWorld()->LineTraceSingleByChannel(Cover,Player->GetActorLocation()+FVector(0,0,45),GetActorLocation()+FVector(0,0,20),ECC_Visibility,Query))return false;
    bOpened=true; // Lock before releasing any reward; simultaneous requests cannot duplicate it.
    RemovePin();RefreshVisual();ForceNetUpdate();
    const FName Weapon=FMath::RandBool()?FName(TEXT("Ridgeline")):FName(TEXT("ServiceNine"));
    for(int32 I=0;I<6;I++)
    {
        FPFItemStack Stack;Stack.Count=1;
        switch(I)
        {
        case 0:Stack.Kind=EPFItemKind::Weapon;Stack.WeaponId=Weapon;Stack.Magazine=Weapon==TEXT("Ridgeline")?5:12;Stack.Rarity=FMath::FRand()<.22f?4:3;break;
        case 1:Stack.Kind=EPFItemKind::Ammo;Stack.WeaponId=Weapon;Stack.Count=Weapon==TEXT("Ridgeline")?15:36;break;
        case 2:Stack.Kind=EPFItemKind::Medkit;Stack.Count=2;break;
        case 3:Stack.Kind=EPFItemKind::ArmorPlate;Stack.Count=3;break;
        case 4:Stack.Kind=EPFItemKind::HeavyVest;Stack.Rarity=3;break;
        default:Stack.Kind=EPFItemKind::Food;Stack.Count=3;break;
        }
        const float Angle=I*2*PI/6;
        FVector Point=GetActorLocation()+FVector(FMath::Cos(Angle)*160,FMath::Sin(Angle)*160,20);
        FHitResult Ground;FCollisionQueryParams GroundQuery(SCENE_QUERY_STAT(PFSupplyLoot),false,this);
        if(GetWorld()->LineTraceSingleByObjectType(Ground,Point+FVector(0,0,100),Point-FVector(0,0,250),FCollisionObjectQueryParams(ECC_WorldStatic),GroundQuery))Point=Ground.ImpactPoint+FVector(0,0,3);
        FActorSpawnParameters Params;Params.Owner=this;Params.SpawnCollisionHandlingOverride=ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        if(auto *Loot=GetWorld()->SpawnActor<APFWorldPickup>(Point,FRotator::ZeroRotator,Params))Loot->Initialize(Stack);
    }
    State->Announcement=TEXT("Supply crate claimed. Watch the approaches.");
    State->SupplyAnnouncement=State->Announcement;State->SupplyAnnouncementUntil=GetWorld()->GetTimeSeconds()+6;
    State->ForceNetUpdate();SetLifeSpan(5);return true;
}
void APFSupplyDrop::EndPlay(const EEndPlayReason::Type Reason)
{
    GetWorldTimerManager().ClearTimer(LandingTimer);RemovePin();Super::EndPlay(Reason);
}
void APFSupplyDrop::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);DOREPLIFETIME(APFSupplyDrop,LandingAt);DOREPLIFETIME(APFSupplyDrop,bOpened);
}
