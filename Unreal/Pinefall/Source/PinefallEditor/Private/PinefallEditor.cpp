#include "Modules/ModuleManager.h"
class FPinefallEditorModule : public IModuleInterface
{
  public:
    virtual void StartupModule() override
    {
        // Commandlet imports can build Bink data before an audio device exists.
        // Register the installed decoder explicitly; this does not open a device.
        if (IsRunningCommandlet())
            FModuleManager::LoadModuleChecked<IModuleInterface>(TEXT("BinkAudioDecoder"));
    }
};
IMPLEMENT_MODULE(FPinefallEditorModule, PinefallEditor);
