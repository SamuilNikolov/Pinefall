#include "PFReserveWorld.h"
#include "EngineUtils.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFMatchState.h"
#include "PFPlayerController.h"
#include "PFReserveLayout.h"
#include "PFVehicle.h"
#include "PFWorldPickup.h"
#include "TimerManager.h"
APFReserveWorld::APFReserveWorld()
{
    PrimaryActorTick.bCanEverTick = false;
    bReplicates = true;
    bAlwaysRelevant = true;
    SetNetUpdateFrequency(1);
}
void APFReserveWorld::BeginPlay()
{
    Super::BeginPlay();
    if (GetNetMode() == NM_Client || !HasAuthority())
        return;
    if (auto *Match = GetWorld()->GetGameState<APFMatchState>())
        Match->ArenaHalfExtent = FPFReserveLayout::HalfExtent - 800;
    ResetVehicles();
    ResetLoot();
    GetWorldTimerManager().SetTimer(MapTimer, this, &APFReserveWorld::RefreshMapFeed, 1, true);
}
void APFReserveWorld::ResetVehicles()
{
    if (GetNetMode() == NM_Client || !HasAuthority())
        return;
    for (auto &Vehicle : Vehicles)
        if (IsValid(Vehicle))
        {
            Vehicle->Exit(true);
            Vehicle->Destroy();
        }
    Vehicles.Reset();
    auto Spawn = [&](const FTransform &Transform, bool Boat) {
        auto *Vehicle = GetWorld()->SpawnActorDeferred<APFVehicle>(
            APFVehicle::StaticClass(), Transform, nullptr, nullptr,
            ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
        if (Vehicle)
        {
            Vehicle->bBoat = Boat;
            Vehicle->FinishSpawning(Transform);
            Vehicles.Add(Vehicle);
            if (!Boat && Vehicles.Num() % 5 == 0)
            {
                Vehicle->Fuel = 0;
                Vehicle->Health = 240;
            }
            if (!Boat && Vehicles.Num() % 9 == 0)
            {
                Vehicle->Health = 0;
                Vehicle->Fuel = 15;
            }
        }
    };
    for (const auto &Point : VehicleStarts)
        Spawn(Point, false);
    for (const auto &Point : BoatStarts)
        Spawn(Point, true);
}
void APFReserveWorld::ResetLoot()
{
    if (GetNetMode() == NM_Client || !HasAuthority())
        return;
    for (TActorIterator<APFWorldPickup> It(GetWorld()); It; ++It)
        It->Destroy();
    FRandomStream Random(FMath::Rand());
    int32 Index = 0;
    for (FVector Position : LootPoints)
    {
        FPFItemStack Item;
        Item.Count = 1;
        // Weighted equipment tiers; rare containers concentrate useful upgrades.
        float Roll = Random.FRand();
        Item.Rarity = Roll < .03 ? 4 : Roll < .12 ? 3 : Roll < .32 ? 2 : Roll < .60 ? 1 : 0;
        switch (Index++ % 11)
        {
        case 0:
        case 1:
            Item.Kind = EPFItemKind::Weapon;
            Item.WeaponId = Index % 2 ? TEXT("Ridgeline") : TEXT("ServiceNine");
            Item.Magazine = Item.WeaponId == TEXT("Ridgeline") ? 5 : 12;
            break;
        case 2:
        case 3:
        case 4:
            Item.Kind = EPFItemKind::Ammo;
            Item.WeaponId = Index % 2 ? TEXT("Ridgeline") : TEXT("ServiceNine");
            Item.Count = Item.WeaponId == TEXT("Ridgeline") ? 15 : 24;
            Item.Rarity = 0;
            break;
        case 5:
        case 6:
            Item.Kind = EPFItemKind::Food;
            Item.Count = 2;
            Item.Rarity = 0;
            break;
        case 7:
            Item.Kind = EPFItemKind::Medkit;
            break;
        case 8:
            Item.Kind = Item.Rarity >= 3   ? EPFItemKind::HeavyVest
                        : Item.Rarity >= 1 ? EPFItemKind::FieldVest
                                           : EPFItemKind::LightVest;
            break;
        case 9:
            Item.Kind = EPFItemKind::ArmorPlate;
            Item.Count = 2;
            break;
        default:
            Item.Kind = EPFItemKind::ServiceKit;
            Item.Rarity = 0;
            break;
        }
        FActorSpawnParameters Params;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        if (auto *Pickup = GetWorld()->SpawnActor<APFWorldPickup>(
                Position, FRotator(0, Random.FRandRange(0, 360), 0), Params))
            Pickup->Initialize(Item);
    }
}
void APFReserveWorld::RefreshMapFeed()
{
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    if (!Match)
        return;
    for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
    {
        auto *PC = Cast<APFPlayerController>(It->Get());
        auto *Mine = PC ? PC->GetPlayerState<APFPlayerState>() : nullptr;
        if (!Mine)
            continue;
        PC->MapMarkers.Reset();
        for (auto &Vehicle : Vehicles)
            if (IsValid(Vehicle))
            {
                auto *Occupant =
                    Vehicle->Driver ? Vehicle->Driver->GetPlayerState<APFPlayerState>() : nullptr;
                if (Occupant && Occupant->TeamId != Mine->TeamId)
                    continue; // Never reveal an enemy through transport.
                FPFMapMarker Pin;
                Pin.Position = Vehicle->GetActorLocation();
                Pin.Kind = Vehicle->bBoat ? 2 : 1;
                Pin.Condition = Vehicle->Health > 0 ? Vehicle->Fuel : -1;
                PC->MapMarkers.Add(Pin);
            }
        for (APlayerState *Entry : Match->PlayerArray)
            if (auto *Ally = Cast<APFPlayerState>(Entry))
                if (Ally->TeamId == Mine->TeamId && !Ally->bEliminated)
                    if (auto *Pawn = Cast<APFCharacter>(Ally->GetPawn()))
                    {
                        FPFMapMarker Pin;
                        Pin.Position = Pawn->GetActorLocation();
                        Pin.Kind = 0;
                        Pin.Label = Ally->GetPlayerName().Left(24);
                        Pin.Condition = Pawn->Health;
                        Pin.PlayerId = Ally->GetPlayerId();
                        PC->MapMarkers.Add(Pin);
                    }
    }
}
