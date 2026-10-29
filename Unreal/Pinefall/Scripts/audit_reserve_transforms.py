import unreal,json
from pathlib import Path
unreal.log('PF_ROTATOR_TEST '+str(unreal.Rotator(0,90,0)))
unreal.EditorLoadingAndSavingUtils.load_map('/Game/Pinefall/Maps/WildfrontReserve')
for actor in unreal.get_editor_subsystem(unreal.EditorActorSubsystem).get_all_level_actors():
 if isinstance(actor,unreal.PFReserveWorld):
  unreal.log('PF_BOAT_SAVED '+str(actor.get_editor_property('boat_starts')))
 if isinstance(actor,unreal.PFWaterBody):unreal.log('PF_WATER_SAVED '+str(actor.get_editor_property('water_bounds').get_scaled_box_extent()))
