using UnrealBuildTool;
public class PinefallEditorTarget : TargetRules {
 public PinefallEditorTarget(TargetInfo Target) : base(Target) {
  Type=TargetType.Editor;DefaultBuildSettings=BuildSettingsVersion.V5;IncludeOrderVersion=EngineIncludeOrderVersion.Unreal5_6;ExtraModuleNames.AddRange(new[]{"Pinefall","PinefallEditor"});
 }
}
