#include "PFWorldBuilder.h"
#include "AssetCompilingManager.h"
#include "ShaderCompiler.h"
#include "Materials/Material.h"
#include "Materials/MaterialExpressionConstant.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "Misc/FileHelper.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Components/SceneComponent.h"
#include "Editor.h"
#include "Engine/Font.h"
#include "Engine/FontFace.h"
#include "Engine/StaticMesh.h"
#include "GameFramework/PlayerStart.h"
#include "Landscape.h"
#include "LandscapeInfo.h"
#include "LandscapeProxy.h"
#include "Materials/MaterialInterface.h"
#include "PFRangeTarget.h"
#include "PhysicsEngine/BodySetup.h"
#include "StaticMeshResources.h"

bool UPFWorldBuilder::HasBuiltNaniteData(UStaticMesh *Mesh)
{
    return Mesh && Mesh->GetRenderData() && Mesh->GetRenderData()->HasValidNaniteData();
}

UFont *UPFWorldBuilder::CreateDisplayFontFromFile(const FString &Filename)
{
    TArray<uint8> Data;
    if (!FFileHelper::LoadFileToArray(Data, *Filename) || Data.IsEmpty())
        return nullptr;
    auto *Package = CreatePackage(TEXT("/Game/Pinefall/Art/UI/F_PinefallFace"));
    auto *Face = FindObject<UFontFace>(Package, TEXT("F_PinefallFace"));
    if (!Face)
    {
        Face = NewObject<UFontFace>(Package, TEXT("F_PinefallFace"), RF_Public | RF_Standalone);
        FAssetRegistryModule::AssetCreated(Face);
    }
    Face->Modify();
    Face->FontFaceData->SetData(MoveTemp(Data));
    Face->SourceFilename = TEXT("BarlowCondensed-SemiBold.ttf");
    Face->LoadingPolicy = EFontLoadingPolicy::Inline;
    Face->CacheSubFaces();
    Face->MarkPackageDirty();
    return CreateDisplayFont(Face);
}

UFont *UPFWorldBuilder::CreateDisplayFont(UFontFace *Face)
{
    if (!Face)
        return nullptr;
    auto *Package = CreatePackage(TEXT("/Game/Pinefall/Art/UI/F_PinefallDisplay"));
    auto *Font = FindObject<UFont>(Package, TEXT("F_PinefallDisplay"));
    if (!Font)
    {
        Font = NewObject<UFont>(Package, TEXT("F_PinefallDisplay"), RF_Public | RF_Standalone);
        FAssetRegistryModule::AssetCreated(Font);
    }
    Font->Modify();
    Font->FontCacheType = EFontCacheType::Runtime;
    Font->CompositeFont.DefaultTypeface.Fonts.Reset();
    auto &Entry = Font->CompositeFont.DefaultTypeface.Fonts.AddDefaulted_GetRef();
    Entry.Name = TEXT("Regular");
    Entry.Font = FFontData(Face);
    Font->MarkPackageDirty();
    return Font;
}

void UPFWorldBuilder::ConfigureCollision(UStaticMesh *Mesh, bool Complex)
{
    if (!Mesh)
        return;
    Mesh->Modify();
    Mesh->CreateBodySetup();
    auto *Body = Mesh->GetBodySetup();
    Body->Modify();
    Body->CollisionTraceFlag = Complex ? CTF_UseComplexAsSimple : CTF_UseSimpleAsComplex;
    if (!Complex)
    {
        Body->AggGeom.EmptyElements();
        FKBoxElem Box;
        Box.Center = Mesh->GetBounds().Origin;
        FVector Size = Mesh->GetBounds().BoxExtent * 2;
        Box.X = Size.X;
        Box.Y = Size.Y;
        Box.Z = Size.Z;
        Body->AggGeom.BoxElems.Add(Box);
    }
    Body->InvalidatePhysicsData();
    Body->CreatePhysicsMeshes();
    Mesh->MarkPackageDirty();
    Mesh->PostEditChange();
}

void UPFWorldBuilder::ConfigureMesh(UStaticMesh *Mesh, bool Nanite)
{
    if (!Mesh)
        return;
    Mesh->Modify();
    Mesh->NaniteSettings.bEnabled = Nanite;
    Mesh->NaniteSettings.bPreserveArea = true;
    // Editor asset subsystems are absent in commandlets; rebuild directly instead.
    Mesh->PostEditChange();
}

