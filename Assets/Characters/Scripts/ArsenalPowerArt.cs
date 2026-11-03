using UnityEngine;
using UnityEngine.Rendering;
namespace GuerraDeEmpujes
{
 public sealed class ArsenalPowerArt:MonoBehaviour
 {
  Transform core;TrailRenderer trail;ParticleSystem sparks;SpellKind kind;
  static Mesh crystal,boomerang,star;static Material green,yellow,red,teal,wardMaterial,solarAura;
  static Material Tint(ref Material cache,Color color){if(cache)return cache;cache=new Material(SpellVfx.FireMaterial);cache.SetColor("_BaseColor",color);cache.SetColor("_Color",color);cache.SetColor("_EmissionColor",color);return cache;}
  static Mesh Shape(Vector3[] vertices,int[] triangles){var m=new Mesh();m.vertices=vertices;m.triangles=triangles;m.RecalculateNormals();m.RecalculateBounds();return m;}
  static Mesh Crystal(){if(crystal)return crystal;return crystal=Shape(new[]{new Vector3(0,0,.65f),new Vector3(-.25f,0,0),new Vector3(0,.23f,0),new Vector3(.25f,0,0),new Vector3(0,-.23f,0),new Vector3(0,0,-.40f)},new[]{0,1,2,0,2,3,0,3,4,0,4,1,5,2,1,5,3,2,5,4,3,5,1,4});}
  static Mesh Blade(){if(boomerang)return boomerang;return boomerang=Shape(new[]{new Vector3(-.72f,0,-.38f),new Vector3(-.44f,0,.16f),new Vector3(0,0,.43f),new Vector3(.44f,0,.16f),new Vector3(.72f,0,-.38f),new Vector3(.28f,0,-.07f),new Vector3(0,0,.11f),new Vector3(-.28f,0,-.07f)},new[]{0,1,7,1,2,7,2,6,7,2,3,6,3,5,6,3,4,5,7,1,0,7,2,1,7,6,2,6,3,2,6,5,3,5,4,3});}
  static Mesh Star(){if(star)return star;var v=new Vector3[9];for(int i=0;i<8;i++){float a=i*Mathf.PI/4;v[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*(i%2==0?.5f:.12f);}var t=new int[24];for(int i=0;i<8;i++){t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=(i+1)%8+1;}return star=Shape(v,t);}
  public void Setup(SpellKind type){
   kind=type;Color c=type==SpellKind.Ricochet?new Color(.18f,3,.35f):type==SpellKind.Curve?new Color(3,2,.12f):type==SpellKind.Fragment?new Color(3,.04f,.12f):new Color(.1f,2.5f,2);
   var mat=type==SpellKind.Ricochet?Tint(ref green,c):type==SpellKind.Curve?Tint(ref yellow,c):type==SpellKind.Fragment?Tint(ref red,c):Tint(ref teal,c);
   if(type==SpellKind.Fragment||type==SpellKind.Boomerang){var g=new GameObject("Cuerpo mágico");g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=type==SpellKind.Fragment?Crystal():Blade();var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;core=g.transform;}
   else core=SpellVfx.Sphere("Núcleo luminoso",transform,Vector3.zero,type==SpellKind.Curve?.74f:.50f,mat).transform;
   if(type==SpellKind.Curve){
    var aura=Tint(ref solarAura,new Color(3,1.6f,.04f,.28f));
    SpellVfx.Sphere("Aura solar",transform,Vector3.zero,1.18f,aura);
    for(int i=0;i<7;i++){
     var flare=new GameObject("Lengua de energía");flare.transform.SetParent(transform,false);
     float a=i*Mathf.PI*2/7;flare.transform.localPosition=new Vector3(Mathf.Cos(a)*.39f,Mathf.Sin(a)*.39f,-.2f);
     flare.transform.localScale=new Vector3(.24f,.24f,.85f);flare.AddComponent<MeshFilter>().sharedMesh=Crystal();var renderer=flare.AddComponent<MeshRenderer>();renderer.sharedMaterial=aura;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
   }
   var w=new GameObject("Estela");w.transform.SetParent(transform,false);trail=w.AddComponent<TrailRenderer>();trail.sharedMaterial=SpellVfx.Glow;trail.time=type==SpellKind.Curve?.32f:.22f;trail.startWidth=type==SpellKind.Curve?.70f:.44f;trail.endWidth=0;trail.startColor=c;trail.endColor=Color.clear;trail.minVertexDistance=.025f;
   sparks=SpellVfx.Particles(transform,c,type==SpellKind.Ricochet?48:24,.5f,.4f,.17f,true);
   if(type==SpellKind.Curve){var r=sparks.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Mesh;r.mesh=Star();r.sharedMaterial=mat;}
  }
  void Update(){if(kind==SpellKind.Boomerang)core.Rotate(0,1000*Time.deltaTime,0,Space.Self);else if(kind==SpellKind.Fragment)core.Rotate(0,0,210*Time.deltaTime,Space.Self);}
  void OnDestroy(){if(trail){trail.transform.SetParent(null,true);trail.emitting=false;trail.autodestruct=true;}if(sparks){sparks.transform.SetParent(null,true);sparks.Stop();Destroy(sparks.gameObject,1);}}
  public static GameObject Angel(Transform hero){
   var root=new GameObject("Égida · luz angelical");root.transform.SetParent(hero,false);
   var mat=Tint(ref yellow,new Color(3,2,.12f));
   var halo=SpellVfx.Ring(root.transform,new Color(3,2,.15f),.48f,.09f,Quaternion.identity);halo.transform.localPosition=Vector3.up*2.15f;
   SpellVfx.Sphere("Luz envolvente",root.transform,Vector3.up,.8f,Tint(ref wardMaterial,new Color(2,1.5f,.2f,.16f))).transform.localScale=new Vector3(.72f,1.8f,.72f);
   for(int side=-1;side<=1;side+=2)for(int feather=0;feather<5;feather++){
    var l=SpellVfx.Line("Pluma de luz",root.transform,new Color(2.3f,1.7f,.3f,.7f),.075f);l.useWorldSpace=false;l.positionCount=3;
    l.SetPositions(new[]{new Vector3(side*.18f,1.15f,0),new Vector3(side*(.65f+feather*.11f),1.9f-feather*.12f,0),new Vector3(side*(.52f+feather*.08f),1.2f-feather*.08f,0)});
   }
   var ps=SpellVfx.Particles(root.transform,new Color(3,2,.2f),24,.65f,.7f,.13f,true);ps.transform.localPosition=Vector3.up*.6f;return root;
  }
 }
}