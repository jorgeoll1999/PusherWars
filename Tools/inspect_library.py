import bpy,json
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes\ArtSource\AnimationLibrary\Quaternius\Animation Library[Standard]\Unity\AnimationLibrary_Unity_Standard.fbx')
report={'objects':[(o.name,o.type) for o in bpy.data.objects],'actions':[(a.name,list(a.frame_range)) for a in bpy.data.actions],'bones':{o.name:[b.name for b in o.data.bones] for o in bpy.data.objects if o.type=='ARMATURE'}}
open(r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes\ArtSource\AnimationLibrary\Inspection.json','w').write(json.dumps(report,indent=2));print(report)
