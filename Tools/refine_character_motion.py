import bpy,math,os,json
from mathutils import Vector,Quaternion,Matrix
BASE=r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes'
scene=bpy.context.scene;scene.render.fps=30
specs={'Idle':90,'Run':18,'Cast01':30,'Cast02':30,'Cast03':30,'Cast04':30,'Death':60}
report=[]
def smooth(t):
 t=max(0,min(1,t));return t*t*(3-2*t)
for rig in [o for o in scene.objects if o.type=='ARMATURE']:
 name=rig.name.replace('_Rig','');rig.animation_data_clear();rig.animation_data_create();rig.data.pose_position='POSE';original_position=rig.location.copy()
 # Repair the hand/forearm skinning transition without modifying UVs or geometry.
 repaired=0
 for obj in [o for o in rig.children if o.type=='MESH']:
  for v in obj.data.vertices:
   x=abs(v.co.x)
   if x<.60 or v.co.z<1.30:continue
   side='Left' if v.co.x>0 else 'Right'
   if not any(obj.vertex_groups[g.group].name in [side+'UpperArm',side+'LowerArm',side+'Hand'] for g in v.groups):continue
   for group in obj.vertex_groups:group.remove([v.index])
   hand=smooth((x-.62)/.065)
   obj.vertex_groups[side+'Hand'].add([v.index],hand,'REPLACE')
   obj.vertex_groups[side+'LowerArm'].add([v.index],1-hand,'REPLACE');repaired+=1
 bpy.context.view_layer.update()
 def rotate(n,axis,degrees):
  pb=rig.pose.bones[n];rest=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=rest.inverted()@Quaternion(Vector(axis),math.radians(degrees))@rest
 def chain(upper,lower,end,target,pole,flat=False):
  bpy.context.view_layer.update();u=rig.pose.bones[upper];l=rig.pose.bones[lower];e=rig.pose.bones[end];hip=u.head.copy();delta=Vector(target)-hip
  length=min(delta.length,u.length+l.length-.006);direction=delta.normalized();a=(u.length*u.length-l.length*l.length+length*length)/(2*length);height=math.sqrt(max(0,u.length*u.length-a*a))
  bend=Vector(pole)-hip;bend-=direction*bend.dot(direction);bend.normalize();knee=hip+direction*a+bend*height;ankle=hip+direction*length
  for pb,head,tail in [(u,hip,knee),(l,knee,ankle)]:
   restdir=pb.bone.tail_local-pb.bone.head_local;q=restdir.rotation_difference(tail-head)@pb.bone.matrix_local.to_quaternion();pb.matrix=Matrix.LocRotScale(head,q,Vector((1,1,1)));bpy.context.view_layer.update()
  q=e.bone.matrix_local.to_quaternion() if flat else l.matrix.to_quaternion()@l.bone.matrix_local.to_quaternion().inverted()@e.bone.matrix_local.to_quaternion()
  e.matrix=Matrix.LocRotScale(ankle,q,Vector((1,1,1)));bpy.context.view_layer.update()
 actions=[]
 for clip,frames in specs.items():
  old=bpy.data.actions.get(name+'_'+clip)
  if old:bpy.data.actions.remove(old)
  action=bpy.data.actions.new(name+'_'+clip);action.use_fake_user=True
  # No active action during authored pose evaluation: avoids evaluating the previous sample over a new pose.
  rig.animation_data.action=None
  for frame in range(1,frames+2):
   t=(frame-1)/frames;p=t*math.tau
   for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)
   rotate('Chest',(1,0,0),math.sin(p)*.6)
   rotate('CapeHair',(1,0,0),math.sin(p-.5)*1.3)
   rig.pose.bones['Hips'].location.y=-.035
   if clip=='Run':
    rig.pose.bones['Hips'].location.y=-.10+.025*math.cos(2*p-.8*math.pi)
    rotate('Hips',(1,0,0),6)
    rotate('Chest',(1,0,0),5)
   for side,sign in [('Left',1),('Right',-1)]:
    wrist=Vector((sign*.27,-.075,1.09));ankle=rig.data.bones[side+'Foot'].head_local.copy()
    if clip=='Run':
     phase=(t+(0 if sign==1 else .5))%1
     # A true run: each stance occupies 40% of a cycle, leaving flight between supports.
     # 0.76 m of stance travel / (0.6 s * 0.4) = 3.17 m/s reference speed.
     if phase<.4:ankle.y=-.38+phase/.4*.76
     else:
      u=(phase-.4)/.6;ankle.y=.38-.76*smooth(u);ankle.z+=.28*math.sin(math.pi*u)**1.2
     wrist.y=.02+math.cos(p)*sign*.23;wrist.z=1.13+.025*math.cos(p*2)
     rotate('Skirt'+side,(1,0,0),-math.sin(p)*sign*5)
     rotate('Skirt'+side+'Tip',(1,0,0),math.sin(p*2-.5)*4)
    if clip.startswith('Cast'):
     v=int(clip[-1]);wind=smooth(t/.30);hit=smooth((t-.30)/.10)*(1-smooth((t-.50)/.50));charge=wind*(1-smooth((t-.30)/.10))
     if v==1 and side=='Right':wrist=Vector((sign*(.27-.14*hit),-.075-.12*charge-.36*hit,1.09+.22*charge+.33*hit))
     elif v==2:wrist=Vector((sign*(.27-.10*hit),-.075-.14*charge-.34*hit,1.09+.25*charge+.30*hit))
     elif v==3 and side=='Right':wrist=Vector((sign*.27,-.075-.36*hit,1.09+.79*charge+.28*hit))
     elif v==4:wrist=Vector((sign*(.27+.12*hit),-.075-.34*hit,1.09+.20*charge+.28*hit))
     rotate('CapeHair',(1,0,0),-hit*4)
    if clip!='Death':
     bpy.context.view_layer.update()
     chain(side+'UpperLeg',side+'LowerLeg',side+'Foot',ankle,Vector((sign*.2,-1,.55)),True)
     chain(side+'UpperArm',side+'LowerArm',side+'Hand',wrist,Vector((sign*.32,.32,1.15)) if clip=='Run' else Vector((sign*.7,.03,1.0)))
     if clip in ('Idle','Run'):
      hand=rig.pose.bones[side+'Hand'];q=Quaternion(Vector((1,0,0)),math.radians((-12*math.sin(p)*sign) if clip=='Run' else -8))@Quaternion(Vector((0,1,0)),math.radians(sign*82))@hand.bone.matrix_local.to_quaternion()
      hand.matrix=Matrix.LocRotScale(hand.head.copy(),q,Vector((1,1,1)));bpy.context.view_layer.update()
    else:
     fall=smooth((t-.15)/.6);rig.pose.bones['Hips'].location.y=-.72*fall
     rotate('Hips',(1,0,0),-82*fall);rotate(side+'UpperArm',(0,1,0),sign*(72-25*fall));rotate(side+'LowerArm',(1,0,0),-12)
   if clip=='Death':
    bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();lowest=0
    for obj in rig.children:
     if obj.type=='MESH':
      ev=obj.evaluated_get(dg);mesh=ev.to_mesh();lowest=min(lowest,min((obj.matrix_world@v.co).z for v in mesh.vertices));ev.to_mesh_clear()
    rig.pose.bones['Hips'].location.y-=lowest
   rig.animation_data.action=action
   for b in rig.pose.bones:
    b.keyframe_insert('rotation_quaternion',frame=frame,group=b.name);b.keyframe_insert('location',frame=frame,group=b.name)
   rig.animation_data.action=None
  actions.append((clip,action,frames))
 rig.animation_data.action=None
 for clip,action,frames in actions:
  track=rig.animation_data.nla_tracks.new();track.name=clip;strip=track.strips.new(clip,1,action);strip.action_frame_start=1;strip.action_frame_end=frames+1
 rig.location=(0,0,0)
 for b in rig.pose.bones:b.rotation_quaternion=Quaternion();b.location=(0,0,0)
 scene.frame_set(1);bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
 for obj in rig.children:
  if obj.type=='MESH':obj.select_set(True)
 bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=BASE+'/Assets/Characters/'+name+'/'+name+'.fbx',use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY')
 for track in rig.animation_data.nla_tracks:track.mute=True
 rig.animation_data.action=actions[0][1];rig.location=original_position
 report.append({'character':name,'hand_forearm_vertices_reweighted':repaired,'clips':7,'authored_fps':30,'method':'two-bone IK baked to quaternion FK; flat planted feet; forearm-aligned wrists'})
scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
with open(BASE+'/ArtSource/Characters/Review/MotionRepair.json','w') as f:json.dump(report,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=BASE+'/ArtSource/Characters/Personajes_Refined.blend')
scene.render.resolution_x=1600;scene.render.resolution_y=600;scene.cycles.samples=16
for clip,frame in [('Idle',1),('Run',8),('Cast02',13)]:
 for rig in [o for o in scene.objects if o.type=='ARMATURE']:rig.animation_data.action=bpy.data.actions[rig.name.replace('_Rig','')+'_'+clip]
 scene.frame_set(frame);scene.render.filepath=BASE+'/Previews/Refined_'+clip+'.png';bpy.ops.render.render(write_still=True)
print('Refined all four rigs and 28 clips')
