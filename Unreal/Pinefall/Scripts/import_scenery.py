"""Import licensed vegetation/rocks and an OFL HUD face into owned game assets."""

from pathlib import Path
import json, unreal

PROJECT = Path(__file__).resolve().parents[1]
RAW = PROJECT / "SourceArt/Scenery"
LIB = unreal.EditorAssetLibrary
MEL = unreal.MaterialEditingLibrary
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()
DEST = "/Game/Pinefall/Art/Scenery"
MAT = "/Game/Pinefall/Materials/Scenery"
TEX = "/Game/Pinefall/Art/SceneryTextures"
for f in [DEST, MAT, TEX, "/Game/Pinefall/Art/UI"]:
    LIB.make_directory(f)


def imported(path, folder, name, factory=None):
    t = unreal.AssetImportTask()
    t.filename = str(path)
    t.destination_path = folder
    t.destination_name = name
    t.automated = True
    t.replace_existing = True
    t.save = True
    if factory:
        t.factory = factory
    TOOLS.import_asset_tasks([t])
    return t.get_objects()


def node(m, kind, **values):
    n = MEL.create_material_expression(m, kind)
    for k, v in values.items():
        n.set_editor_property(k, v)
    return n


def link(a, p, b, q):
    if not MEL.connect_material_expressions(a, p, b, q):
        raise RuntimeError("Material pin " + q)


def output(a, p, m, q):
    if not MEL.connect_material_property(a, p, q):
        raise RuntimeError("Material output " + str(q))


textures = {}
materials = {}
source = {}


def get_texture(asset, uri, normal=False, linear=False):
    name = "T_PH_" + asset + "_" + Path(uri).stem
    key = TEX + "/" + name
    if key in textures:
        return textures[key]
    t = unreal.load_asset(key)
    if not t:
        t = next(
            x
            for x in imported(RAW / asset / uri, TEX, name)
            if isinstance(x, unreal.Texture2D)
        )
    t.set_editor_property("max_texture_size", 2048)
    t.set_editor_property("srgb", not (normal or linear))
    if normal:
        t.set_editor_property(
            "compression_settings", unreal.TextureCompressionSettings.TC_NORMALMAP
        )
        t.set_editor_property("flip_green_channel", True)
    elif linear:
        t.set_editor_property(
            "compression_settings", unreal.TextureCompressionSettings.TC_MASKS
        )
    t.modify()
    LIB.save_loaded_asset(t, False)
    textures[key] = t
    return t


