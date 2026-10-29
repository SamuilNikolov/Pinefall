#include "PFReserveLayout.h"
namespace
{
const FPFReserveSite SiteData[] = {
    {TEXT("Cedar Crossing"), {-32000, -20000}, 0}, {TEXT("Willow Wharf"), {18000, 48000}, 1},
    {TEXT("Rusty Rest"), {-60000, 50000}, 0},      {TEXT("Granite Gorge"), {55000, -48000}, 2},
    {TEXT("Sunset Sands"), {-60000, -60000}, 3},   {TEXT("Frosty Falls"), {65000, 57000}, 4},
    {TEXT("Meadow Market"), {-6000, 4000}, 5},     {TEXT("Marshy Mill"), {-12000, 47000}, 1}};
const FIntPoint RoadData[] = {{0, 6}, {0, 4}, {0, 2}, {2, 6}, {6, 7}, {7, 1},
                              {7, 2}, {6, 3}, {3, 5}, {1, 5}, {4, 3}};
float Natural(float X, float Y)
{
    FVector2D P(X, Y);
    float H = FMath::PerlinNoise2D(P / 33000) * 2300 + FMath::PerlinNoise2D(P / 11000) * 650 +
              FMath::PerlinNoise2D(P / 2900) * 110;
    H += FMath::Exp(-(FMath::Square((X - 65000) / 27000) + FMath::Square((Y - 47000) / 36000))) * 10500;
    H += FMath::Exp(-(FMath::Square((X - 52000) / 25000) + FMath::Square((Y + 48000) / 33000))) * 6300;
    return H;
}
} // namespace
TConstArrayView<FPFReserveSite> FPFReserveLayout::Sites()
{
    return MakeArrayView(SiteData);
}
TConstArrayView<FIntPoint> FPFReserveLayout::Roads()
{
    return MakeArrayView(RoadData);
}
bool FPFReserveLayout::Water(float X, float Y)
{
    return FMath::Square((X - 30000) / 22000) + FMath::Square((Y - 30000) / 17000) < 1;
}
uint8 FPFReserveLayout::Biome(float X, float Y)
{
    if (X > 33000 && Y > 18000)
        return 4;
    if (X > 25000 && Y < -17000)
        return 2;
    if (X < -33000 && Y < -28000)
        return 3;
    if (Water(X, Y) || (X > 5000 && Y > 12000))
        return 1;
    return X < -20000 ? 0 : 5;
}
float FPFReserveLayout::Height(float X, float Y)
{
    FVector2D P(X, Y);
    float H = Natural(X, Y);
    for (const auto &S : SiteData)
    {
        float D = FVector2D::Distance(P, S.Point);
        H = FMath::Lerp(Natural(S.Point.X, S.Point.Y), H, FMath::SmoothStep(3700.f, 6100.f, D));
    }
    for (const auto &R : RoadData)
    {
        auto A = SiteData[R.X].Point, B = SiteData[R.Y].Point;
        float T = FMath::Clamp(FVector2D::DotProduct(P - A, B - A) / (B - A).SizeSquared(), 0.f, 1.f);
        float D = FVector2D::Distance(P, FMath::Lerp(A, B, T));
        if (D < 1000)
        {
            float Z = FMath::Lerp(Natural(A.X, A.Y), Natural(B.X, B.Y), T);
            H = FMath::Lerp(Z, H, FMath::SmoothStep(430.f, 1000.f, D));
        }
    }
    float L = FMath::Sqrt(FMath::Square((X - 30000) / 22000) + FMath::Square((Y - 30000) / 17000));
    H = FMath::Lerp(-1400.f, H, FMath::SmoothStep(.65f, 1.10f, L));
    return H;
}
