#include "PFVehicle.h"
#include "Components/BoxComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/DamageEvents.h"
#include "EngineUtils.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Net/UnrealNetwork.h"
#include "PFCharacter.h"
#include "PFWaterBody.h"

APFVehicle::APFVehicle()
{
    bReplicates = true;
    SetReplicateMovement(true);
    SetNetUpdateFrequency(30);
    SetNetCullDistanceSquared(FMath::Square(60000.f));
    PrimaryActorTick.bCanEverTick = true;
    PrimaryActorTick.bStartWithTickEnabled = false;
    Collision = CreateDefaultSubobject<UBoxComponent>(TEXT("Vehicle collision"));
    SetRootComponent(Collision);
    Collision->SetBoxExtent(FVector(200, 100, 100));
    Collision->SetCollisionProfileName(TEXT("BlockAllDynamic"));
    Collision->SetCollisionResponseToChannel(ECC_GameTraceChannel1, ECR_Block);
    Body = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Vehicle body"));
    Body->SetupAttachment(Collision);
    Body->SetCollisionEnabled(ECollisionEnabled::NoCollision);
}
void APFVehicle::BeginPlay()
{
    Super::BeginPlay();
    bProbe = FParse::Param(FCommandLine::Get(), TEXT("PFReserveProbe"));
    RefreshVisual();
    for (TActorIterator<APFWaterBody> It(GetWorld()); It; ++It)
        Waters.Add(*It);
}
void APFVehicle::RefreshVisual()
{
    Body->SetStaticMesh(LoadObject<UStaticMesh>(
        nullptr, bBoat ? TEXT("/Game/Pinefall/Art/Reserve/SM_RiverBoat.SM_RiverBoat")
                       : TEXT("/Game/Pinefall/Art/Reserve/SM_TrailTruck.SM_TrailTruck")));
    Body->SetRelativeLocation(FVector(0, 0, bBoat ? -55 : -110));
    Body->SetRelativeRotation(FRotator(0, 90, 0));
    Collision->SetBoxExtent(bBoat ? FVector(205, 88, 45) : FVector(200, 100, 100));
}
FString APFVehicle::Label() const
{
    return bBoat ? TEXT("Riverboat") : TEXT("Trail truck");
}
bool APFVehicle::Enter(APFCharacter *Player)
{
    if (!HasAuthority() || !IsValid(Player) || Driver || Player->Vehicle || !Player->CanFight() ||
        Health <= 0 ||
        FVector::DistSquared(Player->GetActorLocation(), GetActorLocation()) > FMath::Square(500.f))
        return false;
    FHitResult Hit;
    FCollisionQueryParams Q(SCENE_QUERY_STAT(PFEnterVehicle), false, Player);
    Q.AddIgnoredActor(this);
    if (GetWorld()->LineTraceSingleByChannel(Hit, Player->GetActorLocation(), GetActorLocation(),
                                             ECC_Visibility, Q))
        return false;
    Driver = Player;
    Collision->IgnoreActorWhenMoving(Player, true);
    Player->Vehicle = this;
    Player->OnRep_Vehicle();
    Control = FVector2D::ZeroVector;
    LastInput = GetWorld()->GetTimeSeconds();
    SetActorTickEnabled(true);
    ForceNetUpdate();
    Player->ForceNetUpdate();
    return true;
}
bool APFVehicle::Exit(bool Force)
{
    if (!HasAuthority() || !Driver)
        return false;
    APFCharacter *Player = Driver;
    FVector Place;
    bool Found = false;
    FCollisionQueryParams Q(SCENE_QUERY_STAT(PFVehicleExit), false, this);
    Q.AddIgnoredActor(Player);
    for (float Side : {1.f, -1.f})
    {
        FVector Candidate = GetActorLocation() + GetActorRightVector() * Side * 180 + FVector(0, 0, 150);
        FHitResult Ground;
        if (GetWorld()->LineTraceSingleByObjectType(Ground, Candidate, Candidate - FVector(0, 0, 650),
                                                    FCollisionObjectQueryParams(ECC_WorldStatic), Q))
            Candidate = Ground.ImpactPoint + FVector(0, 0, 100);
        if (!GetWorld()->OverlapBlockingTestByChannel(Candidate, FQuat::Identity, ECC_Pawn,
                                                      FCollisionShape::MakeCapsule(34, 90), Q))
        {
            Place = Candidate;
            Found = true;
            break;
        }
    }
    if (!Found && !Force)
        return false;
    if (!Found)
        Place = GetActorLocation() + FVector(0, 0, 240);
    Collision->IgnoreActorWhenMoving(Player, false);
    Driver = nullptr;
    Player->Vehicle = nullptr;
    Player->OnRep_Vehicle();
    Player->SetActorLocation(Place);
    Player->ForceNetUpdate();
    Control = FVector2D::ZeroVector;
    Speed = 0;
    SetActorTickEnabled(false);
    ForceNetUpdate();
    return true;
}
void APFVehicle::OnRep_Driver()
{
    SetActorTickEnabled(Driver != nullptr);
    if (Driver)
    {
        Driver->Vehicle = this;
        Driver->OnRep_Vehicle();
    }
}
void APFVehicle::Input(APFCharacter *Player, FVector2D Axes, bool Boost, bool Brake)
{
    if (!HasAuthority() || Driver != Player || !FMath::IsFinite(Axes.X) || !FMath::IsFinite(Axes.Y))
        return;
    Control = Axes.GetClampedToMaxSize(1);
    bBoost = Boost;
    bBrake = Brake;
    LastInput = GetWorld()->GetTimeSeconds();
}
void APFVehicle::Tick(float Delta)
{
    Super::Tick(Delta);
    if (!HasAuthority())
    {
        if (Driver && bHasNetPose)
            SetActorLocationAndRotation(FMath::VInterpTo(GetActorLocation(), NetLocation, Delta, 18),
                                        FMath::RInterpTo(GetActorRotation(), NetRotation, Delta, 18));
        return;
    }
    if (!Driver)
        return;
    if (Driver->Health <= 0 || Health <= 0)
    {
        Exit(true);
        return;
    }
    Delta = FMath::Min(Delta, .05f);
    if (GetWorld()->GetTimeSeconds() - LastInput > .3)
        Control = FVector2D::ZeroVector;
    float Throttle = Fuel > 0 ? Control.Y : 0, Top = bBoat ? 1600 : 2200;
    if (bBoost && Fuel > 0)
        Top *= 1.35f;
    float Target = Throttle * (Throttle < 0 ? Top * .36f : Top);
    Speed = FMath::FInterpConstantTo(Speed, Target, Delta,
                                     (FMath::Abs(Throttle) > .01f ? 950 : 550) * (bBrake ? 3 : 1));
    if (bBrake)
        Speed = FMath::FInterpConstantTo(Speed, 0.f, Delta, 2400);
    FRotator Rotation = GetActorRotation();
    Rotation.Yaw += Control.X * (bBoat ? 70 : 95) * Delta *
                    FMath::Clamp(FMath::Abs(Speed) / 500.f, 0.f, 1.f) * (Speed < 0 ? -1 : 1);
    FVector Move = FRotationMatrix(FRotator(0, Rotation.Yaw, 0)).GetUnitAxis(EAxis::X) * Speed * Delta;
    FVector Destination = GetActorLocation() + Move;
    FCollisionQueryParams Q(SCENE_QUERY_STAT(PFVehicleGround), false, this);
    Q.AddIgnoredActor(Driver);
    if (bBoat)
    {
        // Volumes register spatially; evaluate actual ellipse at the prospective hull.
        bool Wet = false;
        for (auto &Weak : Waters)
            if (auto *Water = Weak.Get())
                if (Water->ContainsWater(
                        FVector(Destination.X, Destination.Y, Water->SurfaceHeight() - 30)))
                {
                    Destination.Z = Water->SurfaceHeight() + 35;
                    Wet = true;
                    break;
                }
        if (!Wet)
        {
            if (bProbe && !bLoggedWater)
            {
                bLoggedWater = true;
                UE_LOG(LogTemp, Display, TEXT("PF_BOAT_NO_WATER count=%d at=%s"), Waters.Num(),
                       *Destination.ToCompactString());
                for (auto &Weak : Waters)
                    if (auto *Water = Weak.Get())
                        UE_LOG(LogTemp, Display, TEXT("PF_WATER_SHAPE at=%s extent=%s"),
                               *Water->GetActorLocation().ToCompactString(),
                               *Water->WaterBounds->GetScaledBoxExtent().ToCompactString());
            }
            Speed = 0;
            return;
        }
        Rotation.Pitch = FMath::Sin(GetWorld()->GetTimeSeconds() * 1.7f) * .7f;
        Rotation.Roll = FMath::Sin(GetWorld()->GetTimeSeconds() * 1.1f) * .8f;
    }
    else
    {
        FHitResult Hit;
        if (!GetWorld()->LineTraceSingleByObjectType(Hit, Destination + FVector(0, 0, 200),
                                                     Destination - FVector(0, 0, 750),
                                                     FCollisionObjectQueryParams(ECC_WorldStatic), Q) ||
            Hit.ImpactNormal.Z < .50f)
        {
            Speed *= .4f;
            return;
        }
        for (auto &Weak : Waters)
            if (auto *Water = Weak.Get())
                if (Water->ContainsWater(Hit.ImpactPoint))
                {
                    Speed = 0;
                    return;
                }
        float Z = Hit.ImpactPoint.Z + 110;
        Destination.Z = FMath::FInterpTo(GetActorLocation().Z, Z, Delta, 18);
        FVector Forward = FRotationMatrix(FRotator(0, Rotation.Yaw, 0)).GetUnitAxis(EAxis::X);
        FVector Surface = (Forward - FVector::DotProduct(Forward, Hit.ImpactNormal) * Hit.ImpactNormal)
                              .GetSafeNormal();
        FRotator Slope = FRotationMatrix::MakeFromXZ(Surface, Hit.ImpactNormal).Rotator();
        Rotation.Pitch = FMath::FInterpTo(Rotation.Pitch, Slope.Pitch, Delta, 7);
        Rotation.Roll = FMath::FInterpTo(Rotation.Roll, Slope.Roll, Delta, 7);
    }
    FHitResult Hit;
    SetActorLocationAndRotation(Destination, Rotation, true, &Hit);
    if (Hit.bBlockingHit)
    {
        if (bProbe && !bLoggedContact)
        {
            bLoggedContact = true;
            UE_LOG(LogTemp, Display, TEXT("PF_VEHICLE_BLOCK boat=%d target=%s at=%s"), bBoat,
                   *GetNameSafe(Hit.GetActor()), *Hit.ImpactPoint.ToCompactString());
        }
        Speed *= .35f;
    }
    if (FMath::Abs(Speed) > 30)
        Fuel = FMath::Max(0.f, Fuel - Delta * (bBoost ? .055f : .022f));
}
bool APFVehicle::Repair(APFCharacter *Player)
{
    if (!HasAuthority() || !Player || !Player->CanFight() ||
        FVector::DistSquared(Player->GetActorLocation(), GetActorLocation()) > 250000 || Driver ||
        (Health >= 500 && Fuel >= 100))
        return false;
    FHitResult Cover;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFVehicleService), false, Player);
    Query.AddIgnoredActor(this);
    if (GetWorld()->LineTraceSingleByChannel(Cover, Player->GetActorLocation() + FVector(0, 0, 35),
                                             GetActorLocation(), ECC_Visibility, Query))
        return false;
    Health = FMath::Min(500.f, Health + 150);
    Fuel = FMath::Min(100.f, Fuel + 35);
    ForceNetUpdate();
    return true;
}
float APFVehicle::TakeDamage(float Amount, FDamageEvent const &Event, AController *EventInstigator,
                             AActor *Causer)
{
    if (!HasAuthority() || !FMath::IsFinite(Amount) || Amount <= 0)
        return 0;
    float Applied = FMath::Min(Health, Amount);
    Health -= Applied;
    if (Health <= 0)
        Exit(true);
    ForceNetUpdate();
    return Applied;
}
void APFVehicle::EndPlay(EEndPlayReason::Type Reason)
{
    if (HasAuthority())
        Exit(true);
    Super::EndPlay(Reason);
}
void APFVehicle::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(APFVehicle, bBoat);
    DOREPLIFETIME(APFVehicle, Driver);
    DOREPLIFETIME(APFVehicle, Health);
    DOREPLIFETIME(APFVehicle, Fuel);
    DOREPLIFETIME(APFVehicle, Speed);
}

void APFVehicle::PostNetReceiveLocationAndRotation()
{
    FVector Previous = GetActorLocation();
    FRotator PreviousRotation = GetActorRotation();
    Super::PostNetReceiveLocationAndRotation();
    NetLocation = GetActorLocation();
    NetRotation = GetActorRotation();
    if (bHasNetPose && Driver && FVector::DistSquared(Previous, NetLocation) < FMath::Square(3000.f))
        SetActorLocationAndRotation(Previous, PreviousRotation);
    bHasNetPose = true;
}
