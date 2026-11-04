#include "PFReserveLayout.h"
#include <queue>
#include "Algo/Reverse.h"
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
    const FVector2D P(X, Y);
    float H = FMath::PerlinNoise2D(P / 39000) * 3400 + FMath::PerlinNoise2D(P / 12500) * 1000 +
              FMath::PerlinNoise2D(P / 3200) * 145;
    H += FMath::Exp(-(FMath::Square((X - 65000) / 42000) + FMath::Square((Y - 53000) / 45000))) * 16000;
    H += FMath::Exp(-(FMath::Square((X - 57000) / 33000) + FMath::Square((Y + 49000) / 38000))) * 12500;
    H += FMath::Exp(-(FMath::Square((X + 53000) / 25000) + FMath::Square((Y - 29000) / 32000))) * 4200;
    H -= FMath::Exp(-(FMath::Square((X + 21000) / 17000) + FMath::Square((Y - 18000) / 29000))) * 1700;
    return H;
}
float Terrain(float X, float Y)
{
    const float H = Natural(X, Y);
    const float Lake = FMath::Sqrt(FMath::Square((X - 30000) / 22000) + FMath::Square((Y - 30000) / 17000));
    // A low bank and broad foothill transition avoid a near-vertical crater
    // where high mountain terrain meets the navigable water surface.
    if (Lake < 1.10f)
        return FMath::Lerp(-1400.f, -100.f, FMath::SmoothStep(.65f, 1.10f, Lake));
    const float Distance = Lake - 1.10f;
    return FMath::Min(H, -100.f + 5000.f * Distance * Distance / (Distance + .08f));
}
float Base(float X, float Y, float *RoadWeight = nullptr)
{
    if (RoadWeight) *RoadWeight=1;
    const FVector2D P(X, Y);
    float H = Terrain(X, Y);
    for (const auto &S : SiteData)
    {
        const FVector2D Local = P - S.Point;
        if (FMath::Abs(Local.X) > 7500 || FMath::Abs(Local.Y) > 7500)
            continue;
        // Small independent foundations preserve hilly village surroundings.
        for (int32 Side : {-1, 1})
            for (float Row : {-2400.f, -800.f, 800.f, 2400.f})
            {
                const FVector2D Center = S.Point + FVector2D(Side * 1800, Row);
                const FVector2D Offset = P - Center;
                if (FMath::Abs(Offset.X) < 1250 && FMath::Abs(Offset.Y) < 750)
                {
                    const float Blend = FMath::Max(FMath::SmoothStep(740.f, 1250.f, float(FMath::Abs(Offset.X))),
                                                   FMath::SmoothStep(500.f, 750.f, float(FMath::Abs(Offset.Y))));
                    H = FMath::Lerp(Terrain(Center.X, Center.Y), H, Blend);
                    if (RoadWeight) *RoadWeight=FMath::Min(*RoadWeight,Blend);
                }
            }
    }
    return H;
}
constexpr int32 GridSize = 127;
constexpr float GridStep = 1600, SpatialStep = 2400;
FVector2D GridPoint(int32 ID)
{
    return FVector2D(-100800 + (ID % GridSize) * GridStep, -100800 + (ID / GridSize) * GridStep);
}
int32 GridID(FVector2D P)
{
    return FMath::Clamp(FMath::RoundToInt((P.Y + 100800) / GridStep), 0, GridSize - 1) * GridSize +
           FMath::Clamp(FMath::RoundToInt((P.X + 100800) / GridStep), 0, GridSize - 1);
}
int64 CellKey(int32 X, int32 Y)
{
    return int64((uint64(uint32(X)) << 32) | uint32(Y));
}
struct FSegment
{
    FVector2D A, B;
    float ZA, ZB, Width, Length;
};
struct FNetwork
{
    TArray<TArray<FVector2D>> Paths;
    TArray<FSegment> Segments;
    TMap<int64, TArray<int32>> Cells;
    FNetwork()
    {
        TArray<float> Elevation;
        Elevation.SetNumUninitialized(GridSize * GridSize);
        TArray<bool> Blocked;
        Blocked.Init(false, GridSize * GridSize);
        for (int32 I = 0; I < Elevation.Num(); I++)
        {
            const FVector2D P = GridPoint(I);
            Elevation[I] = Base(P.X, P.Y);
            const float Lake = FMath::Square((P.X - 30000) / 22000) + FMath::Square((P.Y - 30000) / 17000);
            Blocked[I] = Lake < 1.20f * 1.20f;
            for (const auto &S : SiteData)
                if (FMath::Abs(P.X - S.Point.X) < 3200 && FMath::Abs(P.Y - S.Point.Y) < 6200)
                    Blocked[I] = true;
        }
        struct FOpen
        {
            float Cost;
            int32 ID;
            bool operator<(const FOpen &Other) const
            {
                return Cost > Other.Cost;
            }
        };
        for (int32 LinkIndex = 0; LinkIndex < UE_ARRAY_COUNT(RoadData); LinkIndex++)
        {
            const auto Link = RoadData[LinkIndex];
            const FVector2D A = SiteData[Link.X].Point, B = SiteData[Link.Y].Point;
            const float Sign = B.Y >= A.Y ? 1.f : -1.f;
            auto Gate = [](FVector2D Center, float Direction)
            {
                FVector2D Result = Center + FVector2D(0, Direction * 7200);
                for (int32 J = 1; J <= 12; J++)
                {
                    const FVector2D P = FMath::Lerp(Center, Result, J / 12.f);
                    if (FMath::Square((P.X - 30000) / 22000) + FMath::Square((P.Y - 30000) / 17000) <
                        1.20f * 1.20f)
                        return Center - FVector2D(0, Direction * 7200);
                }
                return Result;
            };
            const FVector2D GateA = Gate(A, Sign), GateB = Gate(B, -Sign);
            const int32 Start = GridID(GateA), Goal = GridID(GateB);
            TArray<float> Cost;
            Cost.Init(FLT_MAX, Elevation.Num());
            TArray<int32> Parent;
            Parent.Init(INDEX_NONE, Elevation.Num());
            TArray<bool> Closed;
            Closed.Init(false, Elevation.Num());
            std::priority_queue<FOpen> Open;
            Cost[Start] = 0;
            Open.push({0, Start});
            while (!Open.empty())
            {
                const int32 ID = Open.top().ID;
                Open.pop();
                if (Closed[ID])
                    continue;
                Closed[ID] = true;
                if (ID == Goal)
                    break;
                const int32 X = ID % GridSize, Y = ID / GridSize;
                for (int32 DY = -1; DY <= 1; DY++)
                    for (int32 DX = -1; DX <= 1; DX++)
                    {
                        if ((DX == 0 && DY == 0) || X + DX < 0 || X + DX >= GridSize || Y + DY < 0 ||
                            Y + DY >= GridSize)
                            continue;
                        const int32 Next = (Y + DY) * GridSize + X + DX;
                        if (Closed[Next] || (Blocked[Next] && Next != Goal))
                            continue;
                        if (DX && DY && (Blocked[Y * GridSize + X + DX] || Blocked[(Y + DY) * GridSize + X]))
                            continue;
                        const float Distance = GridStep * (DX && DY ? 1.41421356f : 1.f),
                                    Grade = FMath::Abs(Elevation[Next] - Elevation[ID]) / Distance;
                        const float Candidate = Cost[ID] + Distance * (1 + FMath::Square(Grade / .10f) * 4);
                        if (Candidate >= Cost[Next])
                            continue;
                        Cost[Next] = Candidate;
                        Parent[Next] = ID;
                        Open.push({Candidate + float(FVector2D::Distance(GridPoint(Next), GateB)), Next});
                    }
            }
            TArray<FVector2D> Raw;
            if (Parent[Goal] != INDEX_NONE)
            {
                for (int32 ID = Goal; ID != Start && ID != INDEX_NONE; ID = Parent[ID])
                    Raw.Add(GridPoint(ID));
                Algo::Reverse(Raw);
            }
            Raw.Insert(GateA, 0);
            Raw.Insert(A, 0);
            Raw.Add(GateB);
            Raw.Add(B);
            // Remove grid-scale steering chatter while preserving real switchbacks.
            // A shortcut must stay on dry ground, clear houses and remain drivable.
            auto CanShortcut = [](FVector2D From, FVector2D To)
            {
                const float Distance = FVector2D::Distance(From, To);
                if (Distance > 25000)
                    return false;
                const int32 Steps = FMath::Max(1, FMath::CeilToInt(Distance / 400));
                float Previous = Base(From.X, From.Y);
                for (int32 I = 1; I <= Steps; ++I)
                {
                    const FVector2D P = FMath::Lerp(From, To, float(I) / Steps);
                    if (FMath::Square((P.X - 30000) / 22000) + FMath::Square((P.Y - 30000) / 17000) <
                        1.18f * 1.18f)
                        return false;
                    for (const auto &Site : SiteData)
                        for (int32 Side : {-1, 1})
                            for (float Row : {-2400.f, -800.f, 800.f, 2400.f})
                            {
                                const FVector2D Local = P - Site.Point - FVector2D(Side * 1800, Row);
                                if (FMath::Abs(Local.X) < 1000 && FMath::Abs(Local.Y) < 850)
                                    return false;
                            }
                    const float Z = Base(P.X, P.Y);
                    if (FMath::Abs(Z - Previous) / FMath::Max(1.f, Distance / Steps) > .30f)
                        return false;
                    Previous = Z;
                }
                return true;
            };
            TArray<FVector2D> Broad;
            Broad.Add(Raw[0]);
            for (int32 I = 0; I < Raw.Num() - 1;)
            {
                int32 Next = I + 1;
                for (int32 J = FMath::Min(I + 40, Raw.Num() - 1); J > I + 1; --J)
                    if (CanShortcut(Raw[I], Raw[J]))
                    {
                        Next = J;
                        break;
                    }
                Broad.Add(Raw[Next]);
                I = Next;
            }
            Raw = MoveTemp(Broad);
            // Corner cutting makes contour paths continuous instead of an eight-direction grid.
            for (int32 Pass = 0; Pass < 2; Pass++)
            {
                TArray<FVector2D> Smooth;
                Smooth.Add(A);
                for (int32 I = 0; I < Raw.Num() - 1; I++)
                {
                    Smooth.Add(FMath::Lerp(Raw[I], Raw[I + 1], .25f));
                    Smooth.Add(FMath::Lerp(Raw[I], Raw[I + 1], .75f));
                }
                Smooth.Add(B);
                bool SafeCurve = true;
                for (int32 I = 0; I < Smooth.Num() - 1; ++I)
                    if (!CanShortcut(Smooth[I], Smooth[I + 1]))
                    {
                        SafeCurve = false;
                        break;
                    }
                if (!SafeCurve)
                    break; // Keep a safe bend rather than cutting across a steep hillside.
                Raw = MoveTemp(Smooth);
            }
            TArray<FVector2D> Path;
            Path.Add(A);
            for (int32 I = 0; I < Raw.Num() - 1; I++)
            {
                const int32 Steps =
                    FMath::Max(1, FMath::CeilToInt(FVector2D::Distance(Raw[I], Raw[I + 1]) / 400));
                for (int32 J = 1; J <= Steps; J++)
                    Path.Add(FMath::Lerp(Raw[I], Raw[I + 1], float(J) / Steps));
            }
            Paths.Add(Path);
            const float Width = (Link.X == 6 || Link.Y == 6) ? 300.f : 240.f;
            TArray<float> Z;
            Z.Reserve(Path.Num());
            for (const auto &P : Path)
                Z.Add(Base(P.X, P.Y));
            for (int32 I = 0; I < Path.Num() - 1; I++)
            {
                if ((Path[I + 1] - Path[I]).SizeSquared() < 1)
                    continue;
                auto Filter = [&](int32 Index)
                {
                    float Sum = 0, Weight = 0;
                    for (int32 K = -3; K <= 3; K++)
                    {
                        const float W = 4 - FMath::Abs(K);
                        Sum += Z[FMath::Clamp(Index + K, 0, Z.Num() - 1)] * W;
                        Weight += W;
                    }
                    return Sum / Weight;
                };
                const int32 ID = Segments.Add({Path[I], Path[I + 1], Filter(I), Filter(I + 1), Width,
                                               float((Path[I + 1] - Path[I]).Size())});
                const auto &S = Segments[ID];
                const float Margin = 850;
                const int32 X0 = FMath::FloorToInt((FMath::Min(S.A.X, S.B.X) - Margin) / SpatialStep),
                            X1 = FMath::FloorToInt((FMath::Max(S.A.X, S.B.X) + Margin) / SpatialStep);
                const int32 Y0 = FMath::FloorToInt((FMath::Min(S.A.Y, S.B.Y) - Margin) / SpatialStep),
                            Y1 = FMath::FloorToInt((FMath::Max(S.A.Y, S.B.Y) + Margin) / SpatialStep);
                for (int32 Y = Y0; Y <= Y1; Y++)
                    for (int32 X = X0; X <= X1; X++)
                        Cells.FindOrAdd(CellKey(X, Y)).Add(ID);
            }
        }
    }
    float GradeTarget(FVector2D P, float Fallback) const
    {
        const auto *IDs =
            Cells.Find(CellKey(FMath::FloorToInt(P.X / SpatialStep), FMath::FloorToInt(P.Y / SpatialStep)));
        if (!IDs)
            return Fallback;
        float Sum = 0, Weight = 0;
        for (int32 ID : *IDs)
        {
            const auto &S = Segments[ID];
            const float D2 = (P - (S.A + S.B) * .5).SizeSquared();
            if (D2 >= 800 * 800)
                continue;
            const float U =
                FMath::Clamp(FVector2D::DotProduct(P - S.A, S.B - S.A) / (S.B - S.A).SizeSquared(), 0., 1.);
            const float W = FMath::Square(1 - D2 / (800 * 800)) * S.Length;
            Sum += FMath::Lerp(S.ZA, S.ZB, U) * W;
            Weight += W;
        }
        return Weight > SMALL_NUMBER ? Sum / Weight : Fallback;
    }
    const FSegment *Nearest(FVector2D P, float &Distance, float &T) const
    {
        Distance = FLT_MAX;
        const FSegment *Result = nullptr;
        const auto *IDs =
            Cells.Find(CellKey(FMath::FloorToInt(P.X / SpatialStep), FMath::FloorToInt(P.Y / SpatialStep)));
        if (!IDs)
            return nullptr;
        for (int32 ID : *IDs)
        {
            const auto &S = Segments[ID];
            const float U =
                FMath::Clamp(FVector2D::DotProduct(P - S.A, S.B - S.A) / (S.B - S.A).SizeSquared(), 0., 1.);
            const float D = (P - FMath::Lerp(S.A, S.B, U)).SizeSquared();
            if (D < Distance)
            {
                Distance = D;
                T = U;
                Result = &S;
            }
        }
        Distance = FMath::Sqrt(Distance);
        return Result;
    }
};
const FNetwork &Network()
{
    static const FNetwork Data;
    return Data;
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
const TArray<TArray<FVector2D>> &FPFReserveLayout::RoadPaths()
{
    return Network().Paths;
}
float FPFReserveLayout::RoadDistance(float X, float Y)
{
    float Distance, T;
    Network().Nearest(FVector2D(X, Y), Distance, T);
    return Distance;
}
float FPFReserveLayout::UngradedHeight(float X, float Y)
{
    return Base(X, Y);
}
bool FPFReserveLayout::Water(float X, float Y)
{
    return FMath::Square((X - 30000) / 22000) + FMath::Square((Y - 30000) / 17000) < 1;
}
uint8 FPFReserveLayout::Biome(float X, float Y)
{
    if (X > 25000 && Y > 15000)
        return 4;
    if (X > 18000 && Y < 0)
        return 2;
    if (X < -22000 && Y < -35000)
        return 3;
    if (Water(X, Y) || (X > -15000 && X < 40000 && Y > 18000))
        return 1;
    return X < -15000 ? 0 : 5;
}
float FPFReserveLayout::Height(float X, float Y)
{
    float RoadWeight;
    const float H = Base(X, Y, &RoadWeight);
    float D, T;
    const auto *S = Network().Nearest(FVector2D(X, Y), D, T);
    if (!S || D >= 850)
        return H;
    const float Target = Network().GradeTarget(FVector2D(X, Y), FMath::Lerp(S->ZA, S->ZB, T));
    // House foundations/approaches take priority over a nearby road shoulder.
    return H + FMath::Clamp(Target - H, -160.f, 160.f) * (1 - FMath::SmoothStep(300.f, 850.f, D)) * RoadWeight;
}
