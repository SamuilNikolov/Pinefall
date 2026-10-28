#include "PFTacticalMap.h"
#include "PFMatchState.h"
#include "PFGameMode.h"
#include "PFCharacter.h"
#include "PFPlayerController.h"
#include "Engine/Texture2D.h"
#include "Rendering/DrawElements.h"
#include "Styling/CoreStyle.h"
void UPFTacticalMap::NativeConstruct()
{
    Super::NativeConstruct();
    SetClipping(EWidgetClipping::ClipToBounds);
    Atlas = LoadObject<UTexture2D>(nullptr, TEXT("/Game/Pinefall/Art/T_CedarAtlas.T_CedarAtlas"));
    AtlasBrush.SetResourceObject(Atlas);
    AtlasBrush.ImageSize = FVector2D(512, 512);
    AtlasBrush.DrawAs = ESlateBrushDrawType::Image;
}
void UPFTacticalMap::NativeTick(const FGeometry &Geometry, float Delta)
{
    Super::NativeTick(Geometry, Delta);
    Refresh -= Delta;
    if (Refresh <= 0)
    {
        Refresh = .1f;
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
    float Side = FMath::Min(G.GetLocalSize().X, G.GetLocalSize().Y) - 32;
    FVector2D Origin((G.GetLocalSize().X - Side) * .5, 16);
    auto *White = FCoreStyle::Get().GetBrush(TEXT("WhiteBrush"));
    auto Box = [&](FVector2D Pos, FVector2D Size, FLinearColor Color, const FSlateBrush *Brush)
    {
        FSlateDrawElement::MakeBox(Out, Layer, G.ToPaintGeometry(Size, FSlateLayoutTransform(Pos)), Brush,
                                   ESlateDrawEffect::None, Color);
    };
    Box(FVector2D::ZeroVector, G.GetLocalSize(), FLinearColor(.025, .04, .045, .97), White);
    Box(Origin, FVector2D(Side), Atlas ? FLinearColor::White : FLinearColor(.16, .23, .20, 1),
        Atlas ? &AtlasBrush : White);
    auto Project = [&](FVector Point)
    {
        return Origin + FVector2D(.5 + Point.Y / (Match->ArenaHalfExtent * 2),
                                  .5 - Point.X / (Match->ArenaHalfExtent * 2)) *
                            Side;
    };
    auto Circle = [&](FVector Point, float Radius, FLinearColor Color, float Width)
    {
        TArray<FVector2D> Points;
        Points.Reserve(97);
        FVector2D Center = Project(Point);
        float R = Radius / (Match->ArenaHalfExtent * 2) * Side;
        for (int32 I = 0; I <= 96; I++)
        {
            float Angle = I * 2 * PI / 96;
            Points.Add(Center + FVector2D(FMath::Cos(Angle) * R, FMath::Sin(Angle) * R));
        }
        FSlateDrawElement::MakeLines(Out, Layer + 1, G.ToPaintGeometry(), Points, ESlateDrawEffect::None,
                                     Color, true, Width);
    };
    if (Match->Phase == EPFRoundPhase::Active || Match->Phase == EPFRoundPhase::Finished)
    {
        Circle(Match->SafeCenter(), Match->SafeRadius(), FLinearColor(.35, .62, 1, 1), 3);
        if (Match->bClosing)
            Circle(Match->CenterTo, Match->RadiusTo, FLinearColor(.95, .95, .88, 1), 2);
    }
    for (APlayerState *Entry : Match->PlayerArray)
        if (auto *Player = Cast<APFPlayerState>(Entry))
        {
            FVector Point = FVector::ZeroVector;
            bool Show = false;
            FLinearColor Color(.85, .58, .25, 1);
            if (Match->Phase == EPFRoundPhase::Landing)
            {
                Point = Player->Landing;
                Show = Player->bInRound && !Point.IsNearlyZero();
                if (Player->bLandingLocked)
                    Color = FLinearColor(.44, .8, .61, 1);
            }
            else if (Player->TeamId == Mine->TeamId && !Player->bEliminated)
            {
                if (auto *Pawn = Player->GetPawn())
                {
                    Point = Pawn->GetActorLocation();
                    Show = true;
                }
                Color = Player == Mine ? FLinearColor(1, 1, .92, 1) : FLinearColor(.26, .77, .88, 1);
            }
            if (!Show)
                continue;
            FVector2D Pos = Project(Point);
            Box(Pos - FVector2D(4, 4), FVector2D(8, 8), Color, White);
            FSlateDrawElement::MakeText(
                Out, Layer + 2,
                G.ToPaintGeometry(FVector2D(140, 20), FSlateLayoutTransform(Pos + FVector2D(7, -5))),
                Player->GetPlayerName().Left(20), FCoreStyle::GetDefaultFontStyle(TEXT("Regular"), 11),
                ESlateDrawEffect::None, Color);
        }
    FSlateDrawElement::MakeText(
        Out, Layer + 3,
        G.ToPaintGeometry(FVector2D(120, 25), FSlateLayoutTransform(Origin + FVector2D(8, 5))), TEXT("North"),
        FCoreStyle::GetDefaultFontStyle(TEXT("Bold"), 14), ESlateDrawEffect::None,
        FLinearColor(.92, .95, .89));
    return Layer + 4;
}
FReply UPFTacticalMap::NativeOnMouseButtonDown(const FGeometry &G, const FPointerEvent &Event)
{
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    if (!Match || !PC || Match->Phase != EPFRoundPhase::Landing ||
        Event.GetEffectingButton() != EKeys::LeftMouseButton)
        return FReply::Unhandled();
    float Side = FMath::Min(G.GetLocalSize().X, G.GetLocalSize().Y) - 32;
    FVector2D Origin((G.GetLocalSize().X - Side) * .5, 16);
    FVector2D UV = (G.AbsoluteToLocal(Event.GetScreenSpacePosition()) - Origin) / Side;
    if (UV.X < 0 || UV.Y < 0 || UV.X > 1 || UV.Y > 1)
        return FReply::Unhandled();
    auto *State = PC->GetPlayerState<APFPlayerState>();
    PC->ServerLanding(
        FVector((.5 - UV.Y) * Match->ArenaHalfExtent * 2, (UV.X - .5) * Match->ArenaHalfExtent * 2, 0),
        State && State->GetPlayerId() == Match->TurnPlayerId);
    return FReply::Handled();
}
