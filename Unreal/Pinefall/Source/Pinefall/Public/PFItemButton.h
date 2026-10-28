#pragma once
#include "CoreMinimal.h"
#include "Components/Button.h"
#include "PFItemButton.generated.h"
class APlayerController;
UCLASS()
class PINEFALL_API UPFItemButton : public UButton
{
    GENERATED_BODY()
  public:
    void Setup(APlayerController *Player, int32 Index, bool Drop);
    uint32 Revision = 0;

  private:
    UPROPERTY() TWeakObjectPtr<APlayerController> Controller;
    int32 Slot = 0;
    bool bDrop = false;
    uint32 PressedRevision = 0;
    UFUNCTION() void Pressed();
    UFUNCTION() void Clicked();
};
