using UnrealBuildTool;
public class PinefallTarget : TargetRules {
 public PinefallTarget(TargetInfo Target) : base(Target) {
  Type=TargetType.Game;DefaultBuildSettings=BuildSettingsVersion.V5;IncludeOrderVersion=EngineIncludeOrderVersion.Unreal5_6;ExtraModuleNames.Add("Pinefall");
 }
}
