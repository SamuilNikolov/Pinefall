#include "PFRangerAnimInstance.h"
#include "PFCharacter.h"
#include "Animation/AnimInstanceProxy.h"
#include "Animation/AnimSequence.h"
#include "Animation/AnimCurveTypes.h"
#include "Animation/AnimationPoseData.h"
#include "AnimationRuntime.h"
#include "Components/SkeletalMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"

// This small native pose graph reads game objects only in PreUpdate. Evaluation
// samples immutable animation assets and uses the engine's compact-pose blending.
class FPFRangerAnimProxy : public FAnimInstanceProxy
{
  public:
    explicit FPFRangerAnimProxy(UAnimInstance *Instance) : FAnimInstanceProxy(Instance) {}
    UAnimSequence *Lower = nullptr, *Previous = nullptr, *Upper = nullptr;
    double LowerTime = 0, PreviousTime = 0, UpperTime = 0;
    float Transition = 1, Rate = 1, Pitch = 0, UpperRate = 1;
    bool bWasReloading = false, bReloading = false, bArmed = true;
    FVector PitchAxis = FVector::RightVector;
    virtual void PreUpdate(UAnimInstance *Instance, float Delta) override
    {
        FAnimInstanceProxy::PreUpdate(Instance, Delta);
        auto *Anim = CastChecked<UPFRangerAnimInstance>(Instance);
        auto *Player = Cast<APFCharacter>(Anim->TryGetPawnOwner());
        if (!Player)
            return;
        FVector Velocity = Player->GetVelocity();
        float Speed = Velocity.Size2D();
        FName Name = Speed < 15 ? TEXT("idle") : Speed > 600 ? TEXT("run") : TEXT("walk");
        FVector Local = Player->GetActorRotation().UnrotateVector(Velocity);
        if (Speed > 15 && Speed <= 600)
            Name = Local.X < -Speed * .6 ? TEXT("walk_back")
                   : FMath::Abs(Local.Y) > Speed * .65
                       ? (Local.Y < 0 ? TEXT("strafe_left") : TEXT("strafe_right"))
                       : TEXT("walk");
        if (Player->GetCharacterMovement()->IsFalling())
            Name = Velocity.Z > 100 ? TEXT("jump") : TEXT("fall");
        if (Player->bIsCrouched && Anim->Clips.Contains(TEXT("CrouchIdle")))
            Name = Speed < 15 ? TEXT("CrouchIdle") : TEXT("CrouchWalk");
        UAnimSequence *Next = Anim->Clips.FindRef(Name).Get();
        if (Next && Next != Lower)
        {
            Previous = Lower;
            PreviousTime = LowerTime;
            Lower = Next;
            LowerTime = 0;
            Transition = 0;
        }
        Rate = Speed < 15 ? 1 : FMath::Clamp(Speed / (Speed > 600 ? 500.f : 220.f), .65f, 2.f);
        bReloading = Player->bReloading;
        bArmed = !Player->WeaponId.IsNone();
        bool Pistol = Player->WeaponId == TEXT("ServiceNine");
        UAnimSequence *Hold = Anim->Clips
                                  .FindRef(bReloading ? (Pistol ? TEXT("PistolReload") : TEXT("RifleReload"))
                                                      : (Pistol ? TEXT("PistolHold") : TEXT("RifleHold")))
                                  .Get();
        if (Hold != Upper || bReloading != bWasReloading)
        {
            Upper = Hold;
            UpperTime = 0;
        }
        UpperRate =
            bReloading && Hold ? Hold->GetPlayLength() / FMath::Max(.1f, Player->Weapon.ReloadSeconds) : 1;
        bWasReloading = bReloading;
        Pitch = FMath::Clamp(FRotator::NormalizeAxis(Player->GetBaseAimRotation().Pitch), -65.f, 65.f);
        PitchAxis = Player->GetMesh()->GetComponentTransform().InverseTransformVectorNoScale(
            Player->GetActorRightVector());
    }
    virtual void UpdateAnimationNode(const FAnimationUpdateContext &Context) override
    {
        float Delta = Context.GetDeltaTime();
        LowerTime += Delta * Rate;
        PreviousTime += Delta * Rate;
        UpperTime += Delta * UpperRate;
        Transition = FMath::Min(1.f, Transition + Delta / .18f);
    }
    void Sample(UAnimSequence *Clip, double Time, bool Loop, FPoseContext &Pose)
    {
        Pose.ResetToRefPose();
        if (!Clip)
            return;
        double Length = Clip->GetPlayLength();
        double Position =
            Length > 0 ? (Loop ? FMath::Fmod(Time, Length) : FMath::Min(Time, Length - .001)) : 0;
        FAnimationPoseData Data(Pose);
        Clip->GetAnimationPose(Data, FAnimExtractContext(Position, false));
    }
    virtual bool Evaluate(FPoseContext &Output) override
    {
        Sample(Lower, LowerTime, true, Output);
        if (Previous && Transition < 1)
        {
            FPoseContext Old(this), Next(this);
            Sample(Previous, PreviousTime, true, Old);
            Sample(Lower, LowerTime, true, Next);
            FAnimationPoseData A(Next), B(Old), Result(Output);
            FAnimationRuntime::BlendTwoPosesTogether(A, B, Transition, Result);
        }
        if (bArmed && Upper)
        {
            FPoseContext Weapon(this);
            Sample(Upper, UpperTime, !bReloading, Weapon);
            const auto &Bones = Output.Pose.GetBoneContainer();
            FCompactPoseBoneIndex Spine(INDEX_NONE);
            for (FCompactPoseBoneIndex Bone : Output.Pose.ForEachBoneIndex())
            {
                FMeshPoseBoneIndex MeshIndex = Bones.MakeMeshPoseIndex(Bone);
                FName Name = Bones.GetReferenceSkeleton().GetBoneName(MeshIndex.GetInt());
                if (Name == TEXT("spine_01"))
                    Spine = Bone;
                FCompactPoseBoneIndex Parent = Bone;
                while (Parent.GetInt() != INDEX_NONE)
                {
                    if (Parent == Spine)
                    {
                        FTransform Blended;
                        Blended.Blend(Output.Pose[Bone], Weapon.Pose[Bone], Bone == Spine ? .35f : 1.f);
                        Output.Pose[Bone] = Blended;
                        break;
                    }
                    Parent = Output.Pose.GetParentBoneIndex(Parent);
                }
            }
            if (Spine.GetInt() != INDEX_NONE)
            {
                FQuat ParentRotation = FQuat::Identity;
                for (auto Parent = Output.Pose.GetParentBoneIndex(Spine); Parent.GetInt() != INDEX_NONE;
                     Parent = Output.Pose.GetParentBoneIndex(Parent))
                    ParentRotation = Output.Pose[Parent].GetRotation() * ParentRotation;
                FQuat Delta = ParentRotation.Inverse() * FQuat(PitchAxis, -FMath::DegreesToRadians(Pitch)) *
                              ParentRotation;
                Output.Pose[Spine].SetRotation((Delta * Output.Pose[Spine].GetRotation()).GetNormalized());
            }
        }
        Output.Pose.NormalizeRotations();
        return true;
    }
};
void UPFRangerAnimInstance::NativeInitializeAnimation()
{
    Super::NativeInitializeAnimation();
    for (const TCHAR *Name :
         {TEXT("idle"), TEXT("walk"), TEXT("run"), TEXT("walk_back"), TEXT("strafe_left"),
          TEXT("strafe_right"), TEXT("jump"), TEXT("fall"), TEXT("RifleHold"), TEXT("PistolHold"),
          TEXT("RifleReload"), TEXT("PistolReload"), TEXT("CrouchIdle"), TEXT("CrouchWalk")})
    {
        FString Path = FString::Printf(TEXT("/Game/Pinefall/Art/Characters/Ranger/%s.%s"), Name, Name);
        if (auto *Clip = LoadObject<UAnimSequence>(nullptr, *Path))
            Clips.Add(FName(Name), Clip);
    }
}
FAnimInstanceProxy *UPFRangerAnimInstance::CreateAnimInstanceProxy()
{
    return new FPFRangerAnimProxy(this);
}
void UPFRangerAnimInstance::DestroyAnimInstanceProxy(FAnimInstanceProxy *Proxy)
{
    delete static_cast<FPFRangerAnimProxy *>(Proxy);
}
