"""Material helpers for the reserve builder. No runtime/editor dependency."""

from pathlib import Path
import unreal


def build_terrain(project, node, wire, output, material, texture, rock=False):
    m = material("M_ReserveGeology" if rock else "M_ReserveTerrain", "/Game/Pinefall/Materials")
    m.set_editor_property('tangent_space_normal', False)
    roles = ["Forest", "Meadow", "Sand", "Snow", "Rock", "Mud", "DesertRock"]
    inputs = []
    for name in ["P", "N", "RockMode"] + [
        role + kind
        for role in roles
        for kind in ["C", "S"]
    ]:
        i = unreal.CustomInput()
        i.set_editor_property("input_name", name)
        inputs.append(i)
    extra = []
    for name, kind in [
        ("OutNormal", unreal.CustomMaterialOutputType.CMOT_FLOAT3),
        ("OutRoughness", unreal.CustomMaterialOutputType.CMOT_FLOAT1),
        ("OutAO", unreal.CustomMaterialOutputType.CMOT_FLOAT1),
    ]:
        i = unreal.CustomOutput()
        i.set_editor_property("output_name", name)
        i.set_editor_property("output_type", kind)
        extra.append(i)
    blend = node(
        m,
        unreal.MaterialExpressionCustom,
        code=(project / "Scripts/biome_ground_pbr.hlsl").read_text(
            encoding="utf-8-sig"
        ),
        output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,
        inputs=inputs,
        additional_outputs=extra,
    )
    wire(node(m, unreal.MaterialExpressionWorldPosition), "", blend, "P")
    wire(node(m, unreal.MaterialExpressionVertexNormalWS), "", blend, "N")
    wire(node(m, unreal.MaterialExpressionConstant, r=1.0 if rock else 0.0), "", blend, "RockMode")
    for role in roles:
        for kind, file in [("C", role + "_Color.png"), ("S", role + "_Surface.png")]:
            t = texture(
                project / "SourceArt/Prepared/BiomeSurfaces" / file,
                "T_BiomeScan_" + role + kind,
                linear=kind == "S",
            )
            t.set_editor_property('max_texture_size', 4096 if kind == 'C' else 2048)
            t.set_editor_property('compression_settings', unreal.TextureCompressionSettings.TC_BC7)
            t.set_editor_property('compression_no_alpha', False)
            unreal.EditorAssetLibrary.save_loaded_asset(t, False)
            obj = node(
                m,
                unreal.MaterialExpressionTextureObject,
                texture=t,
                sampler_type=(
                    unreal.MaterialSamplerType.SAMPLERTYPE_LINEAR_COLOR
                    if kind == "S"
                    else unreal.MaterialSamplerType.SAMPLERTYPE_COLOR
                ),
            )
            wire(obj, "", blend, role + kind)
    output(blend, "", m, unreal.MaterialProperty.MP_BASE_COLOR)
    output(blend, "OutNormal", m, unreal.MaterialProperty.MP_NORMAL)
    output(blend, "OutRoughness", m, unreal.MaterialProperty.MP_ROUGHNESS)
    output(blend, "OutAO", m, unreal.MaterialProperty.MP_AMBIENT_OCCLUSION)
    if rock:
        unreal.MaterialEditingLibrary.set_material_usage(m, unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES)
    unreal.MaterialEditingLibrary.set_material_usage(m, unreal.MaterialUsage.MATUSAGE_NANITE)
    unreal.MaterialEditingLibrary.recompile_material(m)
    unreal.EditorAssetLibrary.save_loaded_asset(m, False)
    return m
