#pragma once
#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "PFPlayerController.generated.h"
USTRUCT()
struct FPFMapMarker
{
    GENERATED_BODY()
    UPROPERTY() FVector_NetQuantize Position = FVector::ZeroVector;
    UPROPERTY() uint8 Kind = 0;
    UPROPERTY() float Condition = 0;
    UPROPERTY() float Armor = 0;
    UPROPERTY() float ArmorMax = 100;
    UPROPERTY() bool bDowned = false;
    UPROPERTY() int32 PlayerId = -1;
    UPROPERTY() FString Label;
};
class UPFInterface;
class APFCharacter;
UCLASS()
class PINEFALL_API APFPlayerController : public APlayerController
{
    GENERATED_BODY()
  public:
    APFPlayerController();
    UPROPERTY(Replicated) TArray<FPFMapMarker> MapMarkers;
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &Out) const override;
    virtual void BeginPlay() override;
    virtual void EndPlay(EEndPlayReason::Type Reason) override;
    void InitializeMovementProbeGeometry();
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
    void AltInventoryReleased();
    void ToggleInventory();
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
    FDelegateHandle ActivationHandle;
    bool bInventorySticky=false;
    void ApplicationActivation(bool Active);
    void RunInventoryAccessProbe();
    FTimerHandle InventoryProbeTimer;
    int32 InventoryProbeStep=0;
    float InventoryProbeAfter=0;
    TArray<FString> InventoryProbePassed,InventoryProbeFailed;
    void RecordRenderFrame();
    void CaptureWhenReady();
    FTimerHandle CapturePoll;
    FString CaptureOutputPath;
    bool bCapturePending=false;
    int32 CaptureAttempts=0;
    float CaptureSentAt=0;
    float CaptureReadyAt = 0, RenderReadyAt = 0, RenderStartedAt = 0;
    bool bQuitAfterCapture = false;
    int32 RenderExpectedPlayers = 1, RenderMinimumPlayers = MAX_int32;
    FString RenderReportPath;
    TWeakObjectPtr<class APFWildlifeDirector> RenderDirector;
    TArray<float> RenderFrameTimes, RenderGPUTimes, RenderGameTimes, RenderDrawTimes, RenderRHITimes;
    uint64 RenderMinimumFreeMemory = MAX_uint64;
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