def make_material(asset, index, snow=False):
    key = (asset, index, snow)
    if key in materials:
        return materials[key]
    d = source[asset]
    spec = d["materials"][index]
    name = ("M_SnowFir_" + str(index)) if snow else "M_PH_" + asset + "_" + str(index)
    path = MAT + "/" + name
    m = (
        unreal.load_asset(path)
        if LIB.does_asset_exist(path)
        else TOOLS.create_asset(name, MAT, unreal.Material, unreal.MaterialFactoryNew())
    )
    m.modify()
    MEL.delete_all_material_expressions(m)
    label = spec.get("name", "").lower()
    foliage = any(x in label for x in ["leav", "twig", "grass", "bush"])
    grass = "grass_bermuda" in asset
    m.set_editor_property("two_sided", True)
    m.set_editor_property(
        "shading_model",
        (
            unreal.MaterialShadingModel.MSM_TWO_SIDED_FOLIAGE
            if foliage
            else unreal.MaterialShadingModel.MSM_DEFAULT_LIT
        ),
    )

    def sample(info, normal=False, linear=False):
        image = d["images"][d["textures"][info["index"]]["source"]]["uri"]
        tex = get_texture(asset, image, normal, linear)
        s = node(
            m,
            unreal.MaterialExpressionTextureSample,
            texture=tex,
            sampler_type=(
                unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL
                if normal
                else (
                    unreal.MaterialSamplerType.SAMPLERTYPE_MASKS
                    if linear
                    else unreal.MaterialSamplerType.SAMPLERTYPE_COLOR
                )
            ),
        )
        tr = info.get("extensions", {}).get("KHR_texture_transform", {})
        scale = tr.get("scale", [1, 1])
        offset = tr.get("offset", [0, 0])
        uv = node(
            m,
            unreal.MaterialExpressionTextureCoordinate,
            coordinate_index=info.get("texCoord", 0),
            u_tiling=scale[0],
            v_tiling=scale[1],
        )
        if offset != [0, 0]:
            add = node(m, unreal.MaterialExpressionAdd)
            link(uv, "", add, "A")
            link(
                node(
                    m,
                    unreal.MaterialExpressionConstant2Vector,
                    r=offset[0],
                    g=offset[1],
                ),
                "",
                add,
                "B",
            )
            uv = add
        link(uv, "", s, "UVs")
        return s, image

    p = spec.get("pbrMetallicRoughness", {})
    base = None
    if "baseColorTexture" in p:
        base, _ = sample(p["baseColorTexture"])
    else:
        base = node(
            m,
            unreal.MaterialExpressionConstant3Vector,
            constant=unreal.LinearColor(*p.get("baseColorFactor", [0.5, 0.5, 0.5, 1])),
        )
    if snow:
        n = node(m, unreal.MaterialExpressionVertexNormalWS)
        z = node(m, unreal.MaterialExpressionComponentMask, r=False, g=False, b=True)
        link(n, "", z, "")
        w = node(m, unreal.MaterialExpressionMultiply, const_b=0.7)
        link(z, "", w, "A")
        w2 = node(m, unreal.MaterialExpressionSaturate)
        link(w, "", w2, "")
        blend = node(m, unreal.MaterialExpressionLinearInterpolate)
        link(
            base,
            "RGB" if isinstance(base, unreal.MaterialExpressionTextureSample) else "",
            blend,
            "A",
        )
        link(
            node(
                m,
                unreal.MaterialExpressionConstant3Vector,
                constant=unreal.LinearColor(0.73, 0.79, 0.82, 1),
            ),
            "",
            blend,
            "B",
        )
        link(w2, "", blend, "Alpha")
        base = blend
    output(
        base,
        "RGB" if isinstance(base, unreal.MaterialExpressionTextureSample) else "",
        m,
        unreal.MaterialProperty.MP_BASE_COLOR,
    )
    if "normalTexture" in spec:
        normal, _ = sample(spec["normalTexture"], normal=True)
        output(normal, "RGB", m, unreal.MaterialProperty.MP_NORMAL)
    if "metallicRoughnessTexture" in p:
        packed, image = sample(p["metallicRoughnessTexture"], linear=True)
        output(packed, "G", m, unreal.MaterialProperty.MP_ROUGHNESS)
        if "_arm_" in image:
            output(packed, "R", m, unreal.MaterialProperty.MP_AMBIENT_OCCLUSION)
    else:
        output(
            node(m, unreal.MaterialExpressionConstant, r=0.86),
            "",
            m,
            unreal.MaterialProperty.MP_ROUGHNESS,
        )
    output(
        node(m, unreal.MaterialExpressionConstant, r=0.15 if foliage else 0.25),
        "",
        m,
        unreal.MaterialProperty.MP_SPECULAR,
    )
    if foliage:
        ss = node(m, unreal.MaterialExpressionMultiply, const_b=0.22)
        link(
            base,
            "RGB" if isinstance(base, unreal.MaterialExpressionTextureSample) else "",
            ss,
            "A",
        )
        output(ss, "", m, unreal.MaterialProperty.MP_SUBSURFACE_COLOR)
    if grass:
        # The scan atlas is RGB on black, not RGBA. Without this cutout the
        # plant cards render as dark polygons rather than individual blades.
        cutout = node(m, unreal.MaterialExpressionCustom,
                      code="return saturate((max(C.r,max(C.g,C.b))-.008)*80);",
                      output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT1)
        ci = unreal.CustomInput(); ci.set_editor_property("input_name", "C")
        cutout.set_editor_property("inputs", [ci]); link(base, "RGB", cutout, "C")
        m.set_editor_property("blend_mode", unreal.BlendMode.BLEND_MASKED)
        fade = node(m, unreal.MaterialExpressionPerInstanceFadeAmount)
        dither = node(
            m,
            unreal.MaterialExpressionMaterialFunctionCall,
            material_function=unreal.load_asset(
                "/Engine/Functions/Engine_MaterialFunctions02/Utility/DitherTemporalAA"
            ),
        )
        visible = node(m, unreal.MaterialExpressionMultiply)
        link(fade, "", visible, "A"); link(cutout, "", visible, "B")
        link(visible, "", dither, "Alpha Threshold")
        wind = node(m, unreal.MaterialExpressionCustom,
                    code="float bend=pow(saturate(L),2); float gust=sin(P.x*.012+P.y*.008+T*1.7)+.35*sin(P.y*.025-T*2.3); return float3(gust, .4*sin(T*1.3+P.y*.009),0)*bend*2.4;",
                    output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3)
        inputs=[]
        for label in ["L","P","T"]:
            item=unreal.CustomInput(); item.set_editor_property("input_name", label); inputs.append(item)
        wind.set_editor_property("inputs", inputs)
        link(node(m, unreal.MaterialExpressionVertexColor), "R", wind, "L")
        link(node(m, unreal.MaterialExpressionWorldPosition), "", wind, "P")
        link(node(m, unreal.MaterialExpressionTime), "", wind, "T")
        output(wind, "", m, unreal.MaterialProperty.MP_WORLD_POSITION_OFFSET)
        output(dither, "", m, unreal.MaterialProperty.MP_OPACITY_MASK)
    MEL.set_material_usage(m, unreal.MaterialUsage.MATUSAGE_INSTANCED_STATIC_MESHES)
    if not grass:
        MEL.set_material_usage(m, unreal.MaterialUsage.MATUSAGE_NANITE)
    MEL.recompile_material(m)
    LIB.save_loaded_asset(m, False)
    materials[key] = m
    return m


