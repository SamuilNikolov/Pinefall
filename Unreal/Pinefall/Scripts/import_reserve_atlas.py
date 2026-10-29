from pathlib import Path
import unreal
p=Path(__file__).resolve().parents[1]
task=unreal.AssetImportTask();task.filename=str(p/'SourceArt/Prepared/Reserve/ReserveAtlas.png');task.destination_path='/Game/Pinefall/Art';task.destination_name='T_ReserveAtlas';task.automated=True;task.replace_existing=True;task.save=True;unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])
t=unreal.load_asset('/Game/Pinefall/Art/T_ReserveAtlas');t.set_editor_property('max_texture_size',2048);t.set_editor_property('lod_group',unreal.TextureGroup.TEXTUREGROUP_UI);t.set_editor_property('mip_gen_settings',unreal.TextureMipGenSettings.TMGS_NO_MIPMAPS);unreal.EditorAssetLibrary.save_loaded_asset(t);unreal.log('PF_RESERVE_ATLAS_OK')
