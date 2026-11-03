import bpy, bmesh, os, json, math
from mathutils import Vector
BASE = r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes'
OUT = os.path.join(BASE, 'ArtSource', 'Characters')
EXPORT = os.path.join(BASE, 'Assets', 'Characters')
os.makedirs(EXPORT, exist_ok=True)
names = {'maga+arcana':'MagaArcana', 'Magafuego':'MagaFuego', 'magohielo':'MagoHielo', 'magoplanta':'MagoPlanta'}
report = []
scene = bpy.context.scene
meshes = [o for o in scene.objects if o.type == 'MESH' and 'character_source' in o]
for index, obj in enumerate(meshes):
    name = names[obj['character_source']]
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active = obj
    obj.location.x -= index * 3
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # Work in metres: two metres from soles to the top of the headgear.
    for v in obj.data.vertices: v.co *= 2
    obj.name = name + '_Mesh'
    bm = bmesh.new(); bm.from_mesh(obj.data)
    before = {'vertices':len(bm.verts), 'faces':len(bm.faces), 'boundary_edges':sum(e.is_boundary for e in bm.edges)}
    # Preserve extremely thin garment folds: welding nearby vertices collapses them.
    boundary = [e for e in bm.edges if e.is_boundary]
    # These source openings are tiny accidental holes, not garment openings.
    new_faces = bmesh.ops.holes_fill(bm, edges=boundary, sides=12).get('faces', []) if boundary else []
    remaining=[e for e in bm.edges if e.is_boundary]
    if remaining:
        fragments=list({f for e in remaining for f in e.link_faces})
        if all(f.calc_area()<0.0002 for f in fragments):
            bmesh.ops.delete(bm,geom=fragments,context='FACES')
            new_faces=[f for f in new_faces if f.is_valid]
            new_faces+=bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=20).get('faces',[])
    uv = bm.loops.layers.uv.active
    if uv:
        new_set=set(new_faces)
        for face in new_faces:
            for loop in face.loops:
                adjacent=[l[uv].uv.copy() for f in loop.vert.link_faces if f not in new_set for l in f.loops if l.vert==loop.vert]
                if adjacent: loop[uv].uv=sum(adjacent, Vector((0,0)))/len(adjacent)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    after = {'vertices':len(bm.verts), 'faces':len(bm.faces), 'boundary_edges':sum(e.is_boundary for e in bm.edges), 'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges)}
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    # Positions are fitted to the T-pose. Coordinates below are fractions of full height.
    arm_z = {'MagaArcana':.758, 'MagaFuego':.754, 'MagoHielo':.752, 'MagoPlanta':.737}[name]
    bones = {}
    def bone(n,h,t,parent=None,deform=True): bones[n]=(Vector(h)*2,Vector(t)*2,parent,deform)
    bone('Root',(0,0,0),(0,0,.1),deform=False)
    bone('Hips',(0,0,.49),(0,0,.57),'Root')
    bone('Spine',(0,0,.57),(0,0,.65),'Hips')
    bone('Chest',(0,0,.65),(0,0,arm_z),'Spine')
    bone('Neck',(0,0,arm_z),(0,0,.81),'Chest')
    bone('Head',(0,0,.81),(0,0,.95),'Neck')
    for side, sign in [('Left',1),('Right',-1)]:
        p=lambda x,y,z:(sign*x,y,z)
        bone(side+'Shoulder',p(.025,0,arm_z),p(.105,0,arm_z),'Chest')
        bone(side+'UpperArm',p(.105,0,arm_z),p(.225,0,arm_z),side+'Shoulder')
        bone(side+'LowerArm',p(.225,0,arm_z),p(.337,0,arm_z),side+'UpperArm')
        bone(side+'Hand',p(.337,0,arm_z),p(.399,0,arm_z),side+'LowerArm')
        bone(side+'UpperLeg',p(.063,0,.51),p(.092,-.009,.285),'Hips')
        bone(side+'LowerLeg',p(.092,-.009,.285),p(.126,0,.065),side+'UpperLeg')
        bone(side+'Foot',p(.126,0,.065),p(.126,-.065,.027),side+'LowerLeg')
        bone(side+'Toes',p(.126,-.065,.027),p(.126,-.09,.025),side+'Foot')
        bone('Skirt'+side,p(.105,.025,.51),p(.19,.025,.29),'Hips')
        bone('Skirt'+side+'Tip',p(.19,.025,.29),p(.25,.025,.15),'Skirt'+side)
    bone('SkirtFront',(0,-.08,.51),(0,-.11,.30),'Hips')
    bone('SkirtBack',(0,.09,.51),(0,.13,.25),'Hips')
    bone('CapeHair',(0,.09,.78),(0,.12,.58),'Chest')
    armdata=bpy.data.armatures.new(name+'_Skeleton')
    rig=bpy.data.objects.new(name+'_Rig',armdata);scene.collection.objects.link(rig)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for n,(h,t,parent,deform) in bones.items():
        b=armdata.edit_bones.new(n);b.head=h;b.tail=t;b.use_deform=deform
        if parent:b.parent=armdata.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.show_in_front=True;armdata.display_type='OCTAHEDRAL'
    obj.select_set(True)
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    # Limit to four normalized influences, as used by the game's mobile skinning.
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    unweighted=[v.index for v in obj.data.vertices if sum(g.weight for g in v.groups)<.999]
    if unweighted:
        from mathutils.kdtree import KDTree
        valid=[v for v in obj.data.vertices if v.index not in unweighted]
        kd=KDTree(len(valid))
        for v in valid:kd.insert(v.co,v.index)
        kd.balance()
        for i in unweighted:
            _,nearest,_=kd.find(obj.data.vertices[i].co)
            for g in obj.data.vertices[nearest].groups:obj.vertex_groups[g.group].add([i],g.weight,'REPLACE')
        bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    unweighted=[v.index for v in obj.data.vertices if sum(g.weight for g in v.groups)<.999]
    if unweighted: raise RuntimeError(name+' unweighted vertices: '+str(len(unweighted)))
    for mat in obj.data.materials:
        if mat: mat.name=name+'_Material'
    # A non-production pose test stays in the Blender source for visual inspection.
    bpy.context.view_layer.objects.active=rig
    for pb in rig.pose.bones: pb.rotation_mode='XYZ'
    for frame in [1,20,40,60]:
        for pb in rig.pose.bones: pb.rotation_euler=(0,0,0)
        if frame in [20,60]:
            for side,sign in [('Left',1),('Right',-1)]:
                rig.pose.bones[side+'UpperArm'].rotation_euler[0]=math.radians(-55)
                rig.pose.bones[side+'LowerArm'].rotation_euler[2]=math.radians(sign*20)
        if frame==40:
            rig.pose.bones['LeftUpperLeg'].rotation_euler[0]=math.radians(-25)
            rig.pose.bones['LeftLowerLeg'].rotation_euler[0]=math.radians(40)
            rig.pose.bones['Chest'].rotation_euler[1]=math.radians(12)
        for pb in rig.pose.bones:pb.keyframe_insert('rotation_euler',frame=frame,group=pb.name)
    action=rig.animation_data.action;action.name=name+'_RigCheck';action.use_fake_user=True
    rig.animation_data.action=None
    for pb in rig.pose.bones:pb.rotation_euler=(0,0,0)
    scene.frame_set(1);bpy.context.view_layer.update()
    dest=os.path.join(EXPORT,name);os.makedirs(dest,exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(dest,name+'.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_armature_deform_only=False,path_mode='COPY',embed_textures=False)
    rig['Instructions']='FK rig. Root moves entire character; skirt/cape bones control garments. RigCheck action is a deformation check, not a game animation.'
    report.append({'name':name,'before':before,'after':after,'bones':len(bones),'unweighted_vertices':len(unweighted),'max_influences':max(len(v.groups) for v in obj.data.vertices)})
    rig.location.x=index*2.3
    print(name,report[-1])
scene.frame_end=60
with open(os.path.join(OUT,'RigReport.json'),'w') as f:json.dump(report,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Personajes_Rigged.blend'))
