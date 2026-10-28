#include "PFInterface.h"
#include "PFCombatHUD.h"
#include "PFCharacter.h"
#include "PFPlayerController.h"
#include "Rendering/DrawElements.h"
#include "Styling/CoreStyle.h"

int32 UPFCombatHUD::NativePaint(const FPaintArgs &Args, const FGeometry &G, const FSlateRect &Cull,
                                FSlateWindowElementList &Out, int32 Layer, const FWidgetStyle &Style,
                                bool ParentEnabled) const
{
    int32 Result = Super::NativePaint(Args, G, Cull, Out, Layer, Style, ParentEnabled);
    return Interface ? Interface->PaintCombat(G, Out, Result) : Result;
}

int32 UPFInterface::PaintCombat(const FGeometry &G, FSlateWindowElementList &Out, int32 Layer) const
{
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    auto *Player = Cast<APFCharacter>(GetOwningPlayerPawn());
    if (!PC || !Player || !Player->CanFight() || PC->bMenuOpen || PC->bMapOpen || PC->bInventoryOpen)
        return Layer;
    const FVector2D Size = G.GetLocalSize(), Center = Size * .5f;
    const auto *White = FCoreStyle::Get().GetBrush(TEXT("WhiteBrush"));
    int32 DrawLayer = Layer;
    auto Box = [&](FVector2D Position, FVector2D Extent, FLinearColor Color)
    {
        FSlateDrawElement::MakeBox(Out, DrawLayer, G.ToPaintGeometry(Extent, FSlateLayoutTransform(Position)),
                                   White, ESlateDrawEffect::None, Color);
    };
    auto Line = [&](FVector2D A, FVector2D B, FLinearColor Color, float Width)
    {
        CombatLines.Reset();
        CombatLines.Add(A);
        CombatLines.Add(B);
        FSlateDrawElement::MakeLines(Out, DrawLayer + 1, G.ToPaintGeometry(), CombatLines,
                                     ESlateDrawEffect::None, Color, true, Width);
    };
    const bool Scope = Player->Weapon.AimFOV <= 35 && Player->AimAlpha > .92f;
    if (Scope)
    {
        const float Radius = FMath::Min(Size.X, Size.Y) * .38f;
        // A circular optic keeps its central image sharp. Mask rows stay outside
        // the lens, with no depth-of-field blur hiding an actual target.
        const float Row = Size.Y / 256.f;
        for (int32 I = 0; I < 256; I++)
        {
            float Y = I * Row, NearY = FMath::Max(0.f, FMath::Abs(Y + Row * .5f - Center.Y) - Row * .5f);
            if (NearY >= Radius)
            {
                Box(FVector2D(0, Y), FVector2D(Size.X, Row + .5f), FLinearColor(0, 0, 0, .98f));
                continue;
            }
            float X = FMath::Sqrt(Radius * Radius - NearY * NearY);
            Box(FVector2D(0, Y), FVector2D(Center.X - X, Row + .5f), FLinearColor(0, 0, 0, .98f));
            Box(FVector2D(Center.X + X, Y), FVector2D(Center.X - X, Row + .5f), FLinearColor(0, 0, 0, .98f));
        }
        CombatLines.Reset();
        for (int32 I = 0; I <= 64; I++)
        {
            float A = I * 2 * PI / 64;
            CombatLines.Add(Center + FVector2D(FMath::Cos(A), FMath::Sin(A)) * Radius);
        }
        FSlateDrawElement::MakeLines(Out, DrawLayer + 1, G.ToPaintGeometry(), CombatLines,
                                     ESlateDrawEffect::None, FLinearColor(.03, .04, .045, 1), true, 7);
        for (int32 Sign : {-1, 1})
        {
            Line(Center + FVector2D(Sign * 9, 0), Center + FVector2D(Sign * Radius * .86f, 0),
                 FLinearColor(0, 0, 0, .9f), 1.2f);
            Line(Center + FVector2D(0, Sign * 9), Center + FVector2D(0, Sign * Radius * .86f),
                 FLinearColor(0, 0, 0, .9f), 1.2f);
        }
        for (int32 I = 1; I < 5; I++)
            Line(Center + FVector2D(-4, I * Radius * .12f), Center + FVector2D(4, I * Radius * .12f),
                 FLinearColor(0, 0, 0, .9f), 1);
        Box(Center - FVector2D(1.5), FVector2D(3), FLinearColor(.85, .18, .12, 1));
        DrawLayer = Layer;
    }
    else
    {
        float Gap = FMath::Lerp(7.f, 3.f, Player->AimAlpha) + Player->ShotBloom +
                    FMath::Min(6.f, Player->GetVelocity().Size2D() / 110.f) * (1 - Player->AimAlpha);
        for (int32 I = 0; I < 4; I++)
        {
            FVector2D Direction(I == 0 ? 1 : I == 1 ? -1 : 0, I == 2 ? 1 : I == 3 ? -1 : 0);
            FVector2D A = Center + Direction * Gap, B = Center + Direction * (Gap + 5);
            Line(A, B, FLinearColor(0, 0, 0, .8f), 3.5f);
            Line(A, B, FLinearColor(.92, .96, .94, .92), 1.5f);
        }
    }
    float Now = GetWorld()->GetTimeSeconds();
    if (bShowTargetBar)
    {
        Box(TargetBarPosition - FVector2D(30, 3), FVector2D(60, 5), FLinearColor(.01, .015, .02, .8));
        Box(TargetBarPosition - FVector2D(29, 2), FVector2D(58 * TargetBarFraction, 3),
            FLinearColor(.75, .88, .8, .95));
    }
    if (Now < Player->HitUntil)
    {
        float Alpha = FMath::Clamp((Player->HitUntil - Now) / .25f, 0.f, 1.f);
        FLinearColor Color = Player->bLastHitKill    ? FLinearColor(1, .62, .22, Alpha)
                             : Player->bLastHitArmor ? FLinearColor(.35, .75, 1, Alpha)
                                                     : FLinearColor(1, 1, 1, Alpha);
        float Gap = Player->bLastHitKill ? 10 : 7;
        for (int32 X : {-1, 1})
            for (int32 Y : {-1, 1})
                Line(Center + FVector2D(X, Y) * Gap, Center + FVector2D(X, Y) * (Gap + 5), Color,
                     Player->bLastHitHead ? 3 : 2);
    }
    float Damage = FMath::Clamp((Player->DamageUntil - Now) / .65f, 0.f, 1.f);
    if (Damage > 0)
    {
        FLinearColor Color =
            Player->bDamageWasArmor ? FLinearColor(.12, .48, .95, 1) : FLinearColor(.72, .07, .035, 1);
        for (int32 I = 0; I < 10; I++)
        {
            float Inset = I * 4.f;
            Color.A = Damage * .1f * (1 - I / 10.f);
            Box(FVector2D(Inset, Inset), FVector2D(Size.X - 2 * Inset, 4), Color);
            Box(FVector2D(Inset, Size.Y - Inset - 4), FVector2D(Size.X - 2 * Inset, 4), Color);
            Box(FVector2D(Inset, Inset), FVector2D(4, Size.Y - 2 * Inset), Color);
            Box(FVector2D(Size.X - Inset - 4, Inset), FVector2D(4, Size.Y - 2 * Inset), Color);
        }
        if (!Player->bDamageWasEnvironmental)
        {
            FVector Direction = Player->DamageSource - Player->GetActorLocation();
            float Angle = FMath::DegreesToRadians(
                FRotator::NormalizeAxis(Direction.Rotation().Yaw - PC->GetControlRotation().Yaw) - 90);
            CombatLines.Reset();
            for (int32 I = 0; I <= 16; I++)
            {
                float A = Angle - .28f + I * .56f / 16;
                CombatLines.Add(Center + FVector2D(FMath::Cos(A), FMath::Sin(A)) * 86);
            }
            Color.A = Damage;
            FSlateDrawElement::MakeLines(Out, Layer + 2, G.ToPaintGeometry(), CombatLines,
                                         ESlateDrawEffect::None, Color, true, 4);
        }
    }
    return Layer + 3;
}
