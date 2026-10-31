"""Prepare licensed scan geometry at natural scales. Run with Blender 4.3+.
Raw CC0 inputs are checked in under SourceArt/Scenery; no work/ dependency.
"""

from pathlib import Path
import bpy, math, json, struct, random, sys
from mathutils import Vector

PROJECT = Path(__file__).resolve().parents[1]
RAW = PROJECT / "SourceArt/Scenery"
OUT = PROJECT / "SourceArt/Prepared/Scenery"
OUT.mkdir(parents=True, exist_ok=True)
report = []
only_grass = "--grass-only" in sys.argv
if only_grass and (OUT / "manifest.json").exists():
    report = [r for r in json.loads((OUT / "manifest.json").read_text()) if "GrassPatch" not in r["name"]]


def export_asset(asset, selected, name, height, cluster=False, ratio=1):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(RAW / asset / (asset + ".gltf")))
    originals = [
        o
        for o in bpy.context.scene.objects
        if o.type == "MESH" and (selected is None or o.name.startswith(selected))
    ]
    if not originals:
        raise RuntimeError("Missing source node " + str(selected))
    meshes = []
    dg = bpy.context.evaluated_depsgraph_get()
    for o in originals:
        m = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        m.transform(o.matrix_world)
        n = bpy.data.objects.new("Prepared_" + o.name, m)
        bpy.context.collection.objects.link(n)
        meshes.append(n)
    for o in list(bpy.context.scene.objects):
        if o not in meshes:
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    bounds = [Vector(p) for p in obj.bound_box]
    lo = Vector(tuple(min(p[i] for p in bounds) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in bounds) for i in range(3)))
    # Root at the trunk/plant base rather than at a lopsided canopy centroid.
    import numpy as np

    xyz = np.empty(len(obj.data.vertices) * 3, dtype=np.float32)
    obj.data.vertices.foreach_get("co", xyz)
    xyz = xyz.reshape((-1, 3))
    base = xyz[xyz[:, 2] < lo.z + (hi.z - lo.z) * 0.05]
    cx, cy = base[:, :2].mean(axis=0) if len(base) else (lo + hi)[:2] / 2
    shift = Vector((-float(cx), -float(cy), -lo.z))
    scale = height / max(0.001, hi.z - lo.z)
    for v in obj.data.vertices:
        v.co = (v.co + shift) * scale
    if ratio < 1:
        mod = obj.modifiers.new("Presentation LOD", "DECIMATE")
        mod.ratio = ratio
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if cluster:
        rng = random.Random(420)
        parts = [obj]
        for i in range(31 if "Dry" in name else 127):
            n = obj.copy()
            n.data = obj.data.copy()
            bpy.context.collection.objects.link(n)
            n.location = (rng.uniform(-1.5, 1.5), rng.uniform(-1.5, 1.5), 0)
            n.rotation_euler.z = rng.random() * math.tau
            n.scale = (rng.uniform(0.75, 1.15),) * 3
            parts.append(n)
        bpy.ops.object.select_all(action="DESELECT")
        for n in parts:
            n.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.join()
    if cluster:
        color = obj.data.color_attributes.new(name="WindWeight", type="FLOAT_COLOR", domain="CORNER")
        for loop in obj.data.loops:
            weight = max(0., min(1., obj.data.vertices[loop.vertex_index].co.z / height))
            color.data[loop.index].color = (weight, weight, weight, 1.)
        obj.data.color_attributes.active_color = color
    obj.name = name
    for mat in obj.data.materials:
        if mat:
            mat.name = "PH_" + asset + "_" + mat.name
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(
        filepath=str(OUT / (name + ".glb")),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_cameras=False,
        export_lights=False,
        export_vertex_color="ACTIVE",
        export_all_vertex_colors=True,
        export_active_vertex_color_when_no_material=True,
    )
    # PH's modeled leaves often carry an unnecessary BLEND flag. Masked rendering
    # is compatible with Nanite and keeps optional alpha edges without translucency.
    path = OUT / (name + ".glb")
    raw = path.read_bytes()
    length, kind = struct.unpack_from("<II", raw, 12)
    d = json.loads(raw[20 : 20 + length])
    changed = False
    for mat in d.get("materials", []):
        if mat.get("alphaMode") == "BLEND":
            mat["alphaMode"] = "MASK"
            mat["alphaCutoff"] = 0.4
            changed = True
    if changed:
        s = json.dumps(d, separators=(",", ":")).encode()
        s += b" " * ((-len(s)) % 4)
        tail = raw[20 + length :]
        path.write_bytes(
            struct.pack("<III", 0x46546C67, 2, 20 + len(s) + len(tail))
            + struct.pack("<II", len(s), 0x4E4F534A)
            + s
            + tail
        )
    row = {
        "name": name,
        "source": asset,
        "node": selected or "all",
        "height_m": height,
        "cluster": cluster,
        "vertices": len(obj.data.vertices),
        "triangles": sum(len(p.vertices) - 2 for p in obj.data.polygons),
        "bytes": path.stat().st_size,
        "materials": [m.name for m in obj.data.materials],
    }
    report.append(row)
    print("PF_SCENERY_PREPARED", json.dumps(row), flush=True)
    if path.stat().st_size > 99 * 1024 * 1024:
        raise RuntimeError("Prepared asset exceeds repository limit " + name)


for args in [
    ("fir_tree_01", None, "SM_ForestFir", 19, False, 1),
    ("tree_small_02", None, "SM_WoodlandTree", 12, False, 0.55),
    ("island_tree_02", None, "SM_JungleTree", 17, False, 1),
    ("quiver_tree_01", None, "SM_DesertTree", 5.5, False, 1),
    ("grass_bermuda_01", "grass_bermuda_01_medium_a", "SM_GrassPatch", 0.24, True, 1),
    ("grass_bermuda_01", "grass_bermuda_01_dead_a", "SM_DryGrassPatch", 0.20, True, 1),
    ("wild_rooibos_bush", "wild_rooibos_bush_a", "SM_ScrubBush", 0.85, False, 1),
    ("rock_moss_set_01", "rock_moss_set_01_rock01", "SM_MossRock", 1.7, False, 1),
    ("rock_moss_set_01", "rock_moss_set_01_rock03", "SM_FieldRock", 1.4, False, 1),
    ("rock_face_02", None, "SM_CliffFace", 6, False, 1),
]:
    if not only_grass or "GrassPatch" in args[2]:
        export_asset(*args)
(OUT / "manifest.json").write_text(json.dumps(report, indent=2))
