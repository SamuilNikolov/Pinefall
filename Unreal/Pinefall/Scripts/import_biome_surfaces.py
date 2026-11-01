"""Explicitly update the reserve's biome scans, materials and ground cover.

Does not regenerate terrain heights, buildings, roads, collision or the atlas.
Ordinary setup/play uses the committed generated assets.
"""
from pathlib import Path
import json
import sys
import unreal

PROJECT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT / 'Scripts'))
from reserve_materials import build_terrain

LIB = unreal.EditorAssetLibrary
MEL = unreal.MaterialEditingLibrary
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()
CACHE = {}


def node(material, kind, **properties):
    expression = MEL.create_material_expression(material, kind)
    for name, value in properties.items():
        expression.set_editor_property(name, value)
    return expression


def wire(source, pin, target, input_name):
    if not MEL.connect_material_expressions(source, pin, target, input_name):
        raise RuntimeError('Material input: ' + input_name)


def output(source, pin, material, property_name):
    if not MEL.connect_material_property(source, pin, property_name):
        raise RuntimeError('Material output: ' + str(property_name))


def material(name, folder):
    path = folder + '/' + name
    value = unreal.load_asset(path) if LIB.does_asset_exist(path) else TOOLS.create_asset(
        name, folder, unreal.Material, unreal.MaterialFactoryNew())
    value.modify()
    MEL.delete_all_material_expressions(value)
    return value


def texture(path, name, linear=False):
    if name in CACHE:
        return CACHE[name]
    task = unreal.AssetImportTask()
    task.filename = str(path)
    task.destination_path = '/Game/Pinefall/Art/Textures'
    task.destination_name = name
    task.automated = True
    task.replace_existing = True
    task.save = True
    TOOLS.import_asset_tasks([task])
    value = unreal.load_asset(task.destination_path + '/' + name)
    if not value:
        raise RuntimeError('Texture import: ' + str(path))
    value.set_editor_property('srgb', not linear)
    CACHE[name] = value
    return value


manifest = json.loads((PROJECT / 'SourceArt/Prepared/BiomeSurfaces/manifest.json').read_text())
if len(manifest) != 7:
    raise RuntimeError('Expected seven verified scan sets')
level = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
if not level.load_level('/Game/Pinefall/Maps/WildfrontReserve'):
    raise RuntimeError('Reserve could not be loaded')
build_terrain(PROJECT, node, wire, output, material, texture)
build_terrain(PROJECT, node, wire, output, material, texture, rock=True)
changed = unreal.PFWorldBuilder.apply_reserve_biome_presentation()
if changed < 0:
    raise RuntimeError('Reserve biome groups/assets missing')
unreal.PFWorldBuilder.finish_rendering_preparation()
if not unreal.PFWorldBuilder.build_reserve_nanite():
    raise RuntimeError('Nanite landscape data missing or outdated')
if not level.save_current_level():
    raise RuntimeError('Reserve could not be saved')
unreal.log('PF_BIOME_SCANS_OK sets=7 textures=' + str(len(CACHE)) + ' changed_cover=' + str(changed))
