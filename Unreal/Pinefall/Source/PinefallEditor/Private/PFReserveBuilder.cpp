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
                FMath::RoundToInt(32768 +
                                  FPFReserveLayout::Height(-100800 + X * 400, -100800 + Y * 400) * 1.28f),
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
    auto Create = [&](UStaticMesh *Mesh, const TCHAR *Label, int32 Cull, bool Block)
    {
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
                I,
                LoadObject<UMaterialInterface>(
                    nullptr, *FString::Printf(
                                 TEXT("/Game/Pinefall/Materials/Scenery/M_SnowFir_%d.M_SnowFir_%d"), I, I)));
    auto *Trunks = Create(LoadObject<UStaticMesh>(
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
    auto LoadScenery = [](const TCHAR *Name)
    {
        return LoadObject<UStaticMesh>(
            nullptr, *FString::Printf(TEXT("/Game/Pinefall/Art/Scenery/%s.%s"), Name, Name));
    };
    auto *Dry = Create(LoadScenery(TEXT("SM_DryGrassPatch")), TEXT("Reserve dry short grass"), 6000, false);
    Dry->SetCastShadow(false);
    Dry->SetAffectDynamicIndirectLighting(false);
    Dry->SetAffectDistanceFieldLighting(false);
    Dry->bEnableDensityScaling = true;
    auto *Brush = Create(Fern, TEXT("Reserve scanned understory"), 18000, false);
    auto *Rocks = Create(LoadScenery(TEXT("SM_MossRock")), TEXT("Reserve scanned boulders"), 60000, true);
    auto *FieldRocks =
        Create(LoadScenery(TEXT("SM_FieldRock")), TEXT("Reserve weathered field stones"), 60000, true);
    auto *Cliffs = Create(LoadScenery(TEXT("SM_CliffFace")), TEXT("Reserve scanned rock faces"), 70000, true);
    // Quantized landscape vertices are shared by every placement. Interpolating
    // the actual 4 m triangles prevents short plants floating above a different
    // analytic height field, and avoids repeated expensive terrain synthesis.
    constexpr int32 Grid = 505;
    TArray<float> Surface;
    Surface.SetNumUninitialized(Grid * Grid);
    for (int32 Y = 0; Y < Grid; Y++)
        for (int32 X = 0; X < Grid; X++)
            Surface[Y * Grid + X] =
                FMath::RoundToInt(FPFReserveLayout::Height(-100800 + X * 400, -100800 + Y * 400) * 1.28f) /
                1.28f;
    auto Ground = [&](float X, float Y)
    {
        float GX = (X + 100800) / 400, GY = (Y + 100800) / 400;
        int32 IX = FMath::Clamp(FMath::FloorToInt(GX), 0, Grid - 2),
              IY = FMath::Clamp(FMath::FloorToInt(GY), 0, Grid - 2);
        float U = FMath::Clamp(GX - IX, 0.f, 1.f), V = FMath::Clamp(GY - IY, 0.f, 1.f);
        float A = Surface[IY * Grid + IX], B = Surface[IY * Grid + IX + 1], C = Surface[(IY + 1) * Grid + IX],
              D = Surface[(IY + 1) * Grid + IX + 1];
        return U > V ? A + (B - A) * U + (D - B) * V : A + (D - C) * U + (C - A) * V;
    };
    FRandomStream Random(31809);
    // Keep the exact house footprint and approach clear. Short grass may fill
    // verges and yards; a whole village is no longer one empty exclusion disc.
    auto Clear = [&](FVector2D P, bool Tree)
    {
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
        if (FPFReserveLayout::RoadDistance(P.X, P.Y) < (Tree ? 650.f : 340.f))
            return true;
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
        // Large ecological patches make glades and groves rather than uniform planting.
        const float Patch = FMath::Clamp(.75f + FMath::PerlinNoise2D(P / 8700) * 1.25f, .30f, 1.25f);
        Density = FMath::Min(.99f, Density * Patch);
        if (Biome == 4 && Z > 15500)
            Density *= .1f; // Windswept ice country above the treeline.
        if (I < 52000 && Slope < 180 && Random.FRand() < Density && Canopies.Num() >= 5)
        {
            int32 N = Biome == 3             ? 3
                      : Biome == 4           ? 4
                      : Biome == 1           ? 2
                      : Biome == 2           ? 0
                      : Random.FRand() < .4f ? 0
                                             : 1;
            float Scale = Random.FRandRange(.65f, 1.35f);
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
        else if (I >= 68000 && Slope < 150 && (Biome != 4 || (Z < 15000 && Random.FRand() < .1f)))
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
                T.SetScale3D(
                    FVector(Biome == 1 ? Random.FRandRange(.7f, 1.1f) : Random.FRandRange(.35f, .75f)));
                Brush->AddInstance(T);
            }
            for (int32 K = 0; K < Patches; K++)
            {
                float GX = X + Random.FRandRange(-180, 180), GY = Y + Random.FRandRange(-180, 180);
                if (Clear(FVector2D(GX, GY), false) || FPFReserveLayout::Water(GX, GY))
                    continue;
                Group->AddInstance(FTransform(
                    FRotator(0, Random.FRandRange(0, 360), 0), FVector(GX, GY, Ground(GX, GY)),
                    FVector(Biome == 4 ? Random.FRandRange(.45f, .7f) : Random.FRandRange(.9f, 1.4f))));
            }
        }
    }
    for (auto *G : Canopies)
        G->BuildTreeIfOutdated(false, true);
    for (auto *G : {Trunks, Low, Dry, Brush, Rocks, FieldRocks, Cliffs})
        G->BuildTreeIfOutdated(false, true);
    ApplyReserveBiomePresentation();
    UE_LOG(LogTemp, Display,
           TEXT("PF_RESERVE_FOLIAGE_OK trees=%d grass=%d dry=%d brush=%d rocks=%d cliffs=%d"), Count,
           Low->GetInstanceCount(), Dry->GetInstanceCount(), Brush->GetInstanceCount(),
           Rocks->GetInstanceCount() + FieldRocks->GetInstanceCount(), Cliffs->GetInstanceCount());
}

