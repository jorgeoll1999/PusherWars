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
        Vector3 pathStart,pathAim,returnStart;float returnLength;bool shard,returning;int bounces,volley;
        readonly System.Collections.Generic.HashSet<ArenaHealth> passed=new System.Collections.Generic.HashSet<ArenaHealth>();
        static int nextVolley;Collider lastBounce;
        SpellKind kind;public SpellKind Kind=>kind;
        public void Initialize(ArenaHealth caster,SpellLevel level,Vector3 aim,SpellKind type=SpellKind.Fireball){owner=caster;stats=level;direction=aim.normalized;kind=type;pathStart=transform.position;pathAim=direction;capsule=GetComponent<CapsuleCollider>();body=GetComponent<Rigidbody>();if(stats.hitRadius>0){capsule.radius=stats.hitRadius;capsule.height=Mathf.Max(capsule.height,capsule.radius*2);}initialized=true;active.Add(this);OnlineSession.Register(this);foreach(var c in caster.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(capsule,c);gameObject.AddComponent<ProjectileVfx>().Setup(type);Destroy(gameObject,Mathf.Max(.1f,stats.range*(type==SpellKind.Boomerang?3:1)/Mathf.Max(.01f,stats.speed))+1);}
        void OnDestroy(){active.Remove(this);}
        bool ProjectileSweep(float dt)
        {
            var p=body.position;foreach(var other in active){if(!other||other==this||(volley!=0&&volley==other.volley)||other.spent||!other.initialized||other.kind==SpellKind.TeleportSwap)continue;var relative=p-other.body.position;var velocity=direction*stats.speed-other.direction*other.stats.speed;float t=velocity.sqrMagnitude>.0001f?Mathf.Clamp(-Vector3.Dot(relative,velocity)/velocity.sqrMagnitude,0,dt):0;float radius=capsule.radius+other.capsule.radius+.08f;if((relative+velocity*t).sqrMagnitude<=radius*radius){Clash(other,(p+direction*stats.speed*t+other.body.position+other.direction*other.stats.speed*t)*.5f);return true;}}return false;
        }
        public void Clash(SpellProjectile other,Vector3 at){if(spent||!other||other==this||(volley!=0&&volley==other.volley)||other.spent||kind==SpellKind.TeleportSwap||other.kind==SpellKind.TeleportSwap)return;spent=other.spent=true;if(capsule)capsule.enabled=false;if(other.capsule)other.capsule.enabled=false;SpellVfx.Burst(at,SpellVfx.White,.9f);Destroy(other.gameObject);Destroy(gameObject);}
        void FixedUpdate()
        {
            if(!initialized || spent)return;
            if(kind!=SpellKind.TeleportSwap && ProjectileSweep(Time.fixedDeltaTime))return;
            float budget=stats.range+(kind==SpellKind.Curve?1.6f:0);
            if(kind==SpellKind.Boomerang){
                if(!owner){Destroy(gameObject);return;}
                if(!returning&&traveled>=stats.range-.001f){returning=true;SpellAudio.Play("BoomerangReturn",body.position,.45f);returnStart=body.position;returnLength=Mathf.Max(.5f,Vector3.ProjectOnPlane(owner.transform.position-returnStart,Vector3.up).magnitude);traveled=0;passed.Clear();}
                if(returning){var delta=owner.transform.position-body.position;delta.y=0;if(delta.magnitude<.38f){SpellAudio.Play("BoomerangCatch",body.position,.32f);Destroy(gameObject);return;}direction=delta.normalized;budget*=2;}
            }
            if(kind==SpellKind.Fragment&&!shard)budget*=.30f;

            float distance=Mathf.Min(stats.speed*Time.fixedDeltaTime,budget-traveled);float sweepDistance=distance;
            if(kind==SpellKind.Curve||kind==SpellKind.Boomerang){
                Vector3 next=FlightPoint(traveled+distance);
                var delta=next-body.position;sweepDistance=delta.magnitude;if(delta.sqrMagnitude>.00001f)direction=delta.normalized;
            }
            if(kind==SpellKind.Fragment&&!shard&&distance<.001f){Split();return;}
            if(kind==SpellKind.Ricochet&&ShrinkingArena.ReflectEdge(body.position,body.position+direction*distance,capsule.radius,ref direction)){bounces++;SpellAudio.Play("RicochetBounce",body.position,.38f);}
            if(bounces>5){Finish(null,body.position);return;}
            if((int)kind>=4)body.rotation=Quaternion.LookRotation(direction);
            if(lastBounce && Vector3.Distance(centerPoint(),lastBounce.ClosestPoint(centerPoint()))>capsule.radius+.36f)lastBounce=null;
            if(distance<=0){Finish(null,body.position);return;}
            if(kind==SpellKind.TeleportSwap){
                var next=body.position+direction*distance;
                traveled+=distance;
                if(traveled>=stats.range-.0001f) Finish(OccupantAt(next),next);
                else body.MovePosition(next);
                return;
            }
            var center=transform.TransformPoint(capsule.center);float segment=Mathf.Max(0,capsule.height*.5f-capsule.radius);
            var hits=Physics.CapsuleCastAll(center+transform.forward*segment,center-transform.forward*segment,capsule.radius,direction,sweepDistance,~0,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){
                if(kind==SpellKind.Ricochet){
                    var h=hit.collider.GetComponentInParent<ArenaHealth>();var c=hit.collider.GetComponentInParent<ArenaCover>();
                    if(hit.collider==lastBounce||h==owner||(!h&&!c))continue;
                    if(h)SpellCombat.Impact(h,direction,stats,owner);if(c)c.Strike();
                    var n=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;if(n.sqrMagnitude<.5f)n=-direction;
                    body.position+=direction*Mathf.Max(0,hit.distance-.02f);direction=Vector3.Reflect(direction,n).normalized;
                    // A centered strike also departs sideways, like an off-center billiard hit.
                    if(h&&Mathf.Abs(Vector3.Dot(direction,pathAim))>.94f)direction=Quaternion.Euler(0,50,0)*direction;
                    SpellAudio.Play("RicochetBounce",hit.point,.38f);SpellVfx.Burst(hit.point,new Color(.3f,2,.4f),.45f);lastBounce=hit.collider;bounces++;traveled+=Mathf.Max(.02f,hit.distance);return;
                }
                if(Hit(hit.collider,hit.point))return;
            }
            if(kind==SpellKind.Curve||kind==SpellKind.Boomerang){
                body.MovePosition(FlightPoint(traveled+distance));
            }else body.MovePosition(body.position+direction*distance);traveled+=distance;
        }
        Vector3 FlightPoint(float progress){
            if(returning){
                float t=Mathf.Clamp01(progress/returnLength);var end=owner.transform.position;end.y=returnStart.y;
                var axis=(end-returnStart).normalized;
                return Vector3.Lerp(returnStart,end,t)+Vector3.Cross(Vector3.up,axis)*Mathf.Sin(t*Mathf.PI)*stats.range*.4f;
            }
            float forward=kind==SpellKind.Curve?progress:Mathf.Min(progress,stats.range);float phase=forward/stats.range;
            return pathStart+pathAim*progress+Vector3.Cross(Vector3.up,pathAim)*Mathf.Sin(phase*Mathf.PI)*stats.range*(kind==SpellKind.Curve?.42f:.4f);
        }
        Vector3 centerPoint()=>transform.TransformPoint(capsule.center);
        void Split(){SpellAudio.Play("FragmentSplit",body.position,.48f);if(!owner){Destroy(gameObject);return;}spent=true;capsule.enabled=false;
            var part=stats;part.range*=.70f;part.knockback*=.5f;part.damage*=.65f;part.hitRadius*=.8f;
            var prefab=owner.GetComponent<SpellLoadout>().slots[0].spell.projectilePrefab;
            int group=++nextVolley;for(int i=-1;i<=1;i++){var dir=Quaternion.Euler(0,i*22,0)*direction;var shot=Instantiate(prefab,body.position,Quaternion.LookRotation(dir));shot.shard=true;shot.volley=group;shot.Initialize(owner,part,dir,kind);}
            SpellVfx.Burst(body.position,new Color(1,.45f,.18f),.5f);Destroy(gameObject);
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
        void OnTriggerEnter(Collider other){if(initialized&&(kind!=SpellKind.Ricochet||other.GetComponentInParent<SpellProjectile>()))Hit(other,body.position);}
        bool Hit(Collider other,Vector3 point)
        {
            if(spent || kind==SpellKind.TeleportSwap || other==capsule)return false;var projectile=other.GetComponentInParent<SpellProjectile>();if(projectile&&projectile!=this){if(projectile.kind==SpellKind.TeleportSwap||(volley!=0&&volley==projectile.volley))return false;Clash(projectile,point);return true;}if(other.isTrigger)return false;
            var cover=other.GetComponentInParent<ArenaCover>();if(cover){if(kind==SpellKind.TeleportSwap)return false;cover.Strike();ImpactAudio(point);Finish(null,point);return true;}var target=other.GetComponentInParent<ArenaHealth>();if(target && target==owner)return false;
            if(kind==SpellKind.Boomerang&&target){if(passed.Add(target)){SpellAudio.Play("BoomerangHit",point,.4f);SpellCombat.Impact(target,direction,stats,owner);}return false;}
            if(kind==SpellKind.Curve&&target&&!target.Protected){var shield=target.GetComponent<BubbleShield>();var h=target.GetComponent<HeroBattleStats>();if(h&&(!shield||!shield.Active))h.SlowUntil=Time.time+stats.duration*h.DebuffTime;}
            Finish(target,point);return true;
        }
        void ImpactAudio(Vector3 point){
            if(kind==SpellKind.Fragment)SpellAudio.Play("FragmentImpact",point,.38f);
            else if(kind==SpellKind.Curve)SpellAudio.Play("CurveImpact",point,.42f);
        }
        void Finish(ArenaHealth target,Vector3 point)
        {
            if(spent)return;spent=true;
            SpellVfx.Burst(point,kind==SpellKind.Fireball?SpellVfx.Fire:SpellVfx.Arcane,kind==SpellKind.Fireball?1.1f:.65f);
            if(kind==SpellKind.TeleportSwap){if(owner && owner.CurrentHealth>0)SpellCombat.Teleport(owner,target,point);}
            else if(target){ImpactAudio(point);SpellCombat.Impact(target,direction,stats,owner);}
            Destroy(gameObject);
        }
    }
}
