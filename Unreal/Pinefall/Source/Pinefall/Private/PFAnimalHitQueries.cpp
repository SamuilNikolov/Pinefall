#include "PFAnimalHitQueries.h"
#include "PFWildlife.h"
#include "Components/SkeletalMeshComponent.h"
#include "PhysicsEngine/PhysicsAsset.h"
#include "PhysicsEngine/SkeletalBodySetup.h"
// Shared exact primitive intersections already used by player rewind.
namespace PFHitHistory
{
double Sphere(const FVector &, const FVector &, const FVector &, double);
double Capsule(const FVector &, const FVector &, double, double);
} // namespace PFHitHistory
void UPFAnimalHitQueries::Register(APFWildlife *Animal)
{
    Animals.AddUnique(Animal);
}
void UPFAnimalHitQueries::Unregister(APFWildlife *Animal)
{
    Animals.Remove(Animal);
}
bool UPFAnimalHitQueries::Trace(const FVector &Start, const FVector &End, FHitResult &Hit, float Radius,
                                const APFWildlife *Ignored) const
{
    bool Found = false;
    double Best = (End - Start).Size();
    for (const auto &Weak : Animals)
        if (auto *Animal = Weak.Get(); Animal && Animal != Ignored && Animal->Health > 0)
        {
            FHitResult Candidate;
            if (Animal->TraceAnimatedBody(Start, End, Candidate, Radius) && Candidate.Distance < Best)
            {
                Best = Candidate.Distance;
                Hit = Candidate;
                Found = true;
            }
        }
    return Found;
}
void APFWildlife::CacheHitGeometry()
{
    ShotShapes.Reset();
    auto *VisualMesh = GetMesh();
    auto *Asset = VisualMesh->GetPhysicsAsset();
    if (!Asset)
    {
        VisualMesh->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
        return;
    }
    for (const auto &Entry : Asset->SkeletalBodySetups)
        if (auto *Body = Entry.Get())
        {
            const int32 Bone = VisualMesh->GetBoneIndex(Body->BoneName);
            if (Bone == INDEX_NONE)
                continue;
            // Keep ordinary physics queries for unsupported geometry; never silently
            // replace authored convex/tapered bodies with an oversized capsule.
            if (!Body->AggGeom.ConvexElems.IsEmpty() || !Body->AggGeom.TaperedCapsuleElems.IsEmpty())
            {
                ShotShapes.Reset();
                VisualMesh->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
                return;
            }
            auto Add = [&](uint8 Kind, FTransform Pose, float Radius, float Half, FVector Extent) {
                FPFHitShape Shape;
                Shape.Kind = Kind;
                Shape.Pose = Pose;
                Shape.Radius = Radius;
                Shape.HalfLength = Half;
                Shape.Extent = Extent;
                Shape.Bone = Body->BoneName;
                Shape.BoneIndex = Bone;
                ShotShapes.Add(Shape);
            };
            for (const auto &Shape : Body->AggGeom.SphereElems)
                Add(0, Shape.GetTransform(), Shape.Radius, 0, FVector::ZeroVector);
            for (const auto &Shape : Body->AggGeom.SphylElems)
                Add(1, Shape.GetTransform(), Shape.Radius, Shape.Length * .5f, FVector::ZeroVector);
            for (const auto &Shape : Body->AggGeom.BoxElems)
                Add(2, Shape.GetTransform(), 0, 0, FVector(Shape.X, Shape.Y, Shape.Z) * .5f);
        }
    VisualMesh->SetCollisionEnabled(ShotShapes.IsEmpty() ? ECollisionEnabled::QueryOnly
                                                         : ECollisionEnabled::NoCollision);
}
bool APFWildlife::TraceAnimatedBody(const FVector &Start, const FVector &End, FHitResult &Hit,
                                    float Sweep) const
{
    if (Health <= 0 || ShotShapes.IsEmpty())
        return false;
    const auto *VisualMesh = GetMesh();
    const auto Bounds = VisualMesh->Bounds;
    if (FVector::DistSquared(FMath::ClosestPointOnSegment(Bounds.Origin, Start, End), Bounds.Origin) >
        FMath::Square(Bounds.SphereRadius + Sweep))
        return false;
    FVector Ray = End - Start;
    double Length = Ray.Size();
    if (Length < 1)
        return false;
    double Best = Length;
    FName Bone = NAME_None;
    for (const auto &Shape : ShotShapes)
    {
        FTransform Pose = Shape.Pose * VisualMesh->GetBoneTransform(Shape.BoneIndex);
        const FVector Scale = Pose.GetScale3D().GetAbs();
        Pose.SetScale3D(FVector::OneVector);
        FVector O = Pose.InverseTransformPosition(Start),
                D = Pose.InverseTransformVectorNoScale(Ray / Length);
        double T = -1;
        if (Shape.Kind == 0)
            T = PFHitHistory::Sphere(O, D, FVector::ZeroVector, Shape.Radius * Scale.GetMax() + Sweep);
        else if (Shape.Kind == 1)
            T = PFHitHistory::Capsule(O, D, Shape.Radius * Scale.GetMax() + Sweep,
                                      Shape.HalfLength * Scale.Z);
        else
        {
            const FVector Extent = Shape.Extent * Scale;
            FVector Point, Normal;
            float Fraction = 0;
            if (FMath::LineExtentBoxIntersection(FBox(-Extent, Extent), O, O + D * Length,
                                                 FVector(Sweep), Point, Normal, Fraction))
                T = Fraction * Length;
        }
        if (T >= 0 && T < Best)
        {
            Best = T;
            Bone = Shape.Bone;
        }
    }
    if (Bone.IsNone())
        return false;
    const FVector Point = Start + Ray / Length * Best;
    Hit = FHitResult(const_cast<APFWildlife *>(this), GetMesh(), Point, -Ray / Length);
    Hit.bBlockingHit = true;
    Hit.Distance = Best;
    Hit.Time = Best / Length;
    Hit.BoneName = Bone;
    Hit.TraceStart = Start;
    Hit.TraceEnd = End;
    return true;
}
