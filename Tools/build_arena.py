import bpy, math, random, json, os
import numpy as np
from mathutils import Vector
from math import sin, cos, pi
random.seed(73)
ROOT = r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes'
ART = os.path.join(ROOT,'ArtSource')
OUT = os.path.join(ROOT,'Assets','Arena')
os.makedirs(ART,exist_ok=True)
# Preserve any previous scene rather than replacing user content.
scene=bpy.data.scenes.new('Caldera • Arena of the Four Seals')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
groups={}
for name in ['01_Platform','02_Surface','03_SpawnPads','04_Ruins','05_Plants','06_Lava','07_Background','08_Presentation']:
    c=bpy.data.collections.new(name);scene.collection.children.link(c);groups[name]=c
colors=[('#48495e','Basalt'),('#64677c','BasaltLight'),('#292e45','BasaltDark'),('#88848f','Stone'),('#c4b3a2','StoneLight'),('#a77453','Earth'),('#d5a46f','Sand'),('#efa94c','Gold'),('#32c9cb','Teal'),('#698fec','Blue'),('#da76be','Rose'),('#b5d66a','Lime'),('#ff6a23','Lava'),('#ffc34c','LavaHot'),('#812e39','Crust'),('#3b7974','Leaves')]
N=512; cell=N//4
pixels=np.ones((N,N,4),dtype=np.float32)
rng=np.random.default_rng(23)
for k,(hx,n) in enumerate(colors):
    rgb=np.array([int(hx[j:j+2],16)/255 for j in (1,3,5)])
    yy,xx=np.mgrid[0:cell,0:cell]; brush=.94+.07*np.sin(xx*.041+np.sin(yy*.027)*2)+.04*np.cos(yy*.067)+rng.normal(0,.009,(cell,cell))
    brush+=.045*(yy/cell)
    pixels[(k//4)*cell:(k//4+1)*cell,(k%4)*cell:(k%4+1)*cell,:3]=np.clip(rgb[None,None,:]*brush[:,:,None],0,1)
img=bpy.data.images.new('Caldera_PaintedAtlas',width=N,height=N)
img.pixels.foreach_set(pixels.ravel());img.filepath_raw=os.path.join(OUT,'Textures','Caldera_PaintedAtlas.png');img.file_format='PNG';img.save()
mats=[]
for name,emission in [('Arena_Painted',0),('Arena_Ember',.75)]:
    m=bpy.data.materials.new(name);m.use_nodes=True
    bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=.88
    tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img
    m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if emission: m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=emission
    mats.append(m)
def finish(o,name,group,tile=0,emit=False):
    o.name=name
    for c in list(o.users_collection): c.objects.unlink(o)
    groups[group].objects.link(o)
    o.data.materials.clear();o.data.materials.append(mats[int(emit)])
    if o.type=='MESH':
        uv=o.data.uv_layers.active or o.data.uv_layers.new(name='PaintedUV')
        for p in o.data.polygons:
            ids=list(p.loop_indices)
            for j,li in enumerate(ids):
                a=2*pi*j/max(3,len(ids));uv.data[li].uv=((tile%4+.5+.39*cos(a))/4,(tile//4+.5+.39*sin(a))/4)
    return o
def mesh(name,v,f,g,t=0,e=False):
    me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);groups[g].objects.link(o);return finish(o,name,g,t,e)
def cyl(name,loc,r,depth,t,g='04_Ruins',verts=8,r2=None):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r,radius2=r if r2 is None else r2,depth=depth,location=loc)
    return finish(bpy.context.object,name,g,t)
def cube(name,loc,scale,t,g='04_Ruins',angle=0,bevel=.06):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=scale;o.rotation_euler.z=angle
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Chipped soft edges','BEVEL');mod.width=bevel;mod.segments=1
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o,name,g,t)
def rock(name,loc,scale,t,g='01_Platform'):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=loc);o=bpy.context.object;o.scale=scale;o.rotation_euler=(random.random(),random.random(),random.random()*3);return finish(o,name,g,t)
def strip(name,pts,w,t,g='02_Surface',emit=False):
    vs=[]
    for i,p in enumerate(pts):
        d=Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(0,i-1)]);n=Vector((-d.y,d.x,0)).normalized()*w/2
        vs.extend([Vector(p)+n,Vector(p)-n])
    return mesh(name,vs,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(pts)-1)],g,t,emit)
