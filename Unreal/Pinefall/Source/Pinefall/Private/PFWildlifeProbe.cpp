// Deliberate synthetic integration test. Never captures a microphone or runs in a normal game.
#include "PFGameMode.h"
#include "PFWildlife.h"
#include "PFCharacter.h"
#include "PFMatchState.h"
#include "PFWorldPickup.h"
#include "PFInventoryComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Components/CapsuleComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Engine/DamageEvents.h"
#include "EngineUtils.h"
#include "TimerManager.h"
#include "Misc/FileHelper.h"
#include "Serialization/JsonSerializer.h"

void APFGameMode::RunWildlifeProbe()
{
    if (!FParse::Param(FCommandLine::Get(), TEXT("PFWildlifeProbe")) || !Host.IsValid() ||
        GetNumPlayers() < 2)
        return;
    double Now = GetWorld()->GetTimeSeconds();
    if (Now < ProbeAfter)
        return;
    auto *Player = Cast<APFCharacter>(Host->GetPawn());
    if (!Player)
        return;
    auto Check = [&](bool Pass, const TCHAR *Text)
    {
        (Pass ? ProbePassed : ProbeFailed).Add(Text);
        UE_LOG(LogTemp, Display, TEXT("PF_FAUNA_CHECK %s %s"), Pass ? TEXT("PASS") : TEXT("FAIL"), Text);
    };
    if (ProbeStep == 0)
    {
        FVector Origin(-10000, 0, 0);
        if (!GroundPoint(Origin))
            return;
        Player->SetActorLocation(Origin);
        Player->Armor = 0;
        for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
            if (It->Get() != Host.Get())
                if (APawn *Pawn = It->Get()->GetPawn())
                    Pawn->SetActorLocation(Origin + FVector(-8000, 0, 0));
        const FName Names[] = {TEXT("Deer"), TEXT("Wolf"), TEXT("Fox")};
        for (int32 I = 0; I < 3; I++)
        {
            FVector Point = Origin + FVector(1300, (I - 1) * 900, 0);
            GroundPoint(Point);
            FTransform Transform(FRotator::ZeroRotator, Point);
            auto *Animal = GetWorld()->SpawnActorDeferred<APFWildlife>(
                APFWildlife::StaticClass(), Transform, nullptr, nullptr,
                ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
            Animal->Species = Names[I];
            Animal->FinishSpawning(Transform);
            ProbeAnimals.Add(Animal);
            ProbeAnimalOrigins.Add(Animal->GetActorLocation());
            TArray<TWeakObjectPtr<APFCharacter>> Players;
            Players.Add(Player);
            Animal->Think(Players, false, 0, 24000);
            Check(Animal->GetMesh()->GetSkeletalMeshAsset() && Animal->GetMesh()->GetSingleNodeInstance(),
                  TEXT("spawned animal has a skeletal mesh and playable animation"));
        }
        Check(ProbeAnimals[0]->Motion == EPFWildlifeMotion::Run && !ProbeAnimals[0]->IsPredator(),
              TEXT("peaceful deer enters a fleeing run"));
        Check(ProbeAnimals[1]->Motion == EPFWildlifeMotion::Run && ProbeAnimals[1]->IsPredator(),
              TEXT("wolf enters a pursuing run"));
        ProbeStep = 1;
        ProbeAfter = Now + 2;
    }
    else if (ProbeStep == 1)
    {
        auto *Deer = ProbeAnimals[0].Get();
        auto *Wolf = ProbeAnimals[1].Get();
        Check(FVector::DistSquared2D(Deer->GetActorLocation(), ProbeAnimalOrigins[0]) >
                      FMath::Square(100.f) &&
                  FVector::DistSquared2D(Deer->GetActorLocation(), Player->GetActorLocation()) >
                      FVector::DistSquared2D(ProbeAnimalOrigins[0], Player->GetActorLocation()),
              TEXT("deer actually moves away through CharacterMovement"));
        Check(FVector::DistSquared2D(Wolf->GetActorLocation(), Player->GetActorLocation()) <
                  FVector::DistSquared2D(ProbeAnimalOrigins[1], Player->GetActorLocation()),
              TEXT("wolf actually closes distance through CharacterMovement"));
        Wolf->SetActorLocation(Player->GetActorLocation() + FVector(130, 0, 0));
        ProbeFoodHunger = Player->Health;
        TArray<TWeakObjectPtr<APFCharacter>> Players;
        Players.Add(Player);
        Wolf->Think(Players, false, 0, 24000);
        Check(Wolf->Motion == EPFWildlifeMotion::Attack, TEXT("nearby predator starts an attack animation"));
        ProbeStep = 2;
        ProbeAfter = Now + .6;
    }
    else if (ProbeStep == 2)
    {
        auto *Deer = ProbeAnimals[0].Get();
        auto *Wolf = ProbeAnimals[1].Get();
        TArray<TWeakObjectPtr<APFCharacter>> Players;
        Players.Add(Player);
        Wolf->Think(Players, false, 0, 24000);
        Check(Player->Health < ProbeFoodHunger, TEXT("attack applies damage after its windup"));
        Deer->Think(Players, true, 0, 24000);
        Check(Deer->bEnraged, TEXT("closing storm enrages normally peaceful prey"));
        Deer->Think(Players, false, 1, 24000);
        Check(!Deer->bEnraged, TEXT("settled storm restores normal animal behavior"));
        Deer->bAlpha = false;
        Deer->ApplyRage();
        float OldRadius = Deer->GetCapsuleComponent()->GetUnscaledCapsuleRadius();
        Deer->bAlpha = true;
        Deer->bEnraged = true;
        Deer->ApplyRage();
        Check(Deer->GetCapsuleComponent()->GetUnscaledCapsuleRadius() > OldRadius,
              TEXT("alpha size changes collision as well as the mesh"));
        float Applied = Deer->TakeDamage(10000, FDamageEvent(), Host.Get(), Player);
        Check(Applied > 0 && Deer->Health == 0 && Deer->Motion == EPFWildlifeMotion::Dead,
              TEXT("authoritative animal kill plays death and disables movement"));
        int32 Food = 0, Equipment = 0;
        for (TActorIterator<APFWorldPickup> It(GetWorld()); It; ++It)
        {
            if (It->Item.Kind == EPFItemKind::Food)
                Food += It->Item.Count;
            else
                Equipment++;
        }
        Check(Food == 3 && Equipment == 2,
              TEXT("alpha kill drops food and exactly two usable equipment rolls"));
        Check(Deer->TakeDamage(10000, FDamageEvent(), Host.Get(), Player) == 0,
              TEXT("dead animal cannot award damage or duplicate rewards"));
        ProbeStep = 3;
        ProbeAfter = Now + 2;
    }
    else
    {
        GetWorldTimerManager().ClearTimer(ProbeTimer);
        auto Result = MakeShared<FJsonObject>();
        TArray<TSharedPtr<FJsonValue>> Passed, Failed;
        for (const auto &Text : ProbePassed)
            Passed.Add(MakeShared<FJsonValueString>(Text));
        for (const auto &Text : ProbeFailed)
            Failed.Add(MakeShared<FJsonValueString>(Text));
        Result->SetArrayField(TEXT("passed"), Passed);
        Result->SetArrayField(TEXT("failed"), Failed);
        Result->SetStringField(
            TEXT("scope"),
            TEXT("Two real processes. Authority arranges encounters and invokes actual wildlife movement, "
                 "attack and damage code. No graphics, audible audio, WAN or load benchmark."));
        FString Json, Path;
        FJsonSerializer::Serialize(Result, TJsonWriterFactory<>::Create(&Json));
        if (FParse::Value(FCommandLine::Get(), TEXT("PFFaunaReport="), Path))
            FFileHelper::SaveStringToFile(Json, *Path);
        UE_LOG(LogTemp, Display, TEXT("PF_FAUNA_PROBE_%s passed=%d failed=%d"),
               ProbeFailed.IsEmpty() ? TEXT("PASS") : TEXT("FAIL"), ProbePassed.Num(), ProbeFailed.Num());
    }
}
