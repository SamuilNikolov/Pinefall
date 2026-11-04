"""Apply credited 1K scan materials and an instanced architecture kit to the reserve."""

from pathlib import Path
import hashlib, json, unreal, sys

P = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from reserve_materials import build_track

L = unreal.EditorAssetLibrary
M = unreal.MaterialEditingLibrary
T = unreal.AssetToolsHelpers.get_asset_tools()
DEST = "/Game/Pinefall/Materials/Village"
TD = "/Game/Pinefall/Art/ArchitectureTextures"
L.make_directory(DEST)
L.make_directory(TD)


def tex(source, name, kind):
    path = TD + "/" + name
    t = unreal.load_asset(path)
    if not t:
        task = unreal.AssetImportTask()
        task.filename = str(source)
        task.destination_path = TD
        task.destination_name = name
        task.automated = True
        task.save = True
        T.import_asset_tasks([task])
        t = unreal.load_asset(path)
    if not t:
        raise RuntimeError("Texture " + name)
    t.set_editor_property("srgb", kind == "Diffuse")
    t.set_editor_property("max_texture_size", 1024)
    if kind == "nor_dx":
        t.set_editor_property(
            "compression_settings", unreal.TextureCompressionSettings.TC_NORMALMAP
        )
    L.save_loaded_asset(t)
    return t


def node(mat, cls, **values):
    n = M.create_material_expression(mat, cls)
    for k, v in values.items():
        n.set_editor_property(k, v)
    return n


def material(name, inputs, tint, metal=0):
    path = DEST + "/" + name
    mat = unreal.load_asset(path)
    if not mat:
        mat = T.create_asset(name, DEST, unreal.Material, unreal.MaterialFactoryNew())
    for _ in range(32):
        if M.get_num_material_expressions(mat) == 0:
            break
        M.delete_all_material_expressions(mat)
    else:
        raise RuntimeError("Cannot clear village material graph")
    diffuse = node(
        mat, unreal.MaterialExpressionTextureSample, texture=inputs["Diffuse"]
    )
    tintnode = node(
        mat,
        unreal.MaterialExpressionConstant3Vector,
        constant=unreal.LinearColor(*tint, 1),
    )
    mul = node(mat, unreal.MaterialExpressionMultiply)
    M.connect_material_expressions(diffuse, "RGB", mul, "A")
    M.connect_material_expressions(tintnode, "", mul, "B")
    M.connect_material_property(mul, "", unreal.MaterialProperty.MP_BASE_COLOR)
    for key, prop in [
        ("nor_dx", unreal.MaterialProperty.MP_NORMAL),
        ("Rough", unreal.MaterialProperty.MP_ROUGHNESS),
    ]:
        if key in inputs:
            sample = node(
                mat, unreal.MaterialExpressionTextureSample, texture=inputs[key]
            )
            if key == "nor_dx":
                sample.set_editor_property(
                    "sampler_type", unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL
                )
            M.connect_material_property(sample, "RGB" if key == "nor_dx" else "R", prop)
        else:
            (
                M.connect_material_property(
                    node(mat, unreal.MaterialExpressionConstant, r=0.82), "", prop
                )
                if key == "Rough"
                else None
            )
    M.connect_material_property(
        node(mat, unreal.MaterialExpressionConstant, r=metal),
        "",
        unreal.MaterialProperty.MP_METALLIC,
    )
    M.set_material_usage(mat, unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES)
    M.recompile_material(mat)
    L.save_loaded_asset(mat)
    return mat


sets = {}
for entry in json.loads((P / "SourceArt/Architecture/sources.json").read_text()):
    for f in entry["files"].values():
        raw = (P / "SourceArt/Architecture" / f["file"]).read_bytes()
        if len(raw) != f["bytes"] or hashlib.sha256(raw).hexdigest() != f["sha256"]:
            raise RuntimeError("Architecture source checksum: " + f["file"])
    sets[entry["id"]] = {
        kind: tex(
            P / "SourceArt/Architecture" / f["file"],
            "T_Village_" + entry["id"] + "_" + kind,
            kind,
        )
        for kind, f in entry["files"].items()
    }
plaster = material("M_VillagePlaster", sets["plastered_wall_02"], (0.94, 0.90, 0.80))
roof = material("M_VillageRoof", sets["worn_corrugated_iron"], (0.62, 0.68, 0.66), 0.55)
wood = unreal.load_asset("/Game/Pinefall/Art/Textures/T_Reserve_WeatheredWood")
if not wood:
    raise RuntimeError("Missing existing timber surface")
timber = material("M_VillageTimber", {"Diffuse": wood}, (0.85, 0.75, 0.60))
build_track()
for name in ["SM_RangerCabin", "SM_AlpineChalet", "SM_AdobeHouse"]:
    mesh = unreal.load_asset("/Game/Pinefall/Art/Reserve/" + name)
    if not mesh:
        raise RuntimeError("Missing cabin " + name)
    for i, slot in enumerate(mesh.get_editor_property("static_materials")):
        key = str(slot.material_slot_name)
        m = (
            plaster
            if key.startswith("Plaster")
            else (
                roof
                if key.startswith("Roof")
                else (
                    timber
                    if key.startswith("Wood") or key.startswith("White")
                    else None
                )
            )
        )
        if m:
            mesh.set_material(i, m)
    L.save_loaded_asset(mesh, False)
unreal.EditorLoadingAndSavingUtils.load_map("/Game/Pinefall/Maps/WildfrontReserve")
actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
for a in actors.get_all_level_actors():
    if a.get_actor_label() == "Reserve village architecture kit":
        actors.destroy_actor(a)
count = unreal.PFWorldBuilder.build_reserve_village_details()
if count != 5072:
    raise RuntimeError("Unexpected architecture instance count: " + str(count))
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True):
    raise RuntimeError("Village assets/map could not be saved")
unreal.log("PF_VILLAGES_UPGRADED " + str(count))
