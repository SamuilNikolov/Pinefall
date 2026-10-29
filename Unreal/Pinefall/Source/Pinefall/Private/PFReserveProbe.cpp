// Opt-in multiplayer reserve integration; positions/stock are fixtures, input is owned remotely.
#include "Components/CapsuleComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/DamageEvents.h"
#include "Engine/StaticMeshActor.h"
#include "EngineUtils.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Materials/MaterialInterface.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFInventoryComponent.h"
#include "PFMatchState.h"
#include "PFPlayerController.h"
#include "PFReserveWorld.h"
#include "PFVehicle.h"
#include "PFWorldPickup.h"
#include "Serialization/JsonSerializer.h"
#include "TimerManager.h"

void APFCharacter::RunReserveProbeInput()
{
    if (!bReserveProbe || HasAuthority() || !IsLocallyControlled())
        return;
    auto *Round = GetWorld()->GetGameState<APFMatchState>();
    auto *PC = Cast<APFPlayerController>(Controller);
    if (!Round || !PC || !Round->Announcement.StartsWith(TEXT("PFReserveCase:")))
        return;
    int32 Case = FCString::Atoi(*Round->Announcement.Mid(14));
    if ((Case == 2 || Case == 7) && Vehicle)
    {
        Move(FInputActionValue(FVector2D(Case == 2 ? .24f : .10f, 1.f)));
        bDriveBoost = true;
    }
    if (Case == LastReserveProbeCase)
        return;
    LastReserveProbeCase = Case;
    if (Case == 1)
        for (TActorIterator<APFReserveWorld> It(GetWorld()); It; ++It)
            UE_LOG(LogTemp, Display, TEXT("PF_RESERVE_CLIENT stock=%d authoritative_world=%d"),
                   It->Vehicles.Num(), It->HasAuthority());
    if (Case == 1 || Case == 6)
    {
        APFVehicle *Closest = nullptr;
        float Distance = FLT_MAX;
        for (TActorIterator<APFVehicle> It(GetWorld()); It; ++It)
        {
            float D = FVector::DistSquared(GetActorLocation(), It->GetActorLocation());
            if (D < Distance)
            {
                Closest = *It;
                Distance = D;
            }
        }
        ServerVehicleInteract(Closest, false);
    }
    if (Case == 3)
        PC->SetMenu(true);
    if (Case == 4)
    {
        PC->SetMenu(false);
        if (Vehicle)
            ServerVehicleInteract(Vehicle, false);
    }
    if (Case == 5)
    {
        APFVehicle *Closest = nullptr;
        float Distance = FLT_MAX;
        for (TActorIterator<APFVehicle> It(GetWorld()); It; ++It)
        {
            float D = FVector::DistSquared(GetActorLocation(), It->GetActorLocation());
            if (D < Distance)
            {
                Closest = *It;
                Distance = D;
            }
        }
        FocusedVehicle = Closest;
        Reload();
    }
    if (Case == 11)
        Crouch();
    if (Case == 12)
        UnCrouch();
    if (Case == 9)
        for (int32 I = 0; I < Inventory->Slots.Num(); I++)
        {
            const auto &Stack = Inventory->Slots[I];
            if (Stack.Kind == EPFItemKind::Food && Stack.Count > 0)
            {
                Inventory->ServerDropSlot(I, 1, Stack.Revision);
                break;
            }
        }
    if (Case == 10)
        for (TActorIterator<APFWorldPickup> It(GetWorld()); It; ++It)
            if (It->GetOwner() == this && It->Item.Kind == EPFItemKind::Food &&
                FVector::DistSquared(GetActorLocation(), It->GetActorLocation()) < 160000)
            {
                ServerInteract(*It);
                break;
            }
}
void APFGameMode::RunReserveProbe()
{
    if (!Host.IsValid() || GetWorld()->GetTimeSeconds() < ProbeAfter)
        return;
    auto *HostPawn = Cast<APFCharacter>(Host->GetPawn());
    APFCharacter *Remote = nullptr;
    for (APlayerState *Entry : Match()->PlayerArray)
        if (auto *P = Cast<APFCharacter>(Entry->GetPawn()))
            if (P != HostPawn)
            {
                Remote = P;
                break;
            }
    if (!Remote || !HostPawn)
        return;
    APFReserveWorld *Reserve = nullptr;
    for (TActorIterator<APFReserveWorld> It(GetWorld()); It; ++It)
    {
        Reserve = *It;
        break;
    }
    auto Check = [&](bool Pass, const TCHAR *Text) {
        (Pass ? ProbePassed : ProbeFailed).Add(Text);
        UE_LOG(LogTemp, Display, TEXT("PF_RESERVE_CHECK %s %s"), Pass ? TEXT("PASS") : TEXT("FAIL"),
               Text);
    };
    auto Finish = [&]() {
        GetWorldTimerManager().ClearTimer(ProbeTimer);
        TSharedRef<FJsonObject> Result = MakeShared<FJsonObject>();
        TArray<TSharedPtr<FJsonValue>> Passed, Failed;
        for (auto &S : ProbePassed)
            Passed.Add(MakeShared<FJsonValueString>(S));
        for (auto &S : ProbeFailed)
            Failed.Add(MakeShared<FJsonValueString>(S));
        Result->SetArrayField(TEXT("passed"), Passed);
        Result->SetArrayField(TEXT("failed"), Failed);
        FString Json, Path;
        FJsonSerializer::Serialize(Result, TJsonWriterFactory<>::Create(&Json));
        if (FParse::Value(FCommandLine::Get(), TEXT("PFReserveReport="), Path))
            FFileHelper::SaveStringToFile(Json, *Path);
    };
    auto CountPickups = [&]() {
        int32 Count = 0;
        for (TActorIterator<APFWorldPickup> It(GetWorld()); It; ++It)
            Count++;
        return Count;
    };
    auto Near = [&](APFVehicle *V) {
        Remote->GetCharacterMovement()->StopMovementImmediately();
        Remote->SetActorLocation(V->GetActorLocation() + V->GetActorRightVector() * 260 +
                                 FVector(0, 0, 25));
    };
    auto Announce = [&](int32 Case, float Wait) {
        Match()->Announcement = FString::Printf(TEXT("PFReserveCase:%d"), Case);
        ProbeAfter = GetWorld()->GetTimeSeconds() + Wait;
    };
    if (!ProbeFailed.IsEmpty())
    {
        Finish();
        return;
    }
    if (ProbeStep == 0)
    {
        Check(Reserve != nullptr, TEXT("larger reserve gameplay actor exists"));
        if (!Reserve)
        {
            Finish();
            return;
        }
        Check(Match()->ArenaHalfExtent > 99000, TEXT("round and map use the larger reserve bounds"));
        Check(Reserve->LootPoints.Num() == 208,
              TEXT("all eight settlements have interior and junction supplies"));
        Check(Reserve->Vehicles.Num() == 19,
              TEXT("sixteen trucks and three boats populate the reserve"));
        Check(CountPickups() == 208, TEXT("actual replicated world loot populated"));
        bool Materials = true;
        for (int32 I = 0; I < Remote->GetMesh()->GetNumMaterials(); I++)
        {
            auto *M = Remote->GetMesh()->GetMaterial(I);
            Materials &= M && M->GetName().StartsWith(TEXT("M_Ranger_"));
        }
        Check(Materials, TEXT("player mesh uses repaired explicit character materials"));
        bool Upright = true;
        AStaticMeshActor *Cabin = nullptr;
        for (TActorIterator<AStaticMeshActor> It(GetWorld()); It; ++It)
            if (It->GetStaticMeshComponent()->GetStaticMesh() &&
                It->GetStaticMeshComponent()->GetStaticMesh()->GetName() == TEXT("SM_RangerCabin"))
            {
                Upright &= FMath::Abs(It->GetActorRotation().Pitch) < 1 &&
                           FMath::Abs(It->GetActorRotation().Roll) < 1;
                Cabin = *It;
            }
        Check(Upright && Cabin, TEXT("settlement buildings stand upright on their foundations"));
        if (Cabin)
        {
            FCollisionQueryParams Q(SCENE_QUERY_STAT(PFCabinDoor));
            auto Transform = Cabin->GetActorTransform();
            bool Door = GetWorld()->SweepTestByChannel(
                Transform.TransformPosition(FVector(0, -600, 130)),
                Transform.TransformPosition(FVector(0, -250, 130)), FQuat::Identity, ECC_Pawn,
                FCollisionShape::MakeCapsule(34, 90), Q);
            bool Wall = GetWorld()->SweepTestByChannel(Transform.TransformPosition(FVector(0, 600, 130)),
                                                       Transform.TransformPosition(FVector(0, 250, 130)),
                                                       FQuat::Identity, ECC_Pawn,
                                                       FCollisionShape::MakeCapsule(34, 90), Q);
            Check(!Door, TEXT("actual player capsule fits through cabin doorway"));
            Check(Wall, TEXT("actual player capsule cannot cross cabin rear wall"));
        }
        ProbeVehicle = Reserve->Vehicles[0];
        FVector Place(-32000, -27000, 0);
        if (GroundPoint(Place))
            Place.Z += 10;
        ProbeVehicle->SetActorLocationAndRotation(Place, FRotator(0, -90, 0));
        Near(ProbeVehicle);
        ProbeStep = 1;
        ProbeReserve = 0;
        ProbeAfter = GetWorld()->GetTimeSeconds() + 2;
        return;
    }
    if (ProbeStep == 1 && ProbeReserve == 0)
    {
        Announce(1, 2);
        ProbeReserve = 1;
        return;
    }
    if (ProbeStep == 1)
    {
        Check(Remote->Vehicle == ProbeVehicle && ProbeVehicle->Driver == Remote,
              TEXT("remote owning client enters truck through validated request"));
        Check(!ProbeVehicle->Enter(HostPawn), TEXT("occupied driver seat rejects a second entrant"));
        Check(Remote->GetAttachParentActor() == ProbeVehicle,
              TEXT("driver attachment is authoritative"));
        Reserve->RefreshMapFeed();
        auto *HostPC = Cast<APFPlayerController>(Host.Get());
        bool Hidden = true;
        for (auto &Pin : HostPC->MapMarkers)
            if (Pin.Kind == 1 &&
                FVector::DistSquared(Pin.Position, ProbeVehicle->GetActorLocation()) < 100)
                Hidden = false;
        Check(Hidden, TEXT("enemy occupied vehicle is absent from host map feed"));
        ReserveProbeOrigin = ProbeVehicle->GetActorLocation();
        ReserveProbeYaw = ProbeVehicle->GetActorRotation().Yaw;
        ProbeStep = 2;
        Announce(2, 4);
        return;
    }
    if (ProbeStep == 2)
    {
        Check(FVector::Dist2D(ReserveProbeOrigin, ProbeVehicle->GetActorLocation()) > 800,
              TEXT("real remote throttle drives truck across terrain"));
        Check(FMath::Abs(
                  FRotator::NormalizeAxis(ProbeVehicle->GetActorRotation().Yaw - ReserveProbeYaw)) > 8,
              TEXT("real remote steering changes truck heading"));
        Check(ProbeVehicle->Fuel < 100, TEXT("moving truck consumes server fuel"));
        ReserveProbeOrigin = ProbeVehicle->GetActorLocation();
        ProbeStep = 3;
        Announce(3, 1);
        return;
    }
    if (ProbeStep == 3)
    {
        Check(FVector::Dist2D(ReserveProbeOrigin, ProbeVehicle->GetActorLocation()) > 100,
              TEXT("vehicle simulation continues while client settings menu is open"));
        ProbeStep = 4;
        Announce(4, 2);
        return;
    }
    if (ProbeStep == 4)
    {
        Check(Remote->Vehicle == nullptr && ProbeVehicle->Driver == nullptr,
              TEXT("remote client exits and releases driver seat"));
        Check(!ProbeVehicle->IsActorTickEnabled(), TEXT("empty parked vehicle stops simulation tick"));
        Check(Remote->GetCapsuleComponent()->GetCollisionEnabled() != ECollisionEnabled::NoCollision,
              TEXT("exit restores player collision"));
        ProbeVehicle->Health = 100;
        ProbeVehicle->Fuel = 10;
        Near(ProbeVehicle);
        FPFItemStack Kit;
        Kit.Kind = EPFItemKind::ServiceKit;
        Kit.Count = 1;
        Remote->Inventory->Add(Kit);
        ProbeStep = 5;
        ProbeReserve = 0;
        ProbeAfter = GetWorld()->GetTimeSeconds() + 1;
        return;
    }
    if (ProbeStep == 5 && ProbeReserve == 0)
    {
        Announce(5, 2);
        ProbeReserve = 1;
        return;
    }
    if (ProbeStep == 5)
    {
        Check(ProbeVehicle->Health == 250 && ProbeVehicle->Fuel == 45,
              TEXT("remote service action repairs and refuels vehicle"));
        Check(Remote->Inventory->CountKind(EPFItemKind::ServiceKit) == 0,
              TEXT("service consumes exactly one carried kit"));
        Remote->ServerVehicleInteract(ProbeVehicle, true);
        Check(ProbeVehicle->Health == 250, TEXT("service without a kit grants no free repair"));
        for (auto &V : Reserve->Vehicles)
            if (V->bBoat)
            {
                ProbeVehicle = V;
                break;
            }
        Near(ProbeVehicle);
        ProbeStep = 6;
        ProbeReserve = 0;
        ProbeAfter = GetWorld()->GetTimeSeconds() + 2;
        return;
    }
    if (ProbeStep == 6 && ProbeReserve == 0)
    {
        Announce(6, 2);
        ProbeReserve = 1;
        return;
    }
    if (ProbeStep == 6)
    {
        Check(Remote->Vehicle == ProbeVehicle && ProbeVehicle->bBoat,
              TEXT("remote client boards the lake boat"));
        ReserveProbeOrigin = ProbeVehicle->GetActorLocation();
        ProbeStep = 7;
        Announce(7, 3);
        return;
    }
    if (ProbeStep == 7)
    {
        Check(FVector::Dist2D(ReserveProbeOrigin, ProbeVehicle->GetActorLocation()) > 800,
              TEXT("real remote throttle moves boat across navigable water"));
        Check(FMath::Abs(ProbeVehicle->GetActorLocation().Z + 265) < 2,
              TEXT("boat stays on the real water surface"));
        ProbeVehicle->TakeDamage(1000, FDamageEvent(), Host.Get(), HostPawn);
        ProbeStep = 8;
        ProbeAfter = GetWorld()->GetTimeSeconds() + 1;
        return;
    }
    if (ProbeStep == 8)
    {
        Check(ProbeVehicle->Health == 0 && ProbeVehicle->Driver == nullptr && Remote->Vehicle == nullptr,
              TEXT("destroying occupied boat safely releases driver"));
        FVector Place(-32000, -27000, 0);
        GroundPoint(Place);
        Remote->SetActorLocation(Place);
        Remote->GetCharacterMovement()->SetMovementMode(MOVE_Walking);
        ReserveProbeFood = Remote->Inventory->CountKind(EPFItemKind::Food);
        ReserveProbePickups = CountPickups();
        ProbeStep = 9;
        ProbeReserve = 0;
        ProbeAfter = GetWorld()->GetTimeSeconds() + 2;
        return;
    }
    if (ProbeStep == 9 && ProbeReserve == 0)
    {
        Announce(9, 2);
        ProbeReserve = 1;
        return;
    }
    if (ProbeStep == 9)
    {
        Check(Remote->Inventory->CountKind(EPFItemKind::Food) == ReserveProbeFood - 1 &&
                  CountPickups() == ReserveProbePickups + 1,
              TEXT("owning client drops a real inventory item into world"));
        ProbeStep = 10;
        Announce(10, 2);
        return;
    }
    if (ProbeStep == 10)
    {
        Check(Remote->Inventory->CountKind(EPFItemKind::Food) == ReserveProbeFood &&
                  CountPickups() == ReserveProbePickups,
              TEXT("owning client picks dropped item up without duplication"));
        ReserveProbeOrigin = Remote->GetMesh()->GetBoneLocation(TEXT("head"));
        ProbeStep = 11;
        Announce(11, 2);
        return;
    }
    if (ProbeStep == 11)
    {
        Check(Remote->bIsCrouched &&
                  FMath::IsNearlyEqual(Remote->GetCapsuleComponent()->GetScaledCapsuleHalfHeight(), 72.f,
                                       1.f),
              TEXT("owning client crouch reaches host with matching smaller capsule"));
        Check(Remote->GetMesh()->GetBoneLocation(TEXT("head")).Z < ReserveProbeOrigin.Z - 15,
              TEXT("remote visible crouch lowers animated head on host"));
        ProbeStep = 12;
        Announce(12, 2);
        return;
    }
    if (ProbeStep == 12)
    {
        Check(!Remote->bIsCrouched &&
                  FMath::IsNearlyEqual(Remote->GetCapsuleComponent()->GetScaledCapsuleHalfHeight(), 90.f,
                                       1.f),
              TEXT("owning client uncrouch restores standing capsule on host"));
        Finish();
    }
}
