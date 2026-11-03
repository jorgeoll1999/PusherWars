using UnityEngine;
using UnityEngine.Rendering;
namespace GuerraDeEmpujes
{
 public static class SpellVfx
 {
  public static readonly Color Fire=new Color(3.2f,.55f,.035f),Arcane=new Color(1.1f,.25f,3.2f),Ice=new Color(.2f,1.5f,2.8f),White=new Color(3,3,2);
  static Material glow,fire,arcane,bubble;
  public static Material Glow=>glow?glow:(glow=Resources.Load<Material>("SpellVfx/Glow"));
  public static Material FireMaterial=>fire?fire:(fire=Resources.Load<Material>("SpellVfx/Fire"));
  public static Material ArcaneMaterial=>arcane?arcane:(arcane=Resources.Load<Material>("SpellVfx/Arcane"));
  public static Material BubbleMaterial=>bubble?bubble:(bubble=Resources.Load<Material>("SpellVfx/Bubble"));
  public static GameObject Sphere(string name,Transform parent,Vector3 position,float size,Material material)
  {
   var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;var c=go.GetComponent<Collider>();if(c){c.enabled=false;Object.Destroy(c);}go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*size;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return go;
  }
  public static LineRenderer Line(string name,Transform parent,Color color,float width,bool loop=false)
  {
   var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Glow;line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.loop=loop;line.numCapVertices=3;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;return line;
  }
  public static LineRenderer Ring(Transform parent,Color color,float radius,float width,Quaternion rotation)
  {
   var line=Line("Energy ring",parent,color,width,true);line.useWorldSpace=false;line.positionCount=64;
   for(int i=0;i<64;i++){float a=i*Mathf.PI/32;line.SetPosition(i,rotation*new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}return line;
  }
  public static ParticleSystem Particles(Transform parent,Color color,int count,float speed,float life,float size,bool continuous=false)
  {
   if(MobileLightweight.Active)count=Mathf.Max(6,Mathf.RoundToInt(count*.55f));var go=new GameObject(continuous?"Embers trail":"Impact sparks");go.transform.SetParent(parent,false);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.loop=continuous;main.duration=1;main.startLifetime=new ParticleSystem.MinMaxCurve(life*.6f,life);main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.35f,speed);main.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);main.startColor=color;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=continuous?100:count;main.gravityModifier=continuous?-.05f:.15f;
   var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
   var emission=ps.emission;emission.rateOverTime=continuous?count:0;if(!continuous)emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
   var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(color*.5f,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.7f,.4f),new GradientAlphaKey(0,1)});colors.color=gradient;
   var sizes=ps.sizeOverLifetime;sizes.enabled=true;sizes.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
   var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Glow;renderer.shadowCastingMode=ShadowCastingMode.Off;if(!continuous){renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=2;renderer.velocityScale=.05f;}ps.Play();if(!continuous)Object.Destroy(go,life+.1f);return ps;
  }
  public static void Burst(Vector3 at,Color color,float radius=1)
  {
   OnlineSession.Emit("burst",at,default(Vector3),radius,color);var root=new GameObject("Spell impact");root.transform.position=at;Particles(root.transform,color,40,4*radius,.45f,.28f);Particles(root.transform,White,14,6*radius,.28f,.12f);
   var ring=Ring(root.transform,color,.15f,.15f,Quaternion.identity);ring.gameObject.AddComponent<VfxExpansion>().Setup(radius,.38f);
   Object.Destroy(root,1);
  }
  public static void Portal(Vector3 feet)
  {
   OnlineSession.Emit("portal",feet);var root=new GameObject("Teleport arrival departure");root.transform.position=feet+Vector3.up*.12f;
   for(int i=0;i<2;i++){var ring=Ring(root.transform,i==0?Arcane:Ice,.55f-i*.13f,.12f,Quaternion.identity);ring.gameObject.AddComponent<VfxExpansion>().Setup(1.35f,.6f);}
   Particles(root.transform,Arcane,36,2,.7f,.24f);Object.Destroy(root,1);
  }
  public static void Lightning(Vector3 start,Vector3 end,CharacterAnimationDriver owner=null){OnlineSession.Emit("lightning",start,end);var go=new GameObject("Forked lightning");var bolt=go.AddComponent<VfxLightning>();bolt.Setup(start,end);bolt.owner=owner;Burst(end,Ice,.65f);}
  public static void BubblePop(Vector3 center,float radius)
  {
   OnlineSession.Emit("pop",center,default(Vector3),radius);SpellAudio.Play("BubblePop",center,.65f);
   var root=new GameObject("Bubble shatter shockwave");root.transform.position=center;
   for(int i=0;i<3;i++){var ring=Ring(root.transform,i==0?White:Ice,.45f,.18f-i*.04f,i==2?Quaternion.Euler(75,0,0):Quaternion.identity);ring.gameObject.AddComponent<VfxExpansion>().Setup(radius,.45f+i*.08f);}
   Particles(root.transform,Ice,65,5,.65f,.22f);Object.Destroy(root,1.2f);
  }
 }
 public sealed class VfxExpansion:MonoBehaviour
 {
  float radius,duration,age,baseRadius;LineRenderer line;Color color;Vector3[] points;
  public void Setup(float r,float d){radius=r;duration=d;line=GetComponent<LineRenderer>();baseRadius=line.GetPosition(0).magnitude;color=line.startColor;points=new Vector3[line.positionCount];line.GetPositions(points);}
  public void PreviewAt(float t){age=duration*t;Render();}
  void Render(){float t=Mathf.Clamp01(age/duration);float scale=Mathf.Lerp(1,radius/Mathf.Max(.01f,baseRadius),1-Mathf.Pow(1-t,3));for(int i=0;i<points.Length;i++)line.SetPosition(i,points[i]*scale);var c=color;c.a=1-t;line.startColor=line.endColor=c;}
  void Update(){age+=Time.deltaTime;Render();if(age>=duration)Destroy(gameObject);}
 }
 [DefaultExecutionOrder(200)]
 public sealed class VfxLightning:MonoBehaviour
 {
  public CharacterAnimationDriver owner;
  void LateUpdate(){if(owner && owner.IsCasting){a=owner.GetCastOrigin(SpellKind.Lightning);Draw();}}
  Vector3 a,b;LineRenderer halo,core;LineRenderer[] branches;float age,next;
  public void Setup(Vector3 start,Vector3 end){a=start;b=end;halo=SpellVfx.Line("Electric halo",transform,SpellVfx.Ice,.28f);core=SpellVfx.Line("White bolt",transform,new Color(4,4,4),.075f);branches=new LineRenderer[3];for(int i=0;i<3;i++)branches[i]=SpellVfx.Line("Fork",transform,SpellVfx.Ice,.075f);Draw();Destroy(gameObject,.24f);}
  void Draw(){var dir=(b-a).normalized;var side=Vector3.Cross(dir,Vector3.up).normalized;var points=new Vector3[23];for(int i=0;i<23;i++){float t=i/22f;points[i]=Vector3.Lerp(a,b,t)+(side*Random.Range(-.24f,.24f)+Vector3.up*Random.Range(-.12f,.12f))*Mathf.Sin(t*Mathf.PI);}halo.positionCount=core.positionCount=points.Length;halo.SetPositions(points);core.SetPositions(points);for(int j=0;j<3;j++){int k=5+j*5;branches[j].positionCount=4;for(int i=0;i<4;i++)branches[j].SetPosition(i,points[k]+side*(j%2==0?1:-1)*i*.22f+dir*i*.18f+Vector3.up*Random.Range(-.12f,.12f));}}
  void Update(){age+=Time.deltaTime;if(age>next){next=age+.04f;Draw();}float alpha=1-age/.24f;foreach(var l in branches){var c=l.startColor;c.a=alpha;l.startColor=l.endColor=c;}var h=halo.startColor;h.a=alpha;halo.startColor=halo.endColor=h;var w=core.startColor;w.a=alpha;core.startColor=core.endColor=w;}
 }
 public sealed class ProjectileVfx:MonoBehaviour
 {
  GameObject orb,swirl;TrailRenderer trail;ParticleSystem sparks;SpellKind kind;
  public void Setup(SpellKind type)
  {
   kind=type;foreach(var r in GetComponentsInChildren<MeshRenderer>())r.enabled=false;
   if(type==SpellKind.Fireball){gameObject.AddComponent<FireballCometVfx>().Initialize();enabled=false;return;}
   if((int)type>=4){gameObject.AddComponent<ArsenalPowerArt>().Setup(type);enabled=false;return;}
   bool fire=type==SpellKind.Fireball;Color color=fire?SpellVfx.Fire:SpellVfx.Arcane;
   orb=SpellVfx.Sphere("Energy core",transform,Vector3.zero,fire?.33f:.29f,fire?SpellVfx.FireMaterial:SpellVfx.ArcaneMaterial);
   var shell=SpellVfx.Sphere("Turbulent shell",transform,Vector3.zero,fire?.6f:.55f,fire?SpellVfx.FireMaterial:SpellVfx.ArcaneMaterial);shell.transform.localScale=new Vector3(.52f,.52f,.8f);
   if(!fire){swirl=new GameObject("Orbiting arcane hoops");swirl.transform.SetParent(transform,false);SpellVfx.Ring(swirl.transform,SpellVfx.Ice,.4f,.10f,Quaternion.Euler(45,0,0));SpellVfx.Ring(swirl.transform,SpellVfx.Arcane,.34f,.12f,Quaternion.Euler(-45,0,0));}
   var tr=new GameObject("Comet tail");tr.transform.SetParent(transform,false);trail=tr.AddComponent<TrailRenderer>();trail.sharedMaterial=SpellVfx.Glow;trail.time=fire?.3f:.4f;trail.startWidth=fire?.5f:.3f;trail.endWidth=0;trail.minVertexDistance=.035f;trail.startColor=color;var end=color;end.a=0;trail.endColor=end;trail.numCapVertices=3;
   sparks=SpellVfx.Particles(transform,color,fire?65:35,fire?1.5f:.5f,.4f,fire?.28f:.16f,true);
   if(fire)SpellVfx.Particles(transform,SpellVfx.White,20,.15f,.15f,.22f,true);
  }
  void Update(){if(orb)orb.transform.localScale=Vector3.one*(.32f+Mathf.Sin(Time.time*22)*.035f);if(swirl)swirl.transform.Rotate(0,0,Time.deltaTime*340,Space.Self);}
  void OnDestroy(){if(trail){trail.transform.SetParent(null,true);trail.autodestruct=true;trail.emitting=false;}if(sparks){sparks.transform.SetParent(null,true);sparks.Stop();Destroy(sparks.gameObject,1);}}
 }
 public sealed class FireballCometVfx:MonoBehaviour
 {
  static Mesh flameMesh;TrailRenderer trail;ParticleSystem embers;Transform core;
  public void Initialize()
  {
   gameObject.AddComponent<FireballFlightParticles>();
   core=SpellVfx.Sphere("White hot fireball core",transform,Vector3.zero,.43f,Resources.Load<Material>("SpellVfx/FireCore")).transform;
   if(!flameMesh){flameMesh=new Mesh();flameMesh.name="Crossed flame ribbon";flameMesh.vertices=new[]{new Vector3(-.60f,0,-.85f),new Vector3(.60f,0,-.85f),new Vector3(-.60f,0,.48f),new Vector3(.60f,0,.48f)};flameMesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};flameMesh.triangles=new[]{0,2,1,1,2,3};flameMesh.RecalculateBounds();}
   for(int i=0;i<2;i++){var go=new GameObject("Flowing flame ribbon");go.transform.SetParent(transform,false);go.transform.localRotation=Quaternion.Euler(0,0,i*90);go.AddComponent<MeshFilter>().sharedMesh=flameMesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("SpellVfx/Flame");renderer.shadowCastingMode=ShadowCastingMode.Off;}
   var tail=new GameObject("Fireball ember wake");tail.transform.SetParent(transform,false);trail=tail.AddComponent<TrailRenderer>();trail.sharedMaterial=SpellVfx.Glow;trail.time=.12f;trail.startWidth=.38f;trail.endWidth=0;trail.minVertexDistance=.025f;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(4,2,.15f),0),new GradientColorKey(new Color(2,.12f,0),.4f),new GradientColorKey(new Color(.4f,.01f,0),1)},new[]{new GradientAlphaKey(.7f,0),new GradientAlphaKey(0,1)});trail.colorGradient=gradient;
   embers=SpellVfx.Particles(transform,new Color(2,.2f,.01f),55,.7f,.18f,.10f,true);
   if(!MobileLightweight.DynamicSpellLights)return;var light=gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.25f,.03f);light.intensity=1.2f;light.range=2.3f;light.shadows=LightShadows.None;
  }
  void Update(){if(core)core.localScale=Vector3.one*(.42f+.02f*Mathf.Sin(Time.time*27));}
  void OnDestroy(){if(trail){trail.transform.SetParent(null,true);trail.emitting=false;trail.autodestruct=true;}if(embers){embers.transform.SetParent(null,true);embers.Stop();Destroy(embers.gameObject,.5f);}}
 }
}



