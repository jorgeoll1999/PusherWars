using UnityEngine;
using UnityEngine.Events;

namespace GuerraDeEmpujes
{
    public sealed class ArenaHealth : MonoBehaviour
    {
        [Min(1)] public float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        public float CurrentHealth => currentHealth;
        public bool invulnerable;
        public ArenaHealth LastAttacker{get;private set;}
        public float LastHitTime{get;private set;}
        public void TagAttacker(ArenaHealth attacker){if(attacker && attacker!=this){LastAttacker=attacker;LastHitTime=Time.time;}}
        public UnityEvent<float> onHealthChanged = new UnityEvent<float>();
        public UnityEvent onDeath = new UnityEvent();
        void Awake() { ResetHealth(); }
        public void ResetHealth() { currentHealth = maxHealth; LastAttacker=null; onHealthChanged.Invoke(currentHealth); }
        public void ApplyNetworkHealth(float value){float old=currentHealth;currentHealth=Mathf.Clamp(value,0,maxHealth);onHealthChanged.Invoke(currentHealth);if(old>0&&currentHealth==0)onDeath.Invoke();}
        public void TakeDamage(float amount,ArenaHealth attacker=null)
        {
            if (OnlineSession.Replica || invulnerable || amount <= 0 || currentHealth <= 0) return;
            TagAttacker(attacker);
            currentHealth = Mathf.Max(0, currentHealth - amount);
            onHealthChanged.Invoke(currentHealth);
            if (currentHealth == 0) onDeath.Invoke();
        }
    }
}
