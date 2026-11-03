#include "PFMatchState.h"
#include "PFPlayerController.h"
#include "PFNarratorComponent.h"
#include "Net/UnrealNetwork.h"
float APFMatchState::SafeRadius() const
{
    float Alpha = bClosing ? FMath::Clamp((GetServerWorldTimeSeconds() - StormStarts) /
                                              FMath::Max(.01, StormEnds - StormStarts),
                                          0., 1.)
                           : 0;
    return FMath::Lerp(RadiusFrom, RadiusTo, Alpha);
}
FVector APFMatchState::SafeCenter() const
{
    float Alpha = bClosing ? FMath::Clamp((GetServerWorldTimeSeconds() - StormStarts) /
                                              FMath::Max(.01, StormEnds - StormStarts),
                                          0., 1.)
                           : 0;
    return FMath::Lerp(FVector(CenterFrom), FVector(CenterTo), Alpha);
}
float APFMatchState::SecondsLeft() const
{
    return FMath::Max(0., PhaseEnds - GetServerWorldTimeSeconds());
}
bool APFMatchState::Outside(const FVector &Point) const
{
    return FVector::DistSquared2D(Point, SafeCenter()) > FMath::Square(SafeRadius());
}
void APFMatchState::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(APFMatchState, Phase);
    DOREPLIFETIME(APFMatchState, TeamSize);
    DOREPLIFETIME(APFMatchState, HostPlayerId);
    DOREPLIFETIME(APFMatchState, TurnPlayerId);
    DOREPLIFETIME(APFMatchState, AlivePlayers);
    DOREPLIFETIME(APFMatchState, AliveTeams);
    DOREPLIFETIME(APFMatchState, WinningTeam);
    DOREPLIFETIME(APFMatchState, StormStage);
    DOREPLIFETIME(APFMatchState, bClosing);
    DOREPLIFETIME(APFMatchState, PhaseEnds);
    DOREPLIFETIME(APFMatchState, StormStarts);
    DOREPLIFETIME(APFMatchState, StormEnds);
    DOREPLIFETIME(APFMatchState, ArenaHalfExtent);
    DOREPLIFETIME(APFMatchState, RadiusFrom);
    DOREPLIFETIME(APFMatchState, RadiusTo);
    DOREPLIFETIME(APFMatchState, CenterFrom);
    DOREPLIFETIME(APFMatchState, CenterTo);
    DOREPLIFETIME(APFMatchState, Announcement);
    DOREPLIFETIME(APFMatchState, SupplyDropPoints);
    DOREPLIFETIME(APFMatchState, SupplyAnnouncement);
    DOREPLIFETIME(APFMatchState, SupplyAnnouncementUntil);
    DOREPLIFETIME(APFMatchState, SurveyWarningUntil);
}

void APFMatchState::NarrateSupply_Implementation(FName Cue)
{
    if(Cue==TEXT("Reset"))
    {
        for(FConstPlayerControllerIterator It=GetWorld()->GetPlayerControllerIterator();It;++It)
            if(auto *PC=Cast<APFPlayerController>(It->Get());PC&&PC->IsLocalController()&&PC->Narrator)PC->Narrator->Clear();
        return;
    }
    if(Cue!=TEXT("Inbound")&&Cue!=TEXT("Landed")&&Cue!=TEXT("Claimed"))return;
    for(FConstPlayerControllerIterator It=GetWorld()->GetPlayerControllerIterator();It;++It)
        if(auto *PC=Cast<APFPlayerController>(It->Get());PC&&PC->IsLocalController()&&PC->Narrator)PC->Narrator->Enqueue(Cue);
}
