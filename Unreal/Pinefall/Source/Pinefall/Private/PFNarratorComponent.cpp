#include "PFNarratorComponent.h"
#include "PFPlayerController.h"
#include "Components/AudioComponent.h"
#include "Sound/SoundBase.h"
#include "TimerManager.h"
UPFNarratorComponent::UPFNarratorComponent(){PrimaryComponentTick.bCanEverTick=false;}
void UPFNarratorComponent::BeginPlay()
{
    Super::BeginPlay();
    auto *PC=Cast<APFPlayerController>(GetOwner());if(!PC||!PC->IsLocalController())return;
    Voice=NewObject<UAudioComponent>(GetOwner());Voice->bAutoActivate=false;Voice->bAllowSpatialization=false;Voice->bIsUISound=true;Voice->RegisterComponent();Voice->SetVolumeMultiplier(.65f);
    for(const FName Name:{FName(TEXT("Inbound")),FName(TEXT("Landed")),FName(TEXT("Claimed"))})
        if(auto *Cue=LoadObject<USoundBase>(nullptr,*FString::Printf(TEXT("/Game/Pinefall/Audio/Supply%s.Supply%s"),*Name.ToString(),*Name.ToString())))Cues.Add(Name,Cue);
}
void UPFNarratorComponent::Enqueue(FName Cue)
{
    if(!Voice||!Cues.Contains(Cue)||Pending.ContainsByPredicate([&](const auto &Item){return Item.Key==Cue;}))return;
    if(Pending.Num()>=4)Pending.RemoveAt(0);
    Pending.Emplace(Cue,GetWorld()->GetTimeSeconds()+15);
    Advance();if(!GetWorld()->GetTimerManager().IsTimerActive(Poll))GetWorld()->GetTimerManager().SetTimer(Poll,this,&UPFNarratorComponent::Advance,.25f,true);
    UE_LOG(LogTemp,Display,TEXT("PF_NARRATION_QUEUED cue=%s"),*Cue.ToString());
}
void UPFNarratorComponent::Advance()
{
    if(!Voice||Voice->IsPlaying())return;
    const double Now=GetWorld()->GetTimeSeconds();
    while(!Pending.IsEmpty())
    {
        const auto Item=Pending[0];Pending.RemoveAt(0);
        if(Item.Value<Now)continue;
        Voice->SetSound(Cues.FindRef(Item.Key));Voice->Play();return;
    }
    GetWorld()->GetTimerManager().ClearTimer(Poll);
}
void UPFNarratorComponent::Clear()
{
    GetWorld()->GetTimerManager().ClearTimer(Poll);Pending.Reset();if(Voice)Voice->Stop();
}
void UPFNarratorComponent::EndPlay(const EEndPlayReason::Type Reason)
{
    GetWorld()->GetTimerManager().ClearTimer(Poll);Pending.Reset();if(Voice)Voice->Stop();Super::EndPlay(Reason);
}