float UPFWorldBuilder::HeightAt(float X, float Y)
{
    float Ridge =
        FMath::Exp(-(FMath::Square((X - 18500) / 6500) + FMath::Square((Y + 6500) / 10000))) * 7800;
    Ridge += FMath::Exp(-(FMath::Square((X + 18000) / 8000) + FMath::Square((Y - 16500) / 6000))) * 4200;
    // Break the smooth mound profile into ridges, smaller saddles and rock shelves.
    FVector2D Warped(X + FMath::PerlinNoise2D(FVector2D(X, Y) / 8500) * 1300,
                     Y + FMath::PerlinNoise2D(FVector2D(Y + 1700, X) / 7300) * 1100);
    Ridge *= .90f + .10f * (1 - FMath::Abs(FMath::PerlinNoise2D(Warped / 4000)));
    Ridge += Ridge * .025f * FMath::PerlinNoise2D(Warped / 2500);
    float H = Ridge + 160 * FMath::Sin(X / 3500) * FMath::Cos(Y / 4700) +
              FMath::PerlinNoise2D(FVector2D(X, Y) / 2200) * 110 +
              FMath::PerlinNoise2D(Warped / 420) * 28;
    float Pad = FVector2D(X + 4500, Y).Size();
    H *= FMath::SmoothStep(3400.f, 8000.f, Pad);
    float Lake = FVector2D((X - 9000) / 4200, (Y - 9500) / 6000).Size();
    H = FMath::Lerp(H, -650.f, 1 - FMath::SmoothStep(.62f, 1.07f, Lake));
    return H;
}
void UPFWorldBuilder::BuildLandscape()
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return;
    constexpr int32 Size = 253;
    TArray<uint16> Heights;
    Heights.SetNumUninitialized(Size * Size);
    for (int32 Y = 0; Y < Size; Y++)
        for (int32 X = 0; X < Size; X++)
            Heights[Y * Size + X] = uint16(FMath::Clamp(
                FMath::RoundToInt(32768 + HeightAt(-25200 + X * 200, -25200 + Y * 200) * 1.28f), 0,
                65535));
    auto *Landscape = World->SpawnActor<ALandscape>();
    Landscape->SetActorLabel(TEXT("Cedar Crossing · native landscape"));
    Landscape->SetActorLocation(FVector(-25200, -25200, 0));
    Landscape->SetActorScale3D(FVector(200, 200, 100));
    Landscape->MaxLODLevel = 2;
    Landscape->LandscapeMaterial =
        LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/Pinefall/Materials/M_Terrain.M_Terrain"));
    TMap<FGuid, TArray<uint16>> HeightData;
    HeightData.Add(FGuid(), MoveTemp(Heights));
    TMap<FGuid, TArray<FLandscapeImportLayerInfo>> Layers;
    Layers.Add(FGuid(), TArray<FLandscapeImportLayerInfo>());
    Landscape->Import(FGuid::NewGuid(), 0, 0, Size - 1, Size - 1, 1, 63, HeightData, nullptr, Layers,
                      ELandscapeImportAlphamapType::Additive, TArrayView<const FLandscapeLayer>());
    Landscape->PostEditChange();
    for (int32 Index = 0; Index < 20; Index++)
    {
        FVector Point(-8500 - (Index / 5) * 250, -800 + (Index % 5) * 400, 0);
        Point.Z = HeightAt(Point.X, Point.Y) + 110;
        auto *Start = World->SpawnActor<APlayerStart>(Point, FRotator(0, 0, 0));
        Start->SetActorLabel(FString::Printf(TEXT("Range start %02d"), Index + 1));
    }
    for (int32 Index = 0; Index < 7; Index++)
    {
        FVector Point(-4000 + Index * 1000, -600 + (Index % 3) * 600, 0);
        Point.Z = HeightAt(Point.X, Point.Y) + 150;
        auto *Target = World->SpawnActor<APFRangeTarget>(Point, FRotator::ZeroRotator);
        Target->SetActorLabel(FString::Printf(TEXT("Steel plate · %d m"), 45 + Index * 10));
    }
    UE_LOG(LogTemp, Display, TEXT("PF_LANDSCAPE_BUILT size=253 span=504m"));
}
void UPFWorldBuilder::PopulateFoliage(const TArray<UStaticMesh *> &Trees, UStaticMesh *Grass,
                                      UStaticMesh *Fern)
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return;
    FRandomStream Random(7319);
    auto Create = [World](UStaticMesh *Mesh, const FString &Name, int32 Range, bool Collision) {
        auto *Actor = World->SpawnActor<AActor>();
        Actor->SetActorLabel(Name);
        auto *Component = NewObject<UHierarchicalInstancedStaticMeshComponent>(Actor);
        Actor->AddInstanceComponent(Component);
        Actor->SetRootComponent(Component);
        Component->SetMobility(EComponentMobility::Static);
        Component->SetStaticMesh(Mesh);
        Component->SetCollisionEnabled(Collision ? ECollisionEnabled::QueryAndPhysics
                                                 : ECollisionEnabled::NoCollision);
        Component->SetCollisionProfileName(Collision ? TEXT("BlockAll") : TEXT("NoCollision"));
        Component->SetCullDistances(Range * .7f, Range);
        Component->bEnableDensityScaling = !Collision;
        Component->bAutoRebuildTreeOnInstanceChanges = false;
        Component->RegisterComponent();
        return Component;
    };
    TArray<UHierarchicalInstancedStaticMeshComponent *> TreeGroups;
    TArray<FVector2D> TrunkSizes;
    for (int32 I = 0; I < Trees.Num(); I++)
        if (Trees[I])
        {
            auto *Group =
                Create(Trees[I], FString::Printf(TEXT("Scanned pines %d"), I + 1), 35000, false);
            Group->bEnableDensityScaling = false;
            TreeGroups.Add(Group);
            float Height = Trees[I]->GetBounds().BoxExtent.Z * 2, Radius = 2;
            auto *Data = Trees[I]->GetRenderData();
            if (Data && Data->LODResources.Num() > 0)
            {
                const auto &Positions = Data->LODResources[0].VertexBuffers.PositionVertexBuffer;
                float Base = Trees[I]->GetBounds().Origin.Z - Trees[I]->GetBounds().BoxExtent.Z;
                for (uint32 V = 0; V < Positions.GetNumVertices(); V++)
                {
                    FVector P(Positions.VertexPosition(V));
                    if (P.Z < Base + Height * .08f)
                        Radius = FMath::Max(Radius, P.Size2D());
                }
            }
            TrunkSizes.Add(FVector2D(FMath::Min(Radius, 20.f), Height * .65f));
        }
    // Canopies must not become giant invisible convex obstacles. Keep simple trunks
    // authoritative while the detailed needles remain a visual instance group.
    auto *Trunks =
        Create(LoadObject<UStaticMesh>(
                   nullptr, TEXT("/Game/Pinefall/Art/World/SM_TrunkCollision.SM_TrunkCollision")),
               TEXT("Tree trunk collision"), 55000, true);
    Trunks->SetHiddenInGame(true);
    Trunks->SetVisibility(false);
    Trunks->SetCastShadow(false);
    auto *GrassGroup = Grass ? Create(Grass, TEXT("Low scanned ground cover"), 9000, false) : nullptr;
    auto *FernGroup = Fern ? Create(Fern, TEXT("Scanned fern beds"), 12000, false) : nullptr;
    for (int32 Index = 0; Index < 27000; Index++)
    {
        float X = Random.FRandRange(-24200, 24200), Y = Random.FRandRange(-24200, 24200);
        float Z = HeightAt(X, Y);
        float Lake = FVector2D((X - 9000) / 4200, (Y - 9500) / 6000).Size();
        bool Range = FMath::Abs(Y) < 1300 && X > -11000 && X < 5000;
        if (Lake < 1.08f || (Range && Index < 3300) ||
            FMath::Abs(HeightAt(X + 100, Y) - Z) + FMath::Abs(HeightAt(X, Y + 100) - Z) > 130)
            continue;
        FTransform Transform(FRotator(0, Random.FRandRange(0, 360), 0), FVector(X, Y, Z));
        if (Index < 2600 && !TreeGroups.IsEmpty())
        {
            float Scale = Random.FRandRange(5.f, 10.f);
            Transform.SetScale3D(FVector(Scale));
            TreeGroups[Index % TreeGroups.Num()]->AddInstance(Transform);
            auto Size = TrunkSizes[Index % TrunkSizes.Num()];
            FTransform Trunk(
                FRotator::ZeroRotator, FVector(X, Y, Z + Size.Y * Scale * .5f),
                FVector(Size.X * 2 * Scale / 100, Size.X * 2 * Scale / 100, Size.Y * Scale / 100));
            Trunks->AddInstance(Trunk);
        }
        else if (Index < 3300 && FernGroup)
        {
            Transform.SetScale3D(FVector(Random.FRandRange(.75f, 1.4f)));
            FernGroup->AddInstance(Transform);
        }
        else if (GrassGroup && FMath::PerlinNoise2D(FVector2D(X, Y) / 700) > -.15f)
        {
            for (int32 Blade = 0; Blade < 7; Blade++)
            {
                float GX = X + Random.FRandRange(-90, 90), GY = Y + Random.FRandRange(-90, 90);
                FTransform Clump(FRotator(0, Random.FRandRange(0, 360), 0),
                                 FVector(GX, GY, HeightAt(GX, GY)),
                                 FVector(Random.FRandRange(.8f, 1.35f)));
                GrassGroup->AddInstance(Clump);
            }
        }
    }
    for (auto *Group : TreeGroups)
        Group->BuildTreeIfOutdated(false, true);
    Trunks->BuildTreeIfOutdated(false, true);
    if (GrassGroup)
        GrassGroup->BuildTreeIfOutdated(false, true);
    if (FernGroup)
        FernGroup->BuildTreeIfOutdated(false, true);
    UE_LOG(LogTemp, Display, TEXT("PF_FOLIAGE_BUILT trees=%d grass=%d"), TreeGroups.Num(),
           GrassGroup ? GrassGroup->GetInstanceCount() : 0);
}

