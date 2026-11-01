"""Build distance LODs without importing textures or modifying world placement."""
from pathlib import Path
import sys
import unreal

project = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(project / 'Scripts'))
from foliage_budget import configure_ground_cover_lods

for name in ['SM_GrassPatch', 'SM_DryGrassPatch']:
    mesh = unreal.load_asset('/Game/Pinefall/Art/Scenery/' + name)
    if not mesh or configure_ground_cover_lods(mesh) != 3:
        raise RuntimeError('Missing grass mesh: ' + name)
unreal.PFWorldBuilder.finish_rendering_preparation()
unreal.log('PF_GROUND_COVER_LODS_OK meshes=2 lods=3 source_lod0=preserved')
