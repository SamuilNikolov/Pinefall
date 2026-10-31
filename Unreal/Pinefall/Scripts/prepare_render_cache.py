"""Optional first-run desktop render preparation; no gameplay or microphone.
Run using UnrealEditor-Cmd with its normal graphics backend, not -nullrhi.
"""

from pathlib import Path
import json, unreal, sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from reserve_materials import build_terrain

PROJECT = Path(__file__).resolve().parents[1]
LIB = unreal.EditorAssetLibrary
for path in LIB.list_assets("/Game/Pinefall/Art/Reserve", True) + LIB.list_assets(
    "/Game/Pinefall/Art/Weapons", True
):
    asset = unreal.load_asset(path)
    if (
        isinstance(asset, unreal.StaticMesh)
        and asset.get_editor_property("nanite_settings").enabled
    ):
        # Windows and vehicle glass need normal translucent rendering; these meshes
        # are modest enough that virtualized geometry isn't useful here.
        unreal.PFWorldBuilder.configure_mesh(asset, False)
        LIB.save_loaded_asset(asset, False)
# Rebuild only the terrain surface after art-direction changes; the saved
# landscape, object stock, and collision stay at the authored checkpoint.
MEL = unreal.MaterialEditingLibrary
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()


def node(m, kind, **values):
    n = MEL.create_material_expression(m, kind)
    for k, v in values.items():
        n.set_editor_property(k, v)
    return n


def wire(a, p, b, q):
    if not MEL.connect_material_expressions(a, p, b, q):
        raise RuntimeError("Material pin " + q)


def output(a, p, m, q):
    if not MEL.connect_material_property(a, p, q):
        raise RuntimeError("Material output " + str(q))


def material(name, folder):
    m = unreal.load_asset(folder + "/" + name)
    m.modify()
    MEL.delete_all_material_expressions(m)
    return m


def texture(path, name, linear=False):
    t = unreal.load_asset("/Game/Pinefall/Art/Textures/" + name)
    if not t:
        raise RuntimeError("Terrain texture missing " + name)
    return t


build_terrain(PROJECT, node, wire, output, material, texture)
unreal.EditorLoadingAndSavingUtils.load_map("/Game/Pinefall/Maps/WildfrontReserve")
for folder in [
    "/Game/Pinefall/Art/Fauna",
    "/Game/Pinefall/Art/Reserve",
    "/Game/Pinefall/Art/Characters/TrailRanger",
]:
    for path in LIB.list_assets(folder, True):
        asset = unreal.load_asset(path)
        if isinstance(asset, unreal.Material):
            if "/Fauna/" in path:
                unreal.MaterialEditingLibrary.set_material_usage(
                    asset, unreal.MaterialUsage.MATUSAGE_SKELETAL_MESH
                )
            unreal.MaterialEditingLibrary.recompile_material(asset)
unreal.PFWorldBuilder.finish_rendering_preparation()
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True):
    raise RuntimeError("Render preparation save failed")
unreal.log("PF_RENDER_PREPARATION_OK")
