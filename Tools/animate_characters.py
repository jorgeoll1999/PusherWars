import bpy, math, os, json
from mathutils import Vector
BASE=r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes'
s=bpy.context.scene;s.render.fps=30
specs={'Idle':90,'Run':24,'Cast01':30,'Cast02':30,'Cast03':30,'Cast04':30,'Death':60}
report=[]
def smooth(a):
 a=max(0,min(1,a));return a*a*(3-2*a)
for rig in [o for o in s.objects if o.type=='ARMATURE']:
 name=rig.name.replace('_Rig',''); pos=rig.location.copy()
 rig.animation_data_clear();rig.animation_data_create()
 actions=[]
 for clip,frames in specs.items():
  old=bpy.data.actions.get(name+'_'+clip)
  if old:bpy.data.actions.remove(old)
  action=bpy.data.actions.new(name+'_'+clip);action.use_fake_user=True;rig.animation_data.action=action
  for frame in list(range(1,frames+2,2))+([frames+1] if frames%2 else []):
   t=(frame-1)/frames;p=t*math.tau
   for b in rig.pose.bones:b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0);b.scale=(1,1,1)
   def rot(n,x=0,y=0,z=0):rig.pose.bones[n].rotation_euler=tuple(math.radians(v) for v in (x,y,z))
   for side,sign in [('Left',1),('Right',-1)]:
    rot(side+'UpperArm',-67,0,sign*4);rot(side+'LowerArm',0,0,sign*12)
   rot('Chest',2+math.sin(p)*1.2)
   rig.pose.bones['Hips'].location.y=.006*math.sin(p)
   rot('CapeHair',math.sin(p-.5)*2)
   if clip=='Run':
    rig.pose.bones['Hips'].location.y=.025*(1-math.cos(2*p))
    rot('Chest',-7,math.sin(p)*4)
    for side,sign in [('Left',1),('Right',-1)]:
     stride=math.sin(p)*sign
     rot(side+'UpperLeg',stride*32)
     rot(side+'LowerLeg',max(0,-stride)*55+6)
     rot(side+'Foot',-max(0,stride)*18)
     rot(side+'UpperArm',-65,0,sign*(15+stride*26))
     rot(side+'LowerArm',0,0,sign*48)
     rot('Skirt'+side,stride*10,0,sign*4)
     rot('Skirt'+side+'Tip',math.sin(p*2-.5)*8)
    rot('CapeHair',-9+math.sin(2*p)*5);rot('SkirtBack',-12+math.sin(2*p)*8)
   elif clip.startswith('Cast'):
    # All four variants release at frame 13 (0.4 s), allowing a shared gameplay timing.
    wind=smooth(t/.30);release=smooth((t-.30)/.10);recover=1-smooth((t-.50)/.50)
    charge=wind*(1-release);hit=release*recover
    v=int(clip[-1]);rot('Chest',2+charge*7-hit*6,(v-2.5)*hit*7)
    if v==1:
     rot('RightUpperArm',-67+charge*35+hit*45,charge*15,-charge*35-hit*65)
     rot('RightLowerArm',0,0,-12-charge*90+hit*10)
     rot('LeftUpperArm',-55,0,15+charge*20)
    elif v==2:
     for side,sign in [('Left',1),('Right',-1)]:
      rot(side+'UpperArm',-67+charge*35+hit*45,0,sign*(charge*30+hit*65))
      rot(side+'LowerArm',0,0,sign*(12+charge*75))
    elif v==3:
     rot('RightUpperArm',-67+charge*175+hit*50,0,-hit*65)
     rot('RightLowerArm',0,0,-12-charge*40)
     rot('LeftUpperArm',-67+charge*40,0,20)
     rot('Chest',2-charge*12+hit*18)
    else:
     rot('RightUpperArm',-40+charge*20,0,-charge*110+hit*15)
     rot('RightLowerArm',0,0,-15-charge*35)
     rot('LeftUpperArm',-67+hit*40,0,hit*65)
     rot('Chest',2,charge*25-hit*25)
    rot('CapeHair',-hit*8);rot('SkirtFront',hit*5)
   elif clip=='Death':
    fall=smooth((t-.15)/.6);flinch=math.sin(min(1,t/.3)*math.pi)
    rig.pose.bones['Hips'].location.y=-.72*fall
    rot('Hips',-82*fall);rot('Chest',12*flinch-7*fall);rot('Head',8*fall)
    for side,sign in [('Left',1),('Right',-1)]:
     rot(side+'UpperArm',-67+fall*45,0,sign*fall*15)
     rot(side+'LowerArm',0,0,sign*(12+20*fall))
     rot(side+'UpperLeg',0);rot(side+'LowerLeg',8*fall)
     rot('Skirt'+side,-12*fall)
    # Correct ground penetration of the evaluated skinned mesh, including clothing.
    rig.animation_data.action=None
    bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
    lowest=0
    for child in rig.children:
     if child.type=='MESH':
      ev=child.evaluated_get(dg);mesh=ev.to_mesh();lowest=min(lowest,min((child.matrix_world@v.co).z for v in mesh.vertices));ev.to_mesh_clear()
    rig.pose.bones['Hips'].location.y-=lowest
    rig.animation_data.action=action
   for b in rig.pose.bones:
    b.keyframe_insert('rotation_euler',frame=frame,group=b.name)
    b.keyframe_insert('location',frame=frame,group=b.name)
  actions.append((clip,action,frames))
 rig.animation_data.action=None
 for clip,action,frames in actions:
  track=rig.animation_data.nla_tracks.new();track.name=clip
  strip=track.strips.new(clip,1,action);strip.name=clip;strip.action_frame_start=1;strip.action_frame_end=frames+1
 rig.location=(0,0,0)
 for b in rig.pose.bones:b.rotation_euler=(0,0,0);b.location=(0,0,0)
 s.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
 for child in rig.children:
  if child.type=='MESH':child.select_set(True)
 bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=os.path.join(BASE,'Assets','Characters',name,name+'.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY')
 for track in rig.animation_data.nla_tracks:track.mute=True
 rig.animation_data.action=actions[0][1];rig.location=pos
 report.append({'character':name,'clips':specs,'fps':30,'cast_release_seconds':.4})
s.frame_start=1;s.frame_end=91;s.frame_set(1)
with open(os.path.join(BASE,'ArtSource','Characters','AnimationReport.json'),'w') as f:json.dump(report,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BASE,'ArtSource','Characters','Personajes_Animated.blend'))
print('Created 28 animation clips for four characters')
