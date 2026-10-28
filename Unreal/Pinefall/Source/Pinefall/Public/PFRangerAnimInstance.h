#pragma once
#include "CoreMinimal.h"
#include "Animation/AnimInstance.h"
#include "PFRangerAnimInstance.generated.h"
class UAnimSequence;
UCLASS(Transient)
class PINEFALL_API UPFRangerAnimInstance : public UAnimInstance
{
    GENERATED_BODY()
  public:
    virtual void NativeInitializeAnimation() override;
    virtual FAnimInstanceProxy *CreateAnimInstanceProxy() override;
    virtual void DestroyAnimInstanceProxy(FAnimInstanceProxy *Proxy) override;
    UPROPERTY(Transient) TMap<FName, TObjectPtr<UAnimSequence>> Clips;
};
