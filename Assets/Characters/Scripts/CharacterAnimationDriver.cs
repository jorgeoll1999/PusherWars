using System;
using System.Collections;
using UnityEngine;
namespace GuerraDeEmpujes
{
    [Serializable] public sealed class SpellCastAnimation
    {
        public SpellKind kind;
        public string state;
        public AnimationClip clip;
        public float releaseTime;
    }
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator),typeof(ArenaHealth))]
    public sealed class CharacterAnimationDriver:MonoBehaviour
    {
        public const float ShieldOpeningTime=1.8f;
        bool shieldBurstQueued,shieldBurstDelivered;
        public const float CastRecovery=.05f,ShieldRecovery=.08f;
        public SpellCastAnimation[] spellCasts=new SpellCastAnimation[0];
        Animator animator;ArenaHealth health;bool casting;float speed;bool running,released,releaseQueued;Action pendingRelease;
        Vector3 aimPoint;float playbackSpeed=1;readonly Vector3[] handAxes=new Vector3[2],handUpAxes=new Vector3[2];
        public void SetAim(Vector3 point){aimPoint=point;}
        public bool IsCasting=>casting;
        public SpellKind ActiveSpellKind{get;private set;}
        public event Action<SpellKind> SpellReleased;
        void Awake()
        {
            animator=GetComponent<Animator>();health=GetComponent<ArenaHealth>();health.onDeath.AddListener(Die);
            for(int i=0;i<2;i++)
            {
                var h=animator.GetBoneTransform(i==0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                var l=animator.GetBoneTransform(i==0?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);if(!h || !l)continue;
                var axis=(h.position-l.position).normalized;
                foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=skin.sharedMesh;if(!mesh || !mesh.isReadable)continue;int index=Array.IndexOf(skin.bones,h);if(index<0)continue;
                    var weights=mesh.boneWeights;var vertices=mesh.vertices;var points=new System.Collections.Generic.List<Vector3>();
                    int forearm=Array.IndexOf(skin.bones,l);
                    var reference=h.InverseTransformDirection(axis);
                    if(forearm>=0){var forward=mesh.bindposes[index].inverse.MultiplyPoint3x4(Vector3.zero)-mesh.bindposes[forearm].inverse.MultiplyPoint3x4(Vector3.zero);reference=mesh.bindposes[index].MultiplyVector(forward).normalized;}
                    float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
                    for(int v=0;v<vertices.Length;v++){var w=weights[v];if((w.boneIndex0==index && w.weight0>.98f)||(w.boneIndex1==index && w.weight1>.98f)||(w.boneIndex2==index && w.weight2>.98f)||(w.boneIndex3==index && w.weight3>.98f)){var point=mesh.bindposes[index].MultiplyPoint3x4(vertices[v]);points.Add(point);float projection=Vector3.Dot(point,reference);minimum=Mathf.Min(minimum,projection);maximum=Mathf.Max(maximum,projection);}}
                    var proximal=Vector3.zero;var distal=Vector3.zero;int near=0,far=0;
                    foreach(var point in points){float t=Vector3.Dot(point,reference);if(t<minimum+(maximum-minimum)*.15f){proximal+=point;near++;}if(t>maximum-(maximum-minimum)*.15f){distal+=point;far++;}}
                    if(near>0 && far>0)axis=h.TransformDirection((distal/far-proximal/near).normalized);

                }
                handAxes[i]=h.InverseTransformDirection(axis);handUpAxes[i]=h.InverseTransformDirection(Vector3.up);
            }
        }
        void Start(){if(!casting && health.CurrentHealth>0){animator.Play("Locomotion");running=true;}}
        public void SetSpeed(float value){speed=value;if(casting || health.CurrentHealth<=0)return;animator.speed=value>1.4f?Mathf.Min(3.2f,value/1.4f):1;animator.SetFloat("MoveSpeed",value,.12f,Time.fixedDeltaTime);if(!running){running=true;animator.CrossFadeInFixedTime("Locomotion",.16f);}}
        public SpellCastAnimation GetCastAnimation(SpellKind kind){foreach(var c in spellCasts)if(c!=null && c.kind==kind)return c;return null;}
        public float GetCastDuration(SpellKind kind){var c=GetCastAnimation(kind);return c!=null && c.clip?c.clip.length/(kind==SpellKind.Bubble?ShieldOpeningTime/1.2f:1):0;}
        public bool TryCast(Action release){return TryCast(SpellKind.Fireball,release);}
        public bool TryCast(SpellKind kind,Action release,float shieldDuration=1.2f)
        {
            if(casting || health.CurrentHealth<=0)return false;
            var profile=GetCastAnimation(kind);if(profile==null || !profile.clip || !animator.HasState(0,Animator.StringToHash(profile.state)))return false;
            playbackSpeed=kind==SpellKind.Bubble?ShieldOpeningTime/Mathf.Max(.1f,shieldDuration):1;
            ActiveSpellKind=kind;pendingRelease=release;released=releaseQueued=shieldBurstQueued=shieldBurstDelivered=false;
            StartCoroutine(Cast(profile));return true;
        }
        IEnumerator Cast(SpellCastAnimation profile)
        {
            casting=true;running=false;animator.speed=playbackSpeed;
            animator.CrossFadeInFixedTime(profile.state,.06f,0,0);
            if(profile.kind==SpellKind.Bubble)releaseQueued=true;
            float started=Time.time;
            float releaseDeadline=started+Mathf.Max(.15f,profile.releaseTime/playbackSpeed+.15f);
            while(!released && Time.time<releaseDeadline)yield return null;
            // Preserve clip events as the primary release; recover if an event is absent.
            if(!released){releaseQueued=true;while(!released)yield return null;}
            if(profile.kind==SpellKind.Bubble){float burstDeadline=started+ShieldOpeningTime/playbackSpeed+.15f;while(!shieldBurstDelivered && Time.time<burstDeadline)yield return null;if(!shieldBurstDelivered){var shield=GetComponent<BubbleShield>();if(shield)shield.Detonate();shieldBurstDelivered=true;}yield return new WaitForSeconds(ShieldRecovery);}
            else yield return new WaitForSeconds(CastRecovery);
            casting=false;running=true;animator.speed=1;pendingRelease=null;releaseQueued=shieldBurstQueued=false;
            if(health.CurrentHealth>0){animator.SetFloat("MoveSpeed",speed);animator.CrossFadeInFixedTime("Locomotion",.16f);}
        }
        // Imported clip events select the release frame. Dispatch after the animated bones update.
        [UnityEngine.Scripting.Preserve] public void OnSpellRelease(int spellKind){if(casting && !released && spellKind==(int)ActiveSpellKind && health.CurrentHealth>0)releaseQueued=true;}
        [UnityEngine.Scripting.Preserve] public void OnShieldBurst(){if(casting && ActiveSpellKind==SpellKind.Bubble && health.CurrentHealth>0)shieldBurstQueued=true;}
        void LateUpdate(){CorrectAimingArms();if(shieldBurstQueued){shieldBurstQueued=false;shieldBurstDelivered=true;var shield=GetComponent<BubbleShield>();if(shield && casting && health.CurrentHealth>0)shield.Detonate();}if(!releaseQueued || !casting || released || health.CurrentHealth<=0)return;releaseQueued=false;released=true;var action=pendingRelease;pendingRelease=null;action?.Invoke();SpellReleased?.Invoke(ActiveSpellKind);}
        void CorrectAimingArms()
        {
            if(!casting || (ActiveSpellKind!=SpellKind.Lightning && ActiveSpellKind!=SpellKind.TeleportSwap))return;
            for(int i=0;i<2;i++)
            {
                var upper=animator.GetBoneTransform(i==0?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
                var lower=animator.GetBoneTransform(i==0?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
                var hand=animator.GetBoneTransform(i==0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                if(!upper || !lower || !hand)continue;
                var direction=aimPoint-upper.position;direction.y=0;
                if(direction.sqrMagnitude<.05f)direction=transform.forward;direction.Normalize();
                float u=Vector3.Distance(upper.position,lower.position),l=Vector3.Distance(lower.position,hand.position),reach=(u+l)*.995f;
                float along=(u*u-l*l+reach*reach)/(2*reach);float bend=Mathf.Sqrt(Mathf.Max(0,u*u-along*along));
                var pole=Vector3.Cross(Vector3.up,direction)*(i==0?1:-1)-Vector3.up;
                pole=Vector3.ProjectOnPlane(pole,direction).normalized;
                var elbow=upper.position+direction*along+pole*bend;
                var target=upper.position+direction*reach;
                upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
                lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
                hand.rotation=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(handAxes[i],handUpAxes[i]));
            }
        }
        public Vector3 GetCastOrigin(SpellKind kind)
        {
            var center=transform.position+Vector3.up;
            if(!casting || ActiveSpellKind!=kind || kind==SpellKind.Bubble)return center;
            var right=animator.GetBoneTransform(HumanBodyBones.RightHand);var left=animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if(!right || !left)return center;
            var palm=kind==SpellKind.Fireball?(left.position+right.position)*.5f:right.position;
            var offset=Vector3.ClampMagnitude(palm+transform.forward*.05f-center,.95f);
            if(offset.sqrMagnitude<.0001f)return center;
            float distance=offset.magnitude;var direction=offset/distance;
            var hits=Physics.SphereCastAll(center,.18f,direction,distance,~0,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){if(hit.collider.GetComponentInParent<ArenaHealth>()==health)continue;distance=Mathf.Max(0,hit.distance-.02f);break;}
            return center+direction*distance;
        }
        void Die(){StopAllCoroutines();casting=false;pendingRelease=null;releaseQueued=shieldBurstQueued=false;animator.speed=1;animator.CrossFadeInFixedTime("Death",.08f);}
        void OnDisable(){StopAllCoroutines();casting=false;pendingRelease=null;releaseQueued=shieldBurstQueued=false;}
        void OnDestroy(){if(health)health.onDeath.RemoveListener(Die);}
    }
}
