// Opt-in two-process movement fixture. Geometry/positions are fixtures; ordinary owner input moves.
#include "Components/CapsuleComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMeshActor.h"
#include "EngineUtils.h"
#include "Misc/FileHelper.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFMatchState.h"
#include "PFMovementComponent.h"
#include "PFPlayerController.h"
#include "Serialization/JsonSerializer.h"
#include "TimerManager.h"
#include "UnrealClient.h"
void APFPlayerController::InitializeMovementProbeGeometry()
{
    for (TActorIterator<AStaticMeshActor> It(GetWorld()); It; ++It)
        if (It->ActorHasTag(TEXT("PFMovementFixture")))
            return;
    UE_LOG(LogTemp, Display, TEXT("PF_MOVEMENT_GEOMETRY world=%s"), *GetWorld()->GetMapName());
    auto Add = [&](FVector At, FVector Scale) {
        auto *Actor = GetWorld()->SpawnActor<AStaticMeshActor>(At, FRotator::ZeroRotator);
        Actor->Tags.Add(TEXT("PFMovementFixture"));
        auto *Body = Actor->GetStaticMeshComponent();
        Body->SetMobility(EComponentMobility::Movable);
        Body->SetStaticMesh(LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube")));
        Body->SetCollisionProfileName(TEXT("BlockAll"));
        Actor->SetActorScale3D(Scale);
        // Static basing uses absolute movement corrections. A locally spawned
        // movable base has no shared network identity and corrupts relative Z.
        Body->SetMobility(EComponentMobility::Static);
    };
    // Identical local fixture geometry in each process, not player-spawnable game content.
    Add(FVector(0, 0, 10000), FVector(1000, 40, 1));
    Add(FVector(-45000, 0, 10225), FVector(12, 15, .5));
}
void APFCharacter::ClientPlaceMovementProbe_Implementation(FVector Point, bool Crouched)
{
    if (!bMovementProbe)
        return;
    if (auto *PC = Cast<APFPlayerController>(Controller))
        PC->InitializeMovementProbeGeometry();
    GetCharacterMovement()->StopMovementImmediately();
    if (Crouched)
    {
        Crouch();
        GetCharacterMovement()->Crouch(false);
    }
    else
    {
        UnCrouch();
        GetCharacterMovement()->UnCrouch(false);
    }
    const bool Placed = TeleportTo(Point, FRotator::ZeroRotator, false, true);
    GetCharacterMovement()->SetMovementMode(MOVE_Walking);
    UE_LOG(LogTemp, Display, TEXT("PF_MOVEMENT_LOCAL_PLACE success=%d z=%.1f"), Placed,
           GetActorLocation().Z);
    if (!HasAuthority())
        ServerMovementProbeReady();
}
void APFCharacter::ServerMovementProbeReady_Implementation()
{
    if (bMovementProbe)
        bMovementProbeReady = true;
}
void APFCharacter::RunMovementProbeInput()
{
    if (!IsLocallyControlled())
        return;
    auto *Round = GetWorld()->GetGameState<APFMatchState>();
    auto *PC = Cast<APFPlayerController>(Controller);
    if (!Round || !PC || !Round->Announcement.StartsWith(TEXT("PFMovementCase:")))
        return;
    int32 Case = FCString::Atoi(*Round->Announcement.Mid(15));
    if (Case == 0 && !HasAuthority() && LastMovementCase != 0)
    {
        // Client travel may retain the controller; BeginPlay alone is insufficient.
        PC->InitializeMovementProbeGeometry();
        ServerMovementProbeReady();
    }
    bool Active = HasAuthority() ? Case >= 14 && Case <= 16 : Case < 14 || Case >= 17;
    if (!Active)
        return;
    if (Case == 2 || Case == 3 || Case == 5 || Case == 6 || Case == 8 || Case == 10 || Case == 14 ||
        Case == 15 || Case == 17 || Case == 18)
    {
        Controller->SetControlRotation(FRotator::ZeroRotator);
        Move(FInputActionValue(FVector2D(0, 1)));
    }
    FString Capture;
    if (Case == 3 && !bMovementCaptured && bSliding &&
        FParse::Value(FCommandLine::Get(), TEXT("PFMovementCapture="), Capture))
    {
        if (MovementCaptureAfter == 0)
            MovementCaptureAfter = GetWorld()->GetTimeSeconds() + .35;
        if (GetWorld()->GetTimeSeconds() >= MovementCaptureAfter)
        {
            FScreenshotRequest::RequestScreenshot(Capture, true, false);
            bMovementCaptured = true;
        }
    }
    if (LastMovementCase == Case)
        return;
    LastMovementCase = Case;
    switch (Case)
    {
    case 1:
        StartCrouch();
        break;
    case 2:
        EndCrouch();
        StartSprint();
        if (FParse::Param(FCommandLine::Get(), TEXT("PFMovementVisual")))
            bFirstPerson = false;
        break;
    case 3:
        UE_LOG(LogTemp, Display, TEXT("PF_MOVEMENT_LOCAL_SLIDE z=%.1f"), GetActorLocation().Z);
        if (GetActorLocation().Z > 10080)
            UE_LOG(LogTemp, Display, TEXT("PF_MOVEMENT_CLIENT_ON_FIXTURE"));
        StartCrouch();
        break;
    case 4:
        EndCrouch();
        EndSprint();
        break;
    case 5:
        StartSprint();
        break;
    case 6:
        StartJump();
        break;
    case 7:
        EndJump();
        EndSprint();
        break;
    case 8:
        StartSprint();
        StartJump();
        break;
    case 9:
        PC->SetMenu(true);
        break;
    case 10:
        PC->SetMenu(false);
        EndJump();
        StartSprint();
        StartJump();
        break;
    case 11:
        EndJump();
        EndSprint();
        StartCrouch();
        break;
    case 12:
        StartJump();
        break;
    case 13:
        EndJump();
        EndCrouch();
        break;
    case 14:
        StartSprint();
        break;
    case 15:
        StartCrouch();
        break;
    case 16:
        EndCrouch();
        EndSprint();
        break;
    case 17:
        EndCrouch();
        EndJump();
        StartSprint();
        StartJump();
        break;
    case 18:
        EndJump();
        StartCrouch();
        break;
    case 19:
        EndCrouch();
        EndSprint();
        break;
    }
}
void APFGameMode::RunMovementProbe()
{
    if (!Host.IsValid())
        return;
    auto *HostPawn = Cast<APFCharacter>(Host->GetPawn());
    APFCharacter *Remote = nullptr;
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *Pawn = Cast<APFCharacter>(Entry->GetPawn()))
            if (Pawn != HostPawn)
                Remote = Pawn;
    if (!HostPawn || !Remote)
        return;
    auto *Move = CastChecked<UPFMovementComponent>(Remote->GetCharacterMovement());
    if (ProbeStep == 0 && !Remote->bMovementProbeReady)
    {
        Match()->Announcement = TEXT("PFMovementCase:0");
        return;
    }
    if (ProbeStep == 100)
    {
        if (Remote->bMovementProbeReady)
        {
            ProbeStep = MovementPendingCase;
            Match()->Announcement = FString::Printf(TEXT("PFMovementCase:%d"), ProbeStep);
            ProbeAfter = GetWorld()->GetTimeSeconds() + MovementPendingWait;
        }
        return;
    }
    if (ProbeStep == 6)
    {
        bool Air = Move->IsFalling();
        if (Air && !bMovementProbeWasAir)
            MovementProbeJumps++;
        bMovementProbeWasAir = Air;
        MovementProbePeak = FMath::Max(MovementProbePeak, float(Remote->GetVelocity().Size2D()));
    }
    if (GetWorld()->GetTimeSeconds() < ProbeAfter)
        return;
    auto Check = [&](bool Pass, const TCHAR *Text) {
        (Pass ? ProbePassed : ProbeFailed).Add(Text);
        UE_LOG(LogTemp, Display, TEXT("PF_MOVEMENT_CHECK %s %s"), Pass ? TEXT("PASS") : TEXT("FAIL"),
               Text);
    };
    auto Finish = [&]() {
        GetWorldTimerManager().ClearTimer(ProbeTimer);
        TSharedRef<FJsonObject> Root = MakeShared<FJsonObject>();
        TArray<TSharedPtr<FJsonValue>> Passed, Failed;
        for (auto &S : ProbePassed)
            Passed.Add(MakeShared<FJsonValueString>(S));
        for (auto &S : ProbeFailed)
            Failed.Add(MakeShared<FJsonValueString>(S));
        Root->SetArrayField(TEXT("passed"), Passed);
        Root->SetArrayField(TEXT("failed"), Failed);
        Root->SetNumberField(TEXT("hops"), MovementProbeJumps);
        Root->SetNumberField(TEXT("peak_cm_s"), MovementProbePeak);
        FString Json, Path;
        FJsonSerializer::Serialize(Root, TJsonWriterFactory<>::Create(&Json));
        if (FParse::Value(FCommandLine::Get(), TEXT("PFMovementReport="), Path))
            FFileHelper::SaveStringToFile(Json, *Path);
    };
    auto Announce = [&](int32 Case, float Seconds) {
        if (!Remote->bMovementProbeReady)
        {
            MovementPendingCase = Case;
            MovementPendingWait = Seconds;
            ProbeStep = 100;
            return;
        }
        ProbeStep = Case;
        Match()->Announcement = FString::Printf(TEXT("PFMovementCase:%d"), Case);
        ProbeAfter = GetWorld()->GetTimeSeconds() + Seconds;
    };
    auto Place = [&](APFCharacter *Pawn, FVector Point) {
        Pawn->GetCharacterMovement()->StopMovementImmediately();
        Pawn->TeleportTo(Point, FRotator::ZeroRotator, false, true);
        Pawn->GetCharacterMovement()->SetMovementMode(MOVE_Walking);
        if (!Pawn->IsLocallyControlled())
            Pawn->bMovementProbeReady = false;
        Pawn->ClientPlaceMovementProbe(Point, Pawn->bIsCrouched);
    };
    if (!ProbeFailed.IsEmpty())
    {
        Finish();
        return;
    }
    switch (ProbeStep)
    {
    case 0:
        Place(Remote, FVector(-35000, 0, 10140));
        Place(HostPawn, FVector(-35000, 1000, 10140));
        Remote->Hunger = 85;
        Remote->Stamina = 100;
        Announce(1, 2);
        break;
    case 1:
        Check(Remote->GetActorLocation().Z > 10080 && Remote->GetActorLocation().Z < 10200,
              TEXT("remote movement stays on the elevated collision fixture"));
        Check(Remote->bIsCrouched && !Remote->bSliding,
              TEXT("stationary crouch does not create a slide"));
        Check(
            FMath::IsNearlyEqual(Remote->GetCapsuleComponent()->GetScaledCapsuleHalfHeight(), 72.f, 1.f),
            TEXT("ordinary remote crouch retains matching collision"));
        Announce(2, 3);
        break;
    case 2:
        Check(Remote->GetVelocity().Size2D() > 850,
              TEXT("owning client sprint reaches full ground speed"));
        Announce(3, .9);
        break;
    case 3:
        Check(Remote->bSliding && Move->IsSliding(),
              TEXT("fast owning-client crouch becomes authoritative slide"));
        Check(Remote->GetVelocity().Size2D() > 650,
              TEXT("slide carries sprint momentum instead of crouch-walking"));
        Check(Remote->bIsCrouched, TEXT("slide presents crouched stance to host"));
        Announce(4, 2);
        break;
    case 4:
        Check(!Remote->bSliding && !Remote->bIsCrouched,
              TEXT("release crouch exits slide and restores stance"));
        Announce(5, 2);
        break;
    case 5:
        MovementProbeJumps = 0;
        ProbeReserve = Move->ServerJumpCount;
        MovementProbePeak = 0;
        bMovementProbeWasAir = false;
        ProbeRemoteOrigin = Remote->GetActorLocation();
        Announce(6, 15);
        break;
    case 6:
        MovementProbeJumps = Move->ServerJumpCount - ProbeReserve;
        Check(MovementProbeJumps >= 7, TEXT("held jump creates repeated real landings and takeoffs"));
        Check(MovementProbePeak > 1800, TEXT("chained hops gain substantial speed above sprint"));
        Check(MovementProbePeak <= 2450,
              TEXT("bunny-hop momentum remains within generous 24 m per second bound"));
        Check(FVector::Dist2D(Remote->GetActorLocation(), ProbeRemoteOrigin) > 18000,
              TEXT("remote hopping produces actual world travel"));
        Announce(7, 3);
        break;
    case 7:
        Check(!Move->IsFalling() && !Move->bWantsHop,
              TEXT("releasing jump ends the repeated hop chain"));
        Place(Remote, FVector(-10000, 0, 10140));
        Announce(8, .6);
        break;
    case 8:
        Announce(9, 3);
        break;
    case 9:
        Check(!Move->IsFalling() && !Move->bWantsHop,
              TEXT("opening menu clears held hop without freezing physics"));
        Place(Remote, FVector(-10000, 0, 10140));
        Remote->Hunger = 10;
        Announce(10, 3);
        break;
    case 10:
        Check(Remote->GetVelocity().Size2D() < 550,
              TEXT("low hunger prevents sprint and boosted hop momentum"));
        Remote->Hunger = 85;
        Announce(11, 2);
        break;
    case 11:
        if (!Remote->bIsCrouched || Move->IsFalling())
        {
            ProbeAfter = GetWorld()->GetTimeSeconds() + .1;
            return;
        }
        Place(Remote, FVector(-45000, 0, 10122));
        Announce(12, 2);
        break;
    case 12:
        UE_LOG(LogTemp, Display, TEXT("PF_CEILING_STATE crouched=%d half=%.1f position=%s falling=%d"),
               Remote->bIsCrouched, Remote->GetCapsuleComponent()->GetScaledCapsuleHalfHeight(),
               *Remote->GetActorLocation().ToCompactString(), Move->IsFalling());
        Check(!Move->IsFalling() && Remote->bIsCrouched,
              TEXT("jump cannot force an uncrouch through a low ceiling"));
        Place(Remote, FVector(-35000, 0, 10140));
        Announce(13, 1);
        break;
    case 13:
        Place(HostPawn, FVector(-35000, 1000, 10140));
        Announce(14, 3);
        break;
    case 14:
        Announce(15, 1);
        break;
    case 15:
        Check(HostPawn->bSliding, TEXT("listen host uses the same slide mechanics"));
        Announce(16, 2);
        break;
    case 16:
        Move->MomentumSpeed = 2000;
        Move->bWantsHop = true;
        Remote->TeleportTo(FVector(-30000, 0, 10140), FRotator::ZeroRotator, false, true);
        Check(Move->MomentumSpeed == 0 && !Move->bWantsHop,
              TEXT("teleport clears stale traversal momentum and held hops"));
        Move->MomentumSpeed = 2000;
        Move->bWantsHop = true;
        Move->SetMovementMode(MOVE_Swimming);
        Check(Move->MomentumSpeed == 0 && !Move->bWantsHop,
              TEXT("entering water clears airborne traversal state"));
        Check(!HostPawn->bSliding, TEXT("host slide exits normally on release"));
        Place(Remote, FVector(-35000, 0, 10140));
        Announce(17, .6);
        break;
    case 17:
        UE_LOG(LogTemp, Display, TEXT("PF_LANDING_AIR speed=%.1f falling=%d z=%.1f"),
               Remote->GetVelocity().Size2D(), Move->IsFalling(), Remote->GetActorLocation().Z);
        Check(Move->IsFalling(), TEXT("owning client takes off before buffered air crouch"));
        Announce(18, 1.5);
        break;
    case 18:
        UE_LOG(LogTemp, Display,
               TEXT("PF_LANDING_SLIDE speed=%.1f falling=%d crouched=%d desired=%d z=%.1f"),
               Remote->GetVelocity().Size2D(), Move->IsFalling(), Remote->bIsCrouched,
               Move->bWantsToCrouch, Remote->GetActorLocation().Z);
        Check(Remote->bSliding && Remote->bIsCrouched,
              TEXT("air-crouch buffers a real landing into slide"));
        Check(Remote->GetVelocity().Size2D() > 650,
              TEXT("buffered landing slide preserves fast arrival speed"));
        Announce(19, .75);
        break;
    case 19:
        Check(!Remote->bSliding && !Remote->bIsCrouched,
              TEXT("buffered landing slide exits on crouch release"));
        Finish();
        break;
    }
}
