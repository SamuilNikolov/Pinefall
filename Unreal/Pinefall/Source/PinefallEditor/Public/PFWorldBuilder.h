#pragma once
#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "PFWorldBuilder.generated.h"
class UStaticMesh;
UCLASS()
class UPFWorldBuilder : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()
  public:
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor")
    static void ConfigureMesh(UStaticMesh *Mesh, bool Nanite);
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor")
    static void ConfigureCollision(UStaticMesh *Mesh, bool Complex);
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor") static void BuildLandscape();
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor")
    static void PopulateFoliage(const TArray<UStaticMesh *> &Trees, UStaticMesh *Grass, UStaticMesh *Fern);
    UFUNCTION(BlueprintPure, Category = "Pinefall|Editor") static float HeightAt(float X, float Y);
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor") static void BuildReserveLandscape();
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor")
    static void PopulateReserve(const TArray<UStaticMesh *> &Trees, UStaticMesh *Grass, UStaticMesh *Fern);
    UFUNCTION(BlueprintPure, Category = "Pinefall|Editor") static float ReserveHeight(float X, float Y);
    UFUNCTION(BlueprintPure, Category = "Pinefall|Editor") static FString ReserveLayoutJson();
    UFUNCTION(BlueprintCallable, Category = "Pinefall|Editor") static int32 BuildReserveRoads();
};
