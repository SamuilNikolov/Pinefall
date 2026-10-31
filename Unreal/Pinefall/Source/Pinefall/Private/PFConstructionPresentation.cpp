#include "GameFramework/PlayerInput.h"
#include "PFCharacter.h"
#include "PFConstructionComponent.h"
#include "PFConstructionSubsystem.h"
#include "PFConstructionWorld.h"
#include "PFBuildPiece.h"
#include "PFMatchState.h"
#include "Camera/CameraActor.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/PlayerController.h"
#include "TimerManager.h"
bool APFCharacter::RunConstructionViewProbe(const FString &Mode)
{
    if(!HasAuthority()||(Mode!=TEXT("Build")&&Mode!=TEXT("Edit")&&Mode!=TEXT("Pickaxe")&&Mode!=TEXT("StormInside")&&Mode!=TEXT("StormOutside")))return false;
    FHitResult Ground;FVector Origin(0,-3000,0);FCollisionQueryParams Query(SCENE_QUERY_STAT(PFConstructionCapture),false,this);
    if(GetWorld()->LineTraceSingleByObjectType(Ground,Origin+FVector(0,0,22000),Origin-FVector(0,0,20000),FCollisionObjectQueryParams(ECC_WorldStatic),Query))Origin.Z=Ground.ImpactPoint.Z;
    SetActorLocation(Origin+FVector(0,0,100));Controller->SetControlRotation(FRotator(-5,0,0));bFirstPerson=false;
    auto *PC=Cast<APlayerController>(Controller);
    if(Mode.StartsWith(TEXT("Storm")))
    {
        auto *Match=GetWorld()->GetGameState<APFMatchState>();Match->Phase=EPFRoundPhase::Active;Match->StormStage=1;Match->bClosing=true;
        Match->CenterFrom=Match->CenterTo=Origin+FVector(Mode==TEXT("StormOutside")?11000:5000,0,0);Match->RadiusFrom=Match->RadiusTo=7000;
        Match->StormStarts=GetWorld()->GetTimeSeconds();Match->StormEnds=Match->PhaseEnds=Match->StormStarts+600;
        return true;
    }
    if(Mode==TEXT("Pickaxe"))
    {
        PC->InputKey(FInputKeyParams(EKeys::One,IE_Pressed,1,false));
        FTimerHandle Release;GetWorldTimerManager().SetTimer(Release,FTimerDelegate::CreateWeakLambda(this,[this](){if(auto *Control=Cast<APlayerController>(Controller)){Control->InputKey(FInputKeyParams(EKeys::One,IE_Released,0,false));UE_LOG(LogTemp,Display,TEXT("PF_BUILD_INPUT_CHECK pickaxe=%d"),Construction->Mode==EPFConstructionMode::Pickaxe);}}),.2,false);
        return true;
    }
    PC->InputKey(FInputKeyParams(EKeys::F1,IE_Pressed,1,false));
    FTimerHandle Build;
    GetWorldTimerManager().SetTimer(Build,FTimerDelegate::CreateWeakLambda(this,[this,Mode]()
    {
        auto *Control=Cast<APlayerController>(Controller);if(!Control)return;
        Control->InputKey(FInputKeyParams(EKeys::F1,IE_Released,0,false));
        UE_LOG(LogTemp,Display,TEXT("PF_BUILD_INPUT_CHECK wall_selection=%d"),Construction->Mode==EPFConstructionMode::Build);
        Control->InputKey(FInputKeyParams(EKeys::LeftMouseButton,IE_Pressed,1,false));
        FTimerHandle Release;
        GetWorldTimerManager().SetTimer(Release,FTimerDelegate::CreateWeakLambda(this,[this,Mode]()
        {
            auto *Control=Cast<APlayerController>(Controller);if(!Control)return;
            Control->InputKey(FInputKeyParams(EKeys::LeftMouseButton,IE_Released,0,false));
            auto *World=GetWorld()->GetSubsystem<UPFConstructionSubsystem>()->Manager.Get();if(!World)return;
            FPFBuildRequest Frame=Construction->PreviewRequest;Frame.Turn=0;Frame.Kind=EPFBuildKind::Wall;auto *Wall=World->Find(Frame);
            UE_LOG(LogTemp,Display,TEXT("PF_BUILD_INPUT_CHECK mouse_placement=%d"),Wall!=nullptr);
            if(Mode==TEXT("Edit")&&Wall){Construction->ServerBeginEdit(Wall);return;}
            for(auto Kind:{EPFBuildKind::Floor,EPFBuildKind::Ramp,EPFBuildKind::Roof})
            {
                Frame.Kind=Kind;Frame.Cell=Kind==EPFBuildKind::Floor?FIntVector(0,0,0):Kind==EPFBuildKind::Ramp?FIntVector(0,1,0):FIntVector(1,1,0);
                APFBuildPiece *Piece=nullptr;FString Error;
                if(World->Place(this,Frame,Piece,Error))Construction->Resources.Wood-=10;
            }
            Control->SetControlRotation(FRotator(-5,90,0));
        }),.2,false);
    }),.3,false);
    return true;
}
