using System;
using UnityEngine;
namespace GuerraDeEmpujes
{
    [RequireComponent(typeof(CapsuleCollider),typeof(Rigidbody))]
    public sealed class SpellProjectile:MonoBehaviour
    {
        static readonly System.Collections.Generic.HashSet<SpellProjectile> active=new System.Collections.Generic.HashSet<SpellProjectile>();
        public bool IsSpent=>spent;public static bool TeleportInFlight(ArenaHealth who){foreach(var p in active)if(p&&!p.spent&&p.owner==who&&p.kind==SpellKind.TeleportSwap)return true;return false;}
        ArenaHealth owner;SpellLevel stats;Vector3 direction;float traveled;bool initialized,spent;
        CapsuleCollider capsule;Rigidbody body;
        SpellKind kind;public SpellKind Kind=>kind;
        public void Initialize(ArenaHealth caster,SpellLevel level,Vector3 aim,SpellKind type=SpellKind.Fireball){owner=caster;stats=level;direction=aim.normalized;kind=type;capsule=GetComponent<CapsuleCollider>();body=GetComponent<Rigidbody>();if(stats.hitRadius>0){capsule.radius=stats.hitRadius;capsule.height=Mathf.Max(capsule.height,capsule.radius*2);}initialized=true;active.Add(this);OnlineSession.Register(this);foreach(var c in caster.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(capsule,c);gameObject.AddComponent<ProjectileVfx>().Setup(type);Destroy(gameObject,Mathf.Max(.1f,stats.range/Mathf.Max(.01f,stats.speed))+1);}
        void OnDestroy(){active.Remove(this);}
        bool ProjectileSweep(float dt)
        {
            var p=body.position;foreach(var other in active){if(!other||other==this||other.spent||!other.initialized||other.kind==SpellKind.TeleportSwap)continue;var relative=p-other.body.position;var velocity=direction*stats.speed-other.direction*other.stats.speed;float t=velocity.sqrMagnitude>.0001f?Mathf.Clamp(-Vector3.Dot(relative,velocity)/velocity.sqrMagnitude,0,dt):0;float radius=capsule.radius+other.capsule.radius+.08f;if((relative+velocity*t).sqrMagnitude<=radius*radius){Clash(other,(p+direction*stats.speed*t+other.body.position+other.direction*other.stats.speed*t)*.5f);return true;}}return false;
        }
        public void Clash(SpellProjectile other,Vector3 at){if(spent||!other||other==this||other.spent||kind==SpellKind.TeleportSwap||other.kind==SpellKind.TeleportSwap)return;spent=other.spent=true;if(capsule)capsule.enabled=false;if(other.capsule)other.capsule.enabled=false;SpellVfx.Burst(at,SpellVfx.White,.9f);Destroy(other.gameObject);Destroy(gameObject);}
        void FixedUpdate()
        {
            if(!initialized || spent)return;
            if(kind!=SpellKind.TeleportSwap && ProjectileSweep(Time.fixedDeltaTime))return;
            float distance=Mathf.Min(stats.speed*Time.fixedDeltaTime,stats.range-traveled);
            if(distance<=0){Finish(null,body.position);return;}
            if(kind==SpellKind.TeleportSwap){
                var next=body.position+direction*distance;
                traveled+=distance;
                if(traveled>=stats.range-.0001f) Finish(OccupantAt(next),next);
                else body.MovePosition(next);
                return;
            }
            var center=transform.TransformPoint(capsule.center);float segment=Mathf.Max(0,capsule.height*.5f-capsule.radius);
            var hits=Physics.CapsuleCastAll(center+transform.forward*segment,center-transform.forward*segment,capsule.radius,direction,distance,~0,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){if(Hit(hit.collider,hit.point))return;}
            body.MovePosition(body.position+direction*distance);traveled+=distance;
        }
        ArenaHealth OccupantAt(Vector3 point)
        {
            ArenaHealth nearest=null;float best=float.PositiveInfinity;
            foreach(var c in Physics.OverlapCapsule(point+Vector3.down,point+Vector3.up,capsule.radius,~0,QueryTriggerInteraction.Ignore)){
                var h=c.GetComponentInParent<ArenaHealth>();
                if(!h || h==owner || h.CurrentHealth<=0)continue;
                var delta=h.transform.position-point;float distance=delta.x*delta.x+delta.z*delta.z;
                if(distance<best){nearest=h;best=distance;}
            }
            return nearest;
        }
        void OnTriggerEnter(Collider other){if(initialized)Hit(other,body.position);}
        bool Hit(Collider other,Vector3 point)
        {
            if(spent || kind==SpellKind.TeleportSwap || other==capsule)return false;var projectile=other.GetComponentInParent<SpellProjectile>();if(projectile&&projectile!=this){if(projectile.kind==SpellKind.TeleportSwap)return false;Clash(projectile,point);return true;}if(other.isTrigger)return false;
            var cover=other.GetComponentInParent<ArenaCover>();if(cover){if(kind==SpellKind.TeleportSwap)return false;cover.Strike();Finish(null,point);return true;}var target=other.GetComponentInParent<ArenaHealth>();if(target && target==owner)return false;
            Finish(target,point);return true;
        }
        void Finish(ArenaHealth target,Vector3 point)
        {
            if(spent)return;spent=true;
            SpellVfx.Burst(point,kind==SpellKind.Fireball?SpellVfx.Fire:SpellVfx.Arcane,kind==SpellKind.Fireball?1.1f:.65f);
            if(kind==SpellKind.TeleportSwap){if(owner && owner.CurrentHealth>0)SpellCombat.Teleport(owner,target,point);}
            else if(target)SpellCombat.Impact(target,direction,stats,owner);
            Destroy(gameObject);
        }
    }
}
