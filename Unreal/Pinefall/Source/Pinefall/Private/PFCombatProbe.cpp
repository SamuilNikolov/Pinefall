// Opt-in real player shooting regression. Fixtures arrange positions, never hits.
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFMatchState.h"
#include "Camera/CameraComponent.h"
#include "Components/BoxComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "TimerManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "Serialization/JsonSerializer.h"

void APFCharacter::RunCombatProbeInput()
{
    if (!bCombatProbe || !IsLocallyControlled())
        return;
    auto *Round = GetWorld()->GetGameState<APFMatchState>();
    if (!Round || !Round->Announcement.StartsWith(TEXT("PFCombatCase:")))
        return;
    const int32 Case = FCString::Atoi(*Round->Announcement.Mid(13));
    if (Case <= 0)
        return;
    const bool Moving = Case == 6 || Case == 7 || Case == 8;
    if (Moving && (HasAuthority() != (Case == 8)))
    {
        AddMovementInput(FVector(0, 1, 0), 1);
        return;
    }
    if (Case == LastCombatProbeCase && !Moving)
        return;
    const bool HostShot = Case == 5 || Case == 8;
    if (HostShot != HasAuthority())
        return;
    APFCharacter *Target = nullptr;
    for (APlayerState *Entry : Round->PlayerArray)
        if (auto *Other = Cast<APFCharacter>(Entry->GetPawn()))
            if (Other != this)
            {
                Target = Other;
                break;
            }
    if (!Target || !Controller)
        return;
    if (Moving)
    {
        FRotator Aim = (Target->GetMesh()->GetBoneLocation(TEXT("spine_03")) - Camera->GetComponentLocation())
                           .Rotation();
        Controller->SetControlRotation(Aim);
        Camera->SetWorldRotation(Aim);
    }
    if (Case == LastCombatProbeCase)
        return;
    LastCombatProbeCase = Case;
    bFirstPerson = Case != 4;
    if (Case == 2)
        StartCrouch();
    else
        EndCrouch();
    StartAim();
    TWeakObjectPtr<APFCharacter> Weak = this, Victim = Target;
    FTimerHandle Trigger;
    GetWorldTimerManager().SetTimer(
        Trigger,
        [Weak, Victim, Case]()
        {
            auto *Shooter = Weak.Get();
            auto *Other = Victim.Get();
            if (!Shooter || !Other || !Shooter->Controller)
                return;
            const FVector Point =
                Other->GetMesh()->GetBoneLocation(Case == 9 ? TEXT("head") : TEXT("spine_03")) +
                (Case == 9 ? FVector(0, 0, 8) : FVector::ZeroVector);
            // Use the rendered body position and the ordinary local aim/trigger path.
            Shooter->Controller->SetControlRotation(
                (Point - Shooter->Camera->GetComponentLocation()).Rotation());
            FTimerHandle Fire;
            Shooter->GetWorldTimerManager().SetTimer(
                Fire,
                [Weak]()
                {
                    if (auto *Player = Weak.Get())
                    {
                        Player->StartFire();
                        Player->StopFire();
                    }
                },
                .15f, false);
        },
        .7f, false);
}

