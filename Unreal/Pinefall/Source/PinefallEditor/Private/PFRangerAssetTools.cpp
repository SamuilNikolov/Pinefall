#include "PFRangerAssetTools.h"
#include "Engine/SkeletalMesh.h"
#include "PhysicsEngine/PhysicsAsset.h"
#include "PhysicsEngine/SkeletalBodySetup.h"
#include "PhysicsEngine/PhysicsConstraintTemplate.h"
void UPFRangerAssetTools::FitHeadCollision(USkeletalMesh *Mesh)
{
    if (!Mesh || !Mesh->GetPhysicsAsset())
        return;
    auto *Asset = Mesh->GetPhysicsAsset();
    const auto &Ref = Mesh->GetRefSkeleton();
    int32 HeadBone = Ref.FindBoneIndex(TEXT("head")), NeckBone = Ref.FindBoneIndex(TEXT("neck_01"));
    int32 NeckIndex = Asset->FindBodyIndex(TEXT("neck_01"));
    if (HeadBone == INDEX_NONE || NeckBone == INDEX_NONE)
        return;
    TArray<FTransform> Global;
    Global.SetNum(Ref.GetNum());
    for (int32 I = 0; I < Global.Num(); I++)
        Global[I] = Ref.GetParentIndex(I) == INDEX_NONE
                        ? Ref.GetRefBonePose()[I]
                        : Ref.GetRefBonePose()[I] * Global[Ref.GetParentIndex(I)];
    FVector Up = (Global[HeadBone].GetLocation() - Global[NeckBone].GetLocation()).GetSafeNormal();
    if (Up.IsNearlyZero())
        return;
    Asset->Modify();
    if (NeckIndex == INDEX_NONE)
    {
        auto *Body = NewObject<USkeletalBodySetup>(Asset, NAME_None, RF_Transactional);
        Body->BoneName = TEXT("neck_01");
        NeckIndex = Asset->SkeletalBodySetups.Add(Body);
    }
    int32 HeadIndex = Asset->FindBodyIndex(TEXT("head"));
    if (HeadIndex == INDEX_NONE)
    {
        auto *Body = NewObject<USkeletalBodySetup>(Asset, NAME_None, RF_Transactional);
        Body->BoneName = TEXT("head");
        HeadIndex = Asset->SkeletalBodySetups.Add(Body);
    }
    auto *Head = Asset->SkeletalBodySetups[HeadIndex].Get();
    auto *Neck = Asset->SkeletalBodySetups[NeckIndex].Get();
    Head->Modify();
    Neck->Modify();
    Head->AggGeom.EmptyElements();
    Neck->AggGeom.EmptyElements();
    // The generator merges the terminal head into its neck body. Split that
    // collision using this ranger's anatomical scale, without enlarging the body.
    FKSphereElem Skull;
    Skull.Radius = 10.5f;
    Skull.Center = Global[HeadBone].InverseTransformPosition(Global[HeadBone].GetLocation() + Up * 4.5f);
    Head->AggGeom.SphereElems.Add(Skull);
    float Distance = FVector::Dist(Global[HeadBone].GetLocation(), Global[NeckBone].GetLocation());
    FKSphylElem Throat;
    Throat.Radius = 4;
    Throat.Length = FMath::Max(0.f, Distance - 4);
    Throat.Center = Global[NeckBone].InverseTransformPosition(
        (Global[HeadBone].GetLocation() + Global[NeckBone].GetLocation()) * .5f);
    Throat.Rotation =
        FQuat::FindBetweenNormals(FVector::UpVector, Global[NeckBone].InverseTransformVectorNoScale(Up))
            .Rotator();
    Neck->AggGeom.SphylElems.Add(Throat);
    bool HasJoint = false;
    for (const auto &Joint : Asset->ConstraintSetup)
        if (Joint && Joint->DefaultInstance.ConstraintBone1 == TEXT("head"))
            HasJoint = true;
    if (!HasJoint)
    {
        auto *Joint = NewObject<UPhysicsConstraintTemplate>(Asset, NAME_None, RF_Transactional);
        auto &Constraint = Joint->DefaultInstance;
        Constraint.ConstraintBone1 = TEXT("head");
        Constraint.ConstraintBone2 = TEXT("neck_01");
        FTransform Anchor(Global[HeadBone].GetRotation(), Global[HeadBone].GetLocation());
        Constraint.SetRefFrame(EConstraintFrame::Frame1, Anchor.GetRelativeTransform(Global[HeadBone]));
        Constraint.SetRefFrame(EConstraintFrame::Frame2, Anchor.GetRelativeTransform(Global[NeckBone]));
        Constraint.SetAngularSwing1Limit(ACM_Limited, 25);
        Constraint.SetAngularSwing2Limit(ACM_Limited, 25);
        Constraint.SetAngularTwistLimit(ACM_Limited, 35);
        Constraint.SetDisableCollision(true);
        Asset->ConstraintSetup.Add(Joint);
    }
    Asset->UpdateBodySetupIndexMap();
    Asset->UpdateBoundsBodiesArray();
    Asset->DisableCollision(HeadIndex, NeckIndex);
    for (FName Name : {FName(TEXT("spine_02")), FName(TEXT("spine_03"))})
    {
        int32 Other = Asset->FindBodyIndex(Name);
        if (Other != INDEX_NONE)
            Asset->DisableCollision(HeadIndex, Other);
    }
    for (auto *Body : {Head, Neck})
    {
        Body->InvalidatePhysicsData();
        Body->CreatePhysicsMeshes();
    }
    Asset->MarkPackageDirty();
    Mesh->MarkPackageDirty();
    UE_LOG(LogTemp, Display, TEXT("PF_RANGER_HEAD_FIT skull=10.5cm neck=4cm bodies=%d"),
           Asset->SkeletalBodySetups.Num());
}
