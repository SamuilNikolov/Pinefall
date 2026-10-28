#include "PFCharacter.h"
#include "PFMovementComponent.h"
#include "PFPlayerController.h"
#include "PFGameMode.h"
#include "PFRangeTarget.h"
#include "PFUserSettings.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "EnhancedInputComponent.h"
#include "EnhancedInputSubsystems.h"
#include "InputMappingContext.h"
#include "InputModifiers.h"
#include "Engine/LocalPlayer.h"
#include "Kismet/GameplayStatics.h"
#include "Sound/SoundAttenuation.h"
#include "Net/UnrealNetwork.h"
#include "TimerManager.h"
#include "UObject/ConstructorHelpers.h"
#include "GameFramework/DamageType.h"
#include "Particles/ParticleSystem.h"
#include "Sound/SoundBase.h"
#include "Animation/AnimInstance.h"
#include "Engine/SkeletalMesh.h"

APFCharacter::APFCharacter(const FObjectInitializer &Init)
    : Super(Init.SetDefaultSubobjectClass<UPFMovementComponent>(ACharacter::CharacterMovementComponentName))
{
    PrimaryActorTick.bCanEverTick = true;
    bReplicates = true;
    SetReplicateMovement(true);
    GetCapsuleComponent()->InitCapsuleSize(34, 90);
    GetCapsuleComponent()->SetCollisionResponseToChannel(ECC_Visibility, ECR_Ignore);
    GetMesh()->SetRelativeLocation(FVector(0, 0, -90));
    GetMesh()->SetRelativeRotation(FRotator(0, -90, 0));
    GetMesh()->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
    GetMesh()->SetCollisionResponseToChannel(ECC_Visibility, ECR_Block);
    static ConstructorHelpers::FObjectFinder<USkeletalMesh> Body(
        TEXT("/Game/Characters/Mannequins/Meshes/SKM_Quinn_Simple.SKM_Quinn_Simple"));
    if (Body.Succeeded())
        GetMesh()->SetSkeletalMesh(Body.Object);
    GetMesh()->VisibilityBasedAnimTickOption = EVisibilityBasedAnimTickOption::AlwaysTickPoseAndRefreshBones;
    static ConstructorHelpers::FClassFinder<UAnimInstance> Anim(
        TEXT("/Game/Characters/Mannequins/Anims/Unarmed/ABP_Unarmed"));
    if (Anim.Succeeded())
        GetMesh()->SetAnimInstanceClass(Anim.Class);
    auto *Movement = GetCharacterMovement();
    Movement->GetNavAgentPropertiesRef().bCanCrouch = true;
    Movement->MaxWalkSpeed = 480;
    Movement->MaxWalkSpeedCrouched = 250;
    Movement->JumpZVelocity = 700;
    Movement->AirControl = .35f;
    Movement->BrakingDecelerationWalking = 1800;
    Movement->bOrientRotationToMovement = false;
    bUseControllerRotationYaw = true;
    CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
    CameraBoom->SetupAttachment(RootComponent);
    CameraBoom->TargetArmLength = 0;
    CameraBoom->SocketOffset = FVector(0, 0, 65);
    CameraBoom->bUsePawnControlRotation = true;
    CameraBoom->bEnableCameraLag = false;
    Camera = CreateDefaultSubobject<UCameraComponent>(TEXT("Camera"));
    Camera->SetupAttachment(CameraBoom);
    Camera->FieldOfView = 85;
    WorldWeapon = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("WorldWeapon"));
    WorldWeapon->SetupAttachment(GetMesh(), TEXT("hand_r"));
    WorldWeapon->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    WorldWeapon->SetRelativeLocation(FVector(8, 0, 0));
    ViewWeapon = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("ViewWeapon"));
    ViewWeapon->SetupAttachment(Camera);
    ViewWeapon->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    ViewWeapon->SetOnlyOwnerSee(true);
    ViewWeapon->CastShadow = false;
    ViewWeapon->SetRelativeLocation(FVector(45, 17, -17));
}
void APFCharacter::BeginPlay()
{
    Super::BeginPlay();
    ApplyWeapon();
    PreviousPosition = GetActorLocation();
    ShotAttenuation = NewObject<USoundAttenuation>(this);
    ShotAttenuation->Attenuation.bSpatialize = true;
    ShotAttenuation->Attenuation.AttenuationShapeExtents = FVector(300);
    ShotAttenuation->Attenuation.FalloffDistance = 22000;
    StepAttenuation = NewObject<USoundAttenuation>(this);
    StepAttenuation->Attenuation.bSpatialize = true;
    StepAttenuation->Attenuation.AttenuationShapeExtents = FVector(100);
    StepAttenuation->Attenuation.FalloffDistance = 1800;
    StepSound = LoadObject<USoundBase>(nullptr, TEXT("/Game/Pinefall/Audio/Step.Step"));
    AddMapping();
    UE_LOG(LogTemp, Display, TEXT("PF_PAWN role=%d owner=%d"), int32(GetLocalRole()), IsLocallyControlled());
}
bool APFCharacter::AcceptsInput() const
{
    auto *PC = Cast<APFPlayerController>(Controller);
    return Health > 0 && PC && !PC->bMenuOpen;
}
void APFCharacter::Tick(float Delta)
{
    Super::Tick(Delta);
    if (IsLocallyControlled())
    {
        CameraBoom->TargetArmLength = FMath::FInterpTo(CameraBoom->TargetArmLength,
                                                       bFirstPerson ? 0
                                                       : bAiming    ? 180
                                                                    : 280,
                                                       Delta, 14);
        CameraBoom->SocketOffset =
            FMath::VInterpTo(CameraBoom->SocketOffset,
                             bFirstPerson ? FVector(0, 0, bIsCrouched ? 32 : 65)
                                          : FVector(0, bAiming ? 40 : 55, bIsCrouched ? 30 : 70),
                             Delta, 18);
        ViewWeapon->SetRelativeLocation(FMath::VInterpTo(ViewWeapon->GetRelativeLocation(),
                                                         bAiming ? FVector(52, 0, -6) : FVector(45, 17, -17),
                                                         Delta, 18));
        Camera->SetFieldOfView(
            FMath::FInterpTo(Camera->FieldOfView, bAiming ? Weapon.AimFOV : 85, Delta, 16));
        GetMesh()->SetOwnerNoSee(bFirstPerson);
        WorldWeapon->SetOwnerNoSee(bFirstPerson);
        ViewWeapon->SetVisibility(bFirstPerson && Health > 0);
        if (bFireHeld && AcceptsInput() && GetWorld()->GetTimeSeconds() >= NextLocalShot)
            FireLocal();
    }
    if (Health > 0 && GetCharacterMovement()->IsMovingOnGround() && GetVelocity().Size2D() > 120)
    {
        Travelled += FVector::Dist2D(GetActorLocation(), PreviousPosition);
        if (Travelled > 190)
        {
            Travelled = 0;
            if (StepSound)
                UGameplayStatics::PlaySoundAtLocation(this, StepSound, GetActorLocation(),
                                                      bIsCrouched ? .07f : .15f,
                                                      FMath::FRandRange(.94f, 1.04f), 0, StepAttenuation);
        }
    }
    PreviousPosition = GetActorLocation();
}
void APFCharacter::CreateActions()
{
    if (Mapping)
        return;
    Mapping = NewObject<UInputMappingContext>(this);
    for (int32 Index = 0; Index < 11; Index++)
    {
        auto *Action = NewObject<UInputAction>(this);
        Action->ValueType = Index < 2 ? EInputActionValueType::Axis2D : EInputActionValueType::Boolean;
        Actions.Add(Action);
    }
    auto MapAxis = [this](int32 Index, FKey Key, bool Negative, bool Swizzle)
    {
        auto &Entry = Mapping->MapKey(Actions[Index], Key);
        if (Negative)
            Entry.Modifiers.Add(NewObject<UInputModifierNegate>(Mapping));
        if (Swizzle)
        {
            auto *Modifier = NewObject<UInputModifierSwizzleAxis>(Mapping);
            Modifier->Order = EInputAxisSwizzle::YXZ;
            Entry.Modifiers.Add(Modifier);
        }
    };
    MapAxis(0, EKeys::W, false, true);
    MapAxis(0, EKeys::S, true, true);
    MapAxis(0, EKeys::D, false, false);
    MapAxis(0, EKeys::A, true, false);
    MapAxis(1, EKeys::MouseX, false, false);
    MapAxis(1, EKeys::MouseY, false, true);
    FKey Keys[] = {EKeys::SpaceBar,
                   EKeys::LeftShift,
                   EKeys::LeftControl,
                   EKeys::LeftMouseButton,
                   EKeys::RightMouseButton,
                   EKeys::R,
                   EKeys::V,
                   EKeys::One,
                   EKeys::Two};
    for (int32 Index = 0; Index < 9; Index++)
        Mapping->MapKey(Actions[Index + 2], Keys[Index]);
}
void APFCharacter::AddMapping()
{
    if (!IsLocallyControlled())
        return;
    CreateActions();
    if (auto *PC = Cast<APlayerController>(Controller))
        if (auto *Local = PC->GetLocalPlayer())
            if (auto *Subsystem = Local->GetSubsystem<UEnhancedInputLocalPlayerSubsystem>())
            {
                Subsystem->RemoveMappingContext(Mapping);
                Subsystem->AddMappingContext(Mapping, 0);
            }
}
void APFCharacter::PawnClientRestart()
{
    Super::PawnClientRestart();
    AddMapping();
}
void APFCharacter::SetupPlayerInputComponent(UInputComponent *Component)
{
    Super::SetupPlayerInputComponent(Component);
    CreateActions();
    auto *Input = CastChecked<UEnhancedInputComponent>(Component);
    Input->BindAction(Actions[0], ETriggerEvent::Triggered, this, &APFCharacter::Move);
    Input->BindAction(Actions[1], ETriggerEvent::Triggered, this, &APFCharacter::Look);
    Input->BindAction(Actions[2], ETriggerEvent::Started, this, &APFCharacter::StartJump);
    Input->BindAction(Actions[2], ETriggerEvent::Completed, this, &APFCharacter::EndJump);
    Input->BindAction(Actions[3], ETriggerEvent::Started, this, &APFCharacter::StartSprint);
    Input->BindAction(Actions[3], ETriggerEvent::Completed, this, &APFCharacter::EndSprint);
    Input->BindAction(Actions[4], ETriggerEvent::Started, this, &APFCharacter::StartCrouch);
    Input->BindAction(Actions[4], ETriggerEvent::Completed, this, &APFCharacter::EndCrouch);
    Input->BindAction(Actions[5], ETriggerEvent::Started, this, &APFCharacter::StartFire);
    Input->BindAction(Actions[5], ETriggerEvent::Completed, this, &APFCharacter::StopFire);
    Input->BindAction(Actions[6], ETriggerEvent::Started, this, &APFCharacter::StartAim);
    Input->BindAction(Actions[6], ETriggerEvent::Completed, this, &APFCharacter::StopAim);
    Input->BindAction(Actions[7], ETriggerEvent::Started, this, &APFCharacter::Reload);
    Input->BindAction(Actions[8], ETriggerEvent::Started, this, &APFCharacter::ToggleCamera);
    Input->BindAction(Actions[9], ETriggerEvent::Started, this, &APFCharacter::WeaponOne);
    Input->BindAction(Actions[10], ETriggerEvent::Started, this, &APFCharacter::WeaponTwo);
    AddMapping();
}
void APFCharacter::Move(const FInputActionValue &Value)
{
    if (!AcceptsInput())
        return;
    auto Axis = Value.Get<FVector2D>();
    auto Yaw = FRotator(0, Controller->GetControlRotation().Yaw, 0);
    AddMovementInput(Yaw.Vector(), Axis.Y);
    AddMovementInput(FRotationMatrix(Yaw).GetUnitAxis(EAxis::Y), Axis.X);
}
void APFCharacter::Look(const FInputActionValue &Value)
{
    if (!AcceptsInput())
        return;
    auto Axis = Value.Get<FVector2D>();
    auto *Settings = UPFUserSettings::Get();
    float Sensitivity = Settings ? Settings->MouseSensitivity : .12f;
    AddControllerYawInput(Axis.X * Sensitivity);
    AddControllerPitchInput(Axis.Y * Sensitivity);
}
void APFCharacter::StartJump()
{
    if (AcceptsInput())
        Jump();
}
void APFCharacter::EndJump()
{
    StopJumping();
}
void APFCharacter::StartSprint()
{
    if (AcceptsInput() && !bAiming)
        CastChecked<UPFMovementComponent>(GetCharacterMovement())->bWantsSprint = true;
}
void APFCharacter::EndSprint()
{
    CastChecked<UPFMovementComponent>(GetCharacterMovement())->bWantsSprint = false;
}
void APFCharacter::StartCrouch()
{
    if (AcceptsInput())
        Crouch();
}
void APFCharacter::EndCrouch()
{
    UnCrouch();
}
void APFCharacter::StartFire()
{
    if (AcceptsInput())
    {
        bFireHeld = true;
        FireLocal();
    }
}
void APFCharacter::StopFire()
{
    bFireHeld = false;
}
void APFCharacter::StartAim()
{
    if (AcceptsInput())
    {
        bAiming = true;
        EndSprint();
        ServerAim(true);
    }
}
void APFCharacter::StopAim()
{
    bAiming = false;
    ServerAim(false);
}
void APFCharacter::ServerAim_Implementation(bool Value)
{
    if (Health > 0)
        bAiming = Value;
}
void APFCharacter::Reload()
{
    if (AcceptsInput())
        ServerReload();
}
void APFCharacter::ToggleCamera()
{
    if (AcceptsInput())
        bFirstPerson = !bFirstPerson;
}
void APFCharacter::WeaponOne()
{
    if (AcceptsInput())
        ServerEquip(TEXT("Ridgeline"));
}
void APFCharacter::WeaponTwo()
{
    if (AcceptsInput())
        ServerEquip(TEXT("ServiceNine"));
}
void APFCharacter::ApplyWeapon()
{
    if (auto *Table = LoadObject<UDataTable>(nullptr, TEXT("/Game/Pinefall/Data/DT_Weapons.DT_Weapons")))
        if (auto *Row = Table->FindRow<FPFWeaponDefinition>(WeaponId, TEXT("Equip")))
            Weapon = *Row;
    WorldWeapon->SetStaticMesh(Weapon.Model);
    ViewWeapon->SetStaticMesh(Weapon.Model);
    if (HasAuthority())
        Ammo = StoredMagazines.Contains(WeaponId) ? StoredMagazines[WeaponId] : Weapon.Magazine;
    UE_LOG(LogTemp, Display, TEXT("PF_WEAPON id=%s magazine=%d ammo=%d mesh=%s"), *WeaponId.ToString(),
           Weapon.Magazine, Ammo, *GetNameSafe(Weapon.Model));
}
void APFCharacter::OnRep_Weapon()
{
    ApplyWeapon();
}
void APFCharacter::ServerEquip_Implementation(FName Id)
{
    if (Health <= 0 || (Id != TEXT("Ridgeline") && Id != TEXT("ServiceNine")) || Id == WeaponId)
        return;
    StoredMagazines.Add(WeaponId, Ammo);
    GetWorldTimerManager().ClearTimer(ReloadTimer);
    bReloading = false;
    WeaponId = Id;
    ApplyWeapon();
}
void APFCharacter::ServerReload_Implementation()
{
    if (Health <= 0 || bReloading || Ammo >= Weapon.Magazine)
        return;
    bReloading = true;
    GetWorldTimerManager().SetTimer(
        ReloadTimer,
        [this]()
        {
            if (Health > 0)
                Ammo = Weapon.Magazine;
            bReloading = false;
        },
        Weapon.ReloadSeconds, false);
}
void APFCharacter::FireLocal()
{
    if (!AcceptsInput() || bReloading || Ammo <= 0 || GetWorld()->GetTimeSeconds() < NextLocalShot)
        return;
    NextLocalShot = GetWorld()->GetTimeSeconds() + Weapon.Interval;
    if (!Weapon.bAutomatic)
        bFireHeld = false;
    FVector Start = Camera->GetComponentLocation(), End = Start + Camera->GetForwardVector() * Weapon.RangeCm;
    FHitResult Hit;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFLocalAim), true, this);
    if (GetWorld()->LineTraceSingleByChannel(Hit, Start, End, ECC_Visibility, Query))
        End = Hit.ImpactPoint;
    ShotEffects(Start, End, true);
    ServerFire(End, ++LocalSequence);
    AddControllerPitchInput(-.6f);
}
void APFCharacter::ServerFire_Implementation(FVector_NetQuantize AimPoint, uint32 Sequence)
{
    if (Health <= 0 || bReloading || Ammo <= 0 || Sequence <= ServerSequence || AimPoint.ContainsNaN() ||
        GetWorld()->GetTimeSeconds() + .015 < NextServerShot)
        return;
    FVector Start = GetActorLocation() + FVector(0, 0, bIsCrouched ? 42 : 65);
    FVector Direction = FVector(AimPoint) - Start;
    if (Direction.SizeSquared() < 1 || Direction.Size() > Weapon.RangeCm + 600 ||
        FVector::DotProduct(Direction.GetSafeNormal(), GetBaseAimRotation().Vector()) < .35f)
        return;
    ServerSequence = Sequence;
    NextServerShot = GetWorld()->GetTimeSeconds() + Weapon.Interval;
    Ammo--;
    FHitResult Hit;
    FCollisionQueryParams Query(SCENE_QUERY_STAT(PFServerShot), true, this);
    FVector End = Start + Direction.GetSafeNormal() * Weapon.RangeCm;
    if (GetWorld()->LineTraceSingleByChannel(Hit, Start, End, ECC_Visibility, Query))
    {
        End = Hit.ImpactPoint;
        bool Head = Hit.BoneName == TEXT("head");
        float Damage = Weapon.Damage * (Head ? 1.4f : 1);
        bool Shield = false, Kill = false;
        if (auto *Other = Cast<APFCharacter>(Hit.GetActor()))
        {
            Shield = Other->Armor > 0;
            UGameplayStatics::ApplyPointDamage(Other, Damage, Direction.GetSafeNormal(), Hit, Controller,
                                               this, UDamageType::StaticClass());
            Kill = Other->Health <= 0;
            ClientConfirmed(Damage, Shield, Kill, Head);
        }
        else if (auto *Target = Cast<APFRangeTarget>(Hit.GetActor()))
        {
            Shield = Target->Armor > 0;
            Target->ReceiveShot(Damage);
            Kill = Target->Health <= 0;
            ClientConfirmed(Damage, Shield, Kill, false);
        }
    }
    MulticastShot(Start, End);
    UE_LOG(LogTemp, Display, TEXT("PF_SHOT owner=%s sequence=%u ammo=%d"), *GetNameSafe(Controller), Sequence,
           Ammo);
}
void APFCharacter::ShotEffects(const FVector &Start, const FVector &End, bool Local)
{
    if (Weapon.ShotSound)
    {
        if (Local)
            UGameplayStatics::PlaySound2D(this, Weapon.ShotSound, .65f);
        else
            UGameplayStatics::PlaySoundAtLocation(this, Weapon.ShotSound, Start, .8f, 1, 0, ShotAttenuation);
    }
    if (auto *Sparks =
            LoadObject<UParticleSystem>(nullptr, TEXT("/Game/StarterContent/Particles/P_Sparks.P_Sparks")))
        UGameplayStatics::SpawnEmitterAtLocation(GetWorld(), Sparks, End, FRotator::ZeroRotator,
                                                 FVector(.15f));
}
void APFCharacter::MulticastShot_Implementation(FVector_NetQuantize Start, FVector_NetQuantize End)
{
    if (!IsLocallyControlled())
        ShotEffects(Start, End, false);
}
void APFCharacter::ClientConfirmed_Implementation(float Amount, bool Shield, bool Kill, bool Head)
{
    HitUntil = GetWorld()->GetTimeSeconds() + .65f;
    LastDamageAmount = Amount;
    bLastHitArmor = Shield;
    bLastHitKill = Kill;
    UE_LOG(LogTemp, Display, TEXT("PF_CONFIRMED damage=%.0f shield=%d kill=%d head=%d"), Amount, Shield, Kill,
           Head);
}
void APFCharacter::OnRep_Health(float Previous)
{
    if (Health < Previous && IsLocallyControlled())
        DamageUntil = GetWorld()->GetTimeSeconds() + .6f;
}
float APFCharacter::TakeDamage(float Amount, FDamageEvent const &Event, AController *EventInstigator,
                               AActor *Causer)
{
    if (!HasAuthority() || Health <= 0 || !FMath::IsFinite(Amount) || Amount <= 0)
        return 0;
    float Old = Health, Absorbed = FMath::Min(Armor, FMath::CeilToFloat(Amount * .35f));
    Armor -= Absorbed;
    Health = FMath::Max(0.f, Health - (Amount - Absorbed));
    OnRep_Health(Old);
    if (Health <= 0)
    {
        GetCharacterMovement()->DisableMovement();
        GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
        if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
            Mode->RespawnAfterDeath(Controller);
        if (EventInstigator && EventInstigator != Controller)
            if (auto *State = EventInstigator->GetPlayerState<APFPlayerState>())
                State->Eliminations++;
    }
    return Amount;
}
void APFCharacter::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(APFCharacter, Health);
    DOREPLIFETIME(APFCharacter, Armor);
    DOREPLIFETIME_CONDITION(APFCharacter, Ammo, COND_OwnerOnly);
    DOREPLIFETIME(APFCharacter, bReloading);
    DOREPLIFETIME(APFCharacter, bAiming);
    DOREPLIFETIME(APFCharacter, WeaponId);
}

void APFCharacter::RunFireProbe()
{
    // Opt-in synthetic input through the ordinary firing path. No authority, ammo,
    // collision, position or damage override; never runs during normal play.
    if (!FParse::Param(FCommandLine::Get(), TEXT("PFSmokeFire")) || !IsLocallyControlled())
        return;
    TArray<AActor *> Targets;
    UGameplayStatics::GetAllActorsOfClass(this, APFRangeTarget::StaticClass(), Targets);
    AActor *Nearest = nullptr;
    float Best = MAX_flt;
    for (auto *Target : Targets)
    {
        float Distance = FVector::DistSquared(GetActorLocation(), Target->GetActorLocation());
        if (Distance < Best)
        {
            Best = Distance;
            Nearest = Target;
        }
    }
    if (!Nearest || !Controller)
        return;
    Controller->SetControlRotation((Nearest->GetActorLocation() - Camera->GetComponentLocation()).Rotation());
    FTimerHandle Trigger;
    GetWorldTimerManager().SetTimer(
        Trigger,
        [this]()
        {
            StartFire();
            StopFire();
        },
        .3f, false);
}
