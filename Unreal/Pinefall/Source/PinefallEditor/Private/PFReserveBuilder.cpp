#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Editor.h"
#include "Engine/StaticMesh.h"
#include "Landscape.h"
#include "LandscapeSubsystem.h"
#include "EngineUtils.h"
#include "Materials/MaterialInterface.h"
#include "PFReserveLayout.h"
#include "PFWorldBuilder.h"
#include "StaticMeshResources.h"

float UPFWorldBuilder::ReserveHeight(float X, float Y)
{
    return FPFReserveLayout::Height(X, Y);
}
void UPFWorldBuilder::BuildReserveLandscape()
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return;
    constexpr int32 Size = 505;
    TArray<uint16> Heights;
    Heights.SetNumUninitialized(Size * Size);
    for (int32 Y = 0; Y < Size; Y++)
        for (int32 X = 0; X < Size; X++)
            Heights[Y * Size + X] = uint16(FMath::Clamp(
                FMath::RoundToInt(
                    32768 + FPFReserveLayout::Height(-100800 + X * 400, -100800 + Y * 400) * 1.28f),
                0, 65535));
    auto *Land = World->SpawnActor<ALandscape>();
    Land->SetActorLabel(TEXT("Wildfront Reserve - 2016 metre landscape"));
    Land->SetActorLocation(FVector(-100800, -100800, 0));
    Land->SetActorScale3D(FVector(400, 400, 100));
    Land->MaxLODLevel = 3;
    Land->LandscapeMaterial = LoadObject<UMaterialInterface>(
        nullptr, TEXT("/Game/Pinefall/Materials/M_ReserveTerrain.M_ReserveTerrain"));
    TMap<FGuid, TArray<uint16>> Data;
    Data.Add(FGuid(), MoveTemp(Heights));
    TMap<FGuid, TArray<FLandscapeImportLayerInfo>> Layers;
    Layers.Add(FGuid(), TArray<FLandscapeImportLayerInfo>());
    Land->Import(FGuid::NewGuid(), 0, 0, Size - 1, Size - 1, 1, 63, Data, nullptr, Layers,
                 ELandscapeImportAlphamapType::Additive, TArrayView<const FLandscapeLayer>());
    Land->PostEditChange();
    UE_LOG(LogTemp, Display, TEXT("PF_RESERVE_LANDSCAPE_OK span=2016m samples=505"));
}
bool UPFWorldBuilder::BuildReserveNanite()
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return false;
    TArray<ALandscapeProxy *> Proxies;
    for (TActorIterator<ALandscape> It(World); It; ++It)
    {
        // Keep the separate Cedar regression arena unchanged.
        if (!It->GetActorLabel().StartsWith(TEXT("Wildfront Reserve")))
            continue;
        It->Modify();
        if (!It->IsNaniteEnabled())
            return false;
        Proxies.Add(*It);
    }
    if (Proxies.IsEmpty())
        return false;
    World->GetSubsystem<ULandscapeSubsystem>()->BuildNanite(UE::Landscape::EBuildFlags::None, Proxies);
    for (auto *Proxy : Proxies)
        if (!Proxy->IsNaniteMeshUpToDate() || !Proxy->HasNaniteComponents())
            return false;
    UE_LOG(LogTemp, Display, TEXT("PF_RESERVE_NANITE_OK landscapes=%d"), Proxies.Num());
    return true;
}
void UPFWorldBuilder::PopulateReserve(const TArray<UStaticMesh *> &Trees, UStaticMesh *Grass,
                                      UStaticMesh *Fern)
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return;
    auto Create = [&](UStaticMesh *Mesh, const TCHAR *Label, int32 Cull, bool Block) {
        auto *Actor = World->SpawnActor<AActor>();
        Actor->SetActorLabel(Label);
        auto *Group = NewObject<UHierarchicalInstancedStaticMeshComponent>(Actor);
        Actor->AddInstanceComponent(Group);
        Actor->SetRootComponent(Group);
        Group->SetMobility(EComponentMobility::Static);
        Group->SetStaticMesh(Mesh);
        Group->SetCullDistances(Cull * .8f, Cull);
        Group->SetCollisionProfileName(Block ? TEXT("BlockAll") : TEXT("NoCollision"));
        Group->RegisterComponent();
        return Group;
    };
    TArray<UHierarchicalInstancedStaticMeshComponent *> Canopies;
    for (auto *Mesh : Trees)
        Canopies.Add(Create(Mesh, TEXT("Reserve canopy - natural-scale scans"), 50000, false));
    if (Canopies.Num() >= 5)
        for (int32 I = 0; I < Trees[4]->GetStaticMaterials().Num(); I++)
            Canopies[4]->SetMaterial(
                I, LoadObject<UMaterialInterface>(
                       nullptr,
                       *FString::Printf(
                           TEXT("/Game/Pinefall/Materials/Scenery/M_SnowFir_%d.M_SnowFir_%d"), I, I)));
    auto *Trunks =
        Create(LoadObject<UStaticMesh>(
                   nullptr, TEXT("/Game/Pinefall/Art/World/SM_TrunkCollision.SM_TrunkCollision")),
               TEXT("Reserve trunk collision"), 50000, true);
    Trunks->SetVisibility(false);
    Trunks->SetHiddenInGame(true);
    Trunks->SetCastShadow(false);
    auto *Low = Create(Grass, TEXT("Reserve low meadow and woodland grass"), 5500, false);
    Low->SetCastShadow(false);
    // Fine, short-lived ground cover receives lighting, but must not populate
    // Lumen's persistent scene alongside the landscape and scanned canopy.
    Low->SetAffectDynamicIndirectLighting(false);
    Low->SetAffectDistanceFieldLighting(false);
    Low->bEnableDensityScaling = true;
    auto LoadScenery = [](const TCHAR *Name) {
        return LoadObject<UStaticMesh>(
            nullptr, *FString::Printf(TEXT("/Game/Pinefall/Art/Scenery/%s.%s"), Name, Name));
    };
    auto *Dry =
        Create(LoadScenery(TEXT("SM_DryGrassPatch")), TEXT("Reserve dry short grass"), 6000, false);
    Dry->SetCastShadow(false);
    Dry->SetAffectDynamicIndirectLighting(false);
    Dry->SetAffectDistanceFieldLighting(false);
    Dry->bEnableDensityScaling = true;
    auto *Brush = Create(Fern, TEXT("Reserve scanned understory"), 18000, false);
    auto *Rocks =
        Create(LoadScenery(TEXT("SM_MossRock")), TEXT("Reserve scanned boulders"), 60000, true);
    auto *FieldRocks =
        Create(LoadScenery(TEXT("SM_FieldRock")), TEXT("Reserve weathered field stones"), 60000, true);
    auto *Cliffs =
        Create(LoadScenery(TEXT("SM_CliffFace")), TEXT("Reserve scanned rock faces"), 70000, true);
    // Quantized landscape vertices are shared by every placement. Interpolating
    // the actual 4 m triangles prevents short plants floating above a different
    // analytic height field, and avoids repeated expensive terrain synthesis.
    constexpr int32 Grid = 505;
    TArray<float> Surface;
    Surface.SetNumUninitialized(Grid * Grid);
    for (int32 Y = 0; Y < Grid; Y++)
        for (int32 X = 0; X < Grid; X++)
            Surface[Y * Grid + X] =
                FMath::RoundToInt(FPFReserveLayout::Height(-100800 + X * 400, -100800 + Y * 400) *
                                  1.28f) /
                1.28f;
    auto Ground = [&](float X, float Y) {
        float GX = (X + 100800) / 400, GY = (Y + 100800) / 400;
        int32 IX = FMath::Clamp(FMath::FloorToInt(GX), 0, Grid - 2),
              IY = FMath::Clamp(FMath::FloorToInt(GY), 0, Grid - 2);
        float U = FMath::Clamp(GX - IX, 0.f, 1.f), V = FMath::Clamp(GY - IY, 0.f, 1.f);
        float A = Surface[IY * Grid + IX], B = Surface[IY * Grid + IX + 1],
              C = Surface[(IY + 1) * Grid + IX], D = Surface[(IY + 1) * Grid + IX + 1];
        return U > V ? A + (B - A) * U + (D - B) * V : A + (D - C) * U + (C - A) * V;
    };
    FRandomStream Random(31809);
    // Keep the exact house footprint and approach clear. Short grass may fill
    // verges and yards; a whole village is no longer one empty exclusion disc.
    auto Clear = [&](FVector2D P, bool Tree) {
        for (const auto &S : FPFReserveLayout::Sites())
        {
            FVector2D Local = P - S.Point;
            if (FMath::Abs(Local.X) < (Tree ? 1400 : 510) && FMath::Abs(Local.Y) < 3300)
                return true;
            for (int32 Side : {-1, 1})
                for (float Row : {-2400.f, -800.f, 800.f, 2400.f})
                    if (FMath::Abs(Local.X - Side * 1800) < (Tree ? 850 : 580) &&
                        FMath::Abs(Local.Y - Row) < (Tree ? 760 : 440))
                        return true;
        }
        auto Sites = FPFReserveLayout::Sites();
        for (const auto &R : FPFReserveLayout::Roads())
        {
            FVector2D A = Sites[R.X].Point, B = Sites[R.Y].Point;
            float T =
                FMath::Clamp(FVector2D::DotProduct(P - A, B - A) / (B - A).SizeSquared(), 0.f, 1.f);
            if (FVector2D::Distance(P, FMath::Lerp(A, B, T)) < (Tree ? 700 : 430))
                return true;
        }
        return false;
    };
    int32 Count = 0;
    for (int32 I = 0; I < 300000; I++)
    {
        float X = Random.FRandRange(-99000, 99000), Y = Random.FRandRange(-99000, 99000);
        FVector2D P(X, Y);
        if (FPFReserveLayout::Water(X, Y) || Clear(P, I < 52000))
            continue;
        uint8 Biome = FPFReserveLayout::Biome(X, Y);
        float Z = Ground(X, Y);
        float Slope = FMath::Abs(Ground(X + 200, Y) - Z) + FMath::Abs(Ground(X, Y + 200) - Z);
        FTransform T(FRotator(0, Random.FRandRange(0, 360), 0), FVector(X, Y, Z));
        if (I < 4200 || (Slope > 130 && I < 10000))
        {
            float Scale = Random.FRandRange(.7f, 2.6f);
            T.SetScale3D(FVector(Scale));
            T.AddToTranslation(FVector(0, 0, -Scale * 18));
            if (Slope > 200 && (Biome == 2 || Biome == 4))
            {
                T.SetScale3D(FVector(Random.FRandRange(1.f, 2.3f)));
                Cliffs->AddInstance(T);
            }
            else if (Biome == 0 || Biome == 1 || Biome == 5)
                Rocks->AddInstance(T);
            else
                FieldRocks->AddInstance(T);
            continue;
        }
        float Density = Biome == 0   ? .94f
                        : Biome == 1 ? .98f
                        : Biome == 2 ? .2f
                        : Biome == 3 ? .14f
                        : Biome == 4 ? .42f
                                     : .14f;
        if (Biome == 4 && Z > 8500)
            Density *= .1f; // Windswept ice country above the treeline.
        if (I < 52000 && Slope < 180 && Random.FRand() < Density && Canopies.Num() >= 5)
        {
            int32 N = Biome == 3             ? 3
                      : Biome == 4           ? 4
                      : Biome == 1           ? 2
                      : Biome == 2           ? 0
                      : Random.FRand() < .4f ? 0
                                             : 1;
            float Scale = Random.FRandRange(.78f, 1.22f);
            T.SetScale3D(FVector(Scale));
            Canopies[N]->AddInstance(T);
            float H = FMath::Min(800.f, Trees[N]->GetBounds().BoxExtent.Z * 1.1f) * Scale;
            float R = (N == 2 ? 35.f : N == 3 ? 20.f : 27.f) * Scale;
            Trunks->AddInstance(FTransform(FRotator::ZeroRotator, FVector(X, Y, Z + H * .5f),
                                           FVector(R * 2 / 100, R * 2 / 100, H / 100)));
            Count++;
        }
        else if (I < 68000 && (Biome == 0 || Biome == 1 || Biome == 5) && Slope < 140)
        {
            T.SetScale3D(FVector(Random.FRandRange(.55f, 1.2f)));
            Brush->AddInstance(T);
        }
        else if (I >= 68000 && Slope < 150 && (Biome != 4 || (Z < 8000 && Random.FRand() < .1f)))
        {
            auto *Group = Biome == 2 || Biome == 3 || Biome == 4 ? Dry : Low;
            int32 Patches = Biome == 3 || Biome == 4 ? 1 : 3;
            if (Biome == 3 && Random.FRand() > .55f)
                continue;
            // Understory grows in small pockets between grass patches, with
            // wetter jungle supporting more cover than woodland or open plains.
            float Understory = Biome == 1 ? .10f : Biome == 0 ? .035f : Biome == 5 ? .006f : 0.f;
            if (Understory > 0 && Random.FRand() < Understory)
            {
                T.SetScale3D(FVector(Biome == 1 ? Random.FRandRange(.7f, 1.1f) : Random.FRandRange(.35f, .75f)));
                Brush->AddInstance(T);
            }
            for (int32 K = 0; K < Patches; K++)
            {
                float GX = X + Random.FRandRange(-180, 180), GY = Y + Random.FRandRange(-180, 180);
                if (Clear(FVector2D(GX, GY), false) || FPFReserveLayout::Water(GX, GY))
                    continue;
                Group->AddInstance(FTransform(FRotator(0, Random.FRandRange(0, 360), 0),
                                              FVector(GX, GY, Ground(GX, GY)),
                                              FVector(Biome == 4 ? Random.FRandRange(.45f, .7f) : Random.FRandRange(.9f, 1.4f))));
            }
        }
    }
    for (auto *G : Canopies)
        G->BuildTreeIfOutdated(false, true);
    for (auto *G : {Trunks, Low, Dry, Brush, Rocks, FieldRocks, Cliffs})
        G->BuildTreeIfOutdated(false, true);
    UE_LOG(LogTemp, Display,
           TEXT("PF_RESERVE_FOLIAGE_OK trees=%d grass=%d dry=%d brush=%d rocks=%d cliffs=%d"), Count,
           Low->GetInstanceCount(), Dry->GetInstanceCount(), Brush->GetInstanceCount(),
           Rocks->GetInstanceCount() + FieldRocks->GetInstanceCount(), Cliffs->GetInstanceCount());
}

