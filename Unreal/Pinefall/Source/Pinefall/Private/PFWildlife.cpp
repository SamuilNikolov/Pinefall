#include "PFWildlife.h"
#include "PFCharacter.h"
#include "PFMatchState.h"
#include "PFInventoryComponent.h"
#include "PFWorldPickup.h"
#include "Animation/AnimSequence.h"
#include "Components/CapsuleComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Engine/SkeletalMesh.h"
#include "Engine/DamageEvents.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/PlayerController.h"
#include "Materials/MaterialInterface.h"
#include "Net/UnrealNetwork.h"
#include "TimerManager.h"
#include "Kismet/GameplayStatics.h"
#include "Sound/SoundBase.h"
#include "Sound/SoundAttenuation.h"

APFWildlife::APFWildlife()
{
    PrimaryActorTick.bCanEverTick = true;
    bReplicates = true;
    SetReplicateMovement(true);
    SetNetUpdateFrequency(12);
    SetMinNetUpdateFrequency(3);
    SetNetCullDistanceSquared(FMath::Square(22000.f));
    GetCapsuleComponent()->InitCapsuleSize(40, 70);
    GetCapsuleComponent()->SetCollisionResponseToChannel(ECC_Visibility, ECR_Ignore);
    GetMesh()->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
    GetMesh()->SetCollisionResponseToAllChannels(ECR_Ignore);
    GetMesh()->SetCollisionResponseToChannel(ECC_Visibility, ECR_Block);
    // The authority needs current bones even when its rendering camera looks away.
    GetMesh()->VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption::AlwaysTickPoseAndRefreshBones;
    GetMesh()->bEnableUpdateRateOptimizations = true;
    GetMesh()->SetRelativeRotation(FRotator(0, -90, 0));
    AutoPossessAI = EAutoPossessAI::PlacedInWorldOrSpawned;
    GetCharacterMovement()->MaxWalkSpeed = 180;
    GetCharacterMovement()->bOrientRotationToMovement = true;
    GetCharacterMovement()->RotationRate = FRotator(0, 210, 0);
    GetCharacterMovement()->MaxStepHeight = 42;
    GetCharacterMovement()->bRunPhysicsWithNoController = true;
}
void APFWildlife::BeginPlay()
{
    Super::BeginPlay();
    ApplySpecies();
    ApplyRage();
    ApplyMotion();
    SetActorTickEnabled(HasAuthority());
    UE_LOG(LogTemp, Display, TEXT("PF_FAUNA_SPAWN species=%s role=%d mesh=%s clips=%d"), *Species.ToString(),
           int32(GetLocalRole()), *GetNameSafe(GetMesh()->GetSkeletalMeshAsset()), Clips.Num());
    Goal = GetActorLocation();
    NextWander = GetWorld()->GetTimeSeconds() + FMath::FRandRange(1.f, 4.f);
    NextVoice = GetWorld()->GetTimeSeconds() + FMath::FRandRange(5.f, 20.f);
    VoiceAttenuation = NewObject<USoundAttenuation>(this);
    VoiceAttenuation->Attenuation.bSpatialize = true;
    VoiceAttenuation->Attenuation.AttenuationShapeExtents = FVector(150);
    VoiceAttenuation->Attenuation.FalloffDistance = 6500;
}
void APFWildlife::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    if (HasAuthority() && Health > 0 && bLocomoting)
        AddMovementInput(MoveDirection, 1, true);
}
void APFWildlife::ApplySpecies()
{
    const FString Folder = TEXT("/Game/Pinefall/Art/Fauna/") + Species.ToString() + TEXT("/");
    if (auto *Model = LoadObject<USkeletalMesh>(
            nullptr, *(Folder + Species.ToString() + TEXT(".") + Species.ToString())))
    {
        GetMesh()->SetSkeletalMesh(Model);
        const float Height = Model->GetBounds().BoxExtent.Z * 2;
        const float Desired = Species == TEXT("Fox")      ? 55
                              : Species == TEXT("Wolf")   ? 90
                              : Species == TEXT("Bull")   ? 145
                              : Species == TEXT("Horse")  ? 165
                              : Species == TEXT("Alpaca") ? 135
                                                          : 140;
        ModelScale = Desired / FMath::Max(Height, 1.f);
        GetCapsuleComponent()->SetCapsuleSize(FMath::Clamp(Desired * .28f, 22.f, 50.f),
                                              FMath::Max(Desired * .5f, 35.f));
        float Bottom = Model->GetBounds().Origin.Z - Model->GetBounds().BoxExtent.Z;
        GetMesh()->SetRelativeLocation(
            FVector(0, 0, -GetCapsuleComponent()->GetUnscaledCapsuleHalfHeight() - Bottom * ModelScale));
        BaseHalfHeight = GetCapsuleComponent()->GetUnscaledCapsuleHalfHeight();
        BaseRadius = GetCapsuleComponent()->GetUnscaledCapsuleRadius();
        BaseMeshZ = GetMesh()->GetRelativeLocation().Z;
    }
    Clips.Reset();
    const bool Canid = Species == TEXT("Wolf") || Species == TEXT("Fox");
    const TCHAR *Names[] = {TEXT("Idle"), TEXT("Walk"), TEXT("Gallop"),
                            Canid ? TEXT("Attack") : TEXT("Attack_Headbutt"), TEXT("Death")};
    for (const TCHAR *Name : Names)
        Clips.Add(LoadObject<UAnimSequence>(nullptr, *(Folder + Name + TEXT(".") + Name)));
    const FString Call = Canid ? TEXT("Canid") : Species == TEXT("Alpaca") ? TEXT("Goat") : TEXT("Elk");
    Voice = LoadObject<USoundBase>(nullptr, *(TEXT("/Game/Pinefall/Audio/") + Call + TEXT(".") + Call));
    ApplyRage();
    ApplyMotion();
}
void APFWildlife::ApplyRage()
{
    GetMesh()->SetRelativeScale3D(FVector(ModelScale * (bAlpha ? 1.6f : 1.f)));
    float Factor = bAlpha ? 1.6f : 1.f;
    float Lift = BaseHalfHeight * Factor - GetCapsuleComponent()->GetUnscaledCapsuleHalfHeight();
    GetCapsuleComponent()->SetCapsuleSize(BaseRadius * Factor, BaseHalfHeight * Factor);
    GetMesh()->SetRelativeLocation(FVector(0, 0, BaseMeshZ * Factor));
    if (HasAuthority() && Lift > 1)
        SetActorLocation(GetActorLocation() + FVector(0, 0, Lift), false);
    GetMesh()->SetOverlayMaterial(
        bEnraged
            ? LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/Pinefall/Materials/M_WildRage.M_WildRage"))
            : nullptr);
}
void APFWildlife::MulticastCall_Implementation()
{
    if (Voice && GetNetMode() != NM_DedicatedServer)
        UGameplayStatics::PlaySoundAtLocation(this, Voice, GetActorLocation(), .32f,
                                              bAlpha ? .8f : FMath::FRandRange(.93f, 1.06f), 0,
                                              VoiceAttenuation);
}
void APFWildlife::ApplyMotion()
{
    const int32 Index = static_cast<int32>(Motion);
    if (Clips.IsValidIndex(Index) && Clips[Index])
        GetMesh()->PlayAnimation(Clips[Index],
                                 Motion != EPFWildlifeMotion::Attack && Motion != EPFWildlifeMotion::Dead);
    UE_LOG(LogTemp, Verbose, TEXT("PF_FAUNA_MOTION species=%s state=%d"), *Species.ToString(), Index);
}
void APFWildlife::SetMotion(EPFWildlifeMotion Value)
{
    if (Motion == Value)
        return;
    Motion = Value;
    ApplyMotion();
    ForceNetUpdate();
}
bool APFWildlife::ClearBite(APFCharacter *Player) const
{
    if (!IsValid(Player) || Player->Health <= 0 ||
        FVector::DistSquared(GetActorLocation(), Player->GetActorLocation()) >
            FMath::Square(bAlpha ? 270.f : 190.f))
        return false;
    FHitResult Hit;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFAnimalBite), false, this);
    Query.AddIgnoredActor(Player);
    return !GetWorld()->LineTraceSingleByChannel(Hit, GetActorLocation(), Player->GetActorLocation(),
                                                 ECC_Visibility, Query);
}
void APFWildlife::Think(const TArray<TWeakObjectPtr<APFCharacter>> &Players, bool Closing, int32 Stage,
                        float Half)
{
    if (!HasAuthority() || Health <= 0)
        return;
    const double Now = GetWorld()->GetTimeSeconds();
    if (bEnraged != Closing)
    {
        bEnraged = Closing;
        ApplyRage();
    }
    if (Closing && LastMutationStage != Stage)
    {
        LastMutationStage = Stage;
        if (!bAlpha && FMath::FRand() < .055f)
        {
            bAlpha = true;
            MaxHealth *= 2.5f;
            Health *= 2.5f;
            ApplyRage();
            ForceNetUpdate();
        }
    }
    APFCharacter *Nearest = nullptr;
    float Distance = FLT_MAX;
    for (const auto &Weak : Players)
        if (auto *Player = Weak.Get())
            if (Player->Health > 0)
            {
                float D = FVector::DistSquared(GetActorLocation(), Player->GetActorLocation());
                if (D < Distance)
                {
                    Distance = D;
                    Nearest = Player;
                }
            }
    bool Nearby = Distance < FMath::Square(16000.f);
    SetActorTickEnabled(Nearby);
    bLocomoting = false;
    GetCharacterMovement()->SetComponentTickEnabled(Nearby);
    // Keep the mesh/collision present for shots; distant AI does not run movement.
    if (!Nearby)
    {
        GetCharacterMovement()->StopMovementImmediately();
        return;
    }
    if (Distance < FMath::Square(6500.f) && Now >= NextVoice)
    {
        MulticastCall();
        NextVoice = Now + FMath::FRandRange(9.f, 24.f);
    }
    if (BiteAt > 0 && Now >= BiteAt)
    {
        if (auto *Target = BiteTarget.Get())
            if (ClearBite(Target))
                Target->TakeDamage((bAlpha ? 28.f : 14.f), FDamageEvent(), nullptr, this);
        BiteAt = 0;
        BiteTarget.Reset();
    }
    if (Now < NextAttack - .9)
        return;
    bool Hostile = IsPredator() || bEnraged;
    bool Threatened = Nearest && (Distance < FMath::Square(Hostile ? 4200.f : 2700.f) || Now < AlertUntil);
    if (Threatened)
    {
        FVector Away = (GetActorLocation() - Nearest->GetActorLocation()).GetSafeNormal2D();
        Goal = Hostile ? Nearest->GetActorLocation() : GetActorLocation() + Away * 1800;
        if (Hostile && ClearBite(Nearest) && Now >= NextAttack)
        {
            GetCharacterMovement()->StopMovementImmediately();
            SetMotion(EPFWildlifeMotion::Attack);
            MulticastCall();
            NextVoice = Now + 10;
            NextAttack = Now + 1.8;
            BiteAt = Now + .35;
            BiteTarget = Nearest;
            return;
        }
    }
    else if (Now >= NextWander)
    {
        float Angle = FMath::FRand() * 2 * PI;
        Goal = GetActorLocation() +
               FVector(FMath::Cos(Angle), FMath::Sin(Angle), 0) * FMath::FRandRange(300.f, 1100.f);
        NextWander = Now + FMath::FRandRange(4.f, 9.f);
    }
    Goal.X = FMath::Clamp(Goal.X, -Half + 500, Half - 500);
    Goal.Y = FMath::Clamp(Goal.Y, -Half + 500, Half - 500);
    FVector Direction = (Goal - GetActorLocation()).GetSafeNormal2D();
    if (FVector::DistSquared2D(Goal, GetActorLocation()) < FMath::Square(110.f))
    {
        GetCharacterMovement()->StopMovementImmediately();
        SetMotion(EPFWildlifeMotion::Idle);
        return;
    }
    // Look ahead at actual collision terrain: no procedural height function in the AI loop.
    FVector Check = GetActorLocation() + Direction * 260;
    FHitResult Ground;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFAnimalGround), false, this);
    bool Grounded = GetWorld()->LineTraceSingleByChannel(Ground, Check + FVector(0, 0, 300),
                                                         Check - FVector(0, 0, 450), ECC_WorldStatic, Query);
    bool Lake = FMath::Square((Check.X - 9000) / 4200) + FMath::Square((Check.Y - 9500) / 6000) < 1 &&
                Ground.ImpactPoint.Z < -130;
    if (!Grounded || Ground.ImpactNormal.Z < .55f || Lake)
    {
        NextWander = 0;
        Goal = GetActorLocation();
        GetCharacterMovement()->StopMovementImmediately();
        SetMotion(EPFWildlifeMotion::Idle);
        return;
    }
    GetCharacterMovement()->MaxWalkSpeed = Threatened ? (bAlpha ? 720 : 560) : 155;
    // Direct requested velocity persists until the next bounded brain update.
    MoveDirection = Direction;
    bLocomoting = true;
    SetMotion(Threatened ? EPFWildlifeMotion::Run : EPFWildlifeMotion::Walk);
}
float APFWildlife::TakeDamage(float Amount, const FDamageEvent &Event, AController *EventInstigator,
                              AActor *Causer)
{
    if (!HasAuthority() || Health <= 0 || !FMath::IsFinite(Amount) || Amount <= 0)
        return 0;
    float Applied = FMath::Min(Amount, Health);
    Health -= Applied;
    AlertUntil = GetWorld()->GetTimeSeconds() + 8;
    if (Health <= 0)
    {
        GetCharacterMovement()->DisableMovement();
        GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
        GetMesh()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
        SetMotion(EPFWildlifeMotion::Dead);
        DropRewards();
        SetLifeSpan(18);
    }
    ForceNetUpdate();
    return Applied;
}
void APFWildlife::DropRewards()
{
    auto Spawn = [&](FPFItemStack Item)
    {
        const FVector Offset = FVector(FMath::FRandRange(-65.f, 65.f), FMath::FRandRange(-65.f, 65.f),
                                       -GetCapsuleComponent()->GetUnscaledCapsuleHalfHeight() + 35);
        auto *Drop = GetWorld()->SpawnActorDeferred<APFWorldPickup>(APFWorldPickup::StaticClass(),
                                                                    FTransform(GetActorLocation() + Offset));
        if (Drop)
        {
            Drop->Item = Item;
            Drop->FinishSpawning(FTransform(GetActorLocation() + Offset));
        }
    };
    FPFItemStack Food;
    Food.Kind = EPFItemKind::Food;
    Food.Count = bAlpha ? 3 : 1;
    Spawn(Food);
    const int32 Rolls = bAlpha ? 2 : FMath::FRand() < (bEnraged ? .8f : .35f) ? 1 : 0;
    for (int32 I = 0; I < Rolls; I++)
    {
        FPFItemStack Reward;
        Reward.Count = 1;
        int32 Roll = FMath::RandRange(0, 5);
        if (Roll == 0)
        {
            Reward.Kind = EPFItemKind::Weapon;
            Reward.WeaponId = FMath::RandBool() ? TEXT("Ridgeline") : TEXT("ServiceNine");
            Reward.Magazine = Reward.WeaponId == TEXT("Ridgeline") ? 5 : 12;
            Reward.Rarity = bEnraged ? 2 : 1;
        }
        else if (Roll == 1)
        {
            Reward.Kind = EPFItemKind::Ammo;
            Reward.WeaponId = TEXT("Ridgeline");
            Reward.Count = 10;
        }
        else
            Reward.Kind = Roll == 2   ? EPFItemKind::Medkit
                          : Roll == 3 ? EPFItemKind::ArmorPlate
                          : Roll == 4 ? EPFItemKind::LightVest
                                      : EPFItemKind::FieldVest;
        Spawn(Reward);
    }
}
void APFWildlife::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(APFWildlife, Species);
    DOREPLIFETIME(APFWildlife, Motion);
    DOREPLIFETIME(APFWildlife, bEnraged);
    DOREPLIFETIME(APFWildlife, bAlpha);
    DOREPLIFETIME(APFWildlife, Health);
    DOREPLIFETIME(APFWildlife, MaxHealth);
}

