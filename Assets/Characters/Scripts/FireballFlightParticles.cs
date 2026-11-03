using UnityEngine;
using UnityEngine.Rendering;
namespace GuerraDeEmpujes
{
 public sealed class FireballFlightParticles:MonoBehaviour
 {
  ParticleSystem flames,smoke;
  void Start(){flames=Create(false);smoke=Create(true);}
  ParticleSystem Create(bool smoky)
  {
   var go=new GameObject(smoky?"Drifting fireball smoke":"Streaming flight flames");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,0,smoky?-.55f:-.2f);go.transform.localRotation=Quaternion.Euler(0,180,0);
   var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var m=ps.main;m.loop=true;m.startLifetime=new ParticleSystem.MinMaxCurve(smoky?.30f:.10f,smoky?.48f:.20f);m.startSpeed=new ParticleSystem.MinMaxCurve(.4f,smoky?1:2);m.startSize=new ParticleSystem.MinMaxCurve(smoky?.30f:.12f,smoky?.55f:.28f);m.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);m.simulationSpace=ParticleSystemSimulationSpace.World;m.maxParticles=smoky?50:90;m.gravityModifier=smoky?-.08f:-.03f;
   var emission=ps.emission;emission.rateOverTime=MobileLightweight.Active?(smoky?24:55):(smoky?46:115);
   var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=smoky?18:10;shape.radius=smoky?.18f:.11f;
   var sizes=ps.sizeOverLifetime;sizes.enabled=true;sizes.size=new ParticleSystem.MinMaxCurve(1,smoky?AnimationCurve.Linear(0,.5f,1,2):AnimationCurve.Linear(0,1,1,.08f));
   var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(smoky?new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.65f,.65f,.65f),1)}:new[]{new GradientColorKey(new Color(4,2,.2f),0),new GradientColorKey(new Color(3,.3f,.015f),.4f),new GradientColorKey(new Color(.6f,.04f,0),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(smoky?.95f:1,.12f),new GradientAlphaKey(0,1)});colors.color=gradient;
   var noise=ps.noise;noise.enabled=!MobileLightweight.Active;noise.strength=smoky?.18f:.08f;noise.frequency=1.5f;noise.scrollSpeed=.5f;noise.quality=ParticleSystemNoiseQuality.Low;
   var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=smoky?Resources.Load<Material>("SpellVfx/Smoke"):SpellVfx.Glow;renderer.shadowCastingMode=ShadowCastingMode.Off;if(!smoky){renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=2;renderer.velocityScale=.08f;}ps.Play();return ps;
  }
  void OnDestroy(){foreach(var ps in new[]{flames,smoke})if(ps){ps.transform.SetParent(null,true);ps.Stop(true,ParticleSystemStopBehavior.StopEmitting);Destroy(ps.gameObject,.8f);}}
 }
}