FString UPFWorldBuilder::ReserveLayoutJson()
{
    FString Json = TEXT("{\"sites\":[");
    bool First = true;
    for (const auto &Site : FPFReserveLayout::Sites())
    {
        if (!First)
            Json += TEXT(",");
        First = false;
        Json += FString::Printf(TEXT("{\"name\":\"%s\",\"x\":%.0f,\"y\":%.0f,\"biome\":%d}"), Site.Name,
                                Site.Point.X, Site.Point.Y, Site.Biome);
    }
    Json += TEXT("],\"roads\":[");
    First = true;
    for (const auto &Road : FPFReserveLayout::Roads())
    {
        if (!First)
            Json += TEXT(",");
        First = false;
        Json += FString::Printf(TEXT("[%d,%d]"), Road.X, Road.Y);
    }
    return Json + TEXT("]}");
}

int32 UPFWorldBuilder::BuildReserveRoads()
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return 0;
    auto *Actor = World->SpawnActor<AActor>();
    Actor->SetActorLabel(TEXT("Connected graded roads"));
    auto *Group = NewObject<UHierarchicalInstancedStaticMeshComponent>(Actor);
    Actor->AddInstanceComponent(Group);
    Actor->SetRootComponent(Group);
    Group->SetMobility(EComponentMobility::Static);
    Group->SetStaticMesh(LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube")));
    Group->SetMaterial(
        0, LoadObject<UMaterialInterface>(
               nullptr, TEXT("/Game/Pinefall/Materials/Reserve/M_ReserveRoad.M_ReserveRoad")));
    Group->SetCollisionProfileName(TEXT("NoCollision"));
    Group->RegisterComponent();
    int32 Count = 0;
    auto Sites = FPFReserveLayout::Sites();
    for (const auto &Link : FPFReserveLayout::Roads())
    {
        FVector2D A = Sites[Link.X].Point, B = Sites[Link.Y].Point, D = B - A;
        float Length = D.Size();
        auto Direction = D.GetSafeNormal();
        int32 Steps = FMath::CeilToInt(Length / 400);
        for (int32 I = 0; I < Steps; I++)
        {
            FVector2D P = FMath::Lerp(A, B, (I + .5f) / Steps);
            float Z = FPFReserveLayout::Height(P.X, P.Y) + 3;
            float H = FPFReserveLayout::Height(P.X + Direction.X * 200, P.Y + Direction.Y * 200) -
                      FPFReserveLayout::Height(P.X - Direction.X * 200, P.Y - Direction.Y * 200);
            FRotator Rotation(FMath::RadiansToDegrees(FMath::Atan2(H, 400.f)),
                              FMath::RadiansToDegrees(FMath::Atan2(D.Y, D.X)), 0);
            Group->AddInstance(FTransform(Rotation, FVector(P.X, P.Y, Z),
                                          FVector(Length / Steps / 100 + .08f, 8, .045f)));
            Count++;
        }
    }
    Group->BuildTreeIfOutdated(false, true);
    return Count;
}
