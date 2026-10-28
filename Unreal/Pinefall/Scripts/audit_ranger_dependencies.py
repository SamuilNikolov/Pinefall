"""Fail if the canonical ranger still depends on the obsolete trial import."""
import unreal
library=unreal.EditorAssetLibrary
registry=unreal.AssetRegistryHelpers.get_asset_registry()
options=unreal.AssetRegistryDependencyOptions(include_hard_package_references=True,include_soft_package_references=True)
found=[]
for path in library.list_assets('/Game/Pinefall/Art/Characters/Ranger',recursive=True,include_folder=False):
    package=path.split('.')[0]
    for dependency in registry.get_dependencies(package,options):
        if str(dependency).startswith('/Game/Pinefall/Art/Characters/FieldRanger/'):
            found.append((package,str(dependency)))
if found:raise RuntimeError('Obsolete ranger dependencies: '+str(found))
unreal.log('PF_RANGER_DEPENDENCIES_OK')
