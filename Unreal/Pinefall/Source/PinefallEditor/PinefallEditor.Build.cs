using UnrealBuildTool;
public class PinefallEditor : ModuleRules {
 public PinefallEditor(ReadOnlyTargetRules Target) : base(Target) {
  PCHUsage=PCHUsageMode.UseExplicitOrSharedPCHs;
  PrivateDependencyModuleNames.AddRange(new[]{"Core","CoreUObject","Engine","Pinefall","UnrealEd","Landscape","AssetTools","AssetRegistry","SlateCore","EditorScriptingUtilities","Foliage"});
  DynamicallyLoadedModuleNames.Add("BinkAudioDecoder");
 }
}
