#pragma once
#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"
#include "PFTacticalMap.generated.h"
class UTexture2D;
UCLASS()
class PINEFALL_API UPFTacticalMap : public UUserWidget
{
    GENERATED_BODY()
  public:
    virtual void NativeConstruct() override;
    virtual void NativeTick(const FGeometry &Geometry, float Delta) override;
    virtual int32 NativePaint(const FPaintArgs &Args, const FGeometry &Geometry, const FSlateRect &Cull,
                              FSlateWindowElementList &Out, int32 Layer, const FWidgetStyle &Style,
                              bool ParentEnabled) const override;
    virtual FReply NativeOnMouseButtonDown(const FGeometry &Geometry, const FPointerEvent &Event) override;

  private:
    UPROPERTY() TObjectPtr<UTexture2D> Atlas;
    FSlateBrush AtlasBrush;
    float Refresh = 0;
};
