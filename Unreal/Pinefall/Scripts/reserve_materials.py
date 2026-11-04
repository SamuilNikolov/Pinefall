"""Material helpers for the reserve builder. No runtime/editor dependency."""

from pathlib import Path
import unreal


def build_track():
    """Reuse the existing mud scan with continuous world UVs across road pieces."""
    lib = unreal.EditorAssetLibrary
    mel = unreal.MaterialEditingLibrary
    mat = unreal.load_asset("/Game/Pinefall/Materials/Reserve/M_ReserveRoad")
    scan = unreal.load_asset("/Game/Pinefall/Art/Textures/T_BiomeScan_MudC")
    if not mat or not scan:
        raise RuntimeError("Missing existing track material/mud scan")
    for _ in range(32):
        if mel.get_num_material_expressions(mat) == 0:
            break
        mel.delete_all_material_expressions(mat)
    else:
        raise RuntimeError("Cannot clear road graph")
    position = mel.create_material_expression(
        mat, unreal.MaterialExpressionWorldPosition
    )
    xy = mel.create_material_expression(mat, unreal.MaterialExpressionComponentMask)
    xy.set_editor_property("r", True)
    xy.set_editor_property("g", True)
    uv = mel.create_material_expression(mat, unreal.MaterialExpressionDivide)
    uv.set_editor_property("const_b", 600.0)
    sample = mel.create_material_expression(mat, unreal.MaterialExpressionTextureSample)
    sample.set_editor_property("texture", scan)
    tint = mel.create_material_expression(mat, unreal.MaterialExpressionMultiply)
    tint.set_editor_property("const_b", 0.65)
    rough = mel.create_material_expression(mat, unreal.MaterialExpressionConstant)
    rough.set_editor_property("r", 0.94)
    mel.connect_material_expressions(position, "", xy, "Input")
    mel.connect_material_expressions(xy, "", uv, "A")
    mel.connect_material_expressions(uv, "", sample, "UVs")
    mel.connect_material_expressions(sample, "RGB", tint, "A")
    mel.connect_material_property(tint, "", unreal.MaterialProperty.MP_BASE_COLOR)
    mel.connect_material_property(rough, "", unreal.MaterialProperty.MP_ROUGHNESS)
    mel.set_material_usage(mat, unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES)
    mel.recompile_material(mat)
    lib.save_loaded_asset(mat, False)


def build_terrain(project, node, wire, output, material, texture, rock=False):
    m = material(
        "M_ReserveGeology" if rock else "M_ReserveTerrain", "/Game/Pinefall/Materials"
    )
    m.set_editor_property("tangent_space_normal", False)
    roles = ["Forest", "Meadow", "Sand", "Snow", "Rock", "Mud", "DesertRock"]
    inputs = []
    for name in ["P", "N", "RockMode"] + [
        role + kind for role in roles for kind in ["C", "S"]
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
    wire(
        node(m, unreal.MaterialExpressionConstant, r=1.0 if rock else 0.0),
        "",
        blend,
        "RockMode",
    )
    for role in roles:
        for kind, file in [("C", role + "_Color.png"), ("S", role + "_Surface.png")]:
            t = texture(
                project / "SourceArt/Prepared/BiomeSurfaces" / file,
                "T_BiomeScan_" + role + kind,
                linear=kind == "S",
            )
            t.set_editor_property("max_texture_size", 4096 if kind == "C" else 2048)
            t.set_editor_property(
                "compression_settings", unreal.TextureCompressionSettings.TC_BC7
            )
            t.set_editor_property("compression_no_alpha", False)
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
        unreal.MaterialEditingLibrary.set_material_usage(
            m, unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES
        )
    unreal.MaterialEditingLibrary.set_material_usage(
        m, unreal.MaterialUsage.MATUSAGE_NANITE
    )
    unreal.MaterialEditingLibrary.recompile_material(m)
    unreal.EditorAssetLibrary.save_loaded_asset(m, False)
    return m
