#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "PFNarratorComponent.generated.h"
UCLASS()
class PINEFALL_API UPFNarratorComponent : public UActorComponent
{
    GENERATED_BODY()
public:
    UPFNarratorComponent();
    virtual void BeginPlay() override;
    virtual void EndPlay(const EEndPlayReason::Type Reason) override;
    void Enqueue(FName Cue);
    void Clear();
    bool HasRecordedCues() const {return Cues.Num()==3;}
private:
    UPROPERTY() TObjectPtr<class UAudioComponent> Voice;
    UPROPERTY() TMap<FName,TObjectPtr<class USoundBase>> Cues;
    TArray<TPair<FName,double>> Pending;
    FTimerHandle Poll;
    void Advance();
};
