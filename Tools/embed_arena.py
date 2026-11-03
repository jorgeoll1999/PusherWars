import bpy, os
ROOT=r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes'
scene=bpy.context.scene
sea=scene.objects['Lava_Abyss']
delta=-.205-sea.location.z
for o in list(scene.objects):
    if o.name.startswith(('Lava_Current','Cooled_Lava')) or o==sea:o.location.z+=delta
    if o.name.startswith(('Lava_Fall_','Lava_Fall_Lip_')):
        o.hide_render=True;o.hide_set(True)
    if o.name.startswith('Floating_Debris'):
        # The old floating rocks become distant rocky outcrops in the lava.
        o.location.z=-.6
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
    if o.type=='MESH' and not o.hide_render:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(ROOT,'Assets/Arena/Models/Caldera_Arena.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'ArtSource/Caldera_Arena.blend'))
print('Lava surface at -0.08m, platform at 0m; rock embedded.')
