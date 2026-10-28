using UnrealBuildTool;
public class Pinefall : ModuleRules {
 public Pinefall(ReadOnlyTargetRules Target) : base(Target) {
  PCHUsage=PCHUsageMode.UseExplicitOrSharedPCHs;
  PublicDependencyModuleNames.AddRange(new[]{"Core","CoreUObject","Engine","InputCore","EnhancedInput","UMG","Slate","SlateCore","NetCore","Sockets","Networking","Json","JsonUtilities","Niagara"});
 }
}
