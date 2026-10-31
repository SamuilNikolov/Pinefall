"""Bake a 4K terrain/road/building/forest atlas from the saved reserve report."""

from pathlib import Path
import json, math, numpy as np
from PIL import Image, ImageDraw

PROJECT = Path(__file__).resolve().parents[1]
data = json.loads((PROJECT / "Saved/ReserveBuild.json").read_text())
size = 4096
half = 100800
raw = np.array(data["heights"], dtype=np.float32).reshape(505, 505).T[::-1, :]
h = np.asarray(Image.fromarray(raw).resize((size, size), Image.Resampling.BICUBIC))
y = np.linspace(-half, half, size)[None, :]
x = np.linspace(half, -half, size)[:, None]
colors = np.zeros((size, size, 3), dtype=np.float32)
colors[:] = [109, 143, 82]


def smooth(a, b, v):
    t = np.clip((v - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def mix(base, tint, w):
    return base * (1 - w[:, :, None]) + np.array(tint)[None, None, :] * w[:, :, None]


woods = np.broadcast_to(1 - smooth(-23000, -7000, x), (size, size))
colors = mix(colors, [66, 108, 73], woods * 0.82)
sand = (1 - smooth(-30000, -14000, x)) * (1 - smooth(-44000, -28000, y))
snow = smooth(18000, 32000, x) * smooth(10000, 22000, y)
alpine = smooth(12000, 26000, x) * (1 - smooth(-6000, 10000, y))
colors = mix(colors, [126, 131, 119], alpine * 0.55)
colors = mix(colors, [186, 162, 113], sand)
colors = mix(colors, [207, 218, 216], snow)
gv, gu = np.gradient(h)
shade = np.clip(1 + (gu - gv) * 0.018 * (size / 2048), 0.57, 1.32)
colors *= shade[:, :, None]
contours = np.abs((h + 50) % 1000 - 50) < 18
colors[contours] *= 0.91
lake = ((x - 30000) / 22000) ** 2 + ((y - 30000) / 17000) ** 2 < 1
wet = lake & (h < -300)
colors[wet] = [44, 111, 133]
colors[wet & (h < -850)] = [31, 91, 119]
im = Image.fromarray(np.uint8(np.clip(colors, 0, 255)), "RGB")
d = ImageDraw.Draw(im)


def pos(x, y):
    return ((0.5 + y / (half * 2)) * (size - 1), (0.5 - x / (half * 2)) * (size - 1))


for index, tree in enumerate(data.get("trees", [])):
    tx, ty = tree[:2]
    u, v = pos(tx, ty)
    r = max(2, tree[2] / (half * 2) * size * 0.70) if len(tree) > 2 else 5
    snowy = tx > 25000 and ty > 15000
    arid = tx < -22000 and ty < -35000
    shade = (88, 111, 104) if snowy else (94, 85, 54) if arid else (37, 62, 43)
    canopy = (
        (160, 185, 169)
        if snowy
        else (
            (126, 136, 76)
            if arid
            else [(57, 92, 59), (64, 103, 65), (73, 111, 68)][index % 3]
        )
    )
    d.ellipse((u - r + 2, v - r + 2, u + r + 3, v + r + 3), fill=shade)
    d.ellipse((u - r, v - r, u + r, v + r), fill=canopy)
for ai, bi in data["roads"]:
    a, b = data["sites"][ai], data["sites"][bi]
    points = [pos(a["x"], a["y"]), pos(b["x"], b["y"])]
    d.line(points, fill=(49, 54, 51), width=20)
    d.line(points, fill=(129, 128, 110), width=13)
    d.line(points, fill=(176, 166, 134), width=1)
for building in data["buildings"]:
    u, v = pos(building["x"], building["y"])
    d.rectangle((u - 11, v - 9, u + 12, v + 10), fill=(43, 48, 42))
    d.rectangle((u - 9, v - 8, u + 9, v + 8), fill=(179, 156, 119))
    d.line((u - 9, v, u + 9, v), fill=(93, 88, 76), width=1)
path = PROJECT / "SourceArt/Prepared/Reserve/ReserveAtlas.png"
im.save(path)
print("PF_RESERVE_ATLAS_BAKED", path)
