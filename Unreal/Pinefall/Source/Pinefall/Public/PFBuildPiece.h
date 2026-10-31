#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PFBuildTypes.h"
#include "PFBuildPiece.generated.h"
class UProceduralMeshComponent;
class UPFConstructionComponent;
UCLASS()
class PINEFALL_API APFBuildPiece : public AActor
{
    GENERATED_BODY()
  public:
    APFBuildPiece();
    virtual void BeginPlay() override;
    virtual void EndPlay(EEndPlayReason::Type Reason) override;
    virtual void Tick(float Delta) override;
    virtual float TakeDamage(float Amount,FDamageEvent const &Event,AController *Instigator,AActor *Causer) override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &Out) const override;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UProceduralMeshComponent> Geometry;
    UPROPERTY(VisibleAnywhere) TObjectPtr<UProceduralMeshComponent> Door;
    UPROPERTY(ReplicatedUsing=OnRep_Build) FPFBuildState Build;
    UPROPERTY(ReplicatedUsing=OnRep_Condition) float Health=30;
    UPROPERTY(Replicated) bool bComplete=false;
    bool bPrediction=false, bGrounded=false, bCollapsing=false;
    int32 GroundNode=-1;
    float Maximum() const;
    void Initialize(const FPFBuildState &State,bool Prediction=false);
    void ApplyGeometry();
    bool SetEdit(uint16 Mask,uint8 RampPattern,bool DoorOpen=false);
    bool ToggleDoor(APawn *User);
    FVector Center() const;
    UFUNCTION() void OnRep_Build();
    UFUNCTION() void OnRep_Condition();
  private:
    uint32 GeometryHash=0;
    float Age=0;
};
