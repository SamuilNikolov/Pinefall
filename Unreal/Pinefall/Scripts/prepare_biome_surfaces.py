"""Fetch verified CC0 scans and pack a bounded, biome-specific PBR library.

Ordinary play/setup uses committed assets; this is an explicit authoring step.
Color + source AO: 4K RGBA, sRGB RGB / linear alpha.
Surface: 2K RGBA, DirectX normal XY / roughness / normalized relief height.
"""
from pathlib import Path
import concurrent.futures
import hashlib
import json
import urllib.request
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[1]
RAW = PROJECT / 'SourceArt/Terrain/Photogrammetry'
OUT = PROJECT / 'SourceArt/Prepared/BiomeSurfaces'
AGENT = 'PinefallAssetPrep/0.5 (https://github.com/SamuilNikolov/Pinefall)'
ASSETS = {
    'Forest': 'forest_leaves_02',
    'Meadow': 'leafy_grass',
    'Sand': 'sand_01',
    'Snow': 'snow_02',
    'Rock': 'rock_boulder_dry',
    'Mud': 'brown_mud_leaves_01',
    'DesertRock': 'sandstone_cracks',
}


def request(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': AGENT}), timeout=90)


def prepare(entry):
    role, asset = entry
    folder = RAW / asset
    folder.mkdir(parents=True, exist_ok=True)
    with request('https://api.polyhaven.com/info/' + asset) as response:
        info = json.load(response)
    with request('https://api.polyhaven.com/files/' + asset) as response:
        files = json.load(response)
    manifest = {'role': role, 'asset': asset, 'license': 'CC0-1.0',
                'source': 'https://polyhaven.com/a/' + asset,
                'authors': info['authors'], 'tile_cm': info['dimensions'][0] / 10,
                'files': {}}
    for kind, resolution, extension in [('Diffuse', '4k', 'jpg'), ('nor_dx', '2k', 'png'),
                                         ('Rough', '2k', 'png'), ('Displacement', '2k', 'png'),
                                         ('AO', '2k', 'jpg')]:
        spec = files[kind][resolution][extension]
        path = folder / (kind + '.' + extension)
        if not path.exists() or hashlib.md5(path.read_bytes()).hexdigest() != spec['md5']:
            with request(spec['url']) as response:
                data = response.read()
            if hashlib.md5(data).hexdigest() != spec['md5']:
                raise RuntimeError('Scan integrity mismatch: ' + asset + '/' + kind)
            path.write_bytes(data)
        manifest['files'][kind] = {'path': path.name, 'url': spec['url'], 'md5': spec['md5'],
                                   'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
    (folder / 'provenance.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    diffuse = Image.open(folder / 'Diffuse.jpg').convert('RGB')
    if diffuse.size != (4096, 4096):
        raise RuntimeError('Expected original 4K color: ' + asset)
    ao = Image.open(folder / 'AO.jpg').convert('L').resize(diffuse.size, Image.Resampling.LANCZOS)
    color = diffuse.convert('RGBA')
    color.putalpha(ao)
    color.save(OUT / (role + '_Color.png'), compress_level=6)
    normal = np.asarray(Image.open(folder / 'nor_dx.png').convert('RGB'), dtype=np.uint8)
    rough = np.asarray(Image.open(folder / 'Rough.png').convert('L'), dtype=np.uint8)
    height = np.asarray(Image.open(folder / 'Displacement.png'), dtype=np.float32)
    if height.ndim == 3:
        height = height[:, :, 0]
    lo, hi = np.percentile(height, [1, 99])
    height = np.clip((height - lo) / max(float(hi - lo), 1), 0, 1)
    packed = np.dstack((normal[:, :, :2], rough, np.rint(height * 255).astype(np.uint8)))
    if packed.shape != (2048, 2048, 4):
        raise RuntimeError('Mismatched surface scan sizes: ' + asset)
    Image.fromarray(packed, 'RGBA').save(OUT / (role + '_Surface.png'), compress_level=6)
    manifest['packing'] = '4K color RGBA: sRGB albedo RGB, linear source AO alpha; 2K linear surface RGBA: DirectX normal XY, roughness, normalized relief height'
    return manifest


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        manifests = list(pool.map(prepare, ASSETS.items()))
    (OUT / 'manifest.json').write_text(json.dumps(manifests, indent=2) + '\n', encoding='utf-8')
    print('Prepared seven biome scan sets with verified upstream hashes.')
