#include "PFConstructionComponent.h"
#include "PFConstructionFixture.h"
#include "PFConstructionWorld.h"
#include "PFBuildPiece.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "TimerManager.h"
void UPFConstructionComponent::ClientConstructionCase_Implementation(int32 Phase,FPFBuildRequest Frame,APFBuildPiece *Target)
{
    if(!FParse::Param(FCommandLine::Get(),TEXT("PFConstructionProbe")))return;
    auto *P=Player();Frame.RequestId=1000+Phase*10;
    if(Phase==0||Phase==-1)PFConstructionFixture::Create(GetWorld(),Frame.Origin);
    if(Phase==0)
    {
        P->GetCharacterMovement()->StopMovementImmediately();P->TeleportTo(FVector(Frame.Origin)+FVector(-700,-500,100),FRotator::ZeroRotator,false,true);
        PreviewRequest=Frame;bReady=true;Mode=EPFConstructionMode::Build;ServerMode(Mode);
        for(auto Kind:{EPFBuildKind::Wall,EPFBuildKind::Floor,EPFBuildKind::Ramp}){Frame.Kind=Kind;Frame.RequestId++;ServerPlace(Frame);}
    }
    else if(Phase==11)
    {
        P->GetCharacterMovement()->StopMovementImmediately();P->TeleportTo(FVector(Frame.Origin)+FVector(-380,0,100),FRotator::ZeroRotator,false,true);
        P->GetCharacterMovement()->SetMovementMode(MOVE_Walking);P->Controller->SetControlRotation(FRotator::ZeroRotator);
        ProbeWalkDirection=FVector::ForwardVector;SetComponentTickInterval(0);
    }
    else if(Phase==12){UE_LOG(LogTemp,Display,TEXT("PF_RAMP_LOCAL at=%s velocity=%s mode=%d floor=%s normal=%s"),*P->GetActorLocation().ToCompactString(),*P->GetVelocity().ToCompactString(),int32(P->GetCharacterMovement()->MovementMode),*GetNameSafe(P->GetCharacterMovement()->CurrentFloor.HitResult.GetActor()),*P->GetCharacterMovement()->CurrentFloor.HitResult.ImpactNormal.ToCompactString());ProbeWalkDirection=FVector::ZeroVector;P->GetCharacterMovement()->StopMovementImmediately();SetComponentTickInterval(.05f);}
    else if(Phase==1)
    {
        Frame.RequestId++;Frame.Kind=EPFBuildKind::Wall;Frame.Cell.X=1;Frame.Turn=2;ServerPlace(Frame);
        Frame.RequestId++;Frame.Cell={40,0,0};Frame.Kind=EPFBuildKind::Floor;ServerPlace(Frame);
        Frame.RequestId++;Frame.Cell={2,0,2};ServerPlace(Frame);
    }
    else if(Phase==2||Phase==4){if(Target)ServerBeginEdit(Target);}
    else if(Phase==3||Phase==5)
    {
        if(Target){RestoreEditVisual();ServerEdit(Target,Target->Build.Revision,Phase==3?495:511,0);EditTarget=nullptr;}
    }
    else if(Phase==6){Frame.Kind=EPFBuildKind::Floor;Frame.Cell.Z=1;ServerPlace(Frame);}
    else if(Phase==7){ServerDropMaterial(0,50,Resources.Revision);}
    else if(Phase==8)
    {
        P->GetCharacterMovement()->StopMovementImmediately();P->TeleportTo(FVector(Frame.Origin)+FVector(-600,300,100),FRotator::ZeroRotator,false,true);
        if(P->Controller)P->Controller->SetControlRotation(FRotator::ZeroRotator);
        Mode=EPFConstructionMode::Pickaxe;ServerMode(Mode);
        GetWorld()->GetTimerManager().SetTimer(ProbeStrikeTimer,FTimerDelegate::CreateWeakLambda(this,[this]() {ServerStrike(FVector::ForwardVector);}),.5,true,.2);
    }
    else if(Phase==9||Phase==10)
    {
        GetWorld()->GetTimerManager().ClearTimer(ProbeStrikeTimer);bool Depleted=false;
        if(Manager())for(auto &Delta:Manager()->Harvest.Items)Depleted|=Delta.Health==0;
        bool Collision=PFConstructionFixture::TreeBlocks(GetWorld(),Frame.Origin);
        UE_LOG(LogTemp,Display,TEXT("PF_BUILD_CLIENT_OBSERVED phase=%d pieces=%d depleted=%d collision=%d wood=%d"),Phase,Manager()?Manager()->Count():-1,Depleted,Collision,Resources.Wood);
        ServerConstructionObserved(Phase,Manager()?Manager()->Count():-1,Depleted,Collision);
    }
}
void UPFConstructionComponent::ServerConstructionObserved_Implementation(int32 Phase,int32 PieceCount,bool Depleted,bool Collision)
{
    if(!FParse::Param(FCommandLine::Get(),TEXT("PFConstructionProbe")))return;
    ObservedPhase=Phase;ObservedPieces=PieceCount;bObservedDepleted=Depleted;bObservedCollision=Collision;
}
