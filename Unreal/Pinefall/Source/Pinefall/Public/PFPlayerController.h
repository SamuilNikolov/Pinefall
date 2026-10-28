#pragma once
#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "PFPlayerController.generated.h"
class UPFInterface;
class APFCharacter;
UCLASS()
class PINEFALL_API APFPlayerController : public APlayerController
{
    GENERATED_BODY()
  public:
    APFPlayerController();
    virtual void BeginPlay() override;
    virtual void SetupInputComponent() override;
    virtual void PlayerTick(float Delta) override;
    virtual void CalcCamera(float Delta, FMinimalViewInfo &Out) override;
    void ToggleMenu();
    void SetMenu(bool bOpen);
    UPROPERTY(BlueprintReadOnly) bool bMenuOpen = false;
    UPROPERTY(BlueprintReadOnly) bool bInventoryOpen = false;
    UPROPERTY(BlueprintReadOnly) bool bMapOpen = false;
    UPROPERTY() TObjectPtr<UPFInterface> Interface;
    UFUNCTION(Server, Reliable) void ServerReady();
    UFUNCTION(Server, Reliable) void ServerTeamSize(int32 Size);
    UFUNCTION(Server, Reliable) void ServerTeam(int32 Team);
    UFUNCTION(Server, Reliable) void ServerStartRound();
    UFUNCTION(Server, Reliable) void ServerBid(int32 Bid, int32 Kit);
    UFUNCTION(Server, Reliable) void ServerLanding(FVector_NetQuantize Point, bool Confirm);
    UFUNCTION(Server, Reliable) void ServerReturnLobby();
    UFUNCTION(Server, Reliable) void ServerCycleSpectator();
    UFUNCTION(Client, Reliable) void ClientSpectate(APFCharacter *Target);
    UFUNCTION(Client, Reliable) void ClientResumeRound();
    UPROPERTY() TWeakObjectPtr<APFCharacter> SpectateTarget;
    bool RoundPanelOpen() const;
    void InventoryPressed();
    void InventoryReleased();
    void ToggleMap();
    void InteractPressed();
    void InteractReleased();
    void RefreshInputMode();
    double ShotViewTime(float SmoothingSeconds) const;
    UFUNCTION(Server, Unreliable) void ServerClock(double Sent);
    UFUNCTION(Client, Unreliable) void ClientClock(double Sent, double ServerTime);

  private:
    bool bLastRoundPanel = false;
    float SpectatePoll = 0;
    void ProbeRoundInput();
    bool bRoundProbe = false, bProbeReadySent = false;
    int32 ProbePhase = -1;
    float LandingRetry = 0;
    bool bCaptureInventory = false;
    bool bHasClock = false;
    double ClockOffset = 0, NextClockRequest = 0, NextClockReply = 0;
    float ClockRoundTrip = 0;
};
