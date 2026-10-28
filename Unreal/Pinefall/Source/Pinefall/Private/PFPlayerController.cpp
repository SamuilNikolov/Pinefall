#include "PFPlayerController.h"
#include "PFInterface.h"
#include "PFCharacter.h"
#include "Blueprint/UserWidget.h"
#include "Engine/World.h"
#include "Camera/PlayerCameraManager.h"
#include "UnrealClient.h"
#include "TimerManager.h"
APFPlayerController::APFPlayerController()
{
    PlayerCameraManagerClass = APlayerCameraManager::StaticClass();
}
void APFPlayerController::BeginPlay()
{
    Super::BeginPlay();
    if (!IsLocalController())
        return;
    Interface = CreateWidget<UPFInterface>(this, UPFInterface::StaticClass());
    if (Interface)
        Interface->AddToViewport();
    SetMenu(!GetWorld()->URL.HasOption(TEXT("play")) && !FParse::Param(FCommandLine::Get(), TEXT("PFSmoke")));
    if (FParse::Param(FCommandLine::Get(), TEXT("PFSmokeFire")))
    {
        FTimerHandle ProbeTimer;
        GetWorldTimerManager().SetTimer(
            ProbeTimer,
            [this]()
            {
                if (auto *Character = Cast<APFCharacter>(GetPawn()))
                    Character->RunFireProbe();
            },
            12.f, false);
    }
    FString CapturePath;
    if (FParse::Value(FCommandLine::Get(), TEXT("PFCapture="), CapturePath))
    {
        FTimerHandle CaptureTimer;
        GetWorldTimerManager().SetTimer(
            CaptureTimer,
            [CapturePath]() { FScreenshotRequest::RequestScreenshot(CapturePath, true, false); }, 25.f,
            false);
        if (FParse::Param(FCommandLine::Get(), TEXT("PFQuitAfterCapture")))
        {
            FTimerHandle QuitTimer;
            GetWorldTimerManager().SetTimer(
                QuitTimer, []() { FPlatformMisc::RequestExit(false); }, 30.f, false);
        }
    }
}
void APFPlayerController::SetupInputComponent()
{
    Super::SetupInputComponent();
    InputComponent->BindKey(EKeys::Escape, IE_Pressed, this, &APFPlayerController::ToggleMenu);
}
void APFPlayerController::ToggleMenu()
{
    SetMenu(!bMenuOpen);
}
void APFPlayerController::SetMenu(bool Open)
{
    bMenuOpen = Open;
    bShowMouseCursor = Open;
    UE_LOG(LogTemp, Display, TEXT("PF_MENU open=%d"), Open);
    if (auto *RangerPawn = Cast<APFCharacter>(GetPawn()))
    {
        RangerPawn->StopFire();
        RangerPawn->StopAim();
    }
    if (Open)
    {
        FInputModeGameAndUI Mode;
        Mode.SetHideCursorDuringCapture(false);
        Mode.SetLockMouseToViewportBehavior(EMouseLockMode::DoNotLock);
        SetInputMode(Mode);
    }
    else
    {
        FInputModeGameOnly Mode;
        SetInputMode(Mode);
    }
    if (Interface)
        Interface->SetMenuVisible(Open);
}
