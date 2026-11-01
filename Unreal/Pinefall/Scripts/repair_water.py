"""Repair the reserve lake using existing geometry and texture-free water shading."""
import unreal

lib = unreal.EditorAssetLibrary
mel = unreal.MaterialEditingLibrary
material = unreal.load_asset('/Game/Pinefall/Materials/M_LakeSurface')
if not material:
    raise RuntimeError('Missing lake material')
# UE 5.6's helper removes entries while iterating the expression collection.
# Drain it completely; otherwise a second run can retain a water output node.
for _ in range(32):
    if mel.get_num_material_expressions(material) == 0:
        break
    mel.delete_all_material_expressions(material)
else:
    raise RuntimeError('Could not clear the water graph')
material.set_editor_property('blend_mode', unreal.BlendMode.BLEND_OPAQUE)
material.set_editor_property('shading_model', unreal.MaterialShadingModel.MSM_SINGLE_LAYER_WATER)
material.set_editor_property('two_sided', True)
material.set_editor_property('tangent_space_normal', False)


def node(kind, **values):
    result = mel.create_material_expression(material, kind)
    for key, value in values.items():
        result.set_editor_property(key, value)
    return result


def constant(value):
    if isinstance(value, tuple):
        return node(unreal.MaterialExpressionConstant3Vector, constant=unreal.LinearColor(*value, 1))
    return node(unreal.MaterialExpressionConstant, r=value)


def wire(source, target, name):
    if not mel.connect_material_expressions(source, '', target, name):
        raise RuntimeError('Water connection: ' + name)


for value, output in [((.012, .065, .09), unreal.MaterialProperty.MP_BASE_COLOR),
                      (.12, unreal.MaterialProperty.MP_ROUGHNESS),
                      (.5, unreal.MaterialProperty.MP_SPECULAR),
                      (.25, unreal.MaterialProperty.MP_OPACITY)]:
    mel.connect_material_property(constant(value), '', output)
water = node(unreal.MaterialExpressionSingleLayerWaterMaterialOutput)
wire(constant((.0004, .0012, .0018)), water, 'ScatteringCoefficients')
wire(constant((.008, .0025, .0015)), water, 'AbsorptionCoefficients')
wire(constant(.2), water, 'PhaseG')
wire(constant((1., 1., 1.)), water, 'ColorScaleBehindWater')

normal = node(unreal.MaterialExpressionCustom,
              output_type=unreal.CustomMaterialOutputType.CMOT_FLOAT3,
              code='''float2 q=P.xy*.005;
float warp=.65*sin(dot(q,float2(.13,.29))+T*.13)+.35*sin(dot(q,float2(-.31,.17))-T*.07);
float a=dot(q,float2(.7,1.4))+T*.8+warp;
float b=dot(q,float2(-1.9,.6))-T*1.07+warp*.5;
float c=dot(q,float2(4.1,2.7))+T*1.4+sin(a)*.4;
float2 slope=float2(.02,.04)*cos(a)+float2(-.018,.005)*cos(b)+float2(.008,.005)*cos(c);
return normalize(float3(slope,1));''')
inputs = []
for name in ['P', 'T']:
    item = unreal.CustomInput()
    item.set_editor_property('input_name', name)
    inputs.append(item)
normal.set_editor_property('inputs', inputs)
wire(node(unreal.MaterialExpressionWorldPosition), normal, 'P')
wire(node(unreal.MaterialExpressionTime), normal, 'T')
mel.connect_material_property(normal, '', unreal.MaterialProperty.MP_NORMAL)
mel.recompile_material(material)
lib.save_loaded_asset(material)

unreal.EditorLoadingAndSavingUtils.load_map('/Game/Pinefall/Maps/WildfrontReserve')
for name in ['M_ReserveTerrain', 'M_ReserveGeology']:
    surface_material = unreal.load_asset('/Game/Pinefall/Materials/' + name)
    if surface_material:
        mel.set_material_usage(surface_material, unreal.MaterialUsage.MATUSAGE_NANITE)
        mel.recompile_material(surface_material)
        lib.save_loaded_asset(surface_material)
actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem).get_all_level_actors()
mesh = unreal.load_asset('/Game/Pinefall/Art/World/SM_Lake/StaticMeshes/SM_Lake')
if not mesh:
    raise RuntimeError('Missing actual imported water mesh')
# Single Layer Water is not a Nanite shading model; this 128-triangle disk
# benefits from ordinary raster rendering and keeps the same physical volume.
unreal.PFWorldBuilder.configure_mesh(mesh, False)
lib.save_loaded_asset(mesh)
count = 0
for actor in actors:
    if isinstance(actor, unreal.PFWaterBody):
        actor.surface.set_static_mesh(mesh)
        actor.surface.set_material(0, material)
        # Native OnConstruction/BeginPlay fits mesh bounds to the water volume.
        count += 1
unreal.PFWorldBuilder.finish_rendering_preparation()
if not unreal.PFWorldBuilder.build_reserve_nanite():
    raise RuntimeError('Reserve terrain Nanite data must match the current materials')
unreal.EditorLoadingAndSavingUtils.save_current_level()
if count != 1:
    raise RuntimeError('Expected one reserve lake: ' + str(count))
unreal.log('PF_WATER_REPAIR_OK surfaces=1 shading=single_layer normal_textures=0')
