#include "PFSupplyDrop.h"
#include "PFCharacter.h"
#include "PFVehicle.h"
#include "PFWaterBody.h"
#include "PFReserveWorld.h"
#include "EngineUtils.h"
#include "Components/AudioComponent.h"
#include "Sound/SoundBase.h"
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
    PrimaryActorTick.TickInterval = 0; // Smooth cosmetic flight; ticking stops at landing.
    Canopy = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Supply canopy"));
    Canopy->SetupAttachment(RootComponent);
    Canopy->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Canopy->SetCastShadow(false);
    Interaction->SetSphereRadius(100);
    Aircraft=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Delivery aircraft"));Aircraft->SetupAttachment(RootComponent);Aircraft->SetCollisionEnabled(ECollisionEnabled::NoCollision);Aircraft->SetCastShadow(false);
    FlightAudio=CreateDefaultSubobject<UAudioComponent>(TEXT("Aircraft rumble"));FlightAudio->SetupAttachment(Aircraft);FlightAudio->bAutoActivate=false;FlightAudio->bOverrideAttenuation=true;
    FlightAudio->AttenuationOverrides.bAttenuate=true;FlightAudio->AttenuationOverrides.bSpatialize=true;FlightAudio->AttenuationOverrides.AttenuationShapeExtents=FVector(2000);FlightAudio->AttenuationOverrides.FalloffDistance=80000;FlightAudio->SetVolumeMultiplier(.35f);
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
    Aircraft->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Game/Pinefall/Art/Supplies/SM_CargoPlane.SM_CargoPlane")));
    FlightAudio->SetSound(LoadObject<USoundBase>(nullptr,TEXT("/Game/Pinefall/Audio/SupplyPropeller.SupplyPropeller")));
    Aircraft->SetRelativeRotation(FRotator(0,90,0));Aircraft->SetVisibility(!bOpened&&!HasLanded());
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
    Canopy->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Game/Pinefall/Art/Supplies/SM_SupplyCanopy.SM_SupplyCanopy")));
    Canopy->SetRelativeScale3D(FVector(1));
    Canopy->SetVisibility(!bOpened&&!HasLanded());
    if(bOpened){FlightAudio->Stop();bFlightSoundActive=false;SetActorTickEnabled(false);}
}
void APFSupplyDrop::Tick(float Delta)
{
    Super::Tick(Delta);
    const auto *State=GetWorld()->GetGameState<APFMatchState>();
    if(!State||bOpened)return;
    const float Remaining=PresentationRemaining>=0?PresentationRemaining:FMath::Max(0.f,float(LandingAt-State->GetServerWorldTimeSeconds()));
    const float Height=FMath::Min(22000.f,Remaining/16.f*22000.f);
    const float Elapsed=26-Remaining;
    Aircraft->SetRelativeLocation(FVector((Elapsed-5)*9000,0,22500));Aircraft->SetVisibility(Elapsed>=0&&Elapsed<=10);
    const bool Flying=Elapsed>=0&&Elapsed<=10;
    if(Flying!=bFlightSoundActive&&GetNetMode()!=NM_DedicatedServer)
    {bFlightSoundActive=Flying;if(Flying)FlightAudio->Play();else FlightAudio->Stop();}
    Body->SetVisibility(Elapsed>=10);Beacon->SetVisibility(Elapsed>=10);
    Body->SetRelativeLocation(FVector(0,0,Height+2));
    Canopy->SetRelativeLocation(FVector(0,0,Height+400));
    Canopy->SetVisibility(Remaining>0&&Elapsed>=10);
    Beacon->SetRelativeLocation(FVector(0,0,Height+100));
    if(Remaining<=0){if(HasAuthority())Land();SetActorTickEnabled(false);}
}
void APFSupplyDrop::Land()
{
    if(bLandedAnnounced)return;
    if(auto *State=GetWorld()->GetGameState<APFMatchState>();State&&State->Phase==EPFRoundPhase::Active&&!bOpened)
    {
        bLandedAnnounced=true;State->NarrateSupply(TEXT("Landed"));
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
        !State||State->Phase!=EPFRoundPhase::Active||FVector::DistSquared(Player->GetActorLocation(),GetActorLocation())>250000)return false;
    FHitResult Cover;FCollisionQueryParams Query(SCENE_QUERY_STAT(PFSupplyOpen),false,Player);Query.AddIgnoredActor(this);
    if(GetWorld()->LineTraceSingleByChannel(Cover,Player->GetActorLocation()+FVector(0,0,45),GetActorLocation()+FVector(0,0,20),ECC_Visibility,Query))return false;
    Land();
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
    ReleaseSpecial(FMath::FRand());State->NarrateSupply(TEXT("Claimed"));
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

int32 APFSupplyDrop::ReleaseSpecial(float Roll)
{
    if(!HasAuthority()||!FMath::IsFinite(Roll)||Roll<0||Roll>=.60f)return 0;
    if(Roll<.35f)
        for(int32 I=0;I<48;I++)
        {
            const float A=I*2.39996f,R=500+I*85.f;
            FVector Point=GetActorLocation()+FVector(FMath::Cos(A)*R,FMath::Sin(A)*R,0);
            auto *State=GetWorld()->GetGameState<APFMatchState>();if(!State||FMath::Abs(Point.X)>State->ArenaHalfExtent-500||FMath::Abs(Point.Y)>State->ArenaHalfExtent-500)continue;
            FHitResult Ground;FCollisionQueryParams Q(SCENE_QUERY_STAT(PFSupplyBike),false,this);
            if(!GetWorld()->LineTraceSingleByObjectType(Ground,Point+FVector(0,0,400),Point-FVector(0,0,700),FCollisionObjectQueryParams(ECC_WorldStatic),Q)||Ground.ImpactNormal.Z<.95f)continue;
            bool Wet=false;for(TActorIterator<APFWaterBody> It(GetWorld());It;++It)Wet|=It->ContainsWater(Ground.ImpactPoint);
            if(Wet)continue;Point=Ground.ImpactPoint+FVector(0,0,75);
            const FVector Forward=(FVector::ForwardVector-Ground.ImpactNormal*FVector::DotProduct(FVector::ForwardVector,Ground.ImpactNormal)).GetSafeNormal();
            const FRotator Rotation=FRotationMatrix::MakeFromXZ(Forward,Ground.ImpactNormal).Rotator();
            if(GetWorld()->OverlapBlockingTestByChannel(Point,Rotation.Quaternion(),ECC_Pawn,FCollisionShape::MakeBox(FVector(130,50,65)),Q))continue;
            const FTransform Transform(Rotation,Point);
            auto *Bike=GetWorld()->SpawnActorDeferred<APFVehicle>(APFVehicle::StaticClass(),Transform,nullptr,nullptr,ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
            if(!Bike)break;
            Bike->bMotorcycle=true;Bike->FinishSpawning(Transform);Bike->Fuel=85;Bike->ForceNetUpdate();
            for(TActorIterator<APFReserveWorld> It(GetWorld());It;++It){It->Vehicles.Add(Bike);break;}
            UE_LOG(LogTemp,Display,TEXT("PF_SUPPLY_SPECIAL motorcycle"));return 1;
        }
    // Obstructed vehicle delivery always falls back to a usable survey receiver.
    FPFItemStack Stack;Stack.Kind=EPFItemKind::SurveyBeacon;Stack.Count=1;Stack.Rarity=3;
    FActorSpawnParameters Params;Params.Owner=this;Params.SpawnCollisionHandlingOverride=ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
    FVector ReceiverPoint=GetActorLocation()+FVector(170,170,3);
    FHitResult Ground;FCollisionQueryParams Q(SCENE_QUERY_STAT(PFSupplyReceiver),false,this);
    if(GetWorld()->LineTraceSingleByObjectType(Ground,ReceiverPoint+FVector(0,0,200),ReceiverPoint-FVector(0,0,500),FCollisionObjectQueryParams(ECC_WorldStatic),Q))
        ReceiverPoint=Ground.ImpactPoint+FVector(0,0,3);
    for(TActorIterator<APFWaterBody> It(GetWorld());It;++It)if(It->ContainsWater(ReceiverPoint-FVector(0,0,3)))ReceiverPoint.Z=It->SurfaceHeight()+3;
    if(auto *Loot=GetWorld()->SpawnActor<APFWorldPickup>(ReceiverPoint,FRotator::ZeroRotator,Params))
    {Loot->Initialize(Stack);UE_LOG(LogTemp,Display,TEXT("PF_SUPPLY_SPECIAL survey"));return 2;}
    return 0;
}
