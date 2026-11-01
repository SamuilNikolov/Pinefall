"""Raster grass distance LODs. Preserve source LOD0 and all instance/collision state."""
import unreal


def configure_ground_cover_lods(mesh):
    if mesh.get_name() not in ['SM_GrassPatch', 'SM_DryGrassPatch']:
        return 0
    # Asset-only native helper works in commandlets without a mesh editor UI.
    if not unreal.PFWorldBuilder.configure_ground_cover_lods(mesh):
        raise RuntimeError('Ground cover LOD build: ' + mesh.get_name())
    for slot in mesh.get_editor_property('static_materials'):
        material = slot.material_interface
        if isinstance(material, unreal.Material):
            material.set_editor_property('dithered_lod_transition', True)
            unreal.MaterialEditingLibrary.recompile_material(material)
            unreal.EditorAssetLibrary.save_loaded_asset(material, False)
    unreal.EditorAssetLibrary.save_loaded_asset(mesh, False)
    return 3
