#include "PFGameMode.h"
#include "PFCharacter.h"
#include "Components/CapsuleComponent.h"
#include "PFConstructionComponent.h"
#include "PFConstructionWorld.h"
#include "PFConstructionSubsystem.h"
#include "PFConstructionFixture.h"
#include "PFBuildPiece.h"
#include "PFWorldPickup.h"
#include "PFInventoryComponent.h"
#include "PFMatchState.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "EngineUtils.h"
#include "TimerManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "Serialization/JsonSerializer.h"
void APFGameMode::RunConstructionProbe()
{
    if(!Host.IsValid()||!FParse::Param(FCommandLine::Get(),TEXT("PFConstructionProbe")))return;
    auto *Registry=GetWorld()->GetSubsystem<UPFConstructionSubsystem>();auto *World=Registry?Registry->Manager.Get():nullptr;
    auto *HostPawn=Cast<APFCharacter>(Host->GetPawn());APFCharacter *Remote=nullptr,*Late=nullptr;
    for(auto Entry:GameState->PlayerArray)if(auto *P=Cast<APFCharacter>(Entry->GetPawn()))if(P!=HostPawn){if(!Remote)Remote=P;else Late=P;}
    if(!World||!HostPawn||!Remote)return;double Now=GetWorld()->GetTimeSeconds();if(Now<ProbeAfter)return;
    auto Check=[&](bool Passed,const TCHAR *Name){(Passed?ProbePassed:ProbeFailed).Add(Name);UE_LOG(LogTemp,Display,TEXT("PF_BUILD_CHECK %s %s"),Passed?TEXT("PASS"):TEXT("FAIL"),Name);};
    FPFBuildRequest Frame;Frame.Origin=ConstructionProbeOrigin;Frame.FrameNonce=777;
    APFBuildPiece *Wall=nullptr,*Ramp=nullptr;int32 Upper=0;
    for(TActorIterator<APFBuildPiece> It(GetWorld());It;++It)if(!It->bPrediction)
    {
        if(It->Build.Request.Kind==EPFBuildKind::Wall)Wall=*It;
        if(It->Build.Request.Kind==EPFBuildKind::Ramp)Ramp=*It;
        if(It->Build.Request.Kind==EPFBuildKind::Floor&&It->Build.Request.Cell.Z==1)Upper++;
    }
    if(Wall)Frame=Wall->Build.Request;
    auto Advance=[&](double Wait){ProbeStep++;ProbeAfter=Now+Wait;};
    if(ProbeStep==0)
    {
        FHitResult Ground;FVector At(1800,1600,0);
        if(GetWorld()->LineTraceSingleByObjectType(Ground,At+FVector(0,0,10000),At-FVector(0,0,5000),FCollisionObjectQueryParams(ECC_WorldStatic)))At.Z=Ground.ImpactPoint.Z;
        ConstructionProbeOrigin=At;Frame.Origin=At;PFConstructionFixture::Create(GetWorld(),At);
        Remote->TeleportTo(At+FVector(-700,-500,100),FRotator::ZeroRotator,false,true);Remote->Controller->SetControlRotation(FRotator::ZeroRotator);
        Check(World->HarvestCount()>0,TEXT("authored instance harvest index has nodes"));
        Check(PFConstructionFixture::TreeBlocks(GetWorld(),At),TEXT("intact tree blocks physical traces"));
        Remote->Construction->ClientConstructionCase(0,Frame,nullptr);Advance(2);return;
    }
    if(ProbeStep==1)
    {
        Check(World->Count()==3,TEXT("remote client places wall floor and ramp through validated RPCs"));
        Check(Remote->Construction->Resources.Wood==470,TEXT("each remote placement costs exactly ten wood"));
        Check(Wall&&Wall->Build.CreatorId==Remote->GetPlayerState()->GetPlayerId(),TEXT("structure records actual remote creator"));
        if(Wall){FPFBuildRequest Reverse=Wall->Build.Request;Reverse.Cell.X++;Reverse.Turn=2;Check(World->Find(Reverse)==Wall,TEXT("opposite cell edge resolves the same wall"));}
        if(Ramp)
        {
            FHitResult Contact;FCollisionQueryParams Query(SCENE_QUERY_STAT(PFRampContact),false,Remote);Query.AddIgnoredActor(HostPawn);
            GetWorld()->SweepSingleByChannel(Contact,ConstructionProbeOrigin+FVector(-380,0,92),ConstructionProbeOrigin+FVector(100,0,92),FQuat::Identity,ECC_Pawn,FCollisionShape::MakeCapsule(Remote->GetCapsuleComponent()->GetScaledCapsuleRadius(),Remote->GetCapsuleComponent()->GetScaledCapsuleHalfHeight()),Query);
            Check(Contact.GetActor()==Ramp&&Contact.ImpactNormal.Z>.7&&Remote->GetCharacterMovement()->IsWalkable(Contact),*FString::Printf(TEXT("ramp capsule contact has upward walkable collision normal (normal=%s impact=%s actor=%s)"),*Contact.Normal.ToCompactString(),*Contact.ImpactNormal.ToCompactString(),*GetNameSafe(Contact.GetActor())));
        }
        Remote->Construction->ClientConstructionCase(1,Frame,Wall);
        Remote->GetCharacterMovement()->StopMovementImmediately();Remote->TeleportTo(FVector(Frame.Origin)+FVector(-380,0,100),FRotator::ZeroRotator,false,true);
        Remote->Construction->ClientConstructionCase(11,Frame,Ramp);Advance(1.2);return;
    }
    if(ProbeStep==2)
    {
        if(ProbeReserve==0)
        {
            const float Rise=Remote->GetActorLocation().Z-ConstructionProbeOrigin.Z-90;
            Check(Rise>120&&Remote->GetCharacterMovement()->IsMovingOnGround(),*FString::Printf(TEXT("real remote owner walks up built ramp (rise %.0f normal %.2f pos %s floor %s)"),Rise,Remote->GetCharacterMovement()->CurrentFloor.HitResult.ImpactNormal.Z,*(Remote->GetActorLocation()-ConstructionProbeOrigin).ToCompactString(),*GetNameSafe(Remote->GetCharacterMovement()->CurrentFloor.HitResult.GetActor())));
            Check(Remote->GetCharacterMovement()->CurrentFloor.HitResult.GetActor()==Ramp&&Remote->GetCharacterMovement()->IsWalkable(Remote->GetCharacterMovement()->CurrentFloor.HitResult),TEXT("built ramp contact is a walkable character floor"));
            Remote->Construction->ClientConstructionCase(12,Frame,Ramp);ProbeReserve=1;ProbeAfter=Now+.4;return;
        }
        if(ProbeReserve==1){ReserveProbeOrigin=Remote->GetActorLocation();ProbeReserve=2;ProbeAfter=Now+.6;return;}
        Check(FVector::DistSquared(Remote->GetActorLocation(),ReserveProbeOrigin)<FMath::Square(12.f)&&Remote->GetCharacterMovement()->IsMovingOnGround(),TEXT("idle remote owner holds position on built ramp without sliding down"));

        Check(World->Count()==3,TEXT("duplicate far and unsupported remote builds are rejected"));
        Check(Remote->Construction->Resources.Wood==470,TEXT("rejected builds consume no resources"));
        Remote->Construction->ClientConstructionCase(2,Frame,Wall);Advance(.8);return;
    }
    if(ProbeStep==3)
    {
        Check(Wall&&Wall->Build.EditorId==Remote->GetPlayerState()->GetPlayerId(),TEXT("remote builder acquires edit lease"));
        if(Wall)HostPawn->Construction->ServerBeginEdit(Wall);
        Check(Wall&&Wall->Build.EditorId==Remote->GetPlayerState()->GetPlayerId(),TEXT("opponent cannot steal or cancel edit lease"));
        Remote->Construction->ClientConstructionCase(3,Frame,Wall);Advance(.8);return;
    }
    if(ProbeStep==4)
    {
        Check(Wall&&Wall->Build.Mask==495,TEXT("remote edit creates a center window"));
        FHitResult Hit;bool Blocked=true;
        if(Wall){auto T=Wall->GetActorTransform();Blocked=GetWorld()->LineTraceSingleByChannel(Hit,T.TransformPosition({156,0,192}),T.TransformPosition({356,0,192}),ECC_Visibility);}
        Check(!Blocked,TEXT("window edit changes physical shot collision"));
        Check(Wall&&Wall->Build.EditorId==-1,TEXT("confirm releases edit lease"));
        Check(Remote->Construction->Resources.Wood==470,TEXT("editing costs no additional materials"));
        if(Wall)
        {
            Wall->SetEdit(493,0);FHitResult DoorHit;auto Transform=Wall->GetActorTransform();
            auto TraceDoor=[&](){return GetWorld()->LineTraceSingleByChannel(DoorHit,Transform.TransformPosition({156,0,64}),Transform.TransformPosition({356,0,64}),ECC_Visibility);};
            Check(TraceDoor(),TEXT("edited closed door blocks gun traces"));
            HostPawn->TeleportTo(Transform.TransformPosition({106,-150,100}),FRotator::ZeroRotator,false,true);HostPawn->ServerBuildDoor(Wall);
            Check(Wall->Build.bDoorOpen&&!TraceDoor(),TEXT("interaction opens door and clears the opening"));
            HostPawn->TeleportTo(Transform.TransformPosition({256,0,100}),FRotator::ZeroRotator,false,true);HostPawn->ServerBuildDoor(Wall);
            Check(Wall->Build.bDoorOpen,TEXT("door refuses to close through a player"));
            HostPawn->TeleportTo(Transform.TransformPosition({106,-150,100}),FRotator::ZeroRotator,false,true);HostPawn->ServerBuildDoor(Wall);
            Check(!Wall->Build.bDoorOpen&&TraceDoor(),TEXT("clear door closes and restores blocking collision"));
        }
        Remote->Construction->ClientConstructionCase(4,Frame,Wall);Advance(.8);return;
    }
    if(ProbeStep==5){Remote->Construction->ClientConstructionCase(5,Frame,Wall);Advance(.8);return;}
    if(ProbeStep==6)
    {
        Check(Wall&&Wall->Build.Mask==511,TEXT("remote reset restores full wall"));
        FHitResult Hit;bool Blocked=false;
        if(Wall){auto T=Wall->GetActorTransform();Blocked=GetWorld()->LineTraceSingleByChannel(Hit,T.TransformPosition({156,0,192}),T.TransformPosition({356,0,192}),ECC_Visibility);}
        Check(Blocked,TEXT("reset restores blocking collision"));
        Remote->Construction->ClientConstructionCase(6,Frame,Wall);Advance(.9);return;
    }
    if(ProbeStep==7)
    {
        Check(Upper==1&&World->Count()==4,TEXT("upper floor snaps to connected wall and ramp tops"));
        Check(Remote->Construction->Resources.Wood==460,TEXT("upper floor charges one placement"));
        if(Wall)Wall->Destroy();if(Ramp)Ramp->Destroy();Advance(.8);return;
    }
    if(ProbeStep==8)
    {
        Check(World->Count()==1,TEXT("unsupported upper floor collapses after supports are removed"));
        Check(Upper==0,TEXT("grounded lower floor survives support collapse"));
        Frame.Origin=ConstructionProbeOrigin;Frame.Frame=0;Frame.FrameNonce=777;
        Remote->Construction->ClientConstructionCase(7,Frame,nullptr);Advance(.9);return;
    }
    if(ProbeStep==9)
    {
        Check(Remote->Construction->Resources.Wood==410,TEXT("remote material drop deducts exactly fifty wood"));
        bool Found=false;for(TActorIterator<APFWorldPickup> It(GetWorld());It;++It)if(It->Item.Kind==EPFItemKind::Wood&&It->Item.Count==50)Found=true;
        Check(Found,TEXT("material drop creates a normal replicated pickup"));
        Frame.Origin=ConstructionProbeOrigin;Remote->TeleportTo(ConstructionProbeOrigin+FVector(-600,300,100),FRotator::ZeroRotator,false,true);Remote->Controller->SetControlRotation(FRotator::ZeroRotator);
        Remote->Construction->ClientConstructionCase(8,Frame,nullptr);Advance(3.8);return;
    }
    if(ProbeStep==10)
    {
        Check(!PFConstructionFixture::TreeBlocks(GetWorld(),ConstructionProbeOrigin),TEXT("harvesting removes tree collision"));
        Check(Remote->Construction->Resources.Wood==470,TEXT("six validated pickaxe strikes award sixty wood"));
        bool Dead=false;for(const auto &Change:World->Harvest.Items)Dead|=Change.Health==0;
        Check(Dead,TEXT("depleted tree is recorded in sparse replicated harvest state"));
        Frame.Origin=ConstructionProbeOrigin;Remote->Construction->ClientConstructionCase(9,Frame,nullptr);Advance(1);return;
    }
    if(ProbeStep==11)
    {
        Check(Remote->Construction->ObservedPhase==9&&Remote->Construction->ObservedPieces==1,TEXT("remote client receives authoritative structure removal"));
        Check(Remote->Construction->bObservedDepleted&&!Remote->Construction->bObservedCollision,TEXT("remote client sees harvested tree and collision removed"));
        UE_LOG(LogTemp,Display,TEXT("PF_BUILD_WAIT_LATEJOIN"));Advance(0);return;
    }
    if(ProbeStep==12)
    {
        if(!Late)return;Frame.Origin=ConstructionProbeOrigin;Late->Construction->ClientConstructionCase(-1,Frame,nullptr);Advance(1.5);return;
    }
    if(ProbeStep==13){Frame.Origin=ConstructionProbeOrigin;Late->Construction->ClientConstructionCase(10,Frame,nullptr);Advance(1);return;}
    if(ProbeStep==14)
    {
        Check(Late&&Late->Construction->ObservedPhase==10&&Late->Construction->ObservedPieces==1,TEXT("late join receives surviving structures"));
        Check(Late&&Late->Construction->bObservedDepleted&&!Late->Construction->bObservedCollision,TEXT("late join receives earlier tree destruction"));
        Check(!PFBuildGrid::ValidEdit(EPFBuildKind::Floor,6)&&!PFBuildGrid::ValidEdit(EPFBuildKind::Floor,9),TEXT("disconnected diagonal floor edits are rejected"));
        Check(PFBuildGrid::Facing(83,0,0)==1&&PFBuildGrid::Facing(46,0,0)==0,TEXT("user-relative facing has stable quarter-turn hysteresis"));
        World->Reset();Check(World->Count()==0&&PFConstructionFixture::TreeBlocks(GetWorld(),ConstructionProbeOrigin),TEXT("session reset clears builds and restores harvested collision"));
        GetWorldTimerManager().ClearTimer(ProbeTimer);
        TSharedRef<FJsonObject> Result=MakeShared<FJsonObject>();TArray<TSharedPtr<FJsonValue>> Passed,Failed;
        for(const auto &Name:ProbePassed)Passed.Add(MakeShared<FJsonValueString>(Name));for(const auto &Name:ProbeFailed)Failed.Add(MakeShared<FJsonValueString>(Name));
        Result->SetArrayField(TEXT("passed"),Passed);Result->SetArrayField(TEXT("failed"),Failed);Result->SetBoolField(TEXT("rendering"),false);Result->SetNumberField(TEXT("players"),GameState->PlayerArray.Num());
        FString Json,Report;FJsonSerializer::Serialize(Result,TJsonWriterFactory<>::Create(&Json));
        if(FParse::Value(FCommandLine::Get(),TEXT("PFConstructionReport="),Report)){IFileManager::Get().MakeDirectory(*FPaths::GetPath(Report),true);FFileHelper::SaveStringToFile(Json,*Report);}
    }
}
