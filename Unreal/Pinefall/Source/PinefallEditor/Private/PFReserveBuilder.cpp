#include "PFWorldBuilder.h"
#include "PFReserveLayout.h"
#include "Editor.h"
#include "Landscape.h"
#include "Materials/MaterialInterface.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Engine/StaticMesh.h"
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
    TArray<float> Radii;
    for (auto *Mesh : Trees)
    {
        Canopies.Add(Create(Mesh, TEXT("Reserve scanned pines"), 42000, false));
        float Radius = 2;
        auto *Data = Mesh->GetRenderData();
        if (Data && !Data->LODResources.IsEmpty())
        {
            const auto &Vertices = Data->LODResources[0].VertexBuffers.PositionVertexBuffer;
            float Base = Mesh->GetBounds().Origin.Z - Mesh->GetBounds().BoxExtent.Z;
            for (uint32 V = 0; V < Vertices.GetNumVertices(); V++)
            {
                FVector P(Vertices.VertexPosition(V));
                if (P.Z < Base + Mesh->GetBounds().BoxExtent.Z * .16f)
                    Radius = FMath::Max(Radius, P.Size2D());
            }
        }
        Radii.Add(FMath::Min(Radius, 20.f));
    }
    auto *Trunks = Create(LoadObject<UStaticMesh>(
                              nullptr, TEXT("/Game/Pinefall/Art/World/SM_TrunkCollision.SM_TrunkCollision")),
                          TEXT("Reserve trunk collision"), 42000, true);
    Trunks->SetVisibility(false);
    Trunks->SetHiddenInGame(true);
    Trunks->SetCastShadow(false);
    auto *Low = Create(Grass, TEXT("Reserve short scanned grass"), 9500, false);
    Low->SetCastShadow(false);
    auto *Brush = Create(Fern, TEXT("Reserve fern clusters"), 15000, false);
    auto *Rocks =
        Create(LoadObject<UStaticMesh>(nullptr, TEXT("/Game/Pinefall/Art/World/SM_Limestone.SM_Limestone")),
               TEXT("Reserve limestone cover"), 55000, true);
    Rocks->SetMaterial(0, LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/Reserve/M_Reserve_Stone.M_Reserve_Stone")));
    FRandomStream Random(31809);
    int32 Count = 0;
    auto Open = [&](FVector2D P)
    {
        for (const auto &S : FPFReserveLayout::Sites())
            if (FVector2D::Distance(P, S.Point) < 4300)
                return true;
        auto Sites = FPFReserveLayout::Sites();
        for (const auto &R : FPFReserveLayout::Roads())
        {
            auto A = Sites[R.X].Point, B = Sites[R.Y].Point;
            float T = FMath::Clamp(FVector2D::DotProduct(P - A, B - A) / (B - A).SizeSquared(), 0.f, 1.f);
            if (FVector2D::Distance(P, FMath::Lerp(A, B, T)) < 550)
                return true;
        }
        return false;
    };
    for (int32 I = 0; I < 160000; I++)
    {
        float X = Random.FRandRange(-99000, 99000), Y = Random.FRandRange(-99000, 99000);
        FVector2D P(X, Y);
        if (Open(P) || FPFReserveLayout::Water(X, Y))
            continue;
        uint8 Biome = FPFReserveLayout::Biome(X, Y);
        float Z = FPFReserveLayout::Height(X, Y);
        float Slope = FMath::Abs(FPFReserveLayout::Height(X + 200, Y) - Z) +
                      FMath::Abs(FPFReserveLayout::Height(X, Y + 200) - Z);
        FTransform Transform(FRotator(0, Random.FRandRange(0, 360), 0), FVector(X, Y, Z));
        if (I < 2200 || (Slope > 100 && I < 7000))
        {
            float Scale = Random.FRandRange(2.f, 7.f);
            Transform.SetScale3D(FVector(Scale, Scale * .8f, Scale * .75f));
            Transform.AddToTranslation(FVector(0, 0, -Scale * 35));
            Rocks->AddInstance(Transform);
            continue;
        }
        if (I < 19000 && Slope < 150 &&
            (Biome == 0 || Biome == 1 || (Biome == 4 && Random.FRand() < .35f) ||
             (Biome == 5 && Random.FRand() < .2f)) &&
            !Canopies.IsEmpty())
        {
            int32 N = I % Canopies.Num();
            float Scale = Random.FRandRange(6.f, 13.f);
            Transform.SetScale3D(FVector(Scale));
            Canopies[N]->AddInstance(Transform);
            // Source pines were grounded by their trunk base; preserve slender collision.
            float H = Trees[N]->GetBounds().BoxExtent.Z * 1.3f * Scale;
            FTransform Trunk(FRotator::ZeroRotator, FVector(X, Y, Z + H * .5f),
                             FVector(Radii[N] * 2 * Scale / 100, Radii[N] * 2 * Scale / 100, H / 100));
            Trunks->AddInstance(Trunk);
            Count++;
        }
        else if (I < 26000 && (Biome == 0 || Biome == 1) && Slope < 120)
        {
            Transform.SetScale3D(FVector(Random.FRandRange(.55f, 1.f)));
            Brush->AddInstance(Transform);
        }
        else if ((Biome == 0 || Biome == 1 || Biome == 5) && Slope < 120)
        {
            for (int32 K = 0; K < 4; K++)
            {
                float GX = X + Random.FRandRange(-140, 140), GY = Y + Random.FRandRange(-140, 140);
                Low->AddInstance(FTransform(FRotator(0, Random.FRandRange(0, 360), 0),
                                            FVector(GX, GY, FPFReserveLayout::Height(GX, GY)),
                                            FVector(Random.FRandRange(.85f, 1.5f))));
            }
        }
    }
    for (auto *G : Canopies)
        G->BuildTreeIfOutdated(false, true);
    for (auto *G : {Trunks, Low, Brush, Rocks})
        G->BuildTreeIfOutdated(false, true);
    UE_LOG(LogTemp, Display, TEXT("PF_RESERVE_FOLIAGE_OK trees=%d grass=%d rocks=%d"), Count,
           Low->GetInstanceCount(), Rocks->GetInstanceCount());
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
    Group->SetMaterial(0, LoadObject<UMaterialInterface>(
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
            Group->AddInstance(
                FTransform(Rotation, FVector(P.X, P.Y, Z), FVector(Length / Steps / 100 + .08f, 8, .045f)));
            Count++;
        }
    }
    Group->BuildTreeIfOutdated(false, true);
    return Count;
}