void UPFWorldBuilder::ConfigureClearCoat(UMaterial *Material)
{
    if (!Material)
        return;
    Material->Modify();
    Material->SetShadingModel(MSM_ClearCoat);
    for (int32 I = 0; I < 2; I++)
    {
        auto *Value = NewObject<UMaterialExpressionConstant>(Material);
        Value->R = I == 0 ? .8f : .16f;
        Material->GetExpressionCollection().AddExpression(Value);
        Material->GetExpressionInputForProperty(I == 0 ? MP_CustomData0 : MP_CustomData1)
            ->Connect(0, Value);
    }
    Material->MarkPackageDirty();
}

void UPFWorldBuilder::FinishRenderingPreparation()
{
    FAssetCompilingManager::Get().FinishAllCompilation();
    if (GShaderCompilingManager)
        GShaderCompilingManager->FinishAllCompilation();
    FAssetCompilingManager::Get().FinishAllCompilation();
}

bool UPFWorldBuilder::ConfigureGroundCoverLods(UStaticMesh *Mesh)
{
    if (!Mesh || Mesh->GetNumSourceModels() < 1 ||
        (Mesh->GetName() != TEXT("SM_GrassPatch") && Mesh->GetName() != TEXT("SM_DryGrassPatch")))
        return false;
    FAssetCompilingManager::Get().FinishAllCompilation();
    const int32 OriginalTriangles = Mesh->GetNumTriangles(0);
    const auto Build = Mesh->GetSourceModel(0).BuildSettings;
    const auto Reduction = Mesh->GetSourceModel(0).ReductionSettings;
    const float Ratios[] = {1.f, .5f, .18f};
    const float Screens[] = {1.f, .18f, .085f};
    Mesh->Modify();
    Mesh->SetNumSourceModels(3);
    Mesh->bAutoComputeLODScreenSize = false;
    for (int32 I = 0; I < 3; ++I)
    {
        auto &Source = Mesh->GetSourceModel(I);
        Source.BuildSettings = Build;
        Source.ReductionSettings = Reduction;
        Source.ReductionSettings.BaseLODModel = 0;
        Source.ReductionSettings.PercentTriangles = Ratios[I];
        Source.ScreenSize = Screens[I];
    }
    Mesh->Build(false);
    FAssetCompilingManager::Get().FinishAllCompilation();
    const bool Valid = Mesh->GetNumLODs() == 3 && Mesh->GetNumTriangles(0) == OriginalTriangles &&
        Mesh->GetNumTriangles(1) < OriginalTriangles && Mesh->GetNumTriangles(2) < Mesh->GetNumTriangles(1);
    UE_LOG(LogTemp, Display, TEXT("PF_GRASS_LODS mesh=%s valid=%d triangles=%d,%d,%d"),
        *Mesh->GetName(), Valid, Mesh->GetNumTriangles(0), Mesh->GetNumTriangles(1), Mesh->GetNumTriangles(2));
    return Valid;
}
