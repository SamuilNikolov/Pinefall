#include "PFCombatHUD.h"
#include "Engine/Texture2D.h"
#include "PFCharacter.h"
#include "PFConstructionComponent.h"
#include "PFInterface.h"
#include "PFInventoryComponent.h"
#include "PFMatchState.h"
#include "PFPlayerController.h"
#include "Rendering/DrawElements.h"
#include "Styling/CoreStyle.h"
#include "Framework/Application/SlateApplication.h"
#include "Rendering/SlateRenderer.h"

int32 UPFCombatHUD::NativePaint(const FPaintArgs &Args, const FGeometry &G, const FSlateRect &Cull,
                                FSlateWindowElementList &Out, int32 Layer, const FWidgetStyle &Style,
                                bool ParentEnabled) const
{
    int32 Result = Super::NativePaint(Args, G, Cull, Out, Layer, Style, ParentEnabled);
    if (!Interface)
        return Result;
    Result = Interface->PaintCombat(G, Out, Result);
    return Interface->PaintExpeditionHUD(G, Out, Result);
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
    auto Box = [&](FVector2D Position, FVector2D Extent, FLinearColor Color) {
        FSlateDrawElement::MakeBox(Out, DrawLayer,
                                   G.ToPaintGeometry(Extent, FSlateLayoutTransform(Position)), White,
                                   ESlateDrawEffect::None, Color);
    };
    auto Line = [&](FVector2D A, FVector2D B, FLinearColor Color, float Width) {
        CombatLines.Reset();
        CombatLines.Add(A);
        CombatLines.Add(B);
        FSlateDrawElement::MakeLines(Out, DrawLayer + 1, G.ToPaintGeometry(), CombatLines,
                                     ESlateDrawEffect::None, Color, true, Width);
    };
    const bool Combat = !Player->Construction || Player->Construction->Mode == EPFConstructionMode::Combat;
    const bool Editing = Player->Construction && Player->Construction->IsEditing();
    const bool Scope = Combat && Player->Weapon.AimFOV <= 35 && Player->AimAlpha > .92f;
    if (Scope)
    {
        const float Radius = FMath::Min(Size.X, Size.Y) * .38f;
        // One reusable mesh replaces hundreds of per-row mask draw elements.
        // The outer ring lies beyond the viewport; the inner edge is the lens.
        ScopeVertices.Reset();ScopeIndices.Reset();
        constexpr int32 Segments = 96;
        for (int32 I = 0; I <= Segments; I++)
        {
            const float A = I * 2 * PI / Segments;
            const FVector2D Direction(FMath::Cos(A), FMath::Sin(A));
            for (float R : {Radius, float(Size.Size())})
                ScopeVertices.Add(FSlateVertex::Make<ESlateVertexRounding::Disabled>(
                    G.GetAccumulatedRenderTransform(), FVector2f(Center + Direction * R), FVector2f(.5f), FColor(0,0,0,250)));
            if (I < Segments)
            {
                const SlateIndex V = I * 2;
                ScopeIndices.Append({V, SlateIndex(V+1), SlateIndex(V+2), SlateIndex(V+2), SlateIndex(V+1), SlateIndex(V+3)});
            }
        }
        FSlateDrawElement::MakeCustomVerts(Out, DrawLayer,
            FSlateApplication::Get().GetRenderer()->GetResourceHandle(*White), ScopeVertices, ScopeIndices, nullptr, 0, 0);
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
                 FLinearColor(1, 1, 1, .8f), 3.2f);
            Line(Center + FVector2D(0, Sign * 9), Center + FVector2D(0, Sign * Radius * .86f),
                 FLinearColor(1, 1, 1, .8f), 3.2f);
            Line(Center + FVector2D(Sign * 9, 0), Center + FVector2D(Sign * Radius * .86f, 0),
                 FLinearColor(0, 0, 0, .9f), 1.2f);
            Line(Center + FVector2D(0, Sign * 9), Center + FVector2D(0, Sign * Radius * .86f),
                 FLinearColor(0, 0, 0, .9f), 1.2f);
        }
        for (int32 I = 1; I < 5; I++)
            Line(Center + FVector2D(-4, I * Radius * .12f), Center + FVector2D(4, I * Radius * .12f),
                 FLinearColor(0, 0, 0, .9f), 1);
        DrawLayer=Layer+2;
        Box(Center-FVector2D(2.5),FVector2D(5),FLinearColor::Black);
        Box(Center-FVector2D(1),FVector2D(2),FLinearColor::White);
        DrawLayer = Layer;
    }
    else if (!Editing)
    {
        Box(Center - FVector2D(2.5), FVector2D(5), FLinearColor(0,0,0,.95));
        const FLinearColor Color = !Combat && Player->Construction->Mode == EPFConstructionMode::Build
            ? (Player->Construction->CanPreviewPlace() ? FLinearColor(.25,.65,1) : FLinearColor(1,.4,.25))
            : FLinearColor::White;
        Box(Center - FVector2D(1), FVector2D(2), Color);
        float Gap = FMath::Lerp(6.f, 3.f, Player->AimAlpha) + Player->ShotBloom * .35f +
                    FMath::Min(4.f, Player->GetVelocity().Size2D() / 150.f) * (1 - Player->AimAlpha);
        for (int32 I = 0; I < (Combat ? 4 : 0); I++)
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

int32 UPFInterface::PaintExpeditionHUD(const FGeometry &G, FSlateWindowElementList &Out,
                                       int32 Layer) const
{
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    if (!PC || PC->bMenuOpen || PC->bMapOpen)
        return Layer;
    const FVector2D Size = G.GetLocalSize();
    const float S = FMath::Clamp(Size.Y / 900.f, .8f, 1.15f);
    const auto *White = FCoreStyle::Get().GetBrush(TEXT("WhiteBrush"));
    const FLinearColor Paper(.94, .96, .98), Muted(.65, .72, .8), Ink(.018, .035, .065, .96);
    const FLinearColor Health(.29, .84, .64), Shield(.25, .57, 1), Amber(.95, .67, .29),
        Storm(.7, .46, 1), Red(.98, .28, .21);
    auto Box = [&](FVector2D P, FVector2D E, FLinearColor C, int32 Z = 0) {
        FSlateDrawElement::MakeBox(Out, Layer + Z, G.ToPaintGeometry(E, FSlateLayoutTransform(P)), White,
                                   ESlateDrawEffect::None, C);
    };
    auto Text = [&](FVector2D P, const FString &Value, const FSlateFontInfo &Font, FLinearColor C) {
        FSlateFontInfo SizedFont = Font;
        SizedFont.Size = FMath::Max(10.f, Font.Size * S);
        FSlateDrawElement::MakeText(
            Out, Layer + 3,
            G.ToPaintGeometry(FVector2D(500, 80), FSlateLayoutTransform(P + FVector2D(0, 1))), Value,
            SizedFont, ESlateDrawEffect::None, FLinearColor(0, 0, 0, .55));
        FSlateDrawElement::MakeText(Out, Layer + 4,
                                    G.ToPaintGeometry(FVector2D(500, 80), FSlateLayoutTransform(P)),
                                    Value, SizedFont, ESlateDrawEffect::None, C);
    };
    auto Line = [&](FVector2D A, FVector2D B, FLinearColor C, float Width = 1.5f) {
        CombatLines.Reset();
        CombatLines.Add(A);
        CombatLines.Add(B);
        FSlateDrawElement::MakeLines(Out, Layer + 2, G.ToPaintGeometry(), CombatLines,
                                     ESlateDrawEffect::None, C, true, Width);
    };
    auto Icon = [&](int32 Kind, FVector2D P, float K, FLinearColor C) {
        auto Edge = [&](float AX, float AY, float BX, float BY) {
            Line(P + FVector2D(AX, AY) * K, P + FVector2D(BX, BY) * K, C, 1.7f);
        };
        if (Kind == 0)
        {
            Box(P + FVector2D(6, 1) * K, FVector2D(4, 14) * K, C, 2);
            Box(P + FVector2D(1, 6) * K, FVector2D(14, 4) * K, C, 2);
        }
        else if (Kind == 1)
        {
            Edge(1, 1, 15, 1);
            Edge(15, 1, 14, 9);
            Edge(14, 9, 8, 15);
            Edge(8, 15, 2, 9);
            Edge(2, 9, 1, 1);
        }
        else if (Kind == 2)
        {
            Edge(9, 0, 3, 9);
            Edge(3, 9, 9, 8);
            Edge(9, 8, 6, 16);
            Edge(6, 16, 14, 5);
            Edge(14, 5, 8, 6);
        }
        else if (Kind == 3)
        {
            Edge(5, 2, 11, 2);
            Edge(11, 2, 15, 7);
            Edge(15, 7, 12, 14);
            Edge(12, 14, 4, 14);
            Edge(4, 14, 1, 7);
            Edge(1, 7, 5, 2);
            Edge(8, 3, 9, 0);
            Edge(9, 0, 13, 0);
        }
        else
        {
            Edge(6, 0, 10, 0);
            Edge(10, 0, 12, 3);
            Edge(12, 3, 10, 7);
            Edge(10, 7, 6, 7);
            Edge(6, 7, 4, 3);
            Edge(4, 3, 6, 0);
            Edge(6, 9, 2, 12);
            Edge(2, 12, 2, 16);
            Edge(2, 16, 14, 16);
            Edge(14, 16, 14, 12);
            Edge(14, 12, 10, 9);
        }
    };
    auto Bar = [&](FVector2D P, FVector2D E, float Value, FLinearColor C, int32 Segments = 0) {
        Box(P, E, FLinearColor(.025, .05, .08, .95));
        Box(P + FVector2D(1),
            FVector2D(FMath::Max(0.f, E.X - 2) * FMath::Clamp(Value, 0.f, 1.f),
                      FMath::Max(0.f, E.Y - 2)),
            C, 1);
        for (int32 I = 1; I < Segments; I++)
            Box(P + FVector2D(E.X * I / Segments, 0), FVector2D(2, E.Y), Ink, 2);
    };
    // Lower-left survival plate: the large value and cross always mean health;
    // shield, food and exertion each have a distinct shape as well as a color.
    FVector2D Self(30, Size.Y - 112 * S);
    Box(Self, FVector2D(310, 84) * S, Ink);
    Box(Self, FVector2D(3, 84) * S, bHUDDowned ? Red : Health, 1);
    if (bHUDDowned)
        Text(Self - FVector2D(0, 26) * S, TEXT("DOWNED"), HUDSmallFont, Red);
    Icon(1, Self + FVector2D(12, 12) * S, .95f * S, Shield);
    Bar(Self + FVector2D(38, 12) * S, FVector2D(215, 13) * S,
        HUDArmorValue / FMath::Max(1.f, HUDArmorMax), Shield, FMath::RoundToInt(HUDArmorMax / 25));
    Text(Self + FVector2D(265, 6) * S, HUDArmor, HUDLabelFont, Shield);
    Icon(0, Self + FVector2D(12, 35) * S, .95f * S, Health);
    Bar(Self + FVector2D(38, 35) * S, FVector2D(215, 17) * S, HUDHealthValue / 100,
        bHUDDowned ? Red : Health);
    Text(Self + FVector2D(265, 29) * S, HUDHealth, HUDLabelFont, Paper);
    Icon(3, Self + FVector2D(38, 63) * S, .7f * S, HUDHungerValue < 25 ? Red : Amber);
    Bar(Self + FVector2D(56, 66) * S, FVector2D(72, 5) * S, HUDHungerValue / 100,
        HUDHungerValue < 25 ? Red : Amber);
    Text(Self + FVector2D(135, 56) * S, HUDHunger, HUDSmallFont, Muted);
    if(HUDStaminaValue<99)
    {
        Icon(2, Self + FVector2D(190, 63) * S, .7f * S, Paper);
        Bar(Self + FVector2D(208, 66) * S, FVector2D(45, 5) * S, HUDStaminaValue / 100, Paper);
        Text(Self + FVector2D(265, 56) * S, HUDStamina, HUDSmallFont, Muted);
    }
    for (int32 I = 0; I < HUDAllies.Num(); I++)
    {
        const auto &Ally = HUDAllies[I];
        FVector2D P = Self - FVector2D(0, (HUDAllies.Num() - I) * 58 * S + 8);
        Box(P, FVector2D(290, 52) * S, Ink);
        Box(P, FVector2D(3, 52) * S, Ally.bDowned ? Red : Ally.Color, 1);
        Icon(4, P + FVector2D(12, 12) * S, 1.1f * S, Ally.bEliminated ? Muted : Paper);
        Text(P + FVector2D(41, 4) * S, Ally.Name, HUDSmallFont, Ally.bEliminated ? Muted : Paper);
        Text(P + FVector2D(214, 4) * S, Ally.Status, HUDSmallFont, Ally.bDowned ? Red : Muted);
        Bar(P + FVector2D(42, 28) * S, FVector2D(232, 5) * S,
            Ally.Armor / FMath::Max(1.f, Ally.ArmorMax), Shield);
        Bar(P + FVector2D(42, 38) * S, FVector2D(232, 6) * S, Ally.Health / 100,
            Ally.bDowned ? Red : Paper);
    }
    // Map and round state share the upper-left column.
    if (!PC->bInventoryOpen && !PC->RoundPanelOpen())
    {
        FVector2D Status(28, 262);
        Box(Status, FVector2D(226, 66), Ink);
        Text(Status + FVector2D(10, 5), HUDStormLabel, HUDSmallFont, Storm);
        Text(Status + FVector2D(10, 26), HUDStormClock, HUDTitleFont, Paper);
        Icon(4, Status + FVector2D(91, 36), 1, Paper);
        Text(Status + FVector2D(114, 27), HUDSurvivors, HUDLabelFont, Paper);
        Icon(0, Status + FVector2D(165, 37), .8f, Muted);
        Text(Status + FVector2D(187, 27), HUDEliminations, HUDLabelFont, Paper);
    }
    const FVector2D Compass(Size.X * .5f, 25);
    Text(Compass - FVector2D(12, 7), HUDHeading, HUDTitleFont, Paper);
    static const FString Directions[] = {TEXT("N"), TEXT("NE"), TEXT("E"), TEXT("SE"),
                                         TEXT("S"), TEXT("SW"), TEXT("W"), TEXT("NW")};
    float Yaw = PC->GetControlRotation().Yaw;
    for (int32 I = 0; I < 8; I++)
    {
        float D = FRotator::NormalizeAxis(I * 45.f - Yaw);
        if (FMath::Abs(D) > 65)
            continue;
        FVector2D P = Compass + FVector2D(D * 3 * S, 27);
        Text(P - FVector2D(7, 0), Directions[I], HUDSmallFont, Muted);
        Line(P - FVector2D(0, 7), P - FVector2D(0, 3), Muted, 1);
    }
    if(bHUDLootFocus)
    {
        FVector2D P(Size.X*.5f+48*S,Size.Y*.5f+30*S);float W=FMath::Min(330*S,Size.X-P.X-20);
        Box(P,FVector2D(W,84*S),FLinearColor(.018,.03,.05,.94));
        FLinearColor Band=HUDLootColor;Band.A=.78f;Box(P,FVector2D(W,38*S),Band,1);
        Box(P-FVector2D(43*S,0),FVector2D(39*S,38*S),Ink,1);
        Text(P-FVector2D(32*S,-4*S),TEXT("E"),HUDLabelFont,Paper);
        Text(P+FVector2D(10,7)*S,HUDLootName,HUDLabelFont,Paper);
        Text(P+FVector2D(W-42*S,8*S),HUDLootQuantity,HUDSmallFont,Paper);
        Text(P+FVector2D(10,44)*S,HUDLootDetail,HUDSmallFont,Muted);
        Text(P+FVector2D(10,62)*S,HUDLootAction,HUDSmallFont,bHUDLootCanTake?Paper:Amber);
    }
    if (PC->bInventoryOpen || PC->RoundPanelOpen())
        return Layer + 6;

    // Three independent, right-aligned tiers, following the supplied concept.
    // No enclosing dashboard or duplicate weapon-information panel.
    const float Tile=76*S, Gap=6*S, Right=Size.X-28*S;
    const FVector2D Axe(Right-7*Tile-6*Gap, Size.Y-28*S-Tile);
    const FLinearColor Outline(.24,.46,.63,1), Selected(.55,.86,1,1);
    auto Frame = [&](FVector2D P,FVector2D E,FLinearColor C,float Width) {
        Box(P,FVector2D(E.X,Width),C,2);
        Box(P+FVector2D(0,E.Y-Width),FVector2D(E.X,Width),C,2);
        Box(P,FVector2D(Width,E.Y),C,2);
        Box(P+FVector2D(E.X-Width,0),FVector2D(Width,E.Y),C,2);
    };
    auto Key = [&](FVector2D P,const FString &Value) {
        Box(P,FVector2D(Value.Len()>1?23*S:17*S,17*S),Ink,3);
        Text(P+FVector2D(3*S,0),Value,HUDSmallFont,Paper);
    };
    for(int32 I=0;I<6;I++)
    {
        const auto &Item=HUDItems[I];
        const FVector2D P=Axe+FVector2D((I+1)*(Tile+Gap),0);
        FLinearColor Fill=Item.bOccupied?Item.Rarity* .38f:FLinearColor(.025,.055,.085);
        Fill.A=.96f;
        Box(P,FVector2D(Tile),Fill);
        Frame(P,FVector2D(Tile),Item.bEquipped?Selected:Item.bOccupied?Item.Rarity:Outline,
              (Item.bEquipped?3.f:1.5f)*S);
        if(Item.Icon.GetResourceObject())
        {
            const FVector2D Available(Tile-12*S,Tile-25*S);
            const FVector2D Source(FMath::Max(1.f,Item.Icon.ImageSize.X),FMath::Max(1.f,Item.Icon.ImageSize.Y));
            const float Fit=FMath::Min(Available.X/Source.X,Available.Y/Source.Y);
            const FVector2D Extent=Source*Fit;
            FSlateDrawElement::MakeBox(Out,Layer+2,
                G.ToPaintGeometry(Extent,FSlateLayoutTransform(P+FVector2D(6*S,5*S)+(Available-Extent)*.5f)),
                &Item.Icon,ESlateDrawEffect::None,Paper);
        }
        Key(P+FVector2D(4*S,4*S),Item.Key);
        if(Item.bOccupied)
        {
            const float CountWidth=Item.Count.Len()*HUDLabelFont.Size*.62f*S;
            Text(P+FVector2D(FMath::Max(6*S,Tile-7*S-CountWidth),Tile-25*S),Item.Count,HUDLabelFont,Paper);
        }
    }
    Box(Axe,FVector2D(Tile),FLinearColor(.025,.24,.37,.97));
    Frame(Axe,FVector2D(Tile),bHUDPickaxe?Selected:Outline,(bHUDPickaxe?3.f:1.5f)*S);
    Key(Axe+FVector2D(4*S,4*S),TEXT("1"));
    // A readable, shaped harvesting-tool silhouette instead of a text label.
    Line(Axe+FVector2D(23,61)*S,Axe+FVector2D(49,21)*S,FLinearColor(.36,.19,.075),6*S);
    Line(Axe+FVector2D(24,60)*S,Axe+FVector2D(49,21)*S,FLinearColor(.75,.48,.22),2*S);
    Line(Axe+FVector2D(27,22)*S,Axe+FVector2D(48,22)*S,Paper,6*S);
    Line(Axe+FVector2D(48,22)*S,Axe+FVector2D(61,35)*S,Paper,5*S);
    Line(Axe+FVector2D(61,35)*S,Axe+FVector2D(62,41)*S,Muted,3*S);

    const float BuildTile=44*S, BuildGap=5*S;
    const FVector2D Builds(Right-4*BuildTile-3*BuildGap,Axe.Y-54*S);
    static const FString BuildKeys[]={TEXT("F1"),TEXT("F2"),TEXT("F3"),TEXT("F4")};
    for(int32 I=0;I<4;I++)
    {
        const FVector2D P=Builds+FVector2D(I*(BuildTile+BuildGap),0);
        const bool Active=bHUDBuild&&HUDShape==I;
        const FLinearColor Tint=Active?Paper:Selected;
        Box(P,FVector2D(BuildTile),Active?FLinearColor(.035,.3,.57,.95):Ink);
        Frame(P,FVector2D(BuildTile),Active?Selected:Outline,(Active?2.5f:1.f)*S);
        Key(P+FVector2D(2*S,1*S),BuildKeys[I]);
        const FVector2D A=P+FVector2D(9,20)*S,B=P+FVector2D(35,35)*S;
        if(I==0)
        {
            Line(A,{B.X,A.Y},Tint);Line({B.X,A.Y},B,Tint);
            Line(B,{A.X,B.Y},Tint);Line({A.X,B.Y},A,Tint);
        }
        else if(I==1||I==3)
        {
            const FVector2D C((A.X+B.X)*.5,A.Y),D((A.X+B.X)*.5,B.Y);
            const FVector2D L(A.X,(A.Y+B.Y)*.5),R(B.X,(A.Y+B.Y)*.5);
            Line(L,C,Tint);Line(C,R,Tint);Line(R,D,Tint);Line(D,L,Tint);
            if(I==3){const FVector2D Peak=C-FVector2D(0,6*S);Line(Peak,L,Tint);Line(Peak,R,Tint);Line(Peak,D,Tint);}
        }
        else {Line({A.X,B.Y},B,Tint);Line({A.X,B.Y},{B.X,A.Y},Tint);Line({B.X,A.Y},B,Tint);}
    }

    const float MaterialTile=50*S;
    const FVector2D Materials(Right-3*MaterialTile-2*Gap,Axe.Y-120*S);
    auto Ring = [&](FVector2D P,float Radius,FLinearColor C) {
        CombatLines.Reset();
        for(int32 I=0;I<=12;I++)
        {const float A=I*2*PI/12;CombatLines.Add(P+FVector2D(FMath::Cos(A),FMath::Sin(A))*Radius);}
        FSlateDrawElement::MakeLines(Out,Layer+2,G.ToPaintGeometry(),CombatLines,ESlateDrawEffect::None,C,true,1.5*S);
    };
    for(int32 I=0;I<3;I++)
    {
        const FVector2D P=Materials+FVector2D(I*(MaterialTile+Gap),0);
        Box(P,FVector2D(MaterialTile,58*S),Ink);
        Frame(P,FVector2D(MaterialTile,58*S),Outline,1.5*S);
        const FVector2D O=P+FVector2D(8,4)*S;
        if(I==0)
        {
            for(int32 Log=0;Log<2;Log++)
            {
                const FVector2D End=O+FVector2D(7+Log*13,24)*S;
                Line(End,End+FVector2D(12,-16)*S,FLinearColor(.37,.2,.07),11*S);
                Line(End,End+FVector2D(12,-16)*S,FLinearColor(.72,.48,.2),7*S);
                Ring(End,4.2*S,FLinearColor(.96,.77,.44));
                Ring(End,1.5*S,FLinearColor(.43,.26,.1));
            }
        }
        else if(I==1)
        {
            Box(O+FVector2D(2,11)*S,FVector2D(30,18)*S,FLinearColor(.48,.2,.075),2);
            Box(O+FVector2D(2,8)*S,FVector2D(30,7)*S,FLinearColor(.76,.4,.17),2);
            for(int32 Hole=0;Hole<3;Hole++)
                Box(O+FVector2D(6+Hole*8,10)*S,FVector2D(4,4)*S,FLinearColor(.19,.1,.06),2);
        }
        else
        {
            const FVector2D A=O+FVector2D(8,27)*S,B=O+FVector2D(25,7)*S;
            Line(A,B,FLinearColor(.3,.38,.47),11*S);
            Line(A,B,FLinearColor(.63,.72,.81),4*S);
            Line(A-FVector2D(6,3)*S,A+FVector2D(6,3)*S,Paper,4*S);
            Line(B-FVector2D(6,3)*S,B+FVector2D(6,3)*S,Paper,4*S);
        }
        const float Width=HUDMaterials[I].Len()*HUDLabelFont.Size*.57f*S;
        Text(P+FVector2D((MaterialTile-Width)*.5f,34*S),HUDMaterials[I],HUDLabelFont,Paper);
    }
    // Instructions are temporary context, never a permanent dashboard row.
    if(bHUDBuild||bHUDPickaxe)
    {
        const FVector2D P(Axe.X,Materials.Y-30*S);
        Box(P,FVector2D(Right-Axe.X,25*S),Ink);
        Text(P+FVector2D(6*S,3*S),HUDBuildHint,HUDSmallFont,Paper);
    }
    else if(bHUDVehicle)
        Text(Axe-FVector2D(0,35*S),HUDMagazine+TEXT(" ")+HUDReserve,HUDLabelFont,Paper);
    else if(!HUDNotice.IsEmpty())
        Text(Axe-FVector2D(0,30*S),HUDNotice,HUDSmallFont,Amber);
    return Layer + 6;
}