def arc(name,c,r,w,z,t,g='02_Surface',start=0,end=2*pi,steps=64,emit=False):
    pts=[(c[0]+r*cos(start+(end-start)*i/steps),c[1]+r*sin(start+(end-start)*i/steps),z) for i in range(steps+1)]
    return strip(name,pts,w,t,g,emit)
# Perfectly level collision footprint, slightly irregular visual underside.
cyl('Walkable_Platform',(0,0,-.30),12,.60,0,'01_Platform',verts=32)
for tier,(r,z) in enumerate([(12,-.4),(11.4,-1.5),(8.8,-3.4),(4.2,-5.8),(1,-7.1)]):
    if tier==0: rings=[]
    rings.append([(r*(1+random.uniform(-.065,.065))*cos(2*pi*i/32),r*(1+random.uniform(-.065,.065))*sin(2*pi*i/32),z+random.uniform(-.28,.28)) for i in range(32)])
for j in range(4):
    for i in range(32):
        q=(i+1)%32;mesh('BasaltFacet_%02d_%02d'%(j,i),[rings[j][i],rings[j][q],rings[j+1][q],rings[j+1][i]],[(0,1,2),(0,2,3)],'01_Platform',random.choice([0,1,2,2]))
for i in range(20):
    a=i*2*pi/20;r=random.uniform(8.6,10.8)
    rock('Hanging_Basalt_%02d'%i,(r*cos(a),r*sin(a),-2.2),(1.35,1.25,random.uniform(2,3.3)),random.choice([0,1,2]))
# Keep underside rocks below the collision surface.
for o in scene.objects:
    if o.name.startswith('Hanging_Basalt'):
        high=max((o.matrix_world@Vector(v)).z for v in o.bound_box)
        if high>-.45:o.location.z-=high+.45
bpy.context.view_layer.update()
# Inlaid stone edge, broken into readable blocks; no railings.
for i in range(64):
    a=2*pi*(i+.5)/64
    cube('Edge_Inlay_%02d'%i,(11.6*cos(a),11.6*sin(a),.025),(.98,.34,.09),3 if i%4 else 4,'02_Surface',a+pi/2,.025)
arc('Inner_Stone_Ring',(0,0),9.5,.09,.025,3)
# Broad restrained dirt motif with fourfold rotational fairness.
for q in range(4):
    rot=q*pi/2
    pts=[]
    for j in range(49):
        a=rot+.17+j/48*(pi/2-.34);r=7.35+.32*sin(j/48*2*pi)
        pts.append((r*cos(a),r*sin(a),.018))
    strip('Winding_Earth_%d'%q,pts,.72,5)
    strip('Painted_Path_Highlight_%d'%q,[(x*.986,y*.986,z+.002) for x,y,z in pts],.07,6)
# Central octagonal seal: flat and visually quiet.
arc('Central_Seal',(0,0),3.9,.065,.028,3,steps=8)
arc('Central_Seal_Inner',(0,0),3.65,.035,.029,1,steps=8)
for q in range(4):
    a=q*pi/2
    strip('Compass_%d'%q,[(.4*cos(a),.4*sin(a),.03),(1.35*cos(a),1.35*sin(a),.03)],.09,3)
    # Four small diamonds define the center without high-contrast clutter.
    v=[(1.8*cos(a)+.23*cos(a+k*pi/2),1.8*sin(a)+.23*sin(a+k*pi/2),.032) for k in range(4)]
    mesh('Compass_Diamond_%d'%q,v,[(0,1,2,3)],'02_Surface',4)
