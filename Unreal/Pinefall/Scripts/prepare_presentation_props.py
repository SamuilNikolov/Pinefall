"""Original detailed reserve props. Blender 4.3; no third-party vehicle model.
Rebuilds the small legacy supplies, then replaces architecture and transport.
"""

from pathlib import Path
import bpy, math, runpy, json
from mathutils import Vector

PROJECT = Path(__file__).resolve().parents[1]
OUT = PROJECT / "SourceArt/Prepared/Reserve"
base = runpy.run_path(str(PROJECT / "Scripts/prepare_reserve_props.py"))
box = base["box"]
cylinder = base["cylinder"]
hull = base["hull"]
objects = base["objects"]
materials = base["materials"]
for name, color in {
    "Plaster": (0.61, 0.59, 0.48, 1),
    "Brick": (0.32, 0.16, 0.11, 1),
    "Leather": (0.055, 0.04, 0.028, 1),
    "RoofAlt": (0.27, 0.29, 0.31, 1),
}.items():
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = color
    materials[name] = m


def smooth(o):
    for p in o.data.polygons:
        p.use_smooth = True
    o.data.set_sharp_from_angle(angle=math.radians(38))
    return o


def tube(name, points, radius, mat="Metal", closed=False):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = 3
    curve.resolution_u = 2
    s = curve.splines.new("POLY")
    s.points.add(len(points) - 1)
    for p, v in zip(s.points, points):
        p.co = (*v, 1)
    s.use_cyclic_u = closed
    o = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.convert(target="MESH")
    o = bpy.context.object
    o.data.materials.append(materials[mat])
    objects.append(o)
    o.select_set(False)
    return o


def loft(name, sections, mat="Paint", open_top=False):
    # Rounded, crowned cross sections, rather than one scaled cube.
    verts = []
    for y, w, b, t in sections:
        verts.extend(
            [
                (x * w, y, z)
                for x, z in [
                    (-0.7, b),
                    (-0.96, b + 0.08),
                    (-1, t - 0.09),
                    (-0.88, t),
                    (-0.35, t + 0.025),
                    (0.35, t + 0.025),
                    (0.88, t),
                    (1, t - 0.09),
                    (0.96, b + 0.08),
                    (0.7, b),
                ]
            ]
        )
    n = 10
    faces = [
        tuple(range(n - 1, -1, -1)),
        tuple((len(sections) - 1) * n + i for i in range(n)),
    ]
    for j in range(len(sections) - 1):
        for i in range(n):
            if open_top and i in [3, 4, 5]:
                continue
            faces.append(
                (
                    j * n + i,
                    j * n + (i + 1) % n,
                    (j + 1) * n + (i + 1) % n,
                    (j + 1) * n + i,
                )
            )
    return smooth(hull(name, verts, faces, mat))


