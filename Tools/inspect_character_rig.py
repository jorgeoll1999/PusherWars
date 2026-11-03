import bpy,json,os
base=r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes'
report=[]
for rig in [o for o in bpy.context.scene.objects if o.type=='ARMATURE']:
 rig.animation_data.action=None
 for track in rig.animation_data.nla_tracks:track.mute=True
 rig.data.pose_position='REST';bpy.context.view_layer.update()
 data={'name':rig.name,'bones':{},'weights':{}}
 for b in rig.data.bones:data['bones'][b.name]={'head':list(b.head_local),'tail':list(b.tail_local),'axes':[list(v) for v in b.matrix_local.to_3x3().transposed()]}
 for obj in rig.children:
  if obj.type!='MESH':continue
  for region,indices in [('hands',[v.index for v in obj.data.vertices if abs(v.co.x)>.68]),('feet',[v.index for v in obj.data.vertices if v.co.z<.18])]:
   groups={}
   for i in indices:
    for g in obj.data.vertices[i].groups:
     name=obj.vertex_groups[g.group].name;groups[name]=groups.get(name,0)+g.weight
   data['weights'][region]={'vertices':len(indices),'total':groups}
 report.append(data)
with open(base+'/ArtSource/Characters/Review/RigInspection.json','w') as f:json.dump(report,f,indent=2)
print('Rig inspection written')
