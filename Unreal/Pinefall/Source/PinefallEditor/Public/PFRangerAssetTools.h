#pragma once
#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "PFRangerAssetTools.generated.h"
class USkeletalMesh;
UCLASS()
class UPFRangerAssetTools : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()
  public:
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor")
    static void FitHeadCollision(USkeletalMesh *Mesh);
};