void APFGameMode::RunCombatProbe()
{
    if (!FParse::Param(FCommandLine::Get(), TEXT("PFCombatProbe")) || !Host.IsValid())
        return;
    double Now = GetWorld()->GetTimeSeconds();
    if (Now < ProbeAfter)
        return;
    auto *HostPawn = Cast<APFCharacter>(Host->GetPawn());
    APFCharacter *Remote = nullptr;
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *Pawn = Cast<APFCharacter>(Entry->GetPawn()))
            if (Pawn != HostPawn)
            {
                Remote = Pawn;
                break;
            }
    if (!HostPawn || !Remote)
        return;
    auto Check = [&](bool Value, const TCHAR *Text)
    {
        (Value ? ProbePassed : ProbeFailed).Add(Text);
        UE_LOG(LogTemp, Display, TEXT("PF_COMBAT_CHECK %s %s"), Value ? TEXT("PASS") : TEXT("FAIL"), Text);
    };
    auto Finish = [&]()
    {
        GetWorldTimerManager().ClearTimer(ProbeTimer);
        TSharedRef<FJsonObject> Result = MakeShared<FJsonObject>();
        TArray<TSharedPtr<FJsonValue>> Passed, Failed;
        for (auto &Text : ProbePassed)
            Passed.Add(MakeShared<FJsonValueString>(Text));
        for (auto &Text : ProbeFailed)
            Failed.Add(MakeShared<FJsonValueString>(Text));
        Result->SetArrayField(TEXT("passed"), Passed);
        Result->SetArrayField(TEXT("failed"), Failed);
        Result->SetStringField(TEXT("scope"),
                               TEXT("Two real processes shoot actual player skeletal collision through "
                                    "ordinary local trigger/server validation. Positions, health and cover "
                                    "are arranged. Stationary and moving targets with current-cover checks; "
                                    "not general latency/loss or WAN evidence."));
        FString Json, Report;
        FJsonSerializer::Serialize(Result, TJsonWriterFactory<>::Create(&Json));
        if (FParse::Value(FCommandLine::Get(), TEXT("PFCombatReport="), Report))
        {
            IFileManager::Get().MakeDirectory(*FPaths::GetPath(Report), true);
            FFileHelper::SaveStringToFile(Json, *Report);
        }
    };
    if (!ProbeFailed.IsEmpty())
    {
        Finish();
        return;
    }
    if (ProbeStep == 0)
    {
        FVector A(-8500, -800, 0), B(-10000, -800, 0);
        if (!GroundPoint(A) || !GroundPoint(B))
        {
            Check(false, TEXT("combat fixture has valid terrain"));
            Finish();
            return;
        }
        HostPawn->SetActorLocation(A);
        ProbeHostOrigin = A;
        Remote->SetActorLocation(B);
        ProbeRemoteOrigin = B;
        HostPawn->GetCharacterMovement()->StopMovementImmediately();
        Remote->GetCharacterMovement()->StopMovementImmediately();
        ProbeStep = 1;
        ProbeAfter = Now + 3;
        return;
    }
    if (ProbeReserve == 0)
    {
        HostPawn->Health = Remote->Health = 100;
        HostPawn->Armor = Remote->Armor = 0;
        HostPawn->Ammo = Remote->Ammo = 5;
        if (ProbeStep >= 6)
        {
            HostPawn->SetActorLocation(ProbeHostOrigin);
            Remote->SetActorLocation(ProbeRemoteOrigin);
            HostPawn->GetCharacterMovement()->StopMovementImmediately();
            Remote->GetCharacterMovement()->StopMovementImmediately();
        }
        if (ProbeStep == 2)
            HostPawn->Crouch();
        else
            HostPawn->UnCrouch();
        if (ProbeCover)
        {
            ProbeCover->Destroy();
            ProbeCover = nullptr;
        }
        if (ProbeStep == 3 || ProbeStep == 7)
        {
            ProbeCover = GetWorld()->SpawnActor<AActor>();
            auto *Box = NewObject<UBoxComponent>(ProbeCover);
            Box->SetBoxExtent(FVector(30, ProbeStep == 7 ? 2000 : 180, 220));
            Box->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
            Box->SetCollisionResponseToAllChannels(ECR_Ignore);
            Box->SetCollisionResponseToChannel(ECC_Visibility, ECR_Block);
            ProbeCover->SetRootComponent(Box);
            Box->RegisterComponent();
            ProbeCover->SetActorLocation((HostPawn->GetActorLocation() + Remote->GetActorLocation()) * .5f);
        }
        // Allow stance, ammo and fixture transforms to reach the owning client.
        Match()->Announcement = TEXT("Preparing combat test");
        ProbeReserve = 1;
        ProbeAfter = Now + 1.5;
        return;
    }
    if (ProbeReserve == 1)
    {
        Match()->Announcement = FString::Printf(TEXT("PFCombatCase:%d"), ProbeStep);
        ProbeReserve = 2;
        ProbeAfter = Now + 3;
        return;
    }
    switch (ProbeStep)
    {
    case 1:
        Check(HostPawn->Health == 20, TEXT("remote scoped torso shot damages actual standing player"));
        Check(Remote->Ammo == 4, TEXT("remote player shot spends exactly one server round"));
        break;
    case 2:
        Check(HostPawn->bIsCrouched, TEXT("target crouch is authoritative"));
        Check(Remote->bIsCrouched, TEXT("remote shooter crouch reaches authority"));
        Check(HostPawn->Health == 20, TEXT("remote shot hits crouched player skeletal collision"));
        break;
    case 3:
        Check(HostPawn->Health == 100, TEXT("solid current cover prevents player damage"));
        Check(Remote->Ammo == 4, TEXT("blocked shot still spends ammunition"));
        break;
    case 4:
        Check(HostPawn->Health == 20, TEXT("third-person scoped shot hits player under crosshair"));
        break;
    case 5:
        Check(Remote->Health == 20, TEXT("host torso shot uses the same player damage"));
        break;
    case 6:
        Check(HostPawn->Health == 20, TEXT("delayed remote shot hits a moving player's rendered torso"));
        Check(FVector::Dist2D(ProbeHostOrigin, HostPawn->GetActorLocation()) > 500,
              TEXT("target actually crosses the scene through CharacterMovement"));
        break;
    case 7:
        Check(HostPawn->Health == 100, TEXT("moving-player rewind cannot bypass current solid cover"));
        Check(Remote->Ammo == 4, TEXT("blocked moving-target shot consumes one round"));
        break;
    case 8:
        Check(Remote->Health == 20, TEXT("host shot hits a moving client using its displayed torso"));
        Check(FVector::Dist2D(ProbeRemoteOrigin, Remote->GetActorLocation()) > 500,
              TEXT("remote target actually moves through predicted CharacterMovement"));
        break;
    case 9:
        Check(HostPawn->Health == 0, TEXT("remote head shot eliminates an unarmored player"));
        break;
    }
    ProbeStep++;
    ProbeReserve = 0;
    ProbeAfter = Now + .5;
    if (ProbeStep > 9)
        Finish();
}
