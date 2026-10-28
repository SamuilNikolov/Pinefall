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
UCLASS()
class PINEFALL_API APFCharacter : public ACharacter
{
    GENERATED_BODY()
  public:
    APFCharacter(const FObjectInitializer &ObjectInitializer);
    virtual void BeginPlay() override;
    virtual void Tick(float Delta) override;
    virtual void PawnClientRestart() override;
    virtual void SetupPlayerInputComponent(UInputComponent *Component) override;
    virtual float TakeDamage(float Amount, FDamageEvent const &Event, AController *EventInstigator,
                             AActor *Causer) override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<USpringArmComponent> CameraBoom;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<UCameraComponent> Camera;
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly) TObjectPtr<UStaticMeshComponent> WorldWeapon;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> ViewWeapon;
    UPROPERTY(ReplicatedUsing = OnRep_Health, BlueprintReadOnly) float Health = 100.f;
    UPROPERTY(Replicated, BlueprintReadOnly) float Armor = 0.f;
    UPROPERTY(Replicated, BlueprintReadOnly) int32 Ammo = 5;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bReloading = false;
    UPROPERTY(Replicated, BlueprintReadOnly) bool bAiming = false;
    UPROPERTY(ReplicatedUsing = OnRep_Weapon, BlueprintReadOnly) FName WeaponId = TEXT("Ridgeline");
    UPROPERTY(BlueprintReadOnly) FPFWeaponDefinition Weapon;
    UPROPERTY(BlueprintReadOnly) float HitUntil = 0, DamageUntil = 0, LastDamageAmount = 0;
    UPROPERTY(BlueprintReadOnly) bool bLastHitArmor = false, bLastHitKill = false;
    bool bFirstPerson = true;
    void StopFire();
    void StopAim();
    void RunFireProbe();

  protected:
    UPROPERTY() TObjectPtr<UInputMappingContext> Mapping;
    UPROPERTY() TArray<TObjectPtr<UInputAction>> Actions;
    UPROPERTY() TObjectPtr<USoundAttenuation> ShotAttenuation;
    UPROPERTY() TObjectPtr<USoundAttenuation> StepAttenuation;
    UPROPERTY() TObjectPtr<USoundBase> StepSound;
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
    void Reload();
    void ToggleCamera();
    void WeaponOne();
    void WeaponTwo();
    void FireLocal();
    void ApplyWeapon();
    void ShotEffects(const FVector &Start, const FVector &End, bool bLocal);
    UFUNCTION() void OnRep_Health(float Previous);
    UFUNCTION() void OnRep_Weapon();
    UFUNCTION(Server, Reliable) void ServerFire(FVector_NetQuantize AimPoint, uint32 Sequence);
    UFUNCTION(Server, Reliable) void ServerReload();
    UFUNCTION(Server, Reliable) void ServerAim(bool bValue);
    UFUNCTION(Server, Reliable) void ServerEquip(FName Id);
    UFUNCTION(NetMulticast, Unreliable)
    void MulticastShot(FVector_NetQuantize Start, FVector_NetQuantize End);
    UFUNCTION(Client, Reliable) void ClientConfirmed(float Amount, bool bShield, bool bKill, bool bHead);
    bool bFireHeld = false;
    float NextLocalShot = 0;
    double NextServerShot = 0;
    uint32 LocalSequence = 0, ServerSequence = 0;
    float Travelled = 0;
    FVector PreviousPosition;
    FTimerHandle ReloadTimer;
    TMap<FName, int32> StoredMagazines;
};
