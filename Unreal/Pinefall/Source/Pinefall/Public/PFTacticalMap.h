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
    bool bMini = false;
    virtual void NativeConstruct() override;
    virtual void NativeTick(const FGeometry &Geometry, float Delta) override;
    virtual int32 NativePaint(const FPaintArgs &Args, const FGeometry &Geometry, const FSlateRect &Cull,
                              FSlateWindowElementList &Out, int32 Layer, const FWidgetStyle &Style,
                              bool ParentEnabled) const override;
    virtual FReply NativeOnMouseButtonDown(const FGeometry &Geometry, const FPointerEvent &Event) override;
    virtual FReply NativeOnMouseButtonUp(const FGeometry &Geometry, const FPointerEvent &Event) override;
    virtual FReply NativeOnMouseMove(const FGeometry &Geometry, const FPointerEvent &Event) override;
    virtual FReply NativeOnMouseWheel(const FGeometry &Geometry, const FPointerEvent &Event) override;

  private:
    UPROPERTY() TObjectPtr<UTexture2D> Atlas;
    FSlateBrush AtlasBrush;
    float Refresh = 0, Zoom = 1;
    FVector2D Center = FVector2D::ZeroVector;
    bool bDragging = false, bReserve = false;
    float HalfExtent() const;
    void ClampCenter();
};