int32 UPFWorldBuilder::ApplyReserveBiomePresentation()
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return -1;
    auto Find = [&](const TCHAR *Label) -> UHierarchicalInstancedStaticMeshComponent *
    {
        for (TActorIterator<AActor> It(World); It; ++It)
            if (It->GetActorLabel() == Label)
                return It->FindComponentByClass<UHierarchicalInstancedStaticMeshComponent>();
        return nullptr;
    };
    auto *Dry = Find(TEXT("Reserve dry short grass"));
    auto *Brush = Find(TEXT("Reserve scanned understory"));
    auto *FieldRocks = Find(TEXT("Reserve weathered field stones"));
    auto *Cliffs = Find(TEXT("Reserve scanned rock faces"));
    auto *Grass =
        LoadObject<UStaticMesh>(nullptr, TEXT("/Game/Pinefall/Art/Scenery/SM_GrassPatch.SM_GrassPatch"));
    auto *Fern = LoadObject<UStaticMesh>(nullptr, TEXT("/Game/Pinefall/Art/Foliage/SM_Fern.SM_Fern"));
    auto *Geology = LoadObject<UMaterialInterface>(
        nullptr, TEXT("/Game/Pinefall/Materials/M_ReserveGeology.M_ReserveGeology"));
    if (!Dry || !Brush || !FieldRocks || !Cliffs || !Grass || !Fern || !Geology)
        return -1;
    for (auto *Group : {FieldRocks, Cliffs})
    {
        Group->Modify();
        for (int32 Slot = 0; Slot < Group->GetStaticMesh()->GetStaticMaterials().Num(); ++Slot)
            Group->SetMaterial(Slot, Geology);
    }
    const FName Applied(TEXT("PFBiomePresentationV1"));
    if (Dry->ComponentHasTag(Applied))
        return 0;
    auto Create = [&](const TCHAR *Label, UStaticMesh *Mesh, int32 Distance, bool Density)
    {
        auto *Actor = World->SpawnActor<AActor>();
        Actor->SetActorLabel(Label);
        auto *Group = NewObject<UHierarchicalInstancedStaticMeshComponent>(Actor);
        Actor->AddInstanceComponent(Group);
        Actor->SetRootComponent(Group);
        Group->SetMobility(EComponentMobility::Static);
        Group->SetStaticMesh(Mesh);
        Group->SetCollisionEnabled(ECollisionEnabled::NoCollision);
        Group->SetCullDistances(Distance * .8f, Distance);
        Group->SetCastShadow(false);
        Group->SetAffectDynamicIndirectLighting(false);
        Group->SetAffectDistanceFieldLighting(false);
        Group->bEnableDensityScaling = Density;
        Group->RegisterComponent();
        return Group;
    };
    auto *Alpine = Create(TEXT("Reserve alpine short grass"), Grass, 5500, true);
    auto *Tundra = Create(TEXT("Reserve tundra grass pockets"), Grass, 5000, true);
    auto *Ferns = Create(TEXT("Reserve woodland and wetland ferns"), Fern, 14000, false);
    TArray<FTransform> Desert, AlpineTransforms, TundraTransforms, Shrubs, FernTransforms;
    for (int32 I = 0; I < Dry->GetInstanceCount(); ++I)
    {
        FTransform T;
        Dry->GetInstanceTransform(I, T, true);
        const FVector P = T.GetLocation();
        const uint8 Biome = FPFReserveLayout::Biome(P.X, P.Y);
        if (Biome == 2 || Biome == 4)
        {
            // A live scan patch contains four times as many plants as the dead
            // patch. Keep the blade budget bounded rather than quadrupling it.
            if ((I & 3) != 0)
                continue;
            T.SetScale3D(T.GetScale3D() * (Biome == 4 ? .65f : .55f));
            (Biome == 4 ? TundraTransforms : AlpineTransforms).Add(T);
        }
        else
        {
            T.SetScale3D(T.GetScale3D() * .55f);
            Desert.Add(T);
        }
    }
    for (int32 I = 0; I < Brush->GetInstanceCount(); ++I)
    {
        FTransform T;
        Brush->GetInstanceTransform(I, T, true);
        const FVector P = T.GetLocation();
        const uint8 Biome = FPFReserveLayout::Biome(P.X, P.Y);
        if ((Biome == 0 || Biome == 1) && (I & 1) == 0)
        {
            T.SetScale3D(T.GetScale3D() * .85f);
            FernTransforms.Add(T);
        }
        else
            Shrubs.Add(T);
    }
    auto Fill = [](UHierarchicalInstancedStaticMeshComponent *Group, const TArray<FTransform> &Transforms)
    {
        Group->Modify();
        Group->ClearInstances();
        Group->AddInstances(Transforms, false, true);
        Group->BuildTreeIfOutdated(false, true);
    };
    Fill(Dry, Desert);
    Fill(Alpine, AlpineTransforms);
    Fill(Tundra, TundraTransforms);
    Fill(Brush, Shrubs);
    Fill(Ferns, FernTransforms);
    Dry->ComponentTags.AddUnique(Applied);
    UE_LOG(LogTemp, Display,
           TEXT("PF_BIOME_PRESENTATION_OK desert=%d alpine=%d tundra=%d shrubs=%d ferns=%d"), Desert.Num(),
           AlpineTransforms.Num(), TundraTransforms.Num(), Shrubs.Num(), FernTransforms.Num());
    return FernTransforms.Num() + AlpineTransforms.Num() + TundraTransforms.Num();
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
    Json += TEXT("],\"road_paths\":[");
    First = true;
    for (const auto &Path : FPFReserveLayout::RoadPaths())
    {
        if (!First)
            Json += TEXT(",");
        First = false;
        Json += TEXT("[");
        bool FirstPoint = true;
        for (const auto &P : Path)
        {
            if (!FirstPoint)
                Json += TEXT(",");
            FirstPoint = false;
            Json += FString::Printf(TEXT("[%.1f,%.1f]"), P.X, P.Y);
        }
        Json += TEXT("]");
    }
    double Length = 0, Straight = 0;
    float Grade = 0, Cut = 0;
    const auto &Paths = FPFReserveLayout::RoadPaths();
    for (int32 K = 0; K < Paths.Num(); K++)
    {
        const auto &Path = Paths[K];
        Straight += FVector2D::Distance(Path[0], Path.Last());
        for (int32 I = 0; I < Path.Num(); I++)
        {
            const auto &P = Path[I];
            const float Z = FPFReserveLayout::Height(P.X, P.Y);
            Cut = FMath::Max(Cut, FMath::Abs(Z - FPFReserveLayout::UngradedHeight(P.X, P.Y)));
            if (I)
            {
                const float D = FVector2D::Distance(P, Path[I - 1]);
                if (D > 1)
                {
                    Length += D;
                    Grade = FMath::Max(
                        Grade, FMath::Abs(Z - FPFReserveLayout::Height(Path[I - 1].X, Path[I - 1].Y)) / D);
                }
            }
        }
    }
    Json += FString::Printf(TEXT("],\"road_metrics\":{\"route_length_cm\":%.1f,\"straight_length_cm\":%.1f,"
                                 "\"max_grade\":%.4f,\"max_center_cut_fill_cm\":%.2f}}"),
                            Length, Straight, Grade, Cut);
    return Json;
}

