#pragma once
#include "CoreMinimal.h"
#include "Net/Serialization/FastArraySerializer.h"
#include "PFHarvestState.generated.h"
class APFConstructionWorld;
USTRUCT()
struct FPFHarvestDelta : public FFastArraySerializerItem
{
    GENERATED_BODY()
    UPROPERTY() int32 Node=-1;
    UPROPERTY() uint16 Health=0;
};
USTRUCT()
struct FPFHarvestChanges : public FFastArraySerializer
{
    GENERATED_BODY()
    UPROPERTY() TArray<FPFHarvestDelta> Items;
    APFConstructionWorld *Owner=nullptr;
    bool NetDeltaSerialize(FNetDeltaSerializeInfo &Info){return FastArrayDeltaSerialize<FPFHarvestDelta,FPFHarvestChanges>(Items,Info,*this);}
    void PreReplicatedRemove(const TArrayView<int32> Removed,int32 FinalSize);
    void PostReplicatedAdd(const TArrayView<int32> Added,int32 FinalSize);
    void PostReplicatedChange(const TArrayView<int32> Changed,int32 FinalSize);
};
template<>struct TStructOpsTypeTraits<FPFHarvestChanges>:TStructOpsTypeTraitsBase2<FPFHarvestChanges>
{
    enum{WithNetDeltaSerializer=true};
};
struct FPFHarvestNode
{
    TWeakObjectPtr<class UInstancedStaticMeshComponent> Collider,Visual;
    TWeakObjectPtr<class UStaticMeshComponent> Prop;
    int32 CollisionIndex=-1,VisualIndex=-1;
    FTransform CollisionTransform,VisualTransform;
    uint16 Maximum=300;
    uint8 Material=0;
};
