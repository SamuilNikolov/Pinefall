#include "PFTacticalMap.h"
#include "PFHUDStyle.h"
#include "PFMatchState.h"
#include "PFGameMode.h"
#include "PFReserveLayout.h"
#include "PFCharacter.h"
#include "PFPlayerController.h"
#include "Engine/Texture2D.h"
#include "Rendering/DrawElements.h"
#include "Styling/CoreStyle.h"
#include "Framework/Application/SlateApplication.h"
#include "Fonts/FontMeasure.h"
#include "Rendering/SlateRenderer.h"
float UPFTacticalMap::HalfExtent() const
{
    return bReserve ? FPFReserveLayout::HalfExtent : 25200;
}
void UPFTacticalMap::NativeConstruct()
{
    Super::NativeConstruct();
    PlayerArrowPoints.Reserve(5);PlayerSpinePoints.Reserve(2);
    SetClipping(EWidgetClipping::ClipToBounds);
    bReserve = GetWorld()->GetMapName().Contains(TEXT("WildfrontReserve"));
    Atlas = LoadObject<UTexture2D>(nullptr,
                                   bReserve ? TEXT("/Game/Pinefall/Art/T_ReserveAtlas.T_ReserveAtlas")
                                            : TEXT("/Game/Pinefall/Art/T_CedarAtlas.T_CedarAtlas"));
    LabelFont =
        LoadObject<UObject>(nullptr, TEXT("/Game/Pinefall/Art/UI/F_PinefallDisplay.F_PinefallDisplay"));
    for (const auto &Site : FPFReserveLayout::Sites())
        SiteLabels.Add(FString(Site.Name).ToUpper());
    AtlasBrush.SetResourceObject(Atlas);
    AtlasBrush.ImageSize = FVector2D(4096);
    AtlasBrush.DrawAs = ESlateBrushDrawType::Image;
}
void UPFTacticalMap::ClampCenter()
{
    float Limit = HalfExtent() - HalfExtent() / Zoom;
    Center.X = FMath::Clamp(Center.X, -Limit, Limit);
    Center.Y = FMath::Clamp(Center.Y, -Limit, Limit);
}
void UPFTacticalMap::NativeTick(const FGeometry &G, float Delta)
{
    Super::NativeTick(G, Delta);
    Refresh -= Delta;
    if (Refresh <= 0)
    {
        Refresh = .1f;
        if (bMini)
        {
            Zoom = FMath::Max(1.f, HalfExtent() / 22000);
            APawn *Tracked=GetOwningPlayerPawn();
            if(!Tracked&&GetOwningPlayer())Tracked=Cast<APawn>(GetOwningPlayer()->GetViewTarget());
            if(Tracked)Center=FVector2D(Tracked->GetActorLocation());
            ClampCenter();
        }
        InvalidateLayoutAndVolatility();
    }
}
int32 UPFTacticalMap::NativePaint(const FPaintArgs &Args, const FGeometry &G, const FSlateRect &Cull,
                                  FSlateWindowElementList &Out, int32 Layer, const FWidgetStyle &Style,
                                  bool ParentEnabled) const
{
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    auto *Mine = PC ? PC->GetPlayerState<APFPlayerState>() : nullptr;
    if (!Match || !Mine)
        return Layer;
    float Margin = bMini ? 4 : 16,
          Side = FMath::Min(G.GetLocalSize().X, G.GetLocalSize().Y) - Margin * 2;
    FVector2D Origin((G.GetLocalSize().X - Side) * .5, Margin);
    float Half = HalfExtent() / Zoom;
    auto *White = FCoreStyle::Get().GetBrush(TEXT("WhiteBrush"));
    auto Box = [&](FVector2D P, FVector2D S, FLinearColor C, const FSlateBrush *B = nullptr) {
        FSlateDrawElement::MakeBox(Out, Layer, G.ToPaintGeometry(S, FSlateLayoutTransform(P)),
                                   B ? B : White, ESlateDrawEffect::None, C);
    };
    auto Project = [&](FVector P) {
        return Origin +
               FVector2D(.5 + (P.Y - Center.Y) / (Half * 2), .5 - (P.X - Center.X) / (Half * 2)) * Side;
    };
    auto Line = [&](TArray<FVector2D> Points, FLinearColor C, float Width = 1.5f) {
        FSlateDrawElement::MakeLines(Out, Layer + 2, G.ToPaintGeometry(), Points, ESlateDrawEffect::None,
                                     C, true, Width);
    };
    auto Text = [&](FVector2D P, const FString &Value, int32 Font, FLinearColor C, bool Badge = false) {
        const FSlateFontInfo FontInfo = Badge && LabelFont
                                            ? FSlateFontInfo(LabelFont, Font + 2)
                                            : FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), Font);
        const FVector2D Size =
            FSlateApplication::Get().GetRenderer()->GetFontMeasureService()->Measure(Value, FontInfo);
        P.X = FMath::Clamp(P.X, Origin.X + 3, FMath::Max(Origin.X + 3, Origin.X + Side - Size.X - 3));
        P.Y = FMath::Clamp(P.Y, Origin.Y + 3, FMath::Max(Origin.Y + 3, Origin.Y + Side - Size.Y - 3));
        if (Badge)
            Box(P - FVector2D(4, 1), Size + FVector2D(8, 3), FLinearColor(.012, .025, .04, .72));
        for (FVector2D Offset : {FVector2D(1, 1), FVector2D(-1, -1)})
            FSlateDrawElement::MakeText(
                Out, Layer + 3, G.ToPaintGeometry(FVector2D(180, 24), FSlateLayoutTransform(P + Offset)),
                Value, FontInfo, ESlateDrawEffect::None, FLinearColor(0, 0, 0, .95));
        FSlateDrawElement::MakeText(Out, Layer + 4,
                                    G.ToPaintGeometry(FVector2D(180, 24), FSlateLayoutTransform(P)),
                                    Value, FontInfo, ESlateDrawEffect::None, C);
    };
    Box(FVector2D::ZeroVector, G.GetLocalSize(), FLinearColor(.025, .04, .045, .94));
    FSlateBrush ViewBrush = AtlasBrush;
    ViewBrush.SetUVRegion(FBox2f(FVector2f(.5f + (Center.Y - Half) / (HalfExtent() * 2),
                                           .5f - (Center.X + Half) / (HalfExtent() * 2)),
                                 FVector2f(.5f + (Center.Y + Half) / (HalfExtent() * 2),
                                           .5f - (Center.X - Half) / (HalfExtent() * 2))));
    Box(Origin, FVector2D(Side), FLinearColor::White, Atlas ? &ViewBrush : White);
    auto Circle = [&](FVector P, float R, FLinearColor C) {
        TArray<FVector2D> Points;
        FVector2D At = Project(P);
        for (int32 I = 0; I <= 96; I++)
        {
            float A = I * 2 * PI / 96;
            Points.Add(At + FVector2D(FMath::Cos(A), FMath::Sin(A)) * R / (Half * 2) * Side);
        }
        Line(Points, C, 2);
    };
    if (Match->Phase == EPFRoundPhase::Active || Match->Phase == EPFRoundPhase::Finished)
    {
        Circle(Match->SafeCenter(), Match->SafeRadius(), FLinearColor(.5, .7, 1, 1));
        if (Match->bClosing)
            Circle(Match->CenterTo, Match->RadiusTo, FLinearColor(1, 1, .9, 1));
    }
    TArray<FBox2D, TInlineAllocator<32>> LabelBoxes;
    if (bReserve)
        for (int32 Index = 0; Index < FPFReserveLayout::Sites().Num(); Index++)
        {
            const auto &Site = FPFReserveLayout::Sites()[Index];
            FVector2D Pin = Project(FVector(Site.Point, 0));
            if (Pin.X < Origin.X + 12 || Pin.Y < Origin.Y + 24 || Pin.X > Origin.X + Side - 12 ||
                Pin.Y > Origin.Y + Side - 18)
                continue;
            int32 Font = bMini ? 11 : Zoom > 1 ? 15 : 13;
            auto Info = LabelFont ? FSlateFontInfo(LabelFont, Font + 2)
                                  : FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), Font);
            FVector2D Extent = FSlateApplication::Get().GetRenderer()->GetFontMeasureService()->Measure(
                SiteLabels[Index], Info);
            FVector2D Choices[] = {
                Pin - FVector2D(Extent.X * .5, Extent.Y + 8), Pin + FVector2D(-Extent.X * .5, 10),
                Pin - FVector2D(Extent.X + 12, Extent.Y * .5), Pin + FVector2D(12, -Extent.Y * .5)};
            FVector2D At = Choices[0];
            for (auto Candidate : Choices)
            {
                Candidate.X = FMath::Clamp(Candidate.X, Origin.X + 8,
                                           FMath::Max(Origin.X + 8, Origin.X + Side - Extent.X - 8));
                Candidate.Y = FMath::Clamp(Candidate.Y, Origin.Y + 24,
                                           FMath::Max(Origin.Y + 24, Origin.Y + Side - Extent.Y - 8));
                FBox2D R(Candidate - FVector2D(6, 3), Candidate + Extent + FVector2D(6, 3));
                bool Overlap = false;
                for (const auto &Existing : LabelBoxes)
                    if (R.Intersect(Existing))
                    {
                        Overlap = true;
                        break;
                    }
                At = Candidate;
                if (!Overlap)
                {
                    LabelBoxes.Add(R);
                    break;
                }
            }
            Box(Pin - FVector2D(2), FVector2D(4), FLinearColor(.95, .82, .52));
            Text(At, SiteLabels[Index], Font, FLinearColor(.98, .98, .94), true);
        }
    for (const auto &Pin : PC->MapMarkers)
    {
        FVector Point = Pin.Position;
        if (Pin.Kind == 0 && Pin.PlayerId == Mine->GetPlayerId())
            if (auto *Pawn = GetOwningPlayerPawn())
                Point = Pawn->GetActorLocation();
        FVector2D Pos = Project(Point);
        if (Pos.X < Origin.X + 9 || Pos.X > Origin.X + Side - 9 || Pos.Y < Origin.Y + 9 ||
            Pos.Y > Origin.Y + Side - 9)
            continue;
        if (Pin.Kind == 0)
        {
            Box(Pos - FVector2D(4), FVector2D(8),
                Pin.PlayerId == Mine->GetPlayerId() ? FLinearColor::White
                                                    : PFHUDStyle::TeamColor(Pin.PlayerId));
            if (!bMini)
                Text(Pos + FVector2D(7, 5), Pin.Label, 11, PFHUDStyle::TeamColor(Pin.PlayerId));
        }
        else
        {
            FLinearColor C = Pin.Condition > 0 ? FLinearColor(.88, .90, .79) : FLinearColor(.9, .58, .2);
            if(Pin.Kind==3)
            {
                Line({Pos+FVector2D(-5,4),Pos+FVector2D(0,-4),Pos+FVector2D(5,4),Pos+FVector2D(-5,4)},FLinearColor(.3,.7,1));
                Box(Pos+FVector2D(-7,3),FVector2D(4),C);Box(Pos+FVector2D(3,3),FVector2D(4),C);
            }
            else if (Pin.Kind == 1)
            {
                Box(Pos - FVector2D(7, 4), FVector2D(14, 8), FLinearColor(.02, .04, .05, .9));
                Line({Pos + FVector2D(-6, -3), Pos + FVector2D(6, -3), Pos + FVector2D(6, 3),
                      Pos + FVector2D(-6, 3), Pos + FVector2D(-6, -3)},
                     C);
                for (int32 X : {-1, 1})
                    for (int32 Y : {-1, 1})
                        Box(Pos + FVector2D(X * 5 - 1, Y * 5 - 1), FVector2D(3, 2), C);
            }
            else
                Line({Pos + FVector2D(-6, -5), Pos + FVector2D(6, -5), Pos + FVector2D(4, 5),
                      Pos + FVector2D(-4, 5), Pos + FVector2D(-6, -5)},
                     C);
        }
    }
    // Draft intent is public before deployment; live enemies never enter this path.
    for (APlayerState *Entry : Match->PlayerArray)
        if (auto *P = Cast<APFPlayerState>(Entry))
        {
            FVector Point = FVector::ZeroVector;
            bool Show = false;
            if (Match->Phase == EPFRoundPhase::Landing)
            {
                Point = P->Landing;
                Show = P->bInRound && !Point.IsNearlyZero();
            }
            else if (!bReserve && P->TeamId == Mine->TeamId && !P->bEliminated)
                if (auto *Pawn = P->GetPawn())
                {
                    Point = Pawn->GetActorLocation();
                    Show = true;
                }
            if (Show)
            {
                auto Pos = Project(Point);
                Box(Pos - FVector2D(4), FVector2D(8),
                    P->bLandingLocked ? FLinearColor(.4, .85, .5) : FLinearColor(1, .75, .2));
                if (!bMini)
                    Text(Pos + FVector2D(7, 4), P->GetPlayerName().Left(20), 11, FLinearColor::White);
            }
        }
    if(!Mine->bEliminated&&Match->GetServerWorldTimeSeconds()<PC->SurveyVisibleUntil)
        for(const auto &Point:PC->SurveyPoints)
        {
            const FVector2D At=Project(Point);
            if(At.X<Origin.X+8||At.X>Origin.X+Side-8||At.Y<Origin.Y+8||At.Y>Origin.Y+Side-8)continue;
            Line({At+FVector2D(0,-6),At+FVector2D(6,0),At+FVector2D(0,6),At+FVector2D(-6,0),At+FVector2D(0,-6)},FLinearColor(1,.3,.25));
        }
    for(const auto &DropPoint:Match->SupplyDropPoints)
    {
        const FVector2D At=Project(DropPoint);
        if(At.X<Origin.X+10||At.X>Origin.X+Side-10||At.Y<Origin.Y+10||At.Y>Origin.Y+Side-10)continue;
        Box(At-FVector2D(7),FVector2D(14),FLinearColor(.025,.035,.045,1));
        Box(At-FVector2D(5),FVector2D(10),FLinearColor(1,.65,.15,1));
        Box(At-FVector2D(1,5),FVector2D(2,10),FLinearColor(.15,.12,.06,1));
        if(!bMini)Text(At+FVector2D(10,-7),TEXT("SUPPLY"),11,FLinearColor(1,.8,.35));
    }
    // The local arrow never depends on a delayed team feed and is painted last.
    APawn *Tracked=GetOwningPlayerPawn();
    if(!Tracked&&PC)Tracked=Cast<APawn>(PC->GetViewTarget());
    if(Tracked)
    {
        FVector2D At=Project(Tracked->GetActorLocation());
        At.X=FMath::Clamp(At.X,Origin.X+15,Origin.X+Side-15);At.Y=FMath::Clamp(At.Y,Origin.Y+15,Origin.Y+Side-15);
        const float Yaw=FMath::DegreesToRadians(PC->GetControlRotation().Yaw);
        FVector2D Forward(FMath::Sin(Yaw),-FMath::Cos(Yaw)),Right(FMath::Cos(Yaw),FMath::Sin(Yaw));
        PlayerArrowPoints.Reset();PlayerArrowPoints.Append({At+Forward*12,At-Forward*8+Right*8,At-Forward*3,At-Forward*8-Right*8,At+Forward*12});
        for(auto Pass:{0,1})FSlateDrawElement::MakeLines(Out,Layer+7+Pass,G.ToPaintGeometry(),PlayerArrowPoints,ESlateDrawEffect::None,Pass?FLinearColor::White:FLinearColor(.015,.025,.04,1),true,Pass?3.5f:8.f);
        PlayerSpinePoints.Reset();PlayerSpinePoints.Append({At-Forward*2,At+Forward*7});
        FSlateDrawElement::MakeLines(Out,Layer+9,G.ToPaintGeometry(),PlayerSpinePoints,ESlateDrawEffect::None,FLinearColor(.15,.8,1),true,4.f);
    }
    Text(Origin + FVector2D(7, 5),
         bMini ? TEXT("N") : FString::Printf(TEXT("N   %.1f×   Wheel zoom · middle drag"), Zoom),
         bMini ? 12 : 13, FLinearColor(.94, .96, .9));
    return Layer + 10;
}
FReply UPFTacticalMap::NativeOnMouseButtonDown(const FGeometry &G, const FPointerEvent &E)
{
    if (bMini)
        return FReply::Unhandled();
    if (E.GetEffectingButton() == EKeys::MiddleMouseButton)
    {
        bDragging = true;
        return FReply::Handled().CaptureMouse(TakeWidget());
    }
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    if (!Match || !PC || Match->Phase != EPFRoundPhase::Landing ||
        E.GetEffectingButton() != EKeys::LeftMouseButton)
        return FReply::Unhandled();
    float Side = FMath::Min(G.GetLocalSize().X, G.GetLocalSize().Y) - 32;
    FVector2D Origin((G.GetLocalSize().X - Side) * .5, 16),
        UV = (G.AbsoluteToLocal(E.GetScreenSpacePosition()) - Origin) / Side;
    if (UV.X < 0 || UV.Y < 0 || UV.X > 1 || UV.Y > 1)
        return FReply::Unhandled();
    auto *State = PC->GetPlayerState<APFPlayerState>();
    float Half = HalfExtent() / Zoom;
    PC->ServerLanding(FVector(Center.X + (.5 - UV.Y) * Half * 2, Center.Y + (UV.X - .5) * Half * 2, 0),
                      State && State->GetPlayerId() == Match->TurnPlayerId);
    return FReply::Handled();
}
FReply UPFTacticalMap::NativeOnMouseButtonUp(const FGeometry &G, const FPointerEvent &E)
{
    if (bDragging && E.GetEffectingButton() == EKeys::MiddleMouseButton)
    {
        bDragging = false;
        return FReply::Handled().ReleaseMouseCapture();
    }
    return FReply::Unhandled();
}
FReply UPFTacticalMap::NativeOnMouseMove(const FGeometry &G, const FPointerEvent &E)
{
    if (!bDragging)
        return FReply::Unhandled();
    float Side = FMath::Min(G.GetLocalSize().X, G.GetLocalSize().Y) - 32;
    auto Delta = G.AbsoluteToLocal(E.GetScreenSpacePosition()) -
                 G.AbsoluteToLocal(E.GetLastScreenSpacePosition());
    Center += FVector2D(Delta.Y, -Delta.X) * (HalfExtent() * 2 / Zoom / Side);
    ClampCenter();
    return FReply::Handled();
}
FReply UPFTacticalMap::NativeOnMouseWheel(const FGeometry &G, const FPointerEvent &E)
{
    if (bMini)
        return FReply::Unhandled();
    Zoom = FMath::Clamp(Zoom * FMath::Pow(1.3f, E.GetWheelDelta()), 1.f, 5.f);
    ClampCenter();
    return FReply::Handled();
}
