#include "PFConstructionWorld.h"
#include "PFBuildPiece.h"
#include "PFHarvestState.h"
#include "Components/InstancedStaticMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMeshActor.h"
#include "Engine/StaticMesh.h"
#include "EngineUtils.h"
#include "Net/UnrealNetwork.h"
#include "PhysicsEngine/BodyInstance.h"
void FPFHarvestChanges::PostReplicatedAdd(const TArrayView<int32> Added,int32 FinalSize)
{
    if(Owner)for(int32 Index:Added)Owner->ApplyHarvest(Index);
}
void FPFHarvestChanges::PostReplicatedChange(const TArrayView<int32> Changed,int32 FinalSize)
{
    if(Owner)for(int32 Index:Changed)Owner->ApplyHarvest(Index);
}
void APFConstructionWorld::IndexHarvestables()
{
    Harvest.Owner=this;Nodes.Reset();HarvestLookup.Reset();HarvestDeltaLookup.Reset();
    TArray<UInstancedStaticMeshComponent *> Groups;
    for(TActorIterator<AActor> It(GetWorld());It;++It)
    {
        TInlineComponentArray<UInstancedStaticMeshComponent *> Components(*It);
        for(auto *Group:Components)if(Group->GetStaticMesh())Groups.Add(Group);
    }
    Groups.Sort([](const UInstancedStaticMeshComponent &A,const UInstancedStaticMeshComponent &B){return A.GetPathName()<B.GetPathName();});
    struct FVisual{UInstancedStaticMeshComponent *Group=nullptr;int32 Index=-1;FTransform Transform;};
    TMap<FIntPoint,FVisual> Trees;
    for(auto *Group:Groups)
    {
        FString Name=Group->GetStaticMesh()->GetName();if(!Name.Contains(TEXT("Tree"))&&!Name.Contains(TEXT("Fir")))continue;
        for(int32 I=0;I<Group->GetInstanceCount();I++)
        {
            FTransform T;Group->GetInstanceTransform(I,T,true);FVector P=T.GetLocation();FVisual V;V.Group=Group;V.Index=I;V.Transform=T;
            Trees.Add(FIntPoint(FMath::RoundToInt(P.X/2),FMath::RoundToInt(P.Y/2)),V);
        }
    }
    for(auto *Group:Groups)
    {
        FString Name=Group->GetStaticMesh()->GetName();bool Tree=Name.Contains(TEXT("TrunkCollision"));
        if(!Tree&&!Name.Contains(TEXT("MossRock"))&&!Name.Contains(TEXT("FieldRock")))continue;
        auto &Lookup=HarvestLookup.Add(Group);Lookup.Init(-1,Group->GetInstanceCount());
        for(int32 I=0;I<Group->GetInstanceCount();I++)
        {
            FPFHarvestNode Node;Node.Collider=Group;Node.CollisionIndex=I;Group->GetInstanceTransform(I,Node.CollisionTransform,true);
            Node.Material=Tree?0:1;Node.Maximum=Tree?300:450;
            if(Tree)
            {
                FVector P=Node.CollisionTransform.GetLocation();auto *V=Trees.Find(FIntPoint(FMath::RoundToInt(P.X/2),FMath::RoundToInt(P.Y/2)));
                if(!V)continue;Node.Visual=V->Group;Node.VisualIndex=V->Index;Node.VisualTransform=V->Transform;
            }
            else {Node.Visual=Group;Node.VisualIndex=I;Node.VisualTransform=Node.CollisionTransform;}
            Lookup[I]=Nodes.Add(Node);
        }
    }
    // Only touched nodes replicate; the tens of thousands of scenery instances
    // remain authored static geometry rather than individual network actors.
    for(int32 I=0;I<Harvest.Items.Num();I++)ApplyHarvest(I);
    UE_LOG(LogTemp,Display,TEXT("PF_HARVEST_INDEX nodes=%d changes=%d"),Nodes.Num(),Harvest.Items.Num());
}
void APFConstructionWorld::ApplyHarvest(int32 DeltaIndex)
{
    if(!Harvest.Items.IsValidIndex(DeltaIndex))return;const auto &Change=Harvest.Items[DeltaIndex];
    if(!Nodes.IsValidIndex(Change.Node))return;const auto &Node=Nodes[Change.Node];
    bool Dead=Change.Health==0;
    auto Transform=[Dead](FTransform T){if(Dead)T.SetScale3D(FVector::ZeroVector);return T;};
    if(auto *Visual=Node.Visual.Get())Visual->UpdateInstanceTransform(Node.VisualIndex,Transform(Node.VisualTransform),true,true,true);
    if(auto *Collider=Node.Collider.Get())
    {
        if(Collider!=Node.Visual.Get())Collider->UpdateInstanceTransform(Node.CollisionIndex,Transform(Node.CollisionTransform),true,true,true);
        if(Collider->InstanceBodies.IsValidIndex(Node.CollisionIndex))
            if(auto *Body=Collider->InstanceBodies[Node.CollisionIndex])Body->SetCollisionEnabled(Dead?ECollisionEnabled::NoCollision:ECollisionEnabled::QueryAndPhysics);
    }
}
float APFConstructionWorld::DamageHarvest(const FHitResult &Hit,float Damage,int32 &Yield,uint8 &Material,float &Fraction)
{
    Yield=0;Fraction=0;if(!HasAuthority()||Damage<=0||!FMath::IsFinite(Damage))return 0;
    auto *Lookup=HarvestLookup.Find(Hit.GetComponent());
    if(!Lookup||!Lookup->IsValidIndex(Hit.Item)||!Nodes.IsValidIndex((*Lookup)[Hit.Item]))return 0;
    int32 Id=(*Lookup)[Hit.Item];auto &Node=Nodes[Id];int32 Index;
    if(auto *Existing=HarvestDeltaLookup.Find(Id))Index=*Existing;
    else{Index=Harvest.Items.AddDefaulted();Harvest.Items[Index].Node=Id;Harvest.Items[Index].Health=Node.Maximum;HarvestDeltaLookup.Add(Id,Index);}
    auto &Change=Harvest.Items[Index];int32 Amount=FMath::Min(int32(FMath::CeilToInt(Damage)),int32(Change.Health));if(Amount<=0)return 0;
    Change.Health-=Amount;Yield=FMath::Max(1,Amount/5);Material=Node.Material;Fraction=float(Change.Health)/Node.Maximum;
    Harvest.MarkItemDirty(Change);if(Change.Health==0)ApplyHarvest(Index);ForceNetUpdate();return Amount;
}
void APFConstructionWorld::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);DOREPLIFETIME(APFConstructionWorld,Harvest);
}

void FPFHarvestChanges::PreReplicatedRemove(const TArrayView<int32> Removed,int32 FinalSize)
{
    if(!Owner)return;
    for(int32 I:Removed){Items[I].Health=65535;Owner->ApplyHarvest(I);}
}
