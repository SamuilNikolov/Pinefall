#pragma once
#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "PFPlayerController.generated.h"
class UPFInterface;
UCLASS()
class PINEFALL_API APFPlayerController : public APlayerController
{
    GENERATED_BODY()
  public:
    APFPlayerController();
    virtual void BeginPlay() override;
    virtual void SetupInputComponent() override;
    void ToggleMenu();
    void SetMenu(bool bOpen);
    UPROPERTY(BlueprintReadOnly) bool bMenuOpen = false;
    UPROPERTY() TObjectPtr<UPFInterface> Interface;
};
