// Opt-in physical-cover/water regression against the saved playable scene.
#include "PFGameMode.h"
#include "PFCharacter.h"
#include "PFWaterBody.h"
#include "Components/CapsuleComponent.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Engine/StaticMeshActor.h"
#include "Engine/StaticMesh.h"
#include "EngineUtils.h"
#include "TimerManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "Serialization/JsonSerializer.h"
void APFGameMode::RunWorldProbe()
{
    if (!Host.IsValid() || !FParse::Param(FCommandLine::Get(), TEXT("PFWorldProbe")))
        return;
    auto *Player = Cast<APFCharacter>(Host->GetPawn());
    if (!Player)
        return;
    double Now = GetWorld()->GetTimeSeconds();
    if (Now < ProbeAfter)
        return;
    auto Check = [&](bool Value, const TCHAR *Text)
    {
        (Value ? ProbePassed : ProbeFailed).Add(Text);
        UE_LOG(LogTemp, Display, TEXT("PF_WORLD_CHECK %s %s"), Value ? TEXT("PASS") : TEXT("FAIL"), Text);
    };
    APFWaterBody *Water = nullptr;
    for (TActorIterator<APFWaterBody> It(GetWorld()); It; ++It)
    {
        Water = *It;
        break;
    }
    if (ProbeStep == 0)
    {
        UHierarchicalInstancedStaticMeshComponent *Trunks = nullptr;
        AStaticMeshActor *Rock = nullptr;
        for (TActorIterator<AActor> It(GetWorld()); It; ++It)
            if (auto *C = It->FindComponentByClass<UHierarchicalInstancedStaticMeshComponent>())
                if (C->GetStaticMesh() && C->GetStaticMesh()->GetName() == TEXT("SM_TrunkCollision"))
                {
                    Trunks = C;
                    break;
                }
        for (TActorIterator<AStaticMeshActor> It(GetWorld()); It; ++It)
            if (UStaticMesh *Mesh = It->GetStaticMeshComponent()->GetStaticMesh())
                if (Mesh->GetName() == TEXT("SM_Limestone"))
                {
                    Rock = *It;
                    break;
                }
        Check(Trunks && Trunks->GetInstanceCount() > 0,
              TEXT("saved forest contains physical trunk instances"));
        FCollisionQueryParams Query(SCENE_QUERY_STAT(PFWorldProbe), false, Player);
        FHitResult Hit;
        if (Trunks)
        {
            FTransform Instance;
            Trunks->GetInstanceTransform(0, Instance, true);
            FVector Center = Instance.GetLocation();
            Center.Z -= Instance.GetScale3D().Z * 50 - 85; // Walking height, not the canopy.
            bool Block = GetWorld()->SweepSingleByChannel(
                Hit, Center - FVector(400, 0, 0), Center + FVector(400, 0, 0), FQuat::Identity, ECC_Pawn,
                FCollisionShape::MakeCapsule(34, 60), Query);
            Check(Block && Hit.GetComponent() == Trunks, TEXT("player capsule cannot cross a tree trunk"));
            Block = GetWorld()->LineTraceSingleByChannel(Hit, Center - FVector(400, 0, 0),
                                                         Center + FVector(400, 0, 0), ECC_Visibility, Query);
            Check(Block && Hit.GetComponent() == Trunks,
                  TEXT("tree trunk blocks bullets while its collision mesh stays hidden"));
        }
        Check(Rock != nullptr, TEXT("saved scene contains rock cover with explicit collision"));
        if (Rock)
        {
            FVector Center, Extent;
            Rock->GetActorBounds(true, Center, Extent);
            bool Block = GetWorld()->SweepSingleByChannel(
                Hit, Center - FVector(Extent.X + 150, 0, 0), Center + FVector(Extent.X + 150, 0, 0),
                FQuat::Identity, ECC_Pawn, FCollisionShape::MakeCapsule(34, 60), Query);
            Check(Block && Hit.GetActor() == Rock, TEXT("player capsule cannot cross a rock outcrop"));
            Block = GetWorld()->LineTraceSingleByChannel(Hit, Center - FVector(Extent.X + 150, 0, 0),
                                                         Center + FVector(Extent.X + 150, 0, 0),
                                                         ECC_Visibility, Query);
            Check(Block && Hit.GetActor() == Rock, TEXT("rock surface blocks bullets"));
        }
        Check(Water != nullptr, TEXT("lake has a real swimming volume"));
        if (Water)
        {
            Check(!Water->ContainsWater(Water->GetActorLocation() + FVector(4100, 5900, 0)),
                  TEXT("water volume excludes rectangular corners outside the shoreline"));
            Player->SetActorLocation(Water->GetActorLocation() + FVector(0, 0, 920));
            Player->GetCapsuleComponent()->UpdatePhysicsVolume(true);
        }
        ProbeStep = 1;
        ProbeAfter = Now + 1;
        return;
    }
    if (ProbeStep == 1)
    {
        Check(Water && Player->GetPhysicsVolume() == Water,
              TEXT("entering lake selects water physics automatically"));
        Check(Player->GetCharacterMovement()->IsSwimming(),
              TEXT("entering lake starts native predicted swimming"));
        Check(Player->GetCharacterMovement()->ImmersionDepth() > 0 &&
                  Player->GetCharacterMovement()->ImmersionDepth() <= 1,
              TEXT("swimming immersion matches the actual lake surface"));
        Player->SetActorLocation(Water ? Water->GetActorLocation() + FVector(0, 0, 650)
                                       : Player->GetActorLocation());
        Player->GetCapsuleComponent()->UpdatePhysicsVolume(true);
        Player->SurvivalSecond();
        Check(Player->Oxygen == 96, TEXT("submerged head consumes air"));
        Player->Oxygen = 0;
        Player->Armor = 50;
        Player->Health = 100;
        Player->SurvivalSecond();
        Check(Player->Health == 92 && Player->Armor == 50,
              TEXT("drowning damages health and bypasses armor"));
        FVector Exit(-8500, -800, 0);
        if (GroundPoint(Exit))
            Player->SetActorLocation(Exit);
        Player->GetCapsuleComponent()->UpdatePhysicsVolume(true);
        ProbeStep = 2;
        ProbeAfter = Now + 1;
        return;
    }
    Check(!Player->GetCharacterMovement()->IsSwimming(), TEXT("leaving the lake restores land movement"));
    Player->SurvivalSecond();
    Check(Player->Oxygen == 24, TEXT("air replenishes after leaving water"));
    GetWorldTimerManager().ClearTimer(ProbeTimer);
    TSharedRef<FJsonObject> Result = MakeShared<FJsonObject>();
    TArray<TSharedPtr<FJsonValue>> Passed, Failed;
    for (auto &Text : ProbePassed)
        Passed.Add(MakeShared<FJsonValueString>(Text));
    for (auto &Text : ProbeFailed)
        Failed.Add(MakeShared<FJsonValueString>(Text));
    Result->SetArrayField(TEXT("passed"), Passed);
    Result->SetArrayField(TEXT("failed"), Failed);
    FString Json, Report;
    FJsonSerializer::Serialize(Result, TJsonWriterFactory<>::Create(&Json));
    if (FParse::Value(FCommandLine::Get(), TEXT("PFWorldReport="), Report))
    {
        IFileManager::Get().MakeDirectory(*FPaths::GetPath(Report), true);
        FFileHelper::SaveStringToFile(Json, *Report);
    }
}
