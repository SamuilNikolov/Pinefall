"""Material helpers for the reserve builder. No runtime/editor dependency."""

from pathlib import Path
import unreal


def build_terrain(project, node, wire, output, material, texture):
    m = material("M_ReserveTerrain", "/Game/Pinefall/Materials")
    inputs = []
    for name in ["P", "N"] + [
        role + kind
        for role in ["Forest", "Meadow", "Sand", "Snow", "Rock"]
        for kind in ["C", "S"]
    ]:
        i = unreal.CustomInput()
        i.set_editor_property("input_name", name)
        inputs.append(i)
    extra = []
    for name, kind in [
        ("OutNormal", unreal.CustomMaterialOutputType.CMOT_FLOAT3),
        ("OutRoughness", unreal.CustomMaterialOutputType.CMOT_FLOAT1),
    ]:
        i = unreal.CustomOutput()
        i.set_editor_property("output_name", name)
        i.set_editor_property("output_type", kind)
        extra.append(i)
    blend = node(
        m,
        unreal.MaterialExpressionCustom,
        code=(project / "Scripts/reserve_ground_pbr.hlsl").read_text(
            encoding="utf-8-sig"
        ),
        output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,
        inputs=inputs,
        additional_outputs=extra,
    )
    wire(node(m, unreal.MaterialExpressionWorldPosition), "", blend, "P")
    wire(node(m, unreal.MaterialExpressionVertexNormalWS), "", blend, "N")
    for role in ["Forest", "Meadow", "Sand", "Snow", "Rock"]:
        for kind, file in [("C", role + "_Color.jpg"), ("S", role + "_Surface.png")]:
            t = texture(
                project / "SourceArt/Prepared/Terrain" / file,
                "T_ReservePBR_" + role + kind,
                linear=kind == "S",
            )
            if kind == "S":
                t.set_editor_property(
                    "compression_settings", unreal.TextureCompressionSettings.TC_BC7
                )
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
    unreal.MaterialEditingLibrary.recompile_material(m)
    unreal.EditorAssetLibrary.save_loaded_asset(m, False)
    return m
