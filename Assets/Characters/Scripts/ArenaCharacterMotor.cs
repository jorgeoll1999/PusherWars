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
        public void ApplyKnockback(Vector3 direction,float strength){if(health.CurrentHealth<=0 || health.invulnerable)return;var stats=GetComponent<HeroBattleStats>();direction.y=0;impulse+=direction.normalized*Mathf.Max(0,strength)*(1-(stats?stats.PushResistance:0));}
        void FixedUpdate()
        {
            if(health.CurrentHealth<=0 || body.isKinematic)return;
            var walk=animationDriver && animationDriver.IsCasting?Vector3.zero:movement*moveSpeed;
            locomotionVelocity=Vector3.MoveTowards(locomotionVelocity,walk,(walk.sqrMagnitude>.01f?18:22)*Time.fixedDeltaTime);
            var displacement=body.position-lastPosition;displacement.y=0;lastPosition=body.position;
            body.linearVelocity=new Vector3(locomotionVelocity.x+impulse.x,body.linearVelocity.y,locomotionVelocity.z+impulse.z);
            impulse=Vector3.MoveTowards(impulse,Vector3.zero,knockbackDrag*Time.fixedDeltaTime);
            if(walk.sqrMagnitude>.01f)body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(walk),600*Time.fixedDeltaTime));
            if(animationDriver)animationDriver.SetSpeed(walk.sqrMagnitude>.01f?Mathf.Min(moveSpeed,Mathf.Max(0,Vector3.Dot(displacement/Time.fixedDeltaTime,walk.normalized))):0);
        }
        void Die(){movement=impulse=Vector3.zero;if(!body.isKinematic)body.linearVelocity=Vector3.zero;body.isKinematic=true;var c=GetComponent<CapsuleCollider>();c.height=.5f;c.center=new Vector3(0,.25f,0);}
        void OnDestroy(){if(health)health.onDeath.RemoveListener(Die);}
    }
}
