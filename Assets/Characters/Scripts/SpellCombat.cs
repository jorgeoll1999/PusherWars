using UnityEngine;
namespace GuerraDeEmpujes
{
 public static class SpellCombat
 {
  static Material effectMaterial;
  public static Material EffectMaterial {get{if(!effectMaterial)effectMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));return effectMaterial;}}
  public static void Impact(ArenaHealth target,Vector3 direction,SpellLevel stats,ArenaHealth source=null)
  {
   if(target.CurrentHealth<=0 || target.invulnerable)return;var shield=target.GetComponent<BubbleShield>();if(shield && shield.Active)return;
   target.TagAttacker(source);var motor=target.GetComponent<ArenaCharacterMotor>();if(motor)motor.ApplyKnockback(direction,stats.knockback);target.TakeDamage(stats.damage,source);
  }
  public static void Beam(Vector3 start,Vector3 end,Color color,float width)
  {
   var go=new GameObject("SpellFlash");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=EffectMaterial;line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.positionCount=2;line.SetPosition(0,start);line.SetPosition(1,end);Object.Destroy(go,.16f);
  }
  static bool Clear(Vector3 feet,ArenaHealth a,ArenaHealth b)
  {
   foreach(var c in Physics.OverlapCapsule(feet+Vector3.up*.3f,feet+Vector3.up*1.5f,.26f,~0,QueryTriggerInteraction.Ignore))
   {var h=c.GetComponentInParent<ArenaHealth>();if(h==a || (b && h==b))continue;return false;}return true;
  }
  public static bool Teleport(ArenaHealth caster,ArenaHealth target,Vector3 point)
  {
   if(!caster || caster.CurrentHealth<=0)return false;var motor=caster.GetComponent<ArenaCharacterMotor>();if(!motor)return false;
   var old=caster.transform.position;
   if(target && target.CurrentHealth>0)
   {
    var other=target.GetComponent<ArenaCharacterMotor>();var dest=target.transform.position;
    if(!other || !Clear(dest,caster,target) || !Clear(old,caster,target))return false;
    motor.Warp(dest);other.Warp(old);SpellAudio.Play("TeleportArrival",dest);SpellVfx.Portal(old);SpellVfx.Portal(dest);return true;
   }
   // If the chosen point is solid, arrive at the closest clear edge.
   Vector3 destination;
   if(!Landing(point,caster,out destination)){
    Vector3 towardStart=old-point;towardStart.y=0;
    if(towardStart.sqrMagnitude<.001f)towardStart=Vector3.forward;
    towardStart.Normalize();
    bool found=false;
    for(int ring=1;ring<=6&&!found;ring++)
     for(int step=0;step<12&&!found;step++){
      var offset=Quaternion.Euler(0,step*30,0)*towardStart*(ring*.3f);
      found=Landing(point+offset,caster,out destination);
     }
    if(!found)return false;
   }
   motor.Warp(destination);SpellAudio.Play("TeleportArrival",destination);
   SpellVfx.Portal(old);SpellVfx.Portal(destination);return true;
  }
  static bool Landing(Vector3 point,ArenaHealth caster,out Vector3 destination)
  {
   destination=default;
   var hits=Physics.RaycastAll(new Vector3(point.x,caster.transform.position.y+4,point.z),Vector3.down,12,~0,QueryTriggerInteraction.Ignore);
   System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
   foreach(var hit in hits){
    if(hit.collider.GetComponentInParent<ArenaCover>())return false;
    if(hit.collider.GetComponentInParent<ArenaHealth>() || hit.normal.y<.6f)continue;
    destination=hit.point+Vector3.up*.015f;
    return Clear(destination,caster,null);
   }return false;
  }
 }
}
