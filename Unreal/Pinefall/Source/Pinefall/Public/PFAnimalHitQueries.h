#pragma once
#include "CoreMinimal.h"
#include "Subsystems/WorldSubsystem.h"
#include "PFAnimalHitQueries.generated.h"
class APFWildlife;
UCLASS()
class PINEFALL_API UPFAnimalHitQueries : public UWorldSubsystem
{
    GENERATED_BODY()
  public:
    void Register(APFWildlife *Animal);
    void Unregister(APFWildlife *Animal);
    bool Trace(const FVector &Start, const FVector &End, FHitResult &Hit, float SweepRadius = 0,
               const APFWildlife *Ignored = nullptr) const;

  private:
    TArray<TWeakObjectPtr<APFWildlife>> Animals;
};
