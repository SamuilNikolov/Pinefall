#include "PFPlayerController.h"
#include "GameFramework/PlayerInput.h"
#include "Framework/Application/SlateApplication.h"
#include "Net/UnrealNetwork.h"
#include "PFInterface.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFMatchState.h"
#include "PFStormVisual.h"
#include "Blueprint/UserWidget.h"
#include "Engine/World.h"
#include "Camera/PlayerCameraManager.h"
#include "UnrealClient.h"
#include "TimerManager.h"
APFPlayerController::APFPlayerController()
{
    PlayerCameraManagerClass = APlayerCameraManager::StaticClass();
}
double APFPlayerController::ShotViewTime(float SmoothingSeconds) const
{
    double Now = GetWorld()->GetTimeSeconds();
    if (HasAuthority())
        return Now;
    if (bHasClock)
        return Now + ClockOffset - ClockRoundTrip * .5 - SmoothingSeconds;
    auto *Round = GetWorld()->GetGameState<APFMatchState>();
    return (Round ? Round->GetServerWorldTimeSeconds() : Now) - SmoothingSeconds;
}
void APFPlayerController::ServerClock_Implementation(double Sent)
{
    double Now = GetWorld()->GetTimeSeconds();
    if (!FMath::IsFinite(Sent) || Now < NextClockReply)
        return;
    NextClockReply = Now + .4;
    ClientClock(Sent, Now);
}
void APFPlayerController::ClientClock_Implementation(double Sent, double ServerTime)
{
    double Now = GetWorld()->GetTimeSeconds(), RoundTrip = Now - Sent;
    if (!FMath::IsFinite(ServerTime) || RoundTrip < 0 || RoundTrip > 1)
        return;
    double Offset = ServerTime + RoundTrip * .5 - Now;
    ClockOffset =
        !bHasClock || FMath::Abs(Offset - ClockOffset) > .15 ? Offset : FMath::Lerp(ClockOffset, Offset, .25);
    ClockRoundTrip = bHasClock ? FMath::Lerp(ClockRoundTrip, float(RoundTrip), .25f) : RoundTrip;
    bHasClock = true;
    if (bRoundProbe || FParse::Param(FCommandLine::Get(), TEXT("PFCombatProbe")))
        UE_LOG(LogTemp, Display, TEXT("PF_CLOCK rtt=%.3f offset=%.3f"), ClockRoundTrip, ClockOffset);
}
void APFPlayerController::BeginPlay()
{
    Super::BeginPlay();
    if(IsLocalController()&&FSlateApplication::IsInitialized())ActivationHandle=FSlateApplication::Get().OnApplicationActivationStateChanged().AddUObject(this,&APFPlayerController::ApplicationActivation);
    if (!IsLocalController())
        return;
    if (FParse::Param(FCommandLine::Get(),TEXT("PFMovementProbe"))) InitializeMovementProbeGeometry();
    bRoundProbe = FParse::Param(FCommandLine::Get(), TEXT("PFRoundProbe"));
    Interface = CreateWidget<UPFInterface>(this, UPFInterface::StaticClass());
    if (Interface)
        Interface->AddToViewport();
    if (GetNetMode() != NM_DedicatedServer)
        GetWorld()->SpawnActor<APFStormVisual>();
    SetMenu(!GetWorld()->URL.HasOption(TEXT("play")) && !FParse::Param(FCommandLine::Get(), TEXT("PFSmoke")));
    if(FParse::Param(FCommandLine::Get(),TEXT("PFInventoryProbe")))GetWorldTimerManager().SetTimer(InventoryProbeTimer,this,&APFPlayerController::RunInventoryAccessProbe,.1,true);
    if (FParse::Value(FCommandLine::Get(), TEXT("PFRenderReport="), RenderReportPath))
    { RenderFrameTimes.Reserve(4096); RenderGPUTimes.Reserve(4096);
        int32 Peers = 0; FParse::Value(FCommandLine::Get(),TEXT("PFRenderPeers="),Peers);
        RenderExpectedPlayers = FMath::Clamp(Peers+1,1,20); }
    FString CombatView;
    if (FParse::Value(FCommandLine::Get(), TEXT("PFCombatView="), CombatView))
    {
        FTimerHandle ViewTimer;
        TWeakObjectPtr<APFPlayerController> Weak = this;
        GetWorldTimerManager().SetTimer(
            ViewTimer,
            [Weak]()
            {
                if (auto *PC = Weak.Get())
                    if (auto *Character = Cast<APFCharacter>(PC->GetPawn()))
                        Character->RunCombatViewProbe();
            },
            6.f, false);
    }
    if (FParse::Param(FCommandLine::Get(), TEXT("PFSmokeFire")))
    {
        FTimerHandle ProbeTimer;
        GetWorldTimerManager().SetTimer(
            ProbeTimer,
            [this]()
            {
                if (auto *RangerPawn = Cast<APFCharacter>(GetPawn()))
                    RangerPawn->RunFireProbe();
            },
            12.f, false);
    }
    if (FParse::Value(FCommandLine::Get(), TEXT("PFCapture="), CaptureOutputPath))
    {
        bCaptureInventory = FParse::Param(FCommandLine::Get(), TEXT("PFCaptureInventory"));
        if (bCaptureInventory)
        {
            FTimerHandle OpenTimer;
            GetWorldTimerManager().SetTimer(
                OpenTimer,
                [this]()
                {
                    SetMenu(false);
                    InventoryPressed();
                },
                6.f, false);
        }
        bQuitAfterCapture = FParse::Param(FCommandLine::Get(),TEXT("PFQuitAfterCapture"));
        GetWorldTimerManager().SetTimer(CapturePoll,this,&APFPlayerController::CaptureWhenReady,.5f,true);

    }
}
void APFPlayerController::ApplicationActivation(bool Active)
{
    if(!Active&&bInventoryOpen&&!bCaptureInventory){bInventorySticky=false;InventoryReleased();}
}
void APFPlayerController::EndPlay(EEndPlayReason::Type Reason)
{
    if(FSlateApplication::IsInitialized()&&ActivationHandle.IsValid())FSlateApplication::Get().OnApplicationActivationStateChanged().Remove(ActivationHandle);
    Super::EndPlay(Reason);
}
void APFPlayerController::SetupInputComponent()
{
    Super::SetupInputComponent();
    if(PlayerInput)PlayerInput->DebugExecBindings.RemoveAll([](const FKeyBind &Binding){return Binding.Key==EKeys::F1||Binding.Key==EKeys::F2||Binding.Key==EKeys::F3||Binding.Key==EKeys::F4;});
    InputComponent->BindKey(EKeys::Escape, IE_Pressed, this, &APFPlayerController::ToggleMenu);
    InputComponent->BindKey(EKeys::LeftAlt, IE_Pressed, this, &APFPlayerController::InventoryPressed);
    InputComponent->BindKey(EKeys::LeftAlt, IE_Released, this, &APFPlayerController::AltInventoryReleased);
    InputComponent->BindKey(EKeys::RightAlt, IE_Pressed, this, &APFPlayerController::InventoryPressed);
    InputComponent->BindKey(EKeys::RightAlt, IE_Released, this, &APFPlayerController::AltInventoryReleased);
    InputComponent->BindKey(EKeys::Tab, IE_Pressed, this, &APFPlayerController::ToggleInventory);
    InputComponent->BindKey(EKeys::M, IE_Pressed, this, &APFPlayerController::ToggleMap);
    InputComponent->BindKey(EKeys::E, IE_Pressed, this, &APFPlayerController::InteractPressed);
    InputComponent->BindKey(EKeys::E, IE_Released, this, &APFPlayerController::InteractReleased);
    InputComponent->BindKey(EKeys::RightBracket, IE_Pressed, this,
                            &APFPlayerController::ServerCycleSpectator);
}
void APFPlayerController::ToggleMenu()
{
    if (bMapOpen)
    {
        ToggleMap();
        return;
    }
    if (bInventoryOpen)
    {
        InventoryReleased();
        return;
    }
    SetMenu(!bMenuOpen);
}
void APFPlayerController::SetMenu(bool Open)
{
    bMenuOpen = Open;
    if(Open){bInventoryOpen=false;bInventorySticky=false;bMapOpen=false;}
    UE_LOG(LogTemp, Display, TEXT("PF_MENU open=%d"), Open);
    if (auto *RangerPawn = Cast<APFCharacter>(GetPawn()))
    {
        RangerPawn->ClearOverlayInput();
    }
    RefreshInputMode();
    if (Interface)
        Interface->SetMenuVisible(Open);
}
void APFPlayerController::RefreshInputMode()
{
    bool Open = bMenuOpen || bInventoryOpen || bMapOpen || RoundPanelOpen();
    bShowMouseCursor = Open;
    if (Open)
    {
        FInputModeGameAndUI Mode;
        if(Interface)Mode.SetWidgetToFocus(Interface->TakeWidget());
        Mode.SetHideCursorDuringCapture(false);
        Mode.SetLockMouseToViewportBehavior(EMouseLockMode::DoNotLock);
        SetInputMode(Mode);
    }
    else
    {
        FInputModeGameOnly Mode;
        SetInputMode(Mode);
    }
}
bool APFPlayerController::RoundPanelOpen() const
{
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    return Match && Match->IsBattleRoyale() && Match->Phase != EPFRoundPhase::Active;
}
void APFPlayerController::AltInventoryReleased(){if(!bInventorySticky)InventoryReleased();}
void APFPlayerController::ToggleInventory()
{
    if(bInventoryOpen){bInventorySticky=false;InventoryReleased();return;}
    InventoryPressed();bInventorySticky=bInventoryOpen;
}
void APFPlayerController::InventoryPressed()
{
    bInventorySticky=false;
    auto *InventoryOwner=Cast<APFCharacter>(GetPawn());
    if (bMenuOpen || RoundPanelOpen() || !InventoryOwner || InventoryOwner->Health<=0 || InventoryOwner->bDowned)
        return;
    bInventoryOpen = true;
    if(Interface)Interface->RefreshInventory();
    bMapOpen = false;
    if (auto *RangerPawn = Cast<APFCharacter>(GetPawn()))
    {RangerPawn->ClearOverlayInput();}
    RefreshInputMode();
}
void APFPlayerController::InventoryReleased()
{
    if (!bInventoryOpen)
        return;
    bInventoryOpen = false;bInventorySticky=false;
    RefreshInputMode();
}
void APFPlayerController::ToggleMap()
{
    bMapOpen = !bMapOpen;
    bInventoryOpen = false;
    if (auto *RangerPawn = Cast<APFCharacter>(GetPawn()))
        RangerPawn->StopFire();
    RefreshInputMode();
}
void APFPlayerController::InteractPressed()
{
    if(bMenuOpen||bInventoryOpen||bMapOpen||RoundPanelOpen())return;
    if (auto *RangerPawn = Cast<APFCharacter>(GetPawn()))
        RangerPawn->BeginInteract();
}
void APFPlayerController::InteractReleased()
{
    if (auto *RangerPawn = Cast<APFCharacter>(GetPawn()))
        RangerPawn->EndInteract();
}
void APFPlayerController::PlayerTick(float Delta)
{
    Super::PlayerTick(Delta);
    if (IsLocalController() && !HasAuthority() && GetWorld()->GetTimeSeconds() >= NextClockRequest)
    {
        NextClockRequest = GetWorld()->GetTimeSeconds() + (bHasClock ? 2 : .5);
        ServerClock(GetWorld()->GetTimeSeconds());
    }
    if (!IsLocalController())
        return;
    if (!RenderReportPath.IsEmpty()) RecordRenderFrame();
    if (bRoundProbe)
        ProbeRoundInput();
    bool Round = RoundPanelOpen();
    if (Round != bLastRoundPanel)
    {
        bLastRoundPanel = Round;
        RefreshInputMode();
    }
    if (auto *State = GetPlayerState<APFPlayerState>())
        if (State->bEliminated && (!SpectateTarget.IsValid() || SpectateTarget->Health <= 0))
        {
            static constexpr float Interval = 1;
            SpectatePoll -= Delta;
            if (SpectatePoll <= 0)
            {
                SpectatePoll = Interval;
                ServerCycleSpectator();
            }
        }
}
void APFPlayerController::ServerReady_Implementation()
{
    if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
        Mode->SetReady(this);
}
void APFPlayerController::ServerTeamSize_Implementation(int32 Size)
{
    if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
        Mode->SetTeamSize(this, Size);
}
void APFPlayerController::ServerTeam_Implementation(int32 Team)
{
    if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
        Mode->ChooseTeam(this, Team);
}
void APFPlayerController::ServerStartRound_Implementation()
{
    if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
        Mode->RequestStart(this);
}
void APFPlayerController::ServerBid_Implementation(int32 Bid, int32 Kit)
{
    if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
        Mode->SubmitBid(this, Bid, Kit);
}
void APFPlayerController::ServerLanding_Implementation(FVector_NetQuantize Point, bool Confirm)
{
    if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
        Mode->SelectLanding(this, Point, Confirm);
}
void APFPlayerController::ServerReturnLobby_Implementation()
{
    if (auto *Mode = GetWorld()->GetAuthGameMode<APFGameMode>())
        Mode->ReturnToLobby(this);
}
void APFPlayerController::ClientResumeRound_Implementation()
{
    SpectateTarget.Reset();
    bAutoManageActiveCameraTarget = true;
    if (GetPawn())
        SetViewTarget(GetPawn());
    bMapOpen = false;
    bInventoryOpen = false;
    SetMenu(false);
}
void APFPlayerController::ServerCycleSpectator_Implementation()
{
    auto *Mine = GetPlayerState<APFPlayerState>();
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    if (!Mine || !Match || !Mine->bEliminated)
        return;
    TArray<APFCharacter *> Allies, Everyone;
    for (APlayerState *Entry : Match->PlayerArray)
        if (auto *State = Cast<APFPlayerState>(Entry))
            if (State != Mine && State->bInRound && !State->bEliminated)
            {
                auto *RangerPawn = Cast<APFCharacter>(State->GetPawn());
                if (RangerPawn && RangerPawn->Health > 0)
                {
                    Everyone.Add(RangerPawn);
                    if (State->TeamId == Mine->TeamId)
                        Allies.Add(RangerPawn);
                }
            }
    const auto &Choices = Match->TeamSize > 1 && !Allies.IsEmpty() ? Allies : Everyone;
    int32 Index = Choices.IndexOfByKey(SpectateTarget.Get());
    APFCharacter *Next = Choices.IsEmpty() ? nullptr : Choices[(Index + 1) % Choices.Num()];
    SpectateTarget = Next;
    bAutoManageActiveCameraTarget = false;
    SetViewTarget(this);
    ClientSpectate(Next);
}
void APFPlayerController::ClientSpectate_Implementation(APFCharacter *Target)
{
    SpectateTarget = Target;
    bAutoManageActiveCameraTarget = false;
    SetViewTarget(this);
    bInventoryOpen = false;
    RefreshInputMode();
}
void APFPlayerController::CalcCamera(float Delta, FMinimalViewInfo &Out)
{
    if (auto *Target = SpectateTarget.Get())
    {
        FVector Focus = Target->GetActorLocation() + FVector(0, 0, 70);
        FVector Desired = Focus - Target->GetActorForwardVector() * 320 + FVector(0, 0, 130);
        FHitResult Hit;
        FCollisionQueryParams Query(SCENE_QUERY_STAT(PFSpectatorCamera), false, Target);
        if (GetWorld()->LineTraceSingleByChannel(Hit, Focus, Desired, ECC_Visibility, Query))
            Desired = Hit.ImpactPoint + Hit.ImpactNormal * 12;
        Out.Location = Desired;
        Out.Rotation = (Focus - Desired).Rotation();
        Out.FOV = 85;
        return;
    }
    Super::CalcCamera(Delta, Out);
}

void APFPlayerController::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME_CONDITION(APFPlayerController, MapMarkers, COND_OwnerOnly);
}
