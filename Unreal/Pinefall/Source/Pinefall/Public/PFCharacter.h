#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "PFWeaponDefinition.h"
#include "InputActionValue.h"
#include "PFCharacter.generated.h"
class UCameraComponent;
class USpringArmComponent;
class UStaticMeshComponent;
class UInputMappingContext;
class UInputAction;
class USoundAttenuation;
class UPFInventoryComponent;
class APFWorldPickup;
class UParticleSystem;
class APFVehicle;
class UPFHitHistoryComponent;
UCLASS()
class PINEFALL_API APFCharacter : public ACharacter
{
    GENERATED_BODY()
  public:
    APFCharacter(const FObjectInitializer &ObjectInitializer);
    virtual void BeginPlay() override;
    virtual void EndPlay(const EEndPlayReason::Type Reason) override;
    virtual void Tick(float Delta) override;
    virtual void CheckJumpInput(float DeltaTime) override;
    virtual void PreReplication(IRepChangedPropertyTracker &Tracker) override;
    virtual void PawnClientRestart() override;
    virtual void SetupPlayerInputComponent(UInputComponent *Component) override;
    virtual float TakeDamage(float Amount, FDamageEvent const &Event, AController *EventInstigator,
                             AActor *Causer) override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<USpringArmComponent> CameraBoom;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<UCameraComponent> Camera;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<UStaticMeshComponent> WorldWeapon;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> ViewWeapon;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<UPFInventoryComponent> Inventory;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UPFHitHistoryComponent> HitHistory;
    UPROPERTY(ReplicatedUsing = OnRep_Health, BlueprintReadOnly) float Health = 100.f;
    UPROPERTY(Replicated, BlueprintReadOnly) float Armor = 0.f;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 ArmorTier = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) float Hunger = 85;
    UPROPERTY(Replicated, BlueprintReadOnly) float Stamina = 100;
    UPROPERTY(Replicated, BlueprintReadOnly) float Oxygen = 100;
    UPROPERTY(ReplicatedUsing = OnRep_Downed, BlueprintReadOnly) bool bDowned = false;
    UPROPERTY(Replicated, BlueprintReadOnly) float ReviveProgress = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) double BleedOutAt = 0;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 Knocks = 0;
    UPROPERTY() TWeakObjectPtr<APFWorldPickup> FocusedPickup;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 Ammo = 5;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bReloading = false;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bAiming = false;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bSliding = false;
    UPROPERTY(ReplicatedUsing = OnRep_Weapon, BlueprintReadOnly) FName WeaponId = TEXT("Ridgeline");
    UPROPERTY(Replicated) double MovementPoseTime = 0;
    double RenderedPoseTime() const;
    FVector VisualSmoothingOffset() const;
    UPROPERTY(BlueprintReadOnly) FPFWeaponDefinition Weapon;
    UPROPERTY(BlueprintReadOnly) float HitUntil = 0, DamageUntil = 0, LastDamageAmount = 0;
    UPROPERTY(BlueprintReadOnly) bool bLastHitArmor = false, bLastHitKill = false;
    bool bLastHitHead = false, bDamageWasArmor = false, bDamageWasEnvironmental = false;
    FVector DamageSource = FVector::ZeroVector;
    UPROPERTY() TWeakObjectPtr<AActor> ConfirmedTarget;
    float TargetBarUntil = 0;
    float ShotBloom = 0, AimAlpha = 0, WeaponKick = 0;
    bool bFirstPerson = true;
    UPROPERTY(ReplicatedUsing = OnRep_Vehicle) TObjectPtr<APFVehicle> Vehicle;
    UPROPERTY() TWeakObjectPtr<APFVehicle> FocusedVehicle;
    UFUNCTION() void OnRep_Vehicle();
    UFUNCTION(Server, Unreliable) void ServerDrive(FVector2D Axes, bool Boost, bool Brake);
    UFUNCTION(Server, Reliable) void ServerVehicleInteract(APFVehicle *Target, bool Service);
    void StopFire();
    void StopAim();
    void RunFireProbe();
    void RunCombatViewProbe();
    void RunCombatProbeInput();
    void RunReserveProbeInput();
    void RunMovementProbeInput();
    UFUNCTION(Server, Reliable) void ServerMovementProbeReady();
    UFUNCTION(Client, Reliable) void ClientPlaceMovementProbe(FVector Point, bool Crouched);
    bool bMovementProbeReady = false;
    void Reload();
    void ResetForRound(int32 Kit);
    void EquipInventoryWeapon(FName Id);
    void StoreMagazine();
    void EnsureEquippedWeapon();
    void EnvironmentalDamage(float Amount);
    void SurvivalSecond();
    bool CanFight() const;
    void BeginInteract();
    void EndInteract();
    UFUNCTION(Server, Reliable) void ServerInteract(APFWorldPickup *Pickup);
    UFUNCTION(Server, Reliable) void ServerRevive(bool Held);

  protected:
    UPROPERTY() TObjectPtr<UInputMappingContext> Mapping;
    UPROPERTY() TArray<TObjectPtr<UInputAction>> Actions;
    UPROPERTY() TObjectPtr<USoundAttenuation> ShotAttenuation;
    UPROPERTY() TObjectPtr<USoundAttenuation> StepAttenuation;
    UPROPERTY() TObjectPtr<USoundBase> StepSound;
    UPROPERTY() TArray<TObjectPtr<USoundBase>> StepVariants;
    int32 LastStepIndex = -1;
    UPROPERTY() TObjectPtr<USoundBase> HitSound;
    UPROPERTY() TObjectPtr<USoundBase> ArmorHitSound;
    UPROPERTY() TObjectPtr<USoundBase> KillSound;
    UPROPERTY() TObjectPtr<USoundBase> HurtSound;
    UPROPERTY() TObjectPtr<USoundBase> ReloadSound;
    UPROPERTY() TObjectPtr<UParticleSystem> ImpactEffect;
    void CreateActions();
    void AddMapping();
    bool AcceptsInput() const;
    void Move(const FInputActionValue &Value);
    void Look(const FInputActionValue &Value);
    void StartJump();
    void EndJump();
    void StartSprint();
    void EndSprint();
    void StartCrouch();
    void EndCrouch();
    void StartFire();
    void StartAim();
    void ToggleCamera();
    void WeaponOne();
    void WeaponTwo();
    void FireLocal();
    void ApplyWeapon();
    void ShotEffects(const FVector &Start, const FVector &End, bool bLocal);
    UFUNCTION() void OnRep_Health(float Previous);
    UFUNCTION() void OnRep_Downed();
    UFUNCTION() void OnRep_Weapon();
    UFUNCTION(Server, Reliable)
    void ServerFire(FVector_NetQuantize AimPoint, uint32 Sequence, double ViewTime);
    UFUNCTION(Server, Reliable) void ServerReload();
    UFUNCTION(Server, Reliable) void ServerAim(bool bValue);
    UFUNCTION(Server, Reliable) void ServerEquip(FName Id);
    UFUNCTION(NetMulticast, Unreliable)
    void MulticastShot(FVector_NetQuantize Start, FVector_NetQuantize End);
    UFUNCTION(Client, Reliable)
    void ClientConfirmed(float Amount, bool bShield, bool bKill, bool bHead, AActor *Target);
    UFUNCTION(Client, Reliable)
    void ClientDamageFeedback(bool ArmorHit, bool Environmental, FVector_NetQuantize Source);
    bool bFireHeld = false;
    float NextLocalShot = 0;
    double NextServerShot = 0;
    uint32 LocalSequence = 0, ServerSequence = 0;
    float Travelled = 0;
    FVector PreviousPosition;
    FTimerHandle ReloadTimer;
    TWeakObjectPtr<APFCharacter> ReviveTarget;
    TWeakObjectPtr<APFCharacter> Reviver;
    float StarvationTimer = 0, FocusTimer = 0;
    bool bEnvironmentalDamage = false;
    bool bDeathPresented = false;
    bool bReloadPresented = false;
    bool bCombatProbe = false, bReserveProbe = false, bMovementProbe = false;
    int32 LastMovementCase = -1;
    bool bMovementPeerObserved = false;
    bool bMovementCaptured = false;
    double MovementCaptureAfter = 0;
    int32 LastReserveProbeCase = 0;
    bool bSwimUpHeld = false, bSwimDownHeld = false;
    int32 LastCombatProbeCase = 0;
    FVector2D DriveAxes = FVector2D::ZeroVector;
    bool bDriveBoost = false, bDriveBrake = false;
    float DriveSendAfter = 0;
    bool bViewBeforeDriving = true,bVehiclePresentationActive=false;
};
