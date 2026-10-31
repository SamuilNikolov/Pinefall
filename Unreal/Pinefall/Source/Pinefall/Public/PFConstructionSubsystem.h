#pragma once
#include "CoreMinimal.h"
#include "Subsystems/WorldSubsystem.h"
#include "PFConstructionSubsystem.generated.h"
class APFConstructionWorld;
UCLASS()
class PINEFALL_API UPFConstructionSubsystem : public UWorldSubsystem
{
    GENERATED_BODY()
  public:
    UPROPERTY() TObjectPtr<APFConstructionWorld> Manager;
};
