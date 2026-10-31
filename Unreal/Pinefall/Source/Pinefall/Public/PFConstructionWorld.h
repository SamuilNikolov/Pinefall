#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PFBuildTypes.h"
#include "PFHarvestState.h"
#include "PFConstructionWorld.generated.h"
class APFBuildPiece;
class APFCharacter;
UCLASS()
class PINEFALL_API APFConstructionWorld : public AActor
{
    GENERATED_BODY()
  public:
    APFConstructionWorld();
    virtual void BeginPlay() override;
    virtual void EndPlay(EEndPlayReason::Type Reason) override;
    void RegisterPiece(APFBuildPiece *Piece);
    void UnregisterPiece(APFBuildPiece *Piece);
    APFBuildPiece *Find(const FPFBuildRequest &Request) const;
    bool HasNearbyInFrame(const FPFBuildRequest &Frame,FIntVector Cell) const;
    bool Place(APFCharacter *User,FPFBuildRequest Request,APFBuildPiece *&Result,FString &Error);
    bool CanPlace(APFCharacter *User,const FPFBuildRequest &Request,FString &Error) const;
    bool FitTerrainFoundation(FPFBuildRequest &Request) const;
    bool CanEdit(APFCharacter *User,APFBuildPiece *Piece) const;
    void Reset();
    void CheckSupport();
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;
    UPROPERTY(Replicated) FPFHarvestChanges Harvest;
    void IndexHarvestables();
    void ApplyHarvest(int32 DeltaIndex);
    float DamageHarvest(const FHitResult &Hit,float Damage,int32 &Yield,uint8 &Material,float &Fraction);
    int32 HarvestCount() const {return Nodes.Num();}
    int32 Count() const { return Pieces.Num(); }
  private:
    TMap<FPFBuildKey,TArray<TWeakObjectPtr<APFBuildPiece>>> AnchorOwners;
    TMap<TWeakObjectPtr<APFBuildPiece>,TArray<FPFBuildKey>> PieceAnchors;
    TArray<FPFHarvestNode> Nodes;
    TMap<TWeakObjectPtr<UPrimitiveComponent>,TArray<int32>> HarvestLookup;
    TMap<int32,int32> HarvestDeltaLookup;
    TMap<FPFBuildKey,TWeakObjectPtr<APFBuildPiece>> Pieces;
    TMap<uint64,int32> AssignedFrames;
    TMap<int32,FPFBuildRequest> Frames;
    int32 NextFrame=1;
    bool bResetting=false;
    FTimerHandle SupportTimer,CollapseTimer;
    TArray<TWeakObjectPtr<APFBuildPiece>> CollapseQueue;
    void CollapseBatch();
    bool IsGrounded(const FPFBuildRequest &Request) const;
    bool HasSupport(const FPFBuildRequest &Request) const;
    void Neighbors(const FPFBuildState &State,TArray<APFBuildPiece *,TInlineAllocator<24>> &Out) const;
};
