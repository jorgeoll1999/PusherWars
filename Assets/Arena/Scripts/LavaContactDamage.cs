using UnityEngine;

namespace GuerraDeEmpujes
{
    [RequireComponent(typeof(ArenaHealth))]
    public sealed class LavaContactDamage : MonoBehaviour
    {
        [Tooltip("Player body collider or CharacterController. Assign explicitly for characters with several colliders.")]
        public Collider bodyCollider;
        [Min(0)] public float groundTolerance = .10f;
        public LayerMask groundLayers = ~0;
        public bool IsOnLava { get; private set; }
        ArenaHealth health;
        void Awake() { health=GetComponent<ArenaHealth>(); if(!bodyCollider)bodyCollider=GetComponent<Collider>(); }
        void FixedUpdate() { TickDamage(Time.fixedDeltaTime); }
        public void TickDamage(float deltaTime)
        {
            IsOnLava=false;
            if(!health)health=GetComponent<ArenaHealth>();
            if(!bodyCollider || !bodyCollider.enabled || !health || deltaTime<=0)return;
            var b=bodyCollider.bounds;
            // Start inside the body: Unity raycasts ignore the originating collider.
            // The nearest solid floor wins, so safe rock cannot be damaged by lava underneath it.
            if(!Physics.Raycast(b.center,Vector3.down,out var hit,b.extents.y+groundTolerance,groundLayers,QueryTriggerInteraction.Ignore))return;
            var lava=hit.collider.GetComponent<LavaSurface>();
            if(!lava || hit.normal.y<.5f)return;
            IsOnLava=true;
            health.TakeDamage(lava.damagePerSecond*deltaTime);
        }
    }
}
