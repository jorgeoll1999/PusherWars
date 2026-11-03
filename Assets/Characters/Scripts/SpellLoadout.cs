using UnityEngine;
namespace GuerraDeEmpujes
{
 [System.Serializable]public sealed class SpellSlot{public SpellDefinition spell;[Min(1)]public int level=1;[System.NonSerialized]public float readyAt;}
 [RequireComponent(typeof(CharacterAnimationDriver),typeof(ArenaHealth))]
 public sealed class SpellLoadout:MonoBehaviour
 {
  public SpellSlot[] slots={new SpellSlot(),new SpellSlot(),new SpellSlot(),new SpellSlot()};
  public void EnsureCatalog(){if(slots.Length==Arsenal.Powers)return;System.Array.Resize(ref slots,Arsenal.Powers);for(int i=4;i<slots.Length;i++)slots[i]=new SpellSlot{spell=Arsenal.Extra(i,slots[0].spell.projectilePrefab),level=0};}
  public int upgradePoints;public bool ReplicaTeleporting;public bool IsTeleporting=>OnlineSession.Replica?ReplicaTeleporting:SpellProjectile.TeleportInFlight(GetComponent<ArenaHealth>());
  public bool GetStats(int index,out SpellLevel stats){stats=default;if(index<0 || index>=slots.Length || slots[index]==null || !slots[index].spell || !slots[index].spell.TryGetLevel(slots[index].level,out stats))return false;var hero=GetComponent<HeroBattleStats>();float area=hero?hero.AreaMultiplier:1;stats.radius*=area;stats.hitRadius=(stats.hitRadius>0?stats.hitRadius:slots[index].spell.kind==SpellKind.Lightning?.12f:.18f)*area;return true;}
  public bool TryUpgrade(int index)
  {
   if(index<0 || index>=slots.Length)return false;var slot=slots[index];SpellLevel next;
   if(slot==null || !slot.spell || !slot.spell.TryGetLevel(slot.level+1,out next) || upgradePoints<next.upgradeCost)return false;
   upgradePoints-=next.upgradeCost;slot.level++;return true;
  }
  public bool TryCast(int index,Vector3 direction){return TryCastAt(index,transform.position+direction.normalized*12);}
  public bool TryCastAt(int index,Vector3 destination)
  {
   if(index<0||index>=slots.Length||slots[index]==null||slots[index].level<1)return false;if(index==3&&IsTeleporting)return false;if(index==2){var shield=GetComponent<BubbleShield>();if(shield&&shield.Active)return false;}if(OnlineSession.Replica)return OnlineSession.Instance.RequestCast(this,index,destination);
   if(index<0 || index>=slots.Length)return false;var slot=slots[index];SpellLevel stats;
   if(slot==null || !slot.spell || Time.time<slot.readyAt || !GetStats(index,out stats))return false;
   if((slot.spell.kind==SpellKind.Fireball || slot.spell.kind==SpellKind.TeleportSwap) && !slot.spell.projectilePrefab)return false;
   var direction=destination-transform.position;direction.y=0;
   if(direction.sqrMagnitude<.001f && !Arsenal.Self(index))return false;
   var driver=GetComponent<CharacterAnimationDriver>();if(GetComponent<ArenaCharacterMotor>().Ramming || driver.IsCasting || GetComponent<ArenaHealth>().CurrentHealth<=0)return false;
   if(direction.sqrMagnitude>.001f){var facing=Quaternion.LookRotation(direction);var body=GetComponent<Rigidbody>();if(body)body.rotation=facing;transform.rotation=facing;}
   driver.SetAim(destination);
   var definition=slot.spell;
   if(!driver.TryCast(Arsenal.Animation(definition.kind),()=>Release(definition,stats,destination),stats.duration))return false;
   OnlineSession.CastAccepted(this,index,destination,stats.duration);slot.readyAt=Time.time+Mathf.Max(1,stats.cooldown);return true;
  }
  public void Release(SpellDefinition spell,SpellLevel stats,Vector3 destination)
  {
   if(GetComponent<ArenaHealth>().CurrentHealth<=0)return;
   Vector3 delta=destination-transform.position;delta.y=0;Vector3 direction=delta.sqrMagnitude>.001f?delta.normalized:transform.forward;
   if(spell.kind==SpellKind.Bubble){var bubble=GetComponent<BubbleShield>();if(!bubble)bubble=gameObject.AddComponent<BubbleShield>();bubble.Activate(stats);return;}
   var origin=GetComponent<CharacterAnimationDriver>().GetCastOrigin(Arsenal.Animation(spell.kind));
   delta=destination-origin;delta.y=0;direction=delta.sqrMagnitude>.001f?delta.normalized:transform.forward;
   if(spell.kind==SpellKind.Aegis){SpellAudio.Play("AegisCast",origin,.48f);GetComponent<ArenaHealth>().Protect(2);SpellVfx.Burst(origin,new Color(1.6f,1,.2f),.9f);return;}
   if(spell.kind==SpellKind.Monolith){SummonedCover.Place(this,destination,stats);return;}
   if(spell.kind==SpellKind.Ram){GetComponent<ArenaCharacterMotor>().BeginRam(direction,stats);return;}
   if(spell.kind==SpellKind.Fireball)SpellAudio.Play("FireCast",origin);
   else if(spell.kind==SpellKind.Lightning)SpellAudio.Play("LightningCast",origin);
   if(spell.kind==SpellKind.Lightning)
   {
    var end=origin+direction*stats.range;
    var hits=Physics.SphereCastAll(origin,stats.hitRadius>0?stats.hitRadius:.12f,direction,stats.range,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
    foreach(var hit in hits){var target=hit.collider.GetComponentInParent<ArenaHealth>();if(target==GetComponent<ArenaHealth>())continue;end=hit.point;var cover=hit.collider.GetComponentInParent<ArenaCover>();if(cover)cover.Strike();if(target)SpellCombat.Impact(target,direction,stats,GetComponent<ArenaHealth>());break;}
    SpellVfx.Lightning(origin,end,GetComponent<CharacterAnimationDriver>());return;
   }
   if(spell.kind==SpellKind.TeleportSwap){stats.range=Mathf.Min(stats.range,delta.magnitude);if(stats.range<.05f)return;}
   if((int)spell.kind>=4)SpellAudio.Play(spell.kind==SpellKind.Ricochet?"RicochetCast":spell.kind==SpellKind.Fragment?"FragmentCast":spell.kind==SpellKind.Curve?"CurveCast":"BoomerangCast",origin,.48f);
   var shot=Instantiate(spell.projectilePrefab,origin,Quaternion.LookRotation(direction));shot.Initialize(GetComponent<ArenaHealth>(),stats,direction,spell.kind);
  }
 }
}