# Four color-coded pads at the same radius, flush decals over shallow foundations.
for q,t in enumerate([8,9,10,11]):
    a=q*pi/2;x,y=8.15*cos(a),8.15*sin(a)
    cyl('SpawnPad_%d_Base'%(q+1),(x,y,.035),1.42,.07,2,'03_SpawnPads',verts=8)
    cyl('SpawnPad_%d_Face'%(q+1),(x,y,.075),1.25,.025,1,'03_SpawnPads',verts=8)
    arc('SpawnPad_%d_Seal'%(q+1),(x,y),1.08,.12,.095,t,'03_SpawnPads',steps=8)
    pts=[(x+.55*cos(a+k*pi/2),y+.55*sin(a+k*pi/2),.098) for k in range(5)]
    strip('SpawnPad_%d_Rune'%(q+1),pts,.10,t,'03_SpawnPads')
    for k in [-1,0,1]:
        b=a+pi/2;cx=x+cos(a)*1.65+cos(b)*k*.25;cy=y+sin(a)*1.65+sin(b)*k*.25
        cube('Pad_Notch_%d_%d'%(q,k),(cx,cy,.04),(.30,.12,.06),t,'03_SpawnPads',a,.015)
# Four matched decorative bays between spawns, all outside radius 9.7.
for q in range(4):
    a=pi/4+q*pi/2;x,y=10.4*cos(a),10.4*sin(a);t=8+q
    cyl('Ruins_%d_Plinth'%q,(x,y,.16),.96,.32,3,verts=8)
    cube('Ruins_%d_PillarFoot'%q,(x,y,.43),(1.15,1.15,.24),4,angle=a)
    height=[1.9,2.2,1.6,2.05][q]
    cyl('Ruins_%d_BrokenShaft'%q,(x,y,.57+height/2),.43,height,3,verts=6,r2=.37)
    cyl('Ruins_%d_Collar'%q,(x,y,1.2),.52,.20,4,verts=6)
    rock('Ruins_%d_BrokenCap'%q,(x,y,.6+height),(.54,.49,.21),4,'04_Ruins')
    # Rune front panel faces toward the arena center.
    tangent=Vector((-sin(a),cos(a),0));inward=Vector((-cos(a),-sin(a),0));base=Vector((x,y,1.65))+inward*.46
    pts=[base+tangent*dx+Vector((0,0,dz)) for dx,dz in [(0,-.32),(-.16,0),(0,.28),(.16,0),(0,-.32)]]
    # Vertical rune ribbon as beveled thin cubes.
    for j in range(4):
        p0,p1=pts[j],pts[j+1];d=p1-p0;o=cube('Ruins_%d_Rune%d'%(q,j),(p0+p1)/2,(.05,.05,d.length),t,bevel=.01);o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    for side in [-1,1]:
        bx=x+side*1.35*(-sin(a));by=y+side*1.35*cos(a)
        for row in range(2):
            cube('Ruins_%d_Wall_%d_%d'%(q,side,row),(bx,by,.22+row*.4),(1.1,.48,.36),3+row%2,angle=a+pi/2)
    # Brazier in a symmetric exterior corner.
    b=a+.17;bx,by=10.9*cos(b),10.9*sin(b)
    cyl('Brazier_%d_Foot'%q,(bx,by,.15),.43,.30,2)
    cyl('Brazier_%d_Stem'%q,(bx,by,.58),.19,.6,7,r2=.26)
    cyl('Brazier_%d_Bowl'%q,(bx,by,.96),.24,.28,7,r2=.51)
    o=cyl('Brazier_%d_Embers'%q,(bx,by,1.11),.41,.04,12);o.data.materials[0]=mats[1]
    for k in range(3):
        o=rock('Brazier_%d_Coal%d'%(q,k),(bx+.17*cos(k*2*pi/3),by+.17*sin(k*2*pi/3),1.19),(.13,.13,.19),13,'04_Ruins');o.data.materials[0]=mats[1]
    # Tiny stylized plants and rubble, confined to decorative bays.
    b=a-.18;px,py=10.9*cos(b),10.9*sin(b)
    for j in range(5):
        rock('Rubble_%d_%d'%(q,j),(px+random.uniform(-.55,.55),py+random.uniform(-.55,.55),.12),(random.uniform(.13,.3),.22,.19),random.choice([1,3,4]),'04_Ruins')
    for j in range(6):
        d=2*pi*j/6
        o=rock('Plant_%d_Leaf%d'%(q,j),(px+.27*cos(d),py+.27*sin(d),.28),(.16,.40,.12),15 if j%2 else 11,'05_Plants');o.rotation_euler=(.5,0,d-pi/2)
