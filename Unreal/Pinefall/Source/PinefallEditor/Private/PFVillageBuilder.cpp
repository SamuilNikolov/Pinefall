#include "PFWorldBuilder.h"
#include "PFReserveLayout.h"
#include "Editor.h"
#include "Engine/StaticMesh.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Materials/MaterialInterface.h"

int32 UPFWorldBuilder::BuildReserveVillageDetails()
{
    UWorld *World = GEditor->GetEditorWorldContext().World();
    if (!World)
        return 0;
    auto *Actor = World->SpawnActor<AActor>();
    Actor->SetActorLabel(TEXT("Reserve village architecture kit"));
    auto Group = [&](const TCHAR *Name, const TCHAR *Material, bool Collision)
    {
        auto *G = NewObject<UHierarchicalInstancedStaticMeshComponent>(Actor, Name);
        Actor->AddInstanceComponent(G);
        if (!Actor->GetRootComponent())
            Actor->SetRootComponent(G);
        else
            G->SetupAttachment(Actor->GetRootComponent());
        G->SetMobility(EComponentMobility::Static);
        G->SetStaticMesh(LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube")));
        G->SetMaterial(0, LoadObject<UMaterialInterface>(nullptr, Material));
        G->SetCollisionProfileName(Collision ? TEXT("BlockAll") : TEXT("NoCollision"));
        G->RegisterComponent();
        return G;
    };
    auto *Timber = Group(TEXT("Porch framing and cover"),
                         TEXT("/Game/Pinefall/Materials/Village/M_VillageTimber.M_VillageTimber"), true);
    auto *Roof = Group(TEXT("Covered porch roofing"),
                       TEXT("/Game/Pinefall/Materials/Village/M_VillageRoof.M_VillageRoof"), true);
    auto *Trim = Group(TEXT("Shutters and joinery"),
                       TEXT("/Game/Pinefall/Materials/Village/M_VillageTimber.M_VillageTimber"), false);
    auto *Stone = Group(TEXT("Courtyard stonework"),
                        TEXT("/Game/Pinefall/Materials/Reserve/M_Reserve_Stone.M_Reserve_Stone"), true);
    int32 Count = 0, House = 0;
    for (const auto &Site : FPFReserveLayout::Sites())
    {
        for (int32 Side : {-1, 1})
            for (float Row : {-2400.f, -800.f, 800.f, 2400.f})
            {
                const FVector2D Center = Site.Point + FVector2D(Side * 1800, Row);
                const FTransform Frame(
                    FRotator(0, Side > 0 ? -90 : 90, 0),
                    FVector(Center.X, Center.Y, FPFReserveLayout::Height(Center.X, Center.Y)));
                auto Part = [&](auto *G, FVector P, FVector Size, FRotator Rotation = FRotator::ZeroRotator)
                {
                    G->AddInstance(FTransform(Frame.TransformRotation(Rotation.Quaternion()),
                                              Frame.TransformPosition(P), Size / 100),
                                   true);
                    Count++;
                };
                // Covered porches alternate with open entries, retaining the tested doorway.
                if ((House++ % 2) == 0)
                {
                    Part(Roof, FVector(0, -535, 290), FVector(520, 220, 12), FRotator(0, 0, 12));
                    for (float X : {-230.f, 230.f})
                    {
                        Part(Timber, FVector(X, -615, 140), FVector(18, 18, 280));
                        Part(Timber, FVector(X, -535, 85), FVector(9, 150, 12));
                        Part(Timber, FVector(X, -535, 45), FVector(9, 150, 10));
                        for (float Y : {-590.f, -550.f, -510.f, -470.f})
                            Part(Trim, FVector(X, Y, 62), FVector(6, 6, 70));
                    }
                }
                // Shutters and sills break up long identical wall planes without sealing windows.
                for (float X : {-383.f, 383.f})
                    for (float Y : {-270.f, 0.f, 270.f})
                    {
                        Part(Trim, FVector(X, Y, 126), FVector(16, 180, 12));
                        if (Site.Biome != 3)
                            for (float Sign : {-1.f, 1.f})
                                for (int32 K = 0; K < 6; K++)
                                    Part(Trim, FVector(X, Y + Sign * 93, 144 + K * 16), FVector(9, 35, 13));
                    }
                if (Site.Biome != 3)
                    for (float X : {-48.f, 48.f})
                        Part(Trim, FVector(X, -437, 365), FVector(9, 9, 72));
            }
        // A small shared courtyard landmark gives each settlement a recognisable centre.
        FVector2D C = Site.Point + FVector2D(1000, -3800);
        const float Z = FPFReserveLayout::Height(C.X, C.Y);
        for (int32 I = 0; I < 8; I++)
        {
            const float A = I * 2 * PI / 8;
            const FVector At(C.X + FMath::Cos(A) * 145, C.Y + FMath::Sin(A) * 145, Z + 30);
            Stone->AddInstance(
                FTransform(FRotator(0, FMath::RadiansToDegrees(A) + 90, 0), At, FVector(1.15, .4, 1.2)),
                true);
            Count++;
        }
    }
    for (auto *G : {Timber, Roof, Trim, Stone})
        G->BuildTreeIfOutdated(false, true);
    UE_LOG(LogTemp, Display, TEXT("PF_VILLAGE_DETAILS_OK instances=%d"), Count);
    return Count;
}
