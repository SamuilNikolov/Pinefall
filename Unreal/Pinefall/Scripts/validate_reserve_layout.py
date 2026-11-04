"""Check the exported geography before accepting regenerated reserve assets.

This checks authored data, not rendered FPS, car handling or capsule collision.
Run the native reserve integration and real presentation captures separately.
"""

from pathlib import Path
import json
import math

project = Path(__file__).resolve().parents[1]
data = json.loads((project / "Saved/ReserveBuild.json").read_text())
checks = []


def check(condition, description):
    if not condition:
        raise RuntimeError(description)
    checks.append(description)


heights = data["heights"]
check(
    len(heights) == 505 * 505 and all(math.isfinite(z) for z in heights),
    "Finite complete Landscape height samples",
)
check(
    min(heights) > -25600 and max(heights) < 25599,
    "Terrain fits native height encoding without clipping",
)
check(max(heights) - min(heights) > 15000, "Over 150 metres of terrain relief")


def sampled_height(x, y):
    gx, gy = (x + 100800) / 400, (y + 100800) / 400
    ix, iy = max(0, min(503, int(gx))), max(0, min(503, int(gy)))
    u, v = gx - ix, gy - iy
    a, b = heights[iy * 505 + ix : iy * 505 + ix + 2]
    c, d = heights[(iy + 1) * 505 + ix : (iy + 1) * 505 + ix + 2]
    return a + (b - a) * u + (d - b) * v if u > v else a + (d - c) * u + (c - a) * v


banks = [
    sampled_height(
        30000 + math.cos(i * math.pi / 4) * 22000 * 1.05,
        30000 + math.sin(i * math.pi / 4) * 17000 * 1.05,
    )
    for i in range(8)
]
check(
    min(banks) > -500 and max(banks) < 300,
    "Low lake banks remain close to water rather than forming mountain crater walls",
)
check(
    len(data["sites"]) == 8 and len(data["buildings"]) == 64,
    "Eight settlements and all 64 enterable houses retained",
)
check(
    len(data["road_paths"]) == len(data["roads"]) == 11,
    "All eleven town links have authored routes",
)
for index, (path, link) in enumerate(zip(data["road_paths"], data["roads"])):
    start, end = (data["sites"][i] for i in link)
    check(
        math.dist(path[0], [start["x"], start["y"]]) < 1
        and math.dist(path[-1], [end["x"], end["y"]]) < 1,
        f"Road {index} connects its named settlements",
    )
    check(
        all(
            ((x - 30000) / 22000) ** 2 + ((y - 30000) / 17000) ** 2 > 1.02**2
            for x, y in path
        ),
        f"Road {index} stays outside navigable lake",
    )
    check(
        all(math.dist(a, b) < 401 for a, b in zip(path, path[1:])),
        f"Road {index} has continuous short geometry segments",
    )
metrics = data["road_metrics"]
check(
    metrics["max_center_cut_fill_cm"] <= 160.01,
    "Road grading cannot create an endpoint-height embankment",
)
check(metrics["max_grade"] < 0.32, "No authored road centre exceeds 32 percent grade")
check(
    metrics["route_length_cm"] > metrics["straight_length_cm"] * 1.15,
    "Routes follow terrain instead of straight highway links",
)
result = {
    "passed": checks,
    "failed": [],
    "road_metrics": metrics,
    "minimum_height_cm": min(heights),
    "maximum_height_cm": max(heights),
    "scope": "Exported authoring geometry; separate runtime/collision/render checks required",
}
(project / "Saved/ReserveLayoutValidation.json").write_text(
    json.dumps(result, indent=2)
)
print(f"PF_RESERVE_LAYOUT_OK checks={len(checks)}")
