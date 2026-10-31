"""Pack coherent terrain normals/roughness into ten GPU texture bindings."""

from pathlib import Path
import json, numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[1]
RAW = PROJECT / "SourceArt/Terrain/Scans"
OUT = PROJECT / "SourceArt/Prepared/Terrain"
OUT.mkdir(parents=True, exist_ok=True)
rows = []
for role, stem, diff in [
    ("Forest", "forest_leaves_02", "diffuse"),
    ("Rock", "rocky_terrain", "diff"),
    ("Meadow", "Expansion/Meadow", "diff"),
    ("Sand", "Expansion/Sand", "diff"),
    ("Snow", "Expansion/Snow", "diff"),
]:
    suffix = "" if "/" in stem else "_2k"

    def load(kind):
        return Image.open(RAW / (stem + "_" + kind + suffix + ".jpg")).convert("RGB")

    color = load(diff)
    color.save(OUT / (role + "_Color.jpg"), quality=95)
    normal = np.asarray(load("nor_gl")).copy()
    rough = np.asarray(load("rough").resize(color.size))
    packed = np.empty((*normal.shape[:2], 4), dtype=np.uint8)
    packed[:, :, 0] = normal[:, :, 0]
    packed[:, :, 1] = 255 - normal[:, :, 1]
    packed[:, :, 2] = rough[:, :, 0]
    packed[:, :, 3] = 255
    Image.fromarray(packed, "RGBA").save(OUT / (role + "_Surface.png"))
    rows.append(
        {
            "role": role,
            "source_stem": stem,
            "normal_space": "DirectX tangent XY",
            "channels": "R normal X, G normal Y, B roughness, A reserved 1",
        }
    )
(OUT / "manifest.json").write_text(json.dumps(rows, indent=2))
print("PF_TERRAIN_PACK_OK")
