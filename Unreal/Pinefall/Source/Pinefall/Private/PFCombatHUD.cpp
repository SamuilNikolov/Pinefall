#include "PFCombatHUD.h"
#include "Engine/Texture2D.h"
#include "PFCharacter.h"
#include "PFInterface.h"
#include "PFInventoryComponent.h"
#include "PFMatchState.h"
#include "PFPlayerController.h"
#include "Rendering/DrawElements.h"
#include "Styling/CoreStyle.h"

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
            Box(FVector2D(Center.X + X, Y), FVector2D(Center.X - X, Row + .5f),
                FLinearColor(0, 0, 0, .98f));
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

int32 UPFInterface::PaintExpeditionHUD(const FGeometry &G, FSlateWindowElementList &Out,
                                       int32 Layer) const
{
    auto *PC = Cast<APFPlayerController>(GetOwningPlayer());
    if (!PC || PC->bMenuOpen || PC->bMapOpen)
        return Layer;
    const FVector2D Size = G.GetLocalSize();
    const float S = FMath::Clamp(Size.Y / 900.f, .8f, 1.15f);
    const auto *White = FCoreStyle::Get().GetBrush(TEXT("WhiteBrush"));
    const FLinearColor Paper(.94, .96, .98), Muted(.65, .72, .8), Ink(.018, .035, .065, .78);
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
    FVector2D Self(30, Size.Y - 140 * S);
    Box(Self, FVector2D(350, 112) * S, Ink);
    Box(Self, FVector2D(3, 112) * S, bHUDDowned ? Red : Health, 1);
    Icon(4, Self + FVector2D(14, 14) * S, 1.25f * S, Paper);
    Text(Self + FVector2D(43, 8) * S, HUDName, bHUDDowned ? HUDSmallFont : HUDLabelFont, Paper);
    if (bHUDDowned)
        Text(Self + FVector2D(240, 10) * S, TEXT("DOWNED"), HUDSmallFont, Red);
    Icon(1, Self + FVector2D(14, 43) * S, .95f * S, Shield);
    Bar(Self + FVector2D(44, 43) * S, FVector2D(235, 13) * S,
        HUDArmorValue / FMath::Max(1.f, HUDArmorMax), Shield, FMath::RoundToInt(HUDArmorMax / 25));
    Text(Self + FVector2D(291, 37) * S, HUDArmor, HUDLabelFont, Shield);
    Icon(0, Self + FVector2D(14, 65) * S, .95f * S, Health);
    Bar(Self + FVector2D(44, 65) * S, FVector2D(235, 17) * S, HUDHealthValue / 100,
        bHUDDowned ? Red : Health);
    Text(Self + FVector2D(291, 59) * S, HUDHealth, HUDLabelFont, Paper);
    Icon(3, Self + FVector2D(44, 92) * S, .7f * S, HUDHungerValue < 25 ? Red : Amber);
    Bar(Self + FVector2D(62, 95) * S, FVector2D(81, 5) * S, HUDHungerValue / 100,
        HUDHungerValue < 25 ? Red : Amber);
    Text(Self + FVector2D(149, 85) * S, HUDHunger, HUDSmallFont, Muted);
    Icon(2, Self + FVector2D(195, 92) * S, .7f * S, Paper);
    Bar(Self + FVector2D(213, 95) * S, FVector2D(66, 5) * S, HUDStaminaValue / 100, Paper);
    Text(Self + FVector2D(291, 85) * S, HUDStamina, HUDSmallFont, Muted);
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
    if (PC->bInventoryOpen || PC->RoundPanelOpen() || HUDWeaponName.IsEmpty())
        return Layer + 6;
    const float Tile = 64 * S, Gap = 6 * S;
    FVector2D Start(Size.X - 30 - 6 * Tile - 5 * Gap, Size.Y - 30 - Tile);
    Box(Start-FVector2D(Tile+Gap+8,52*S),FVector2D(7*Tile+6*Gap+16,Tile+60*S),Ink);
    // Stable equipment and consumable keys stay visible regardless of pickup order.
    for (int32 I = 0; I < 6; I++)
    {
        const auto &Item = HUDItems[I];
        FVector2D P = Start + FVector2D(I * (Tile + Gap), 0);
        Box(P, FVector2D(Tile), Ink);

        FLinearColor Tint = Item.bOccupied ? Item.Rarity : FLinearColor(.12,.16,.21);
        Tint.A = .23f;
        Box(P, FVector2D(Tile), Tint, 1);
        Box(P + FVector2D(0, Tile - 4), FVector2D(Tile, 4), Item.bOccupied?Item.Rarity:Muted*.3f, 2);
        if (Item.bEquipped)
        {
            Box(P - FVector2D(1), FVector2D(Tile + 2, 2), Paper, 2);
            Box(P - FVector2D(1), FVector2D(2, Tile + 2), Paper, 2);
            Box(P + FVector2D(Tile - 1, 0), FVector2D(2, Tile), Paper, 2);
        }
        if (Item.Icon.GetResourceObject())
            FSlateDrawElement::MakeBox(Out, Layer + 2,
                                       G.ToPaintGeometry(FVector2D(Tile - 10, Tile - 21),
                                                         FSlateLayoutTransform(P + FVector2D(5, 2))),
                                       &Item.Icon, ESlateDrawEffect::None, Paper);
        if (!Item.Key.IsEmpty())
        {
            Box(P + FVector2D(4, 4), FVector2D(16, 18), Paper, 3);
            Text(P + FVector2D(7, 2), Item.Key, HUDSmallFont, FLinearColor(.02, .035, .06));
        }
        Text(P + FVector2D(8, Tile - 23), Item.Count, HUDSmallFont, Paper);
        if (!Item.Caption.IsEmpty())
            Text(P + FVector2D(5, -20), Item.Caption, HUDSmallFont, Muted);
    }
    Text(Start - FVector2D(0, 44), TEXT("ALT HOLD / TAB  INVENTORY     M  MAP"), HUDSmallFont, Muted);
    const FVector2D Axe=Start-FVector2D(Tile+Gap,0);
    Box(Axe,FVector2D(Tile),bHUDPickaxe?FLinearColor(.1,.29,.44,.88):Ink);
    Text(Axe+FVector2D(5,2),TEXT("1"),HUDSmallFont,Paper);
    Line(Axe+FVector2D(20,49)*S,Axe+FVector2D(43,18)*S,Paper,4*S);
    Line(Axe+FVector2D(23,19)*S,Axe+FVector2D(49,33)*S,Paper,5*S);
    Text(Axe-FVector2D(0,20),TEXT("PICKAXE"),HUDSmallFont,Muted);
    static const FString BuildNames[]={TEXT("WALL"),TEXT("FLOOR"),TEXT("RAMP"),TEXT("ROOF")};
    static const FString BuildKeys[]={TEXT("F1"),TEXT("F2"),TEXT("F3"),TEXT("F4")};
    for(int32 I=0;I<4;I++)
    {
        FVector2D P=Start+FVector2D(I*99*S,-106*S);FLinearColor Tint=bHUDBuild&&HUDShape==I?Shield:Muted;
        Box(P,FVector2D(92,43)*S,Ink);
        Text(P+FVector2D(5,2)*S,BuildKeys[I],HUDSmallFont,Tint);
        Text(P+FVector2D(30,22)*S,BuildNames[I],HUDSmallFont,Tint);
        FVector2D A=P+FVector2D(43,6)*S,B=P+FVector2D(78,20)*S;
        if(I==0){Line(A,{B.X,A.Y},Tint);Line({B.X,A.Y},B,Tint);Line(B,{A.X,B.Y},Tint);Line({A.X,B.Y},A,Tint);}
        else if(I==1||I==3)
        {
            FVector2D C((A.X+B.X)*.5,A.Y),D((A.X+B.X)*.5,B.Y);
            Line({A.X,(A.Y+B.Y)*.5},C,Tint);Line(C,{B.X,(A.Y+B.Y)*.5},Tint);Line({B.X,(A.Y+B.Y)*.5},D,Tint);Line(D,{A.X,(A.Y+B.Y)*.5},Tint);
            if(I==3){Line(C-FVector2D(0,6*S),{A.X,(A.Y+B.Y)*.5},Tint);Line(C-FVector2D(0,6*S),{B.X,(A.Y+B.Y)*.5},Tint);Line(C-FVector2D(0,6*S),D,Tint);}
        }
        else{Line({A.X,B.Y},B,Tint);Line({A.X,B.Y},{B.X,A.Y},Tint);Line({B.X,A.Y},B,Tint);}
    }
    Box(Start+FVector2D(-4,-141)*S,FVector2D(396,28)*S,Ink);
    Text(Start-FVector2D(0,135*S),TEXT("WOOD"),HUDSmallFont,Muted);Text(Start+FVector2D(50*S,-135*S),HUDMaterials[0],HUDSmallFont,Paper);
    Text(Start+FVector2D(108*S,-135*S),TEXT("STONE"),HUDSmallFont,Muted);Text(Start+FVector2D(167*S,-135*S),HUDMaterials[1],HUDSmallFont,Paper);
    Text(Start+FVector2D(222*S,-135*S),TEXT("METAL"),HUDSmallFont,Muted);Text(Start+FVector2D(283*S,-135*S),HUDMaterials[2],HUDSmallFont,Paper);
    if(bHUDBuild||bHUDPickaxe)
    {
        Box(Start-FVector2D(8,198*S),FVector2D(404*S,57*S),Ink);
        Text(Start-FVector2D(0,191*S),bHUDPickaxe?TEXT("PICKAXE"):TEXT("BUILD MODE"),HUDTitleFont,Paper);
        Text(Start-FVector2D(0,158*S),HUDBuildHint,HUDSmallFont,Muted);
        return Layer+6;
    }
    const FVector2D Weapon(Size.X - 330 * S - 30, Start.Y - 300 * S);
    Box(Weapon-FVector2D(12,6)*S,FVector2D(350,161)*S,Ink);
    Text(Weapon, HUDWeaponName, HUDTitleFont, Paper);
    Text(Weapon + FVector2D(0, 39) * S, HUDWeaponType, HUDSmallFont, Muted);
    if (HUDWeaponIcon.GetResourceObject() && !bHUDVehicle)
        FSlateDrawElement::MakeBox(
            Out, Layer + 2,
            G.ToPaintGeometry(FVector2D(124, 64) * S,
                              FSlateLayoutTransform(Weapon + FVector2D(0, 75) * S)),
            &HUDWeaponIcon, ESlateDrawEffect::None, Paper);
    Text(Weapon + FVector2D(151, 63) * S, HUDMagazine, HUDNumberFont, bHUDReloading ? Amber : Paper);
    Text(Weapon + FVector2D(242, 90) * S, HUDReserve, HUDLabelFont, Muted);
    Line(Weapon + FVector2D(231, 93) * S, Weapon + FVector2D(237, 115) * S, Muted);
    if (!HUDNotice.IsEmpty())
        Text(Weapon + FVector2D(151, 129) * S, HUDNotice, HUDSmallFont, Amber);
    return Layer + 6;
}
