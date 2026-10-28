#include "PFHitHistoryComponent.h"
#include "PFCharacter.h"
#include "Components/SkeletalMeshComponent.h"
#include "PhysicsEngine/PhysicsAsset.h"
#include "PhysicsEngine/SkeletalBodySetup.h"

namespace PFHitHistory
{
double Sphere(const FVector &O, const FVector &D, const FVector &C, double Radius)
{
    FVector M = O - C;
    double B = M | D, Coefficient = M.SizeSquared() - Radius * Radius;
    if (Coefficient <= 0)
        return 0;
    double Discriminant = B * B - Coefficient;
    if (Discriminant < 0)
        return -1;
    double T = -B - FMath::Sqrt(Discriminant);
    return T >= 0 ? T : -1;
}
double Capsule(const FVector &O, const FVector &D, double Radius, double Half)
{
    double Best = TNumericLimits<double>::Max();
    if (FVector(O.X, O.Y, FMath::Max(0., FMath::Abs(O.Z) - Half)).SizeSquared() <= Radius * Radius)
        return 0;
    // Intersection with the finite cylindrical side, followed by both end caps.
    double A = D.X * D.X + D.Y * D.Y, B = O.X * D.X + O.Y * D.Y, C = O.X * O.X + O.Y * O.Y - Radius * Radius;
    double Discriminant = B * B - A * C;
    if (A > 1.e-10 && Discriminant >= 0)
    {
        double Root = FMath::Sqrt(Discriminant);
        for (double T : {(-B - Root) / A, (-B + Root) / A})
            if (T >= 0 && FMath::Abs(O.Z + T * D.Z) <= Half)
                Best = FMath::Min(Best, T);
    }
    for (double Z : {-Half, Half})
    {
        double T = Sphere(O, D, FVector(0, 0, Z), Radius);
        if (T >= 0)
            Best = FMath::Min(Best, T);
    }
    return Best < TNumericLimits<double>::Max() ? Best : -1;
}
} // namespace PFHitHistory
UPFHitHistoryComponent::UPFHitHistoryComponent()
{
    PrimaryComponentTick.bCanEverTick = true;
    PrimaryComponentTick.TickInterval = 1.f / 30.f;
    PrimaryComponentTick.TickGroup = TG_PostPhysics;
}
void UPFHitHistoryComponent::BeginPlay()
{
    Super::BeginPlay();
    auto *Player = Cast<APFCharacter>(GetOwner());
    if (!Player || !Player->HasAuthority())
    {
        SetComponentTickEnabled(false);
        return;
    }
    auto *Mesh = Player->GetMesh();
    auto *Asset = Mesh->GetPhysicsAsset();
    if (!Asset)
    {
        SetComponentTickEnabled(false);
        return;
    }
    AddTickPrerequisiteComponent(Mesh);
    for (const auto &BodyPointer : Asset->SkeletalBodySetups)
    {
        auto *Body = BodyPointer.Get();
        if (!Body)
            continue;
        int32 Bone = Mesh->GetBoneIndex(Body->BoneName);
        if (Bone == INDEX_NONE)
            continue;
        // Unsupported convex/tapered bodies fall back to ordinary current traces
        // for this character; never silently replace them with oversized shapes.
        if (!Body->AggGeom.ConvexElems.IsEmpty() || !Body->AggGeom.TaperedCapsuleElems.IsEmpty())
        {
            Templates.Empty();
            SetComponentTickEnabled(false);
            return;
        }
        auto Add = [&](uint8 Kind, const FTransform &Pose, float Radius, float Half, FVector Extent)
        {
            FPFHitShape Shape;
            Shape.Kind = Kind;
            Shape.Pose = Pose;
            Shape.Radius = Radius;
            Shape.HalfLength = Half;
            Shape.Extent = Extent;
            Shape.Bone = Body->BoneName;
            Shape.BoneIndex = Bone;
            Templates.Add(Shape);
        };
        for (const auto &Shape : Body->AggGeom.SphereElems)
            Add(0, Shape.GetTransform(), Shape.Radius, 0, FVector::ZeroVector);
        for (const auto &Shape : Body->AggGeom.SphylElems)
            Add(1, Shape.GetTransform(), Shape.Radius, Shape.Length * .5f, FVector::ZeroVector);
        for (const auto &Shape : Body->AggGeom.BoxElems)
            Add(2, Shape.GetTransform(), 0, 0, FVector(Shape.X, Shape.Y, Shape.Z) * .5f);
    }
    UE_LOG(LogTemp, Display, TEXT("PF_HIT_HISTORY shapes=%d window=%.2f"), Templates.Num(), MaxRewindSeconds);
}
void UPFHitHistoryComponent::Capture(FPFHitFrame &Frame, double Time) const
{
    auto *Player = CastChecked<APFCharacter>(GetOwner());
    auto *Mesh = Player->GetMesh();
    Frame.Time = Time;
    Frame.Origin = Player->GetActorLocation();
    Frame.Shapes.Reset();
    for (const auto &Template : Templates)
    {
        FPFHitShape Shape = Template;
        FTransform Bone = Mesh->GetBoneTransform(Shape.BoneIndex);
        Shape.Pose = Template.Pose * Bone;
        // Listen-server mesh smoothing is cosmetic, not an extra movement delay
        // in authoritative history. Host shots select their displayed pose time.
        Shape.Pose.AddToTranslation(-Player->VisualSmoothingOffset());
        FVector Scale = Shape.Pose.GetScale3D().GetAbs();
        Shape.Radius *= Scale.GetMax();
        Shape.HalfLength *= Scale.Z;
        Shape.Extent *= Scale;
        Shape.Pose.SetScale3D(FVector::OneVector);
        Frame.Shapes.Add(Shape);
    }
}
void UPFHitHistoryComponent::TickComponent(float Delta, ELevelTick Type, FActorComponentTickFunction *Tick)
{
    Super::TickComponent(Delta, Type, Tick);
    auto *Player = CastChecked<APFCharacter>(GetOwner());
    if (Player->Health <= 0)
    {
        FrameCount = 0;
        return;
    }
    if (FrameCount > 0 &&
        FVector::DistSquared(Player->GetActorLocation(),
                             Frames[(NextFrame + Capacity - 1) % Capacity].Origin) > FMath::Square(500.f))
        FrameCount = 0; // A teleport must not create a hittable trail across the map.
    Capture(Frames[NextFrame], GetWorld()->GetTimeSeconds());
    NextFrame = (NextFrame + 1) % Capacity;
    FrameCount = FMath::Min(FrameCount + 1, Capacity);
}
bool UPFHitHistoryComponent::TraceAt(double Time, const FVector &Start, const FVector &End,
                                     FHitResult &Hit) const
{
    if (Templates.IsEmpty())
        return false;
    FPFHitFrame Current;
    const FPFHitFrame *A = nullptr;
    const FPFHitFrame *B = nullptr;
    double Now = GetWorld()->GetTimeSeconds();
    if (Now - Time > MaxRewindSeconds + .02 || Time > Now + .075)
        return false;
    // Current host shots use the current pose, avoiding a one-sample disadvantage.
    if (FrameCount == 0 || Time >= Frames[(NextFrame + Capacity - 1) % Capacity].Time)
    {
        Capture(Current, Now);
        A = B = &Current;
    }
    else
    {
        for (int32 I = 0; I < FrameCount; I++)
        {
            const auto &Frame = Frames[(NextFrame + Capacity - 1 - I) % Capacity];
            if (Frame.Time <= Time)
            {
                A = &Frame;
                break;
            }
            B = &Frame;
        }
        if (!A)
            return false;
        if (!B)
            B = A;
        if (FVector::DistSquared(A->Origin, B->Origin) > FMath::Square(500.f))
            return false;
    }
    float Alpha = A == B ? 0 : FMath::Clamp(float((Time - A->Time) / (B->Time - A->Time)), 0.f, 1.f);
    FVector Ray = End - Start;
    if (FParse::Param(FCommandLine::Get(), TEXT("PFCombatProbe")))
    {
        FVector Origin = FMath::Lerp(A->Origin, B->Origin, Alpha);
        double T = FMath::Abs(Ray.X) > 1 ? (Origin.X - Start.X) / Ray.X : 0;
        UE_LOG(LogTemp, Display, TEXT("PF_HISTORY actor=%s sampled=%s current=%s ray=%s"),
               *GetNameSafe(GetOwner()), *Origin.ToCompactString(),
               *GetOwner()->GetActorLocation().ToCompactString(), *(Start + Ray * T).ToCompactString());
    }
    double Length = Ray.Size();
    if (Length < 1)
        return false;
    double Best = Length;
    const FPFHitShape *BestShape = nullptr;
    for (int32 I = 0; I < A->Shapes.Num(); I++)
    {
        const auto &Shape = A->Shapes[I];
        FTransform Pose = Shape.Pose;
        if (B != A)
            Pose.Blend(Shape.Pose, B->Shapes[I].Pose, Alpha);
        FVector O = Pose.InverseTransformPosition(Start),
                D = Pose.InverseTransformVectorNoScale(Ray / Length);
        double T = -1;
        if (Shape.Kind == 0)
            T = PFHitHistory::Sphere(O, D, FVector::ZeroVector, Shape.Radius);
        else if (Shape.Kind == 1)
            T = PFHitHistory::Capsule(O, D, Shape.Radius, Shape.HalfLength);
        else
        {
            FVector Point, Normal;
            float Fraction = 0;
            if (FMath::LineExtentBoxIntersection(FBox(-Shape.Extent, Shape.Extent), O, O + D * Length,
                                                 FVector::ZeroVector, Point, Normal, Fraction))
                T = Fraction * Length;
        }
        if (T >= 0 && T < Best)
        {
            Best = T;
            BestShape = &Shape;
        }
    }
    if (!BestShape)
        return false;
    auto *Player = CastChecked<APFCharacter>(GetOwner());
    FVector Point = Start + Ray / Length * Best;
    Hit = FHitResult(Player, Player->GetMesh(), Point, -Ray / Length);
    Hit.bBlockingHit = true;
    Hit.Distance = Best;
    Hit.Time = Best / Length;
    Hit.TraceStart = Start;
    Hit.TraceEnd = End;
    Hit.BoneName = BestShape->Bone;
    return true;
}
