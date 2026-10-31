from pathlib import Path
import json, urllib.request, hashlib, copy, time

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Unreal/Pinefall/SourceArt/Scenery"
OUT.mkdir(parents=True, exist_ok=True)
HEAD = {"User-Agent": "Pinefall/0.4 asset preparation (Powered by Poly Haven)"}


def request(url, start=None, end=None):
    h = dict(HEAD)
    if start is not None:
        h["Range"] = f"bytes={start}-{end}"
    for attempt in range(3):
        try:
            with urllib.request.urlopen(
                urllib.request.Request(url, headers=h), timeout=120
            ) as r:
                if start is not None and r.status != 206:
                    raise RuntimeError("Range request not honored " + url)
                return r.read()
        except Exception:
            if attempt == 2:
                raise
            time.sleep(2 + attempt)


def fetch_asset(name, chosen=None):
    folder = OUT / name
    folder.mkdir(exist_ok=True)
    manifest = json.loads(request("https://api.polyhaven.com/files/" + name))
    entry = manifest["gltf"]["2k"]["gltf"]
    source = request(entry["url"])
    assert hashlib.md5(source).hexdigest() == entry["md5"]
    d = json.loads(source)
    nodes = [
        x for x in d["nodes"] if "mesh" in x and (not chosen or x.get("name") in chosen)
    ]
    meshes = sorted({n["mesh"] for n in nodes})
    materials = sorted(
        {p["material"] for i in meshes for p in d["meshes"][i]["primitives"]}
    )
    accessors = sorted(
        {
            a
            for i in meshes
            for p in d["meshes"][i]["primitives"]
            for a in list(p["attributes"].values()) + [p["indices"]]
        }
    )
    views = sorted({d["accessors"][i]["bufferView"] for i in accessors})
    blob = bytearray()
    range_manifest = []
    newviews = []
    binname = d["buffers"][0]["uri"]
    info = entry["include"][binname]
    # Download only selected variant buffers. Large source forests contain several
    # multi-million-vertex alternatives; unrelated variants aren't normal build inputs.
    for i in views:
        view = d["bufferViews"][i]
        start = view.get("byteOffset", 0)
        length = view["byteLength"]
        cached = folder / ("range_" + str(i) + ".bin")
        raw = (
            cached.read_bytes()
            if cached.exists() and cached.stat().st_size == length
            else request(info["url"], start, start + length - 1)
        )
        assert len(raw) == length
        cached.write_bytes(raw)
        while len(blob) % 4:
            blob.append(0)
        v = dict(view)
        v["buffer"] = 0
        v["byteOffset"] = len(blob)
        newviews.append(v)
        blob.extend(raw)
        range_manifest.append(
            {
                "buffer_view": i,
                "offset": start,
                "bytes": length,
                "sha256": hashlib.sha256(raw).hexdigest(),
            }
        )
    av = {old: new for new, old in enumerate(accessors)}
    vv = {old: new for new, old in enumerate(views)}
    mm = {old: new for new, old in enumerate(meshes)}
    matmap = {old: new for new, old in enumerate(materials)}
    selected = []
    for i in meshes:
        mesh = copy.deepcopy(d["meshes"][i])
        for p in mesh["primitives"]:
            p["attributes"] = {k: av[v] for k, v in p["attributes"].items()}
            p["indices"] = av[p["indices"]]
            p["material"] = matmap[p["material"]]
        selected.append(mesh)
    newaccess = []
    for i in accessors:
        a = copy.deepcopy(d["accessors"][i])
        a["bufferView"] = vv[a["bufferView"]]
        newaccess.append(a)
    # Keep original material/texture definitions and all texture files so extension
    # references and UV transform metadata remain intact during Blender preparation.
    d["meshes"] = selected
    d["accessors"] = newaccess
    d["bufferViews"] = newviews
    d["buffers"] = [{"uri": name + ".bin", "byteLength": len(blob)}]
    d["materials"] = [d["materials"][i] for i in materials]
    for n in nodes:
        n["mesh"] = mm[n["mesh"]]
        n.pop("children", None)
    d["nodes"] = nodes
    d["scenes"] = [{"nodes": list(range(len(nodes)))}]
    d["scene"] = 0
    (folder / (name + ".bin")).write_bytes(blob)
    (folder / (name + ".gltf")).write_text(
        json.dumps(d, separators=(",", ":")), encoding="utf-8"
    )
    textures = []
    for image in d.get("images", []):
        uri = image.get("uri")
        if not uri:
            continue
        meta = entry["include"][uri]
        target = folder / uri
        target.parent.mkdir(parents=True, exist_ok=True)
        raw = target.read_bytes() if target.exists() else request(meta["url"])
        assert hashlib.md5(raw).hexdigest() == meta["md5"]
        target.write_bytes(raw)
        textures.append(
            {
                "path": uri,
                "url": meta["url"],
                "md5": meta["md5"],
                "sha256": hashlib.sha256(raw).hexdigest(),
            }
        )
    # Range cache is transient duplicate data and is not a required source input.
    for f in folder.glob("range_*.bin"):
        f.unlink()
    (folder / "provenance.json").write_text(
        json.dumps(
            {
                "asset": name,
                "source_page": "https://polyhaven.com/a/" + name,
                "license": "CC0-1.0",
                "license_url": "https://polyhaven.com/license",
                "gltf_url": entry["url"],
                "upstream_gltf_md5": entry["md5"],
                "original_buffer_url": info["url"],
                "original_buffer_md5": info["md5"],
                "selected_nodes": chosen or "all",
                "selected_ranges": range_manifest,
                "textures": textures,
            },
            indent=2,
        )
    )
    print("PF_SCENERY_DOWNLOADED", name, len(blob), flush=True)


if __name__ == "__main__":
    for name, chosen in [
        ("fir_tree_01", ["fir_tree_01_c_LOD0"]),
        ("tree_small_02", None),
        ("island_tree_02", None),
        ("quiver_tree_01", None),
        ("grass_bermuda_01", None),
        ("wild_rooibos_bush", None),
        ("rock_moss_set_01", None),
        ("rock_face_02", None),
    ]:
        fetch_asset(name, chosen)
