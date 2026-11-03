#include "PFPlayerController.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFInventoryComponent.h"
#include "PFMatchState.h"

void APFPlayerController::UseSurvey()
{
    if(bMenuOpen||bInventoryOpen||bMapOpen||RoundPanelOpen())return;
    if(auto *Ranger=Cast<APFCharacter>(GetPawn()))
        for(int32 I=0;I<Ranger->Inventory->Slots.Num();I++)
        {
            const auto &Slot=Ranger->Inventory->Slots[I];
            if(Slot.Count>0&&Slot.Kind==EPFItemKind::SurveyBeacon)
            {Ranger->Inventory->ServerUseSlot(I,Slot.Revision);return;}
        }
}
bool APFPlayerController::ActivateSurvey()
{
    auto *Ranger=Cast<APFCharacter>(GetPawn());
    auto *State=GetWorld()->GetGameState<APFMatchState>();
    const double Now=GetWorld()->GetTimeSeconds();
    if(!HasAuthority()||!Ranger||!Ranger->CanFight()||Ranger->Vehicle||!State||
        State->Phase!=EPFRoundPhase::Active||Now<SurveyEnds)return false;
    SurveyEnds=Now+12;NextSurveyPulse=0;
    State->SurveyWarningUntil=FMath::Max(State->SurveyWarningUntil,SurveyEnds);State->ForceNetUpdate();
    SurveySecond();return true;
}
void APFPlayerController::SurveySecond()
{
    auto *State=GetWorld()->GetGameState<APFMatchState>();
    auto *Ranger=Cast<APFCharacter>(GetPawn());
    auto *Mine=GetPlayerState<APFPlayerState>();
    const double Now=GetWorld()->GetTimeSeconds();
    if(!HasAuthority())return;
    if(Now>=SurveyVisibleUntil&&!SurveyPoints.IsEmpty()){SurveyPoints.Reset();ForceNetUpdate();}
    if(!State||State->Phase!=EPFRoundPhase::Active||!Ranger||!Ranger->CanFight()||!Mine){SurveyEnds=0;return;}
    if(Now>=SurveyEnds||Now<NextSurveyPulse)return;
    NextSurveyPulse=Now+4;
    TArray<FVector_NetQuantize> Points;Points.Reserve(20);
    for(APlayerState *Entry:State->PlayerArray)
        if(auto *Other=Cast<APFPlayerState>(Entry);Other&&Other->TeamId!=Mine->TeamId&&!Other->bEliminated)
            if(auto *Target=Cast<APFCharacter>(Other->GetPawn());Target&&Target->Health>0)Points.Add(Target->GetActorLocation());
    // Never put enemy coordinates on GameState, PlayerState or a public actor.
    for(FConstPlayerControllerIterator It=GetWorld()->GetPlayerControllerIterator();It;++It)
        if(auto *Recipient=Cast<APFPlayerController>(It->Get()))
            if(auto *Ally=Recipient->GetPlayerState<APFPlayerState>();Ally&&Ally->TeamId==Mine->TeamId&&!Ally->bEliminated)
            {
                Recipient->SurveyPoints=Points;
                Recipient->SurveyVisibleUntil=FMath::Max(Recipient->SurveyVisibleUntil,SurveyEnds);
                Recipient->ForceNetUpdate();
            }
}
void APFPlayerController::ResetSurvey()
{
    SurveyPoints.Reset();SurveyVisibleUntil=SurveyEnds=NextSurveyPulse=0;ForceNetUpdate();
}