int32 UPFWorldBuilder::BuildReserveRoads()
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return 0;
    auto *Actor = World->SpawnActor<AActor>();
    Actor->SetActorLabel(TEXT("Connected contour tracks"));
    auto *Group = NewObject<UHierarchicalInstancedStaticMeshComponent>(Actor);
    Actor->AddInstanceComponent(Group);
    Actor->SetRootComponent(Group);
    Group->SetMobility(EComponentMobility::Static);
    Group->SetStaticMesh(LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube")));
    Group->SetMaterial(0, LoadObject<UMaterialInterface>(
                              nullptr, TEXT("/Game/Pinefall/Materials/Reserve/M_ReserveRoad.M_ReserveRoad")));
    Group->SetCollisionProfileName(TEXT("NoCollision"));
    Group->RegisterComponent();
    int32 Count = 0, Index = 0;
    for (const auto &Path : FPFReserveLayout::RoadPaths())
    {
        const auto Link = FPFReserveLayout::Roads()[Index++];
        const float Width = (Link.X == 6 || Link.Y == 6) ? 600.f : 480.f;
        for (int32 I = 0; I < Path.Num() - 1; I++)
        {
            const FVector2D A = Path[I], B = Path[I + 1], P = (A + B) * .5;
            FVector Direction(B.X - A.X, B.Y - A.Y,
                              FPFReserveLayout::Height(B.X, B.Y) - FPFReserveLayout::Height(A.X, A.Y));
            const float Length = Direction.Size();
            if (Length < 1)
                continue;
            const float DX =
                FPFReserveLayout::Height(P.X + 100, P.Y) - FPFReserveLayout::Height(P.X - 100, P.Y);
            const float DY =
                FPFReserveLayout::Height(P.X, P.Y + 100) - FPFReserveLayout::Height(P.X, P.Y - 100);
            const FVector Normal = FVector(-DX, -DY, 200).GetSafeNormal();
            const FRotator Rotation =
                FRotationMatrix::MakeFromXZ(Direction.GetSafeNormal(), Normal).Rotator();
            Group->AddInstance(FTransform(Rotation, FVector(P.X, P.Y, FPFReserveLayout::Height(P.X, P.Y) + 7),
                                          FVector(Length / 100 + .06f, Width / 100, .025f)));
            Count++;
        }
    }
    Group->BuildTreeIfOutdated(false, true);
    return Count;
}