def save(name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    o = bpy.context.object
    o.name = name
    o.data.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    # Consistent two-metre texel scale, so a long wall doesn't stretch one square image.
    uv = o.data.uv_layers.active or o.data.uv_layers.new(name="UVMap")
    for p in o.data.polygons:
        axis = max(range(3), key=lambda i: abs(p.normal[i]))
        axes = [i for i in range(3) if i != axis]
        for li in p.loop_indices:
            v = o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv = (v[axes[0]] * 0.5, v[axes[1]] * 0.5)
    bpy.ops.export_scene.gltf(
        filepath=str(OUT / (name + ".glb")),
        export_format="GLB",
        use_selection=True,
        export_animations=False,
    )
    print("PF_PRESENTATION_PROP", name, "vertices", len(o.data.vertices), flush=True)
    bpy.data.objects.remove(o, do_unlink=True)
    objects.clear()


# Four independent tyres allow the game to show steering and rolling.
smooth(
    cylinder("Tyre carcass", (0, 0, 0), 0.455, 0.30, "Rubber", (0, math.pi / 2, 0), 64)
)
for j in range(48):
    a = j * math.tau / 48
    for side in [-1, 1]:
        box(
            "Chevron tread",
            (side * 0.077, math.cos(a) * 0.453, math.sin(a) * 0.453),
            (0.145, 0.072, 0.034),
            "Rubber",
            0.006,
            (a - math.pi / 2, 0, side * 0.14),
        )
for x in [-0.16, 0.16]:
    cylinder("Brake disc", (x, 0, 0), 0.26, 0.018, "Metal", (0, math.pi / 2, 0), 48)
    bpy.ops.mesh.primitive_torus_add(
        major_segments=48,
        minor_segments=8,
        location=(x, 0, 0),
        major_radius=0.26,
        minor_radius=0.022,
        rotation=(0, math.pi / 2, 0),
    )
    o = bpy.context.object
    o.data.materials.append(materials["Metal"])
    objects.append(o)
    smooth(o)
    for i in range(6):
        a = i * math.tau / 6
        box(
            "Alloy spoke",
            (x * 1.03, 0.13 * math.cos(a), 0.13 * math.sin(a)),
            (0.025, 0.28, 0.045),
            "Metal",
            0.012,
            (a, 0, 0),
        )
        cylinder(
            "Lug bolt",
            (x * 1.12, 0.075 * math.cos(a), 0.075 * math.sin(a)),
            0.014,
            0.025,
            "Metal",
            (0, math.pi / 2, 0),
            12,
        )
cylinder("Axle hub", (0, 0, 0), 0.09, 0.36, "Metal", (0, math.pi / 2, 0), 32)
save("SM_OffroadWheel")

# Unbranded four-door trail 4x4, with an actual greenhouse and cabin interior.
box("Ladder chassis", (0, 0, 0.43), (1.55, 3.9, 0.16), "Metal", 0.06)
body = loft(
    "Pressed body",
    [
        (-2.07, 0.72, 0.52, 0.98),
        (-1.96, 0.94, 0.5, 1.08),
        (-1.6, 1, 0.49, 1.15),
        (-0.5, 1, 0.49, 1.17),
        (0.45, 0.98, 0.49, 1.16),
        (1.5, 0.96, 0.5, 1.13),
        (1.95, 0.84, 0.55, 1.03),
        (2.12, 0.66, 0.62, 0.94),
    ],
)
for x in [-1, 1]:
    for y in [-1.34, 1.32]:
        cutter = cylinder(
            "Wheel clearance",
            (x, y, 0.46),
            0.53,
            0.7,
            "Rubber",
            (0, math.pi / 2, 0),
            64,
        )
        objects.remove(cutter)
        bpy.context.view_layer.objects.active = body
        mod = body.modifiers.new("Formed wheel well", "BOOLEAN")
        mod.object = cutter
        mod.operation = "DIFFERENCE"
        mod.solver = "EXACT"
        bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.data.objects.remove(cutter, do_unlink=True)
        tube(
            "Wheel arch",
            [
                (x * 0.985, y + math.cos(a) * 0.55, 0.46 + math.sin(a) * 0.55)
                for a in [i * math.pi / 32 for i in range(33)]
            ],
            0.045,
            "Paint",
        )
    box("Running board", (x * 1.02, -0.02, 0.50), (0.18, 1.55, 0.10), "Metal", 0.045)
loft(
    "Crowned roof",
    [
        (-1.5, 0.70, 1.78, 1.86),
        (-1.3, 0.79, 1.84, 1.95),
        (-0.5, 0.82, 1.89, 2.01),
        (0.25, 0.79, 1.86, 1.98),
        (0.65, 0.70, 1.78, 1.87),
    ],
)
for side in [-1, 1]:
    x = side * 0.93
    for ya, yb in [(-1.38, -0.46), (-0.41, 0.81)]:
        hull(
            "Side glass",
            [
                (x, ya, 1.19),
                (x, yb, 1.19),
                (side * 0.79, yb - 0.21, 1.81),
                (side * 0.79, ya + 0.07, 1.84),
            ],
            [(0, 1, 2, 3)],
            "Glass",
        )
    for y0, y1 in [(-1.45, -1.32), (-0.44, -0.44), (0.88, 0.58)]:
        tube(
            "Window pillar",
            [(side * 0.94, y0, 1.13), (side * 0.78, y1, 1.86)],
            0.038,
            "Paint",
        )
    for y in [-0.5, 0.8]:
        tube(
            "Door shut line",
            [(side * 1.002, y, 0.63), (side * 1.002, y, 1.15)],
            0.008,
            "Rubber",
        )
        box(
            "Door handle",
            (side * 1.005, y - 0.18, 1.09),
            (0.035, 0.18, 0.045),
            "Metal",
            0.014,
        )
    tube("Window lower trim", [(x, -1.4, 1.17), (x, 0.86, 1.17)], 0.016)
    box("Mirror arm", (side * 1.05, 0.75, 1.33), (0.22, 0.045, 0.04), "Metal", 0.015)
    box("Mirror housing", (side * 1.15, 0.76, 1.38), (0.13, 0.25, 0.15), "Paint", 0.05)
    box("Mirror lens", (side * 1.16, 0.64, 1.39), (0.1, 0.018, 0.11), "Glass", 0.012)
hull(
    "Raked windscreen",
    [(-0.9, 0.91, 1.19), (0.9, 0.91, 1.19), (0.72, 0.57, 1.83), (-0.72, 0.57, 1.83)],
    [(0, 1, 2, 3)],
    "Glass",
)
hull(
    "Rear glass",
    [
        (-0.92, -1.47, 1.20),
        (-0.73, -1.32, 1.84),
        (0.73, -1.32, 1.84),
        (0.92, -1.47, 1.20),
    ],
    [(0, 1, 2, 3)],
    "Glass",
)
box("Cabin floor", (0, -0.35, 0.68), (1.65, 2.3, 0.08), "Rubber", 0.04)
for x in [-0.45, 0.45]:
    for y in [-0.87, 0.18]:
        box("Seat cushion", (x, y, 0.81), (0.62, 0.55, 0.18), "Leather", 0.08)
        box(
            "Seat back",
            (x, y - 0.25, 1.14),
            (0.59, 0.15, 0.64),
            "Leather",
            0.07,
            (-0.10, 0, 0),
        )
        box("Head restraint", (x, y - 0.28, 1.55), (0.34, 0.14, 0.20), "Leather", 0.055)
box("Dashboard", (0, 0.64, 1.13), (1.62, 0.32, 0.22), "Rubber", 0.07)
tube(
    "Steering wheel",
    [
        (
            -0.45 + math.cos(i * math.tau / 48) * 0.18,
            0.50,
            1.21 + math.sin(i * math.tau / 48) * 0.18,
        )
        for i in range(48)
    ],
    0.019,
    closed=True,
)
for y in [-2.10, 2.13]:
    box("Bumper", (0, y, 0.65), (1.83, 0.17, 0.2), "Rubber", 0.075)
box("Front grille", (0, 2.055, 0.91), (1.14, 0.028, 0.22), "Rubber", 0.02)
for x in [-0.46, -0.3, -0.15, 0, 0.15, 0.3, 0.46]:
    box("Grille vane", (x, 2.078, 0.91), (0.035, 0.018, 0.18), "Metal", 0.008)
for x in [-0.72, 0.72]:
    box("Headlight inset", (x, 1.98, 1.03), (0.29, 0.06, 0.18), "Metal", 0.04)
    box("Headlight lens", (x, 2.015, 1.035), (0.25, 0.02, 0.14), "White", 0.035)
    box("Rear lamp", (x, -2.055, 1.00), (0.16, 0.035, 0.22), "Red", 0.025)
    tube(
        "Roof rail",
        [(x, -1.1, 2.02), (x, -1.0, 2.10), (x, 0.25, 2.10), (x, 0.35, 2.01)],
        0.025,
    )
for y in [-0.90, 0.2]:
    tube("Roof crossbar", [(-0.70, y, 2.10), (0, y, 2.14), (0.70, y, 2.10)], 0.022)
for x in [-0.45, 0.45]:
    tube("Windshield wiper", [(x, 0.96, 1.21), (x + 0.16, 0.86, 1.40)], 0.009, "Rubber")
save("SM_TrailTruck")

# Smooth flared hull, inner liner, deck planking, rails and helm.
sections = [
    (-1.86, 0.69, 0.12, 0.81),
    (-1.65, 0.83, 0.06, 0.84),
    (-0.85, 0.93, 0.04, 0.86),
    (0.25, 0.95, 0.07, 0.90),
    (1.0, 0.79, 0.16, 0.96),
    (1.65, 0.43, 0.35, 1.05),
    (2.15, 0.025, 0.76, 1.12),
]
loft("River hull", sections, open_top=True)
for side in [-1, 1]:
    tube("Gunwale", [(side * w, y, t) for y, w, b, t in sections], 0.047, "Metal")
for x in [-0.60, -0.40, -0.20, 0, 0.20, 0.40, 0.60]:
    box("Deck plank", (x, -0.20, 0.31), (0.185, 2.95, 0.065), "Wood", 0.012)
for y in [-1.18, -0.22, 0.9]:
    box("Bench structure", (0, y, 0.58), (1.46, 0.26, 0.34), "Paint", 0.045)
    box("Bench cushion", (0, y, 0.77), (1.44, 0.30, 0.10), "Leather", 0.045)
box("Helm console", (-0.48, 0.48, 0.80), (0.43, 0.40, 0.72), "White", 0.065)
tube(
    "Helm wheel",
    [
        (
            -0.48 + math.cos(i * math.tau / 40) * 0.14,
            0.22,
            1.14 + math.sin(i * math.tau / 40) * 0.14,
        )
        for i in range(40)
    ],
    0.018,
    closed=True,
)
box("Outboard cowling", (0, -2.0, 0.84), (0.46, 0.44, 0.57), "Metal", 0.14)
box("Outboard leg", (0, -2.08, 0.23), (0.14, 0.17, 0.73), "Metal", 0.05)
cylinder(
    "Propeller hub", (0, -2.13, -0.13), 0.085, 0.19, "Metal", (math.pi / 2, 0, 0), 32
)
for a in [0, math.tau / 3, math.tau * 2 / 3]:
    box(
        "Propeller blade",
        (0.13 * math.cos(a), -2.25, -0.13 + 0.13 * math.sin(a)),
        (0.30, 0.035, 0.075),
        "Metal",
        0.02,
        (0, -a, 0),
    )
for side in [-1, 1]:
    for y in [-1.35, 0.7]:
        cylinder(
            "Mooring cleat",
            (side * 0.79, y, 0.91),
            0.025,
            0.16,
            "Metal",
            (math.pi / 2, 0, 0),
            16,
        )
    tube(
        "Bow safety rail",
        [
            (side * 0.75, 0.9, 1.01),
            (side * 0.64, 1.2, 1.28),
            (side * 0.30, 1.77, 1.41),
            (0, 2.05, 1.43),
        ],
        0.022,
    )
save("SM_RiverBoat")

# All houses keep the tested open doorway, interior floor and rear wall footprint.
for name, style in [
    ("SM_RangerCabin", "timber"),
    ("SM_AlpineChalet", "alpine"),
    ("SM_AdobeHouse", "adobe"),
]:
    wall = "Plaster" if style == "adobe" else "Wood"
    roof = "RoofAlt" if style == "alpine" else "Roof"
    box("Stone foundation", (0, 0, 0.12), (7.8, 9.2, 0.24), "Stone", 0.05)
    for x in [i * 0.20 - 3.5 for i in range(36)]:
        box("Floor boards", (x, 0, 0.29), (0.194, 8.65, 0.10), "Wood", 0.009)

    def wall_piece(label, loc, size):
        if style == "adobe":
            box(label, loc, size, wall, 0.045)
            return
        x, y, z = loc
        sx, sy, sz = size
        n = max(1, round(sz / 0.20))
        for i in range(n):
            box(
                label,
                (x, y, z - sz / 2 + (i + 0.5) * sz / n),
                (sx, sy, sz / n - 0.003),
                wall,
                0.012,
            )

    for x in [-3.65, 3.65]:
        wall_piece("Siding below windows", (x, 0, 0.85), (0.18, 8.6, 1.1))
        wall_piece("Siding above windows", (x, 0, 2.68), (0.18, 8.6, 0.75))
        for y in [-3.8, -1.6, 1.6, 3.8]:
            wall_piece("Window piers", (x, y, 1.85), (0.18, 0.8, 0.95))
        for y in [-2.7, 0, 2.7]:
            box("Window glass", (x, y, 1.84), (0.026, 1.35, 0.90), "Glass", 0.005)
            for dz in [-0.5, 0.5]:
                box(
                    "Window horizontal trim",
                    (x * 1.02, y, 1.84 + dz),
                    (0.10, 1.52, 0.10),
                    "White",
                    0.012,
                )
            for dy in [-0.74, 0, 0.74]:
                box(
                    "Window vertical trim",
                    (x * 1.02, y + dy, 1.84),
                    (0.10, 0.065, 1.06),
                    "White",
                    0.012,
                )
    wall_piece("Rear wall", (0, -4.3, 1.69), (7.3, 0.18, 2.8))
    for x in [-2.25, 2.25]:
        wall_piece("Front wings", (x, 4.3, 1.69), (2.8, 0.18, 2.8))
    wall_piece("Door header", (0, 4.3, 2.86), (1.7, 0.22, 0.45))
    for x in [-0.87, 0.87]:
        box("Open door frame", (x, 4.4, 1.42), (0.12, 0.17, 2.35), "White", 0.015)
    for x in [i * 0.2 - 2.3 for i in range(24)]:
        box("Porch board", (x, 4.98, 0.22), (0.194, 1.45, 0.16), "Wood", 0.009)
    for x in [-3.76, 3.76]:
        for y in [-4.40, 4.40]:
            box("Corner timber", (x, y, 1.70), (0.23, 0.23, 3.02), "Wood", 0.018)
    for z, dy, width in [(0.10, 5.9, 3.6), (0.18, 5.65, 4.0)]:
        box("Entry step", (0, dy, z), (width, 0.35, 0.12), "Stone", 0.025)
    rise = 2.75 if style == "alpine" else 1.85
    if style == "adobe":
        box("Flat roof", (0, 0, 3.18), (7.9, 9.6, 0.20), "Plaster", 0.06)
        for x in [-3.8, 3.8]:
            box("Parapet", (x, 0, 3.49), (0.18, 9.6, 0.44), "Plaster", 0.045)
        for y in [-4.65, 4.65]:
            box("Parapet", (0, y, 3.49), (7.6, 0.18, 0.44), "Plaster", 0.045)
    else:
        angle = math.atan2(rise, 4.15)
        length = math.hypot(rise, 4.15)
        for side in [-1, 1]:
            box(
                "Roof plane",
                (side * 2.075, 0, 3.15 + rise / 2),
                (length, 9.7, 0.14),
                roof,
                0.018,
                (0, side * angle, 0),
            )
            for y in [-4.86, 4.86]:
                box(
                    "Gable fascia",
                    (side * 2.075, y, 3.13 + rise / 2),
                    (length, 0.13, 0.25),
                    "White",
                    0.018,
                    (0, side * angle, 0),
                )
            tube(
                "Rain gutter",
                [(side * 4.14, -4.8, 3.12), (side * 4.14, 4.8, 3.12)],
                0.055,
            )
        for y in [-4.3, 4.3]:
            hull(
                "Gable panel",
                [(-3.7, y, 3.08), (3.7, y, 3.08), (0, y, 3.15 + rise)],
                [(0, 1, 2)],
                wall,
            )
        # Real standing seams add relief without a heavy tiled-roof draw budget.
        for y in [i * 0.45 - 4.7 for i in range(22)]:
            for side in [-1, 1]:
                tube(
                    "Roof seam",
                    [(0, y, 3.18 + rise), (side * 4.15, y, 3.18)],
                    0.014,
                    roof,
                )
        tube("Ridge cap", [(0, -4.88, 3.2 + rise), (0, 4.88, 3.2 + rise)], 0.07, roof)
    box("Chimney", (-2.1, -2.4, 3.8), (0.68, 0.72, 1.9), "Stone", 0.035)
    box("Chimney cap", (-2.1, -2.4, 4.78), (0.84, 0.88, 0.13), "Stone", 0.035)
    # Interior dressing stays along the walls, leaving stock loot and movement clear.
    box("Work bench", (-2.75, -2.7, 0.85), (1.1, 1.9, 0.12), "Wood", 0.02)
    for x in [-3.1, -2.4]:
        for y in [-3.4, -2.0]:
            box("Bench leg", (x, y, 0.5), (0.1, 0.1, 0.7), "Wood", 0.012)
    box("Shelf", (2.9, -2.4, 1.2), (0.7, 2.2, 0.1), "Wood", 0.014)
    save(name)
(OUT / "presentation-provenance.json").write_text(
    json.dumps(
        {
            "author": "Original Pinefall meshes",
            "generator": "Scripts/prepare_presentation_props.py",
            "license": "MIT (Pinefall repository)",
            "third_party_vehicle_model": False,
            "meshes": [
                "SM_OffroadWheel",
                "SM_TrailTruck",
                "SM_RiverBoat",
                "SM_RangerCabin",
                "SM_AlpineChalet",
                "SM_AdobeHouse",
            ],
            "notes": "Independent cosmetic wheels; open tested house footprint; world-scale UV mapping.",
        },
        indent=2,
    )
)
