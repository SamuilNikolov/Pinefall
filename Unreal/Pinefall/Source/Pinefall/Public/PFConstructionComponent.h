#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "PFBuildTypes.h"
#include "PFConstructionComponent.generated.h"
class APFBuildPiece;
class APFConstructionWorld;
class APFCharacter;
class UMaterialInterface;
class UStaticMeshComponent;
class UProceduralMeshComponent;
UCLASS(ClassGroup=Game)
class PINEFALL_API UPFConstructionComponent : public UActorComponent
{
    GENERATED_BODY()
  public:
    UPFConstructionComponent();
    virtual void BeginPlay() override;
    virtual void TickComponent(float Delta,ELevelTick Tick,FActorComponentTickFunction *Function) override;
    virtual void EndPlay(EEndPlayReason::Type Reason) override;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &Out) const override;
    void Bind(UInputComponent *Input);
    bool PrimaryPressed();
    void PrimaryReleased();
    void CancelHeldInput(){bHeld=false;bDragging=false;}
    bool SecondaryPressed();
    bool SecondaryReleased();
    bool Rotate();
    void Combat();
    void ResolvePrediction(APFBuildPiece *Piece);
    UPROPERTY(ReplicatedUsing=OnRep_Mode) EPFConstructionMode Mode=EPFConstructionMode::Combat;
    UPROPERTY(Replicated) FPFBuildResources Resources;
    EPFBuildKind Selected=EPFBuildKind::Wall;
    EPFBuildMaterial Material=EPFBuildMaterial::Wood;
    FString Hint;
    float HarvestBarUntil=0,HarvestBarFraction=0;
    FVector HarvestBarPoint=FVector::ZeroVector;
    UPROPERTY(Replicated) double LastSwingTime=-10;
    bool IsEditing() const {return EditTarget.IsValid();}
    uint16 EditMask=511;
    uint8 RampPattern=0;
    int32 HoveredCell=-1;
    FPFBuildRequest PreviewRequest;
    UFUNCTION(Client,Reliable) void ClientConstructionCase(int32 Phase,FPFBuildRequest Frame,APFBuildPiece *Target);
    UFUNCTION(Server,Reliable) void ServerConstructionObserved(int32 Phase,int32 PieceCount,bool Depleted,bool Collision);
    int32 ObservedPhase=-1,ObservedPieces=0;
    bool bObservedDepleted=false,bObservedCollision=true;
    FTimerHandle ProbeStrikeTimer;
    FVector ProbeWalkDirection=FVector::ZeroVector;
    UFUNCTION(Server,Reliable) void ServerDropMaterial(uint8 Kind,int32 Count,uint32 Revision);
    UFUNCTION(Server,Reliable) void ServerPlace(FPFBuildRequest Request);
    UFUNCTION(Client,Reliable) void ClientPlaced(uint32 RequestId,int32 Frame,const FString &Error);
    UFUNCTION(Server,Reliable) void ServerMode(EPFConstructionMode Value);
    UFUNCTION(Server,Reliable) void ServerBeginEdit(APFBuildPiece *Target);
    UFUNCTION(Client,Reliable) void ClientEditing(APFBuildPiece *Target,uint32 Revision,const FString &Error);
    UFUNCTION(Server,Reliable) void ServerEdit(APFBuildPiece *Target,uint32 Revision,uint16 Mask,uint8 Pattern);
    UFUNCTION(Server,Reliable) void ServerCancelEdit();
    UFUNCTION(Server,Unreliable) void ServerEditHeartbeat(APFBuildPiece *Target,uint32 Revision);
    UFUNCTION(Server,Reliable) void ServerStrike(FVector_NetQuantizeNormal Direction);
    UFUNCTION(Client,Reliable) void ClientStrike(AActor *Target,float Amount,FVector_NetQuantize Impact,float Fraction);
    UFUNCTION() void OnRep_Mode();
  private:
    void PaintEditGrid();
    void RestoreEditVisual();
    UPROPERTY() TObjectPtr<UProceduralMeshComponent> EditGrid;
    UPROPERTY() TObjectPtr<UMaterialInterface> RemovedEditMaterial;
    uint32 PaintedEdit=0;
    UPROPERTY() TObjectPtr<UStaticMeshComponent> WorldPickaxe;
    UPROPERTY() TObjectPtr<UStaticMeshComponent> ViewPickaxe;
    UPROPERTY() TObjectPtr<UMaterialInterface> ValidPreviewMaterial;
    UPROPERTY() TObjectPtr<UMaterialInterface> InvalidPreviewMaterial;
    APFCharacter *Player() const;
    APFConstructionWorld *Manager() const;
    void ToggleBuild();void Pickaxe();void Wall();void Floor();void Ramp();void Roof();void Edit();
    void Select(EPFBuildKind Kind);
    void Food();void Medkit();void Plate();void Service();void UseQuick(int32 Slot);
    void UpdatePreview();void PlaceLocal();void ConfirmEdit();void CancelEdit();
    void StrikeLocal();
    UPROPERTY() TObjectPtr<APFBuildPiece> Ghost;
    UPROPERTY() TMap<uint32,TObjectPtr<APFBuildPiece>> Pending;
    UPROPERTY() TWeakObjectPtr<APFBuildPiece> EditTarget;
    UPROPERTY() TWeakObjectPtr<APFBuildPiece> ServerEditTarget;
    double LastPlace=-10,LastStrike=-10,LastServerPlace=-10,LastServerStrike=-10,LeaseUntil=0;
    float PlaceTokens=8;
    double PlaceTokenTime=0;
    double NextEditHeartbeat=0;
    uint32 Sequence=0,Nonce=1,EditRevision=0;
    uint16 Visited=0;
    int32 Facing=0,Rotation=0,RampFirstCell=-1;
    bool bHeld=false,bDragging=false,bReady=false,bCanPlace=false;
};
