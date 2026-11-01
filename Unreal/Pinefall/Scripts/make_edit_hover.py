"""Build a distinct hovered edit-cell material without importing any textures."""
import unreal

lib = unreal.EditorAssetLibrary
mel = unreal.MaterialEditingLibrary
path = '/Game/Pinefall/Materials'
material = unreal.load_asset(path + '/M_BuildEditHover')
if not material:
    material = unreal.AssetToolsHelpers.get_asset_tools().create_asset(
        'M_BuildEditHover', path, unreal.Material, unreal.MaterialFactoryNew())
for _ in range(32):
    if mel.get_num_material_expressions(material) == 0:
        break
    mel.delete_all_material_expressions(material)
else:
    raise RuntimeError('Could not clear the edit-hover graph')
material.set_editor_property('two_sided', True)
material.set_editor_property('blend_mode', unreal.BlendMode.BLEND_TRANSLUCENT)
material.set_editor_property('shading_model', unreal.MaterialShadingModel.MSM_UNLIT)
color = mel.create_material_expression(material, unreal.MaterialExpressionConstant3Vector)
color.set_editor_property('constant', unreal.LinearColor(.9, .98, 1., 1))
eye = mel.create_material_expression(material, unreal.MaterialExpressionEyeAdaptation)
safe = mel.create_material_expression(material, unreal.MaterialExpressionMax)
safe.set_editor_property('const_b', .0001)
mel.connect_material_expressions(eye, '', safe, 'A')
emission = mel.create_material_expression(material, unreal.MaterialExpressionDivide)
mel.connect_material_expressions(color, '', emission, 'A')
mel.connect_material_expressions(safe, '', emission, 'B')
mel.connect_material_property(emission, '', unreal.MaterialProperty.MP_EMISSIVE_COLOR)
opacity = mel.create_material_expression(material, unreal.MaterialExpressionConstant)
opacity.set_editor_property('r', .72)
mel.connect_material_property(opacity, '', unreal.MaterialProperty.MP_OPACITY)
mel.recompile_material(material)
lib.save_loaded_asset(material)
unreal.PFWorldBuilder.finish_rendering_preparation()
unreal.log('PF_EDIT_HOVER_OK textures_added=0')
