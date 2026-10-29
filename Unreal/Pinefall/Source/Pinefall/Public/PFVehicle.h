#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PFVehicle.generated.h"
class APFWaterBody;
class UBoxComponent;
class UStaticMeshComponent;
class APFCharacter;
UCLASS()
class PINEFALL_API APFVehicle : public AActor
{
    GENERATED_BODY()
  public:
    APFVehicle();
    virtual void BeginPlay() override;
    virtual void Tick(float Delta) override;
    virtual void PostNetReceiveLocationAndRotation() override;
    virtual void EndPlay(EEndPlayReason::Type Reason) override;
    virtual float TakeDamage(float Amount, struct FDamageEvent const &Event, AController *Instigator,
                             AActor *Causer) override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &Out) const override;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UBoxComponent> Collision;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> Body;
    UPROPERTY(EditAnywhere, ReplicatedUsing = RefreshVisual) bool bBoat = false;
    UPROPERTY(ReplicatedUsing = OnRep_Driver) TObjectPtr<APFCharacter> Driver;
    UPROPERTY(Replicated) float Health = 500;
    UPROPERTY(Replicated) float Fuel = 100;
    UPROPERTY(Replicated) float Speed = 0;
    bool Enter(APFCharacter *Player);
    bool Exit(bool Force = false);
    void Input(APFCharacter *Player, FVector2D Axes, bool Boost, bool Brake);
    FString Label() const;
    bool Repair(APFCharacter *Player);
    UFUNCTION() void RefreshVisual();
    UFUNCTION() void OnRep_Driver();

  private:
    UPROPERTY() TArray<TWeakObjectPtr<APFWaterBody>> Waters;
    FVector2D Control = FVector2D::ZeroVector;
    double LastInput = 0;
    bool bBoost = false, bBrake = false;
    FVector NetLocation = FVector::ZeroVector;
    FRotator NetRotation = FRotator::ZeroRotator;
    bool bHasNetPose = false,bProbe=false,bLoggedContact=false,bLoggedWater=false;
};