rows = json.loads((PROJECT / "SourceArt/Prepared/Scenery/manifest.json").read_text())
report = []
material_only = "-PFSceneryMaterialsOnly" in unreal.SystemLibrary.get_command_line()
for row in rows:
    if "-PFGrassOnly" in unreal.SystemLibrary.get_command_line() and "GrassPatch" not in row["name"]:
        continue
    asset = row["source"]
    if asset not in source:
        source[asset] = json.loads((RAW / asset / (asset + ".gltf")).read_text())
    name = row["name"]
    if (
        not material_only
        and "-PFSceneryResume" in unreal.SystemLibrary.get_command_line()
        and LIB.does_asset_exist(DEST + "/" + name)
        and "GrassPatch" not in name
    ):
        mesh = unreal.load_asset(DEST + "/" + name)
        report.append(
            {
                "name": name,
                "nanite_enabled": bool(
                    mesh.get_editor_property("nanite_settings").enabled
                ),
                "built_nanite_data": unreal.PFWorldBuilder.has_built_nanite_data(mesh),
                "bounds_cm": str(mesh.get_bounding_box()),
                "materials": len(mesh.get_editor_property("static_materials")),
            }
        )
        continue
    objects = (
        [unreal.load_asset(DEST + "/" + name)]
        if material_only
        else imported(
            PROJECT / "SourceArt/Prepared/Scenery" / (name + ".glb"), DEST, name
        )
    )
    candidates = [o for o in objects if isinstance(o, unreal.StaticMesh)]
    if not candidates:
        candidates = [
            unreal.load_asset(p)
            for p in LIB.list_assets(DEST + "/" + name, True)
            if isinstance(unreal.load_asset(p), unreal.StaticMesh)
        ]
    mesh = next((o for o in candidates if o.get_name() == name), None)
    if not mesh:
        raise RuntimeError("Missing imported mesh " + name)
    if mesh.get_path_name().split(".")[0] != DEST + "/" + name:
        if not LIB.rename_asset(mesh.get_path_name(), DEST + "/" + name):
            raise RuntimeError("Canonical mesh " + name)
        mesh = unreal.load_asset(DEST + "/" + name)
    lookup = {
        "PH_" + asset + "_" + m.get("name", ""): i
        for i, m in enumerate(source[asset]["materials"])
    }
    mesh.modify()
    for i, slot in enumerate(mesh.get_editor_property("static_materials")):
        label = str(slot.material_slot_name)
        if label not in lookup:
            raise RuntimeError(
                "Unexpected material slot "
                + name
                + " "
                + label
                + " "
                + str(list(lookup))
            )
        mesh.set_material(i, make_material(asset, lookup[label]))
    # Dense opaque/modeled foliage, cliff faces and rocks benefit from Nanite.
    # Short grass keeps ordinary raster instance distance fades and no shadows.
    nanite = "GrassPatch" not in name
    if not material_only:
        settings = mesh.get_editor_property("nanite_settings")
        settings.enabled = nanite
        settings.preserve_area = True
        settings.fallback_target = unreal.NaniteFallbackTarget.PERCENT_TRIANGLES
        settings.fallback_percent_triangles = 0.12
        mesh.set_editor_property("nanite_settings", settings)
        unreal.PFWorldBuilder.configure_mesh(mesh, nanite)
        if "Rock" in name or "Cliff" in name:
            unreal.PFWorldBuilder.configure_collision(mesh, True)
    LIB.save_loaded_asset(mesh, False)
    report.append(
        {
            "name": name,
            "nanite_enabled": bool(mesh.get_editor_property("nanite_settings").enabled),
            "built_nanite_data": unreal.PFWorldBuilder.has_built_nanite_data(mesh),
            "bounds_cm": str(mesh.get_bounding_box()),
            "materials": len(mesh.get_editor_property("static_materials")),
        }
    )
    unreal.log("PF_SCENERY_IMPORTED " + name)
for index in range(len(source.get("fir_tree_01", {}).get("materials", []))):
    make_material("fir_tree_01", index, True)
fontpath = PROJECT / "SourceArt/UI/Fonts/BarlowCondensed-SemiBold.ttf"
font = unreal.PFWorldBuilder.create_display_font_from_file(str(fontpath))
if not font:
    raise RuntimeError("Display font import failed")
LIB.save_loaded_asset(unreal.load_asset("/Game/Pinefall/Art/UI/F_PinefallFace"), False)
LIB.save_loaded_asset(font, False)
# Legacy animal imports also need their skeletal usage saved, rather than
# compiling a missing permutation the first time each species appears in play.
for path in LIB.list_assets("/Game/Pinefall/Art/Fauna", True):
    m = unreal.load_asset(path)
    if isinstance(m, unreal.Material):
        m.modify()
        MEL.set_material_usage(m, unreal.MaterialUsage.MATUSAGE_SKELETAL_MESH)
        MEL.recompile_material(m)
        LIB.save_loaded_asset(m, False)
if not unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True):
    raise RuntimeError("Scenery save failed")
(PROJECT / "Saved/SceneryImport.json").write_text(json.dumps(report, indent=2))
unreal.log("PF_SCENERY_IMPORT_OK meshes=" + str(len(report)))
