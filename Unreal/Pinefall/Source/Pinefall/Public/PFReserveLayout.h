#pragma once
#include "CoreMinimal.h"

// Shared authored geography. Height evaluation is editor-only; runtime uses collision.
struct FPFReserveSite
{
    const TCHAR *Name;
    FVector2D Point;
    uint8 Biome;
};
struct PINEFALL_API FPFReserveLayout
{
    static constexpr float HalfExtent = 100800;
    static constexpr float WaterLevel = -300;
    static TConstArrayView<FPFReserveSite> Sites();
    static TConstArrayView<FIntPoint> Roads();
    static float Height(float X, float Y);
    static uint8 Biome(float X, float Y);
    static bool Water(float X, float Y);
};
