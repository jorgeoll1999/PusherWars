using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GuerraDeEmpujes
{
 public sealed class BubbleShield:MonoBehaviour
 {
  public bool Active{get;private set;}GameObject visual;SpellLevel stats;Coroutine timer;AudioSource charge;
  public void Activate(SpellLevel level){if(timer!=null)StopCoroutine(timer);Cleanup();stats=level;Active=true;charge=SpellAudio.Play("BubbleCast"+Mathf.Clamp(Mathf.RoundToInt(level.duration*10),12,16),transform.position);visual=SpellVfx.Sphere("ProtectiveBubble",transform,Vector3.up*.9f,2.1f,SpellVfx.BubbleMaterial);SpellVfx.Ring(visual.transform,SpellVfx.Ice,.5f,.035f,Quaternion.identity);timer=StartCoroutine(Expire());}
  IEnumerator Expire(){var driver=GetComponent<CharacterAnimationDriver>();bool animated=driver && driver.IsCasting && driver.ActiveSpellKind==SpellKind.Bubble;yield return new WaitForSeconds(stats.duration+(animated?.15f:0));Detonate();}
  public void Detonate()
  {
   if(!Active)return;Active=false;var owner=GetComponent<ArenaHealth>();
   if(owner.CurrentHealth>0)
   {
    var hit=new HashSet<ArenaHealth>();
    foreach(var c in Physics.OverlapSphere(transform.position+Vector3.up*.9f,stats.radius,~0,QueryTriggerInteraction.Ignore))
    {var target=c.GetComponentInParent<ArenaHealth>();if(!target || target==owner || !hit.Add(target) || target.CurrentHealth<=0)continue;RaycastHit block;var origin=transform.position+Vector3.up*.9f;var delta=target.transform.position+Vector3.up*.9f-origin;bool blocked=false;foreach(var hitBlock in Physics.RaycastAll(origin,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))if(hitBlock.collider.GetComponentInParent<ArenaCover>()){blocked=true;break;}if(blocked)continue;var direction=target.transform.position-transform.position;direction.y=0;if(direction.sqrMagnitude<.001f)direction=transform.forward;var motor=target.GetComponent<ArenaCharacterMotor>();if(motor){target.TagAttacker(owner);motor.ApplyKnockback(direction,stats.knockback);}}
    SpellVfx.BubblePop(transform.position+Vector3.up*.35f,stats.radius);
   }
   Cleanup();
  }
  void Cleanup(){if(charge){charge.Stop();Destroy(charge.gameObject);}if(visual)Destroy(visual);}
  void OnDisable(){Active=false;if(timer!=null)StopCoroutine(timer);Cleanup();}
 }
}
