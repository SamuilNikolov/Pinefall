import unreal,json
from pathlib import Path
mesh=unreal.load_asset('/Game/Pinefall/Art/Characters/Ranger/FieldRanger')
rows=[]
for slot in mesh.get_editor_property('materials'):
 m=slot.material_interface
 row={'slot':str(slot.material_slot_name),'material':m.get_path_name() if m else None}
 if isinstance(m,unreal.MaterialInstanceConstant):
  row['parent']=m.parent.get_path_name() if m.parent else None
  row['textures']=[{'name':str(x.parameter_info.name),'value':x.parameter_value.get_path_name() if x.parameter_value else None} for x in m.get_editor_property('texture_parameter_values')]
 rows.append(row)
Path(unreal.Paths.project_saved_dir(),'CharacterMaterials.json').write_text(json.dumps(rows,indent=2))
unreal.log('PF_CHARACTER_AUDIT_OK')
