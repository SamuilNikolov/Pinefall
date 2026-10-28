#pragma once
#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"
#include "PFCombatHUD.generated.h"
class UPFInterface;
/** Drawn below foreground controls, so the optic never covers vitals or ammunition. */
UCLASS()
class PINEFALL_API UPFCombatHUD : public UUserWidget
{
    GENERATED_BODY()
  public:
    UPROPERTY() TObjectPtr<UPFInterface> Interface;
    virtual int32 NativePaint(const FPaintArgs &Args, const FGeometry &Geometry, const FSlateRect &Cull,
                              FSlateWindowElementList &Out, int32 Layer, const FWidgetStyle &Style,
                              bool ParentEnabled) const override;
};