APFWildlifeDirector::APFWildlifeDirector()
{
    PrimaryActorTick.bCanEverTick = false;
}
void APFWildlifeDirector::BeginPlay()
{
    Super::BeginPlay();
    if (HasAuthority())
        GetWorldTimerManager().SetTimer(BrainTimer, this, &APFWildlifeDirector::Step, .25f, true);
}
void APFWildlifeDirector::EndPlay(EEndPlayReason::Type Reason)
{
    GetWorldTimerManager().ClearTimer(BrainTimer);
    Super::EndPlay(Reason);
}
void APFWildlifeDirector::Step()
{
    auto *Round = GetWorld()->GetGameState<APFMatchState>();
    if (!Round)
        return;
    bool Active = Round->Phase == EPFRoundPhase::Active;
    if (!Active)
    {
        if (bRoundRunning)
        {
            for (auto &Animal : Population)
                if (IsValid(Animal))
                    Animal->Destroy();
            Population.Reset();
        }
        bRoundRunning = false;
        return;
    }
    bRoundRunning = true;
    Players.Reset();
    for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
        if (auto *Player = Cast<APFCharacter>(It->Get()->GetPawn()))
            if (Player->Health > 0)
                Players.Add(Player);
    Population.RemoveAll([](const APFWildlife *Animal) { return !IsValid(Animal); });
    int32 Living = 0;
    for (auto &Animal : Population)
        if (Animal->Health > 0)
            Living++;
    const double Now = GetWorld()->GetTimeSeconds();
    if (!FParse::Param(FCommandLine::Get(), TEXT("PFRoundProbe")) && Living < 28 && Now >= NextReplenish)
    {
        for (int32 I = 0; I < 2 && Living < 28; I++)
            if (SpawnAnimal(Round->ArenaHalfExtent))
                Living++;
        NextReplenish = Now + 1.5;
    }
    if (Population.IsEmpty())
        return;
    const int32 Budget = FMath::Min(16, Population.Num());
    for (int32 I = 0; I < Budget; I++)
    {
        Cursor %= Population.Num();
        auto *Animal = Population[Cursor++].Get();
        if (IsValid(Animal))
            Animal->Think(Players, Round->bClosing, Round->StormStage, Round->ArenaHalfExtent);
    }
}
bool APFWildlifeDirector::SpawnAnimal(float Half)
{
    for (int32 Attempt = 0; Attempt < 12; Attempt++)
    {
        FVector Point(FMath::FRandRange(-Half + 1000, Half - 1000),
                      FMath::FRandRange(-Half + 1000, Half - 1000), 0);
        bool TooClose = false;
        for (const auto &Weak : Players)
            if (auto *Player = Weak.Get())
                if (FVector::DistSquared2D(Point, Player->GetActorLocation()) < FMath::Square(2200.f))
                {
                    TooClose = true;
                    break;
                }
        if (TooClose)
            continue;
        FHitResult Hit;
        FCollisionQueryParams Query(SCENE_QUERY_STAT(PFWildlifeSpawn));
        if (!GetWorld()->LineTraceSingleByChannel(Hit, Point + FVector(0, 0, 12000),
                                                  Point - FVector(0, 0, 8000), ECC_WorldStatic, Query) ||
            Hit.ImpactNormal.Z < .65f)
            continue;
        if (FMath::Square((Point.X - 9000) / 4200) + FMath::Square((Point.Y - 9500) / 6000) < 1 &&
            Hit.ImpactPoint.Z < -130)
            continue;
        const FName SpeciesList[] = {TEXT("Deer"), TEXT("Stag"),  TEXT("Fox"),   TEXT("Wolf"),
                                     TEXT("Bull"), TEXT("Horse"), TEXT("Alpaca")};
        FTransform Transform(FRotator(0, FMath::FRandRange(0.f, 360.f), 0),
                             Hit.ImpactPoint + FVector(0, 0, 100));
        auto *Animal = GetWorld()->SpawnActorDeferred<APFWildlife>(
            APFWildlife::StaticClass(), Transform, nullptr, nullptr,
            ESpawnActorCollisionHandlingMethod::AdjustIfPossibleButDontSpawnIfColliding);
        if (!Animal)
            return false;
        Animal->Species = SpeciesList[FMath::RandRange(0, 6)];
        Animal->Health = Animal->MaxHealth = Animal->Species == TEXT("Wolf") ? 100 : 65;
        Animal->FinishSpawning(Transform);
        Population.Add(Animal);
        return true;
    }
    return false;
}
