using UnityEngine;
namespace GuerraDeEmpujes
{
    public enum SpellKind { Fireball, Lightning, TeleportSwap, Bubble, Ram, Ricochet, Aegis, Fragment, Curve, Monolith, Boomerang }
    [System.Serializable] public struct SpellLevel
    {
        [Min(0)]public float damage,knockback;
        [Min(.01f)]public float cooldown,speed,range;
        [Min(0)]public int upgradeCost;
        [Min(0)]public float radius,duration;
        [Min(0)]public float hitRadius;
    }
    [CreateAssetMenu(menuName="Guerra de empujes/Spell Definition")]
    public sealed class SpellDefinition:ScriptableObject
    {
        public string displayName;
        public SpellKind kind;
        public SpellProjectile projectilePrefab;
        public SpellLevel[] levels=new SpellLevel[0];
        public bool TryGetLevel(int level,out SpellLevel stats){stats=default; if(level<1 || levels==null || level>levels.Length)return false;stats=levels[level-1];return true;}
    }
}
