using UnityEngine;
namespace GuerraDeEmpujes
{
    [RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider),typeof(ArenaHealth))]
    public sealed class ArenaCharacterMotor:MonoBehaviour
    {
        public float moveSpeed=3.2f,knockbackDrag=5f;
        Rigidbody body;ArenaHealth health;CharacterAnimationDriver animationDriver;Vector3 movement,impulse,lastPosition,locomotionVelocity;
        public Vector3 KnockbackVelocity=>impulse;public Vector3 DesiredMove=>movement;
        public void Warp(Vector3 position){impulse=locomotionVelocity=Vector3.zero;lastPosition=position;body.position=position;transform.position=position;if(!body.isKinematic)body.linearVelocity=Vector3.zero;Physics.SyncTransforms();}
        void Awake(){body=GetComponent<Rigidbody>();lastPosition=body.position;health=GetComponent<ArenaHealth>();animationDriver=GetComponent<CharacterAnimationDriver>();health.onDeath.AddListener(Die);}
        public void SetMoveDirection(Vector3 direction){movement=Vector3.ClampMagnitude(new Vector3(direction.x,0,direction.z),1);}
        public void ApplyKnockback(Vector3 direction,float strength){if(health.CurrentHealth<=0 || health.Protected)return;var stats=GetComponent<HeroBattleStats>();direction.y=0;impulse+=direction.normalized*Mathf.Max(0,strength)*(1-(stats?stats.PushResistance:0));}
        Vector3 ramDir;float ramLeft,lastRamHit;SpellLevel ram;
        public bool Ramming=>ramLeft>0;
        public void BeginRam(Vector3 dir,SpellLevel stats){SpellAudio.Play("RamCast",transform.position,.55f);ram=stats;ramLeft=stats.range;ramDir=dir;impulse=locomotionVelocity=Vector3.zero;lastRamHit=-1;}
        void StepRam(){
            float step=Mathf.Min(ramLeft,ram.speed*Time.fixedDeltaTime);ramLeft-=step;
            var pos=body.position;if(ShrinkingArena.ReflectEdge(pos,pos+ramDir*step,.45f,ref ramDir))SpellAudio.Play("RamImpact",pos,.45f);
            if(Time.time>=lastRamHit){
                var hits=Physics.SphereCastAll(pos+Vector3.up*.85f,.40f,ramDir,step+.05f,~0,QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
                foreach(var hit in hits){var h=hit.collider.GetComponentInParent<ArenaHealth>();var c=hit.collider.GetComponentInParent<ArenaCover>();
                    if(h==health||(!h&&!c))continue;
                    if(h){var impact=ram;impact.knockback*=2;SpellCombat.Impact(h,ramDir,impact,health);ramLeft=Mathf.Min(ramLeft,1.2f);}
                    SpellAudio.Play("RamImpact",hit.point,.5f);var norm=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
                    ramDir=norm.sqrMagnitude>.5f?Vector3.Reflect(ramDir,norm).normalized:-ramDir;lastRamHit=Time.time+.09f;break;}
            }
            body.linearVelocity=ramDir*(step/Time.fixedDeltaTime)+Vector3.up*body.linearVelocity.y;body.MoveRotation(Quaternion.LookRotation(ramDir));lastPosition=pos;
        }
        void FixedUpdate()
        {
            if(health.CurrentHealth<=0 || body.isKinematic)return;
            if(Ramming){StepRam();return;}var hero=GetComponent<HeroBattleStats>();var walk=animationDriver && animationDriver.IsCasting?Vector3.zero:movement*moveSpeed*(hero&&Time.time<hero.SlowUntil?.6f:1);
            locomotionVelocity=Vector3.MoveTowards(locomotionVelocity,walk,(walk.sqrMagnitude>.01f?18:22)*Time.fixedDeltaTime);
            var displacement=body.position-lastPosition;displacement.y=0;lastPosition=body.position;
            body.linearVelocity=new Vector3(locomotionVelocity.x+impulse.x,body.linearVelocity.y,locomotionVelocity.z+impulse.z);
            impulse=Vector3.MoveTowards(impulse,Vector3.zero,knockbackDrag*Time.fixedDeltaTime/(hero?hero.DebuffTime:1));
            if(walk.sqrMagnitude>.01f)body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(walk),600*Time.fixedDeltaTime));
            if(animationDriver)animationDriver.SetSpeed(walk.sqrMagnitude>.01f?Mathf.Min(moveSpeed,Mathf.Max(0,Vector3.Dot(displacement/Time.fixedDeltaTime,walk.normalized))):0);
        }
        void Die(){movement=impulse=Vector3.zero;if(!body.isKinematic)body.linearVelocity=Vector3.zero;body.isKinematic=true;var c=GetComponent<CapsuleCollider>();c.height=.5f;c.center=new Vector3(0,.25f,0);}
        void OnDestroy(){if(health)health.onDeath.RemoveListener(Die);}
    }
}
