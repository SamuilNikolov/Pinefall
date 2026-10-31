#include "PFPlayerController.h"
#include "UnrealClient.h"
#include "TimerManager.h"
#if WITH_EDITOR
#include "ShaderCompiler.h"
#include "AssetCompilingManager.h"
#endif
#include "PFMatchState.h"
#include "PFWildlife.h"
#include "EngineUtils.h"
#include "PFUserSettings.h"
#include "DynamicRHI.h"
#include "RenderTimer.h"
#include "HAL/PlatformMemory.h"
#include "Engine/GameViewportClient.h"
#include "Engine/Engine.h"
#include "Dom/JsonObject.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"
#include "Misc/App.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "HAL/IConsoleManager.h"

namespace
{
bool PresentationAssetsPending()
{
#if WITH_EDITOR
    return (GShaderCompilingManager && GShaderCompilingManager->IsCompiling()) ||
           FAssetCompilingManager::Get().GetNumRemainingAssets() > 0;
#else
    return false;
#endif
}
} // namespace
void APFPlayerController::CaptureWhenReady()
{
    const float Now=GetWorld()->GetTimeSeconds();
    if(CaptureOutputPath.IsEmpty())return;
    if(bCapturePending)
    {
        if(IFileManager::Get().FileSize(*CaptureOutputPath)>0)
        {
            UE_LOG(LogTemp,Display,TEXT("PF_CAPTURE_SAVED %s"),*CaptureOutputPath);CaptureOutputPath.Empty();GetWorldTimerManager().ClearTimer(CapturePoll);
            if(bQuitAfterCapture){FTimerHandle Quit;GetWorldTimerManager().SetTimer(Quit,[]{FPlatformMisc::RequestExit(false);},1,false);}return;
        }
        if(Now-CaptureSentAt<4)return;
        if(CaptureAttempts>=3){UE_LOG(LogTemp,Error,TEXT("PF_CAPTURE_FAILED screenshot was not saved"));GetWorldTimerManager().ClearTimer(CapturePoll);if(bQuitAfterCapture)FPlatformMisc::RequestExitWithStatus(false,1);return;}
        CaptureAttempts++;CaptureSentAt=Now;FScreenshotRequest::RequestScreenshot(CaptureOutputPath,true,false);return;
    }
    if(Now<12)return;
    if(PresentationAssetsPending()||!RenderReportPath.IsEmpty()){CaptureReadyAt=Now+3;return;}
    if(Now<CaptureReadyAt)return;
    bCapturePending=true;CaptureSentAt=Now;CaptureAttempts=1;FScreenshotRequest::RequestScreenshot(CaptureOutputPath,true,false);
}
void APFPlayerController::RecordRenderFrame()
{
    const float Now = GetWorld()->GetTimeSeconds();
    auto *State = GetWorld()->GetGameState<APFMatchState>();
    int32 Players = State ? State->PlayerArray.Num() : 0;
    if(!RenderDirector.IsValid())for(TActorIterator<APFWildlifeDirector> It(GetWorld());It;++It){RenderDirector=*It;break;}
    const int32 Population=RenderDirector.IsValid()?RenderDirector->Population.Num():0;
    const int32 PopulationTarget=State&&State->ArenaHalfExtent>50000?84:28;
    if (PresentationAssetsPending() || Players < RenderExpectedPlayers || Population<PopulationTarget)
    {
        RenderReadyAt = Now + 8;
        RenderStartedAt = 0;
        RenderMinimumPlayers = MAX_int32;
        RenderFrameTimes.Reset();
        RenderGPUTimes.Reset();RenderGameTimes.Reset();RenderDrawTimes.Reset();RenderRHITimes.Reset();RenderMinimumFreeMemory=MAX_uint64;
        return;
    }
    if (Now < FMath::Max(28.f, RenderReadyAt))
        return;
    if (RenderStartedAt <= 0)
        RenderStartedAt = Now;
    if (Now - RenderStartedAt < 22 && RenderFrameTimes.Num() < 8192)
    {
        RenderMinimumPlayers = FMath::Min(RenderMinimumPlayers, Players);
        RenderFrameTimes.Add(FApp::GetDeltaTime() * 1000);
        RenderGameTimes.Add(FPlatformTime::ToMilliseconds(GGameThreadTime));
        RenderDrawTimes.Add(FPlatformTime::ToMilliseconds(GRenderThreadTime));
        RenderRHITimes.Add(FPlatformTime::ToMilliseconds(GRHIThreadTime));
        RenderMinimumFreeMemory=FMath::Min(RenderMinimumFreeMemory,FPlatformMemory::GetStats().AvailablePhysical);
        const float GPU = FPlatformTime::ToMilliseconds(RHIGetGPUFrameCycles());
        if (GPU > 0)
            RenderGPUTimes.Add(GPU);
        return;
    }
    auto Report = MakeShared<FJsonObject>();
    FIntPoint Resolution=FIntPoint::ZeroValue;
    if (GEngine && GEngine->GameViewport && GEngine->GameViewport->Viewport)
        Resolution = GEngine->GameViewport->Viewport->GetSizeXY();
    const bool Rendered = FApp::CanEverRender() && GDynamicRHI &&
                          !FParse::Param(FCommandLine::Get(), TEXT("nullrhi")) && Resolution.X > 0;
    Report->SetBoolField(TEXT("rendered"), Rendered);
    Report->SetNumberField(TEXT("wildlife_population"),Population);
    Report->SetStringField(TEXT("timestamp_utc"), FDateTime::UtcNow().ToIso8601());
    Report->SetStringField(TEXT("cpu"), FPlatformMisc::GetCPUBrand());
    Report->SetStringField(TEXT("gpu"), GRHIAdapterName);
    Report->SetStringField(TEXT("rhi"), GDynamicRHI ? GDynamicRHI->GetName() : TEXT("None"));
    Report->SetNumberField(TEXT("width"), Resolution.X);
    Report->SetNumberField(TEXT("height"), Resolution.Y);
    Report->SetStringField(TEXT("map"), GetWorld()->GetMapName());
    FString View;
    FParse::Value(FCommandLine::Get(), TEXT("PFCombatView="), View);
    Report->SetStringField(TEXT("view"), View);
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    Report->SetNumberField(TEXT("players"), Match ? Match->PlayerArray.Num() : 0);
    Report->SetNumberField(TEXT("frames"), RenderFrameTimes.Num());
    Report->SetNumberField(TEXT("measurement_seconds"), Now - RenderStartedAt);
    Report->SetNumberField(TEXT("minimum_players"), RenderMinimumPlayers);
    auto Stats = [&](const TCHAR *Prefix, TArray<float> &Values) {
        if (Values.IsEmpty())
            return;
        double Sum = 0;
        int32 Slow = 0;
        TArray<TSharedPtr<FJsonValue>> Raw;
        Raw.Reserve(Values.Num());
        for (float MS : Values)
        {
            Sum += MS;
            Slow += MS > 33.333;
            Raw.Add(MakeShared<FJsonValueNumber>(MS));
        }
        Values.Sort();
        FString P(Prefix);
        Report->SetNumberField(P + TEXT("_avg_ms"), Sum / Values.Num());
        Report->SetNumberField(P + TEXT("_p50_ms"), Values[FMath::FloorToInt((Values.Num() - 1) * .5)]);
        Report->SetNumberField(P + TEXT("_p95_ms"), Values[FMath::FloorToInt((Values.Num() - 1) * .95)]);
        Report->SetNumberField(P + TEXT("_p99_ms"), Values[FMath::FloorToInt((Values.Num() - 1) * .99)]);
        Report->SetNumberField(P + TEXT("_over_33ms_percent"), Slow * 100. / Values.Num());
        Report->SetArrayField(P + TEXT("_samples_ms"), Raw);
    };
    Stats(TEXT("frame"), RenderFrameTimes);
    Stats(TEXT("gpu"), RenderGPUTimes);
    Stats(TEXT("game_thread"), RenderGameTimes);Stats(TEXT("render_thread"), RenderDrawTimes);Stats(TEXT("rhi_thread"), RenderRHITimes);
    Report->SetNumberField(TEXT("minimum_free_physical_gb"),double(RenderMinimumFreeMemory)/(1024.*1024.*1024.));
    Report->SetNumberField(TEXT("process_physical_gb"),double(FPlatformMemory::GetStats().UsedPhysical)/(1024.*1024.*1024.));
    for (const TCHAR *Name : {TEXT("r.Nanite"), TEXT("r.ScreenPercentage"), TEXT("r.ViewDistanceScale"),
                              TEXT("foliage.DensityScale"), TEXT("t.MaxFPS"), TEXT("r.VSync"),
                              TEXT("landscape.RenderNanite"), TEXT("sg.GlobalIlluminationQuality"),
                              TEXT("sg.ReflectionQuality")})
        if (auto *Variable = IConsoleManager::Get().FindConsoleVariable(Name))
            Report->SetNumberField(Name, Variable->GetFloat());
    if (auto *Settings = UPFUserSettings::Get())
        Report->SetNumberField(TEXT("scalability"), Settings->GetOverallScalabilityLevel());
    FString Json;
    auto Writer = TJsonWriterFactory<>::Create(&Json);
    FJsonSerializer::Serialize(Report, Writer);
    bool Saved = FFileHelper::SaveStringToFile(Json, *RenderReportPath);
    UE_LOG(LogTemp, Display, TEXT("PF_RENDER_REPORT rendered=%d saved=%d frames=%d peers=%d"), Rendered,
           Saved, RenderFrameTimes.Num(), Match ? Match->PlayerArray.Num() - 1 : 0);
    if (!bRenderComparisonDone && FParse::Param(FCommandLine::Get(), TEXT("PFRenderCompareLighting")))
    {
        bRenderComparisonDone = true;
        auto *Settings = UPFUserSettings::Get();
        if (Settings)
        {
            bRenderOriginalBalanced = Settings->bBalancedLighting;
            RenderOriginalGI = Settings->GetGlobalIlluminationQuality();
            RenderOriginalReflections = Settings->GetReflectionQuality();
            Settings->bBalancedLighting = true;
            // Exercise the actual profile application, without saving test settings.
            Settings->ApplyNonResolutionSettings();
            if (auto *Cap = IConsoleManager::Get().FindConsoleVariable(TEXT("t.MaxFPS")))
                Cap->Set(0.f, ECVF_SetByConsole);
        }
        RenderReportPath = FPaths::GetPath(RenderReportPath) / TEXT("render-balanced.json");
        RenderStartedAt = 0;
        RenderReadyAt = Now + 12;
        RenderMinimumPlayers = MAX_int32;
        RenderMinimumFreeMemory = MAX_uint64;
        RenderFrameTimes.Reset();RenderGPUTimes.Reset();RenderGameTimes.Reset();RenderDrawTimes.Reset();RenderRHITimes.Reset();
        return;
    }
    if (bRenderComparisonDone)
        if (auto *Settings = UPFUserSettings::Get())
        {
            // Restore saved-settings state without disturbing the final image.
            // No benchmark profile is persisted when the process exits.
            Settings->bBalancedLighting = bRenderOriginalBalanced;
            Settings->SetGlobalIlluminationQuality(RenderOriginalGI);
            Settings->SetReflectionQuality(RenderOriginalReflections);
        }
    RenderReportPath.Empty();
    RenderFrameTimes.Empty();
    RenderGPUTimes.Empty();RenderGameTimes.Empty();RenderDrawTimes.Empty();RenderRHITimes.Empty();
}