# Small fissures are outside the main combat area, not fake holes in the center.
for q in range(4):
    a=q*pi/2+.36
    pts=[]
    for j in range(7):
        r=9.3+j*.43;b=a+.035*sin(j*2);pts.append((r*cos(b),r*sin(b),.045))
    strip('Lava_Fissure_%d'%q,pts,.09,12,emit=True)
    p=pts[-1];vs=[(p[0]-.18,p[1],-.1),(p[0]+.18,p[1],-.1),(p[0]+.1,p[1]+.2,-4.1),(p[0]-.1,p[1]+.2,-4.1)]
    mesh('Lava_Fall_%d'%q,vs,[(0,1,2,3)],'06_Lava',12,True)
    strip('Lava_Fall_Lip_%d'%q,pts[-3:],.28,12,'06_Lava',True)
# Lava sea with large painted flow shapes and dark cooled plates.
cyl('Lava_Abyss',(0,0,-9),65,.25,12,'06_Lava',verts=64).data.materials[0]=mats[1]
for j in range(22):
    cx,cy=random.uniform(-34,34),random.uniform(-34,34);r=random.uniform(2,7)
    arc('Lava_Current_%02d'%j,(cx,cy),r,random.uniform(.12,.35),-8.85,13,'06_Lava',start=random.random()*pi,end=pi*(1.2+random.random()),steps=20,emit=True)
for j in range(26):
    a=random.random()*2*pi;r=random.uniform(15,40)
    rock('Cooled_Lava_%02d'%j,(r*cos(a),r*sin(a),-8.86),(random.uniform(1,3),random.uniform(.8,2),.12),14,'06_Lava')
for j in range(12):
    a=2*pi*j/12+.12;r=random.uniform(17,24)
    rock('Floating_Debris_%02d'%j,(r*cos(a),r*sin(a),random.uniform(-6,-3)),(random.uniform(.6,1.4),random.uniform(.7,1.4),random.uniform(1,2)),2,'07_Background')
# Presentation rig, excluded from FBX.
world=bpy.data.worlds.new('Caldera_Daylight');world.use_nodes=True;scene.world=world
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.25,.32,.47,1)
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.6
def light(name,kind,loc,power,color,size=10):
    data=bpy.data.lights.new(name,kind);data.energy=power;data.color=color
    if kind=='AREA':data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);groups['08_Presentation'].objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,-1))-o.location).to_track_quat('-Z','Y').to_euler();return o
light('Soft_Daylight','AREA',(-8,-12,22),2300,(1,.88,.74),14)
light('Sky_Fill','AREA',(10,5,16),1800,(.62,.78,1),12)
sun=light('Sun','SUN',(-10,-15,20),2,(1,.91,.81));sun.data.angle=.35
camdata=bpy.data.cameras.new('Arena_Isometric');cam=bpy.data.objects.new('Arena_Isometric',camdata);groups['08_Presentation'].objects.link(cam)
cam.location=(27,-34,30);cam.rotation_euler=(Vector((0,0,-1.8))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=37;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
# Export transforms and hierarchy in meters, Unity Y up.
bpy.ops.object.select_all(action='DESELECT')
for g,c in groups.items():
    if g!='08_Presentation':
        for o in c.objects:o.select_set(True)
bpy.context.view_layer.objects.active=next(o for o in scene.objects if o.type=='MESH')
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'Models','Caldera_Arena.fbx'),use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
triangles=sum(len(p.vertices)-2 for o in scene.objects if o.type=='MESH' for p in o.data.polygons)
report={'name':'Caldera — Four Seals','diameter_m':24,'spawn_radius_m':8.15,'center_clear_radius_m':6.5,'triangles':triangles,'mesh_objects':sum(o.type=='MESH' for o in scene.objects),'materials':2,'atlas_size':512}
with open(os.path.join(ART,'ArenaStats.json'),'w') as f:json.dump(report,f,indent=2)
img.pack();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ART,'Caldera_Arena.blend'))
scene.render.filepath=os.path.join(ROOT,'Previews','Caldera_Blender.png')
print(json.dumps(report))


# Final gameplay revision: solid lava level around the rock.
exec(compile(open(os.path.join(ROOT,'Tools','embed_arena.py'),encoding='utf-8').read(),'embed_arena.py','exec'))
