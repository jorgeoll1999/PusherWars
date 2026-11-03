using System;
using UnityEngine;
namespace GuerraDeEmpujes
{
 [Serializable] public sealed class HeroProgression
 {
  public int gold=600;public int[] spells={1,0,0,1,0,0,0,0,0,0,0};public int[] items=new int[Arsenal.Items];public bool shopOpen=true;
  public int Count {get{int n=0;foreach(var level in spells)if(level>0)n++;return n;}}
  public int SpellCost(int i)=>i<0||i>=spells.Length||spells[i]>=5||(spells[i]==0&&Count>=Arsenal.MaxEquipped)?0:100+Mathf.Max(0,spells[i]-1)*50;
  public int ItemCost(int i)=>i<0||i>=items.Length||items[i]>=5?0:100+items[i]*60+(i==0?20:0);
  public bool BuySpell(int i){int cost=SpellCost(i);if(!shopOpen||cost<=0||gold<cost)return false;gold-=cost;spells[i]++;return true;}
  public bool BuyItem(int i){int cost=ItemCost(i);if(!shopOpen||cost<=0||gold<cost)return false;gold-=cost;items[i]++;return true;}
  public void Earn(int amount){if(amount>0)gold=Mathf.Min(99999,gold+amount);}
 }
 public sealed class HeroBattleStats:MonoBehaviour
 {
  public const float BaseMoveSpeed=2.72f;
  public static float MoveSpeed(int bootsLevel)=>BaseMoveSpeed*(1+Mathf.Clamp(bootsLevel,0,5)*.12f);
  public HeroProgression progression;
  public float PushResistance=>progression==null?0:progression.items[1]*.08f;
  public float AreaMultiplier=>progression==null?1:1+progression.items[2]*.035f;
  public float DebuffTime=>progression==null||progression.items[4]==0?1:.8f-.1f*progression.items[4];
  public bool ReplicaRamming;TrailRenderer dashTrail;ArenaCharacterMotor motor;public float SlowUntil; public GameObject summoned; public int summonType;
  ArenaHealth health;float regen;GameObject ward;ParticleSystem dashSparks;
  void Awake(){motor=GetComponent<ArenaCharacterMotor>();health=GetComponent<ArenaHealth>();}
  void Update(){bool dash=OnlineSession.Replica?ReplicaRamming:motor.Ramming;
   if(dash&&!dashTrail){var wick=new GameObject("Estela del ariete");wick.transform.SetParent(transform,false);wick.transform.localPosition=Vector3.up*.8f;dashTrail=wick.AddComponent<TrailRenderer>();dashTrail.sharedMaterial=SpellVfx.Glow;dashTrail.time=.40f;dashTrail.startWidth=.95f;dashTrail.endWidth=.02f;dashTrail.minVertexDistance=.06f;dashTrail.startColor=new Color(2,1,.12f);dashTrail.endColor=Color.clear;}
   if(dashTrail){if(dash&&!dashTrail.emitting)dashTrail.Clear();dashTrail.emitting=dash;}
   if(dash&&!dashSparks)dashSparks=SpellVfx.Particles(transform,new Color(3,1.4f,.12f),48,1,.4f,.22f,true);
   if(dashSparks){if(dash&&!dashSparks.isPlaying)dashSparks.Play();if(!dash&&dashSparks.isPlaying)dashSparks.Stop();}
   bool active=health.WardRemaining>0;if(active&&!ward)ward=ArsenalPowerArt.Angel(transform);if(ward)ward.SetActive(active);if(OnlineSession.Replica||progression==null||health.CurrentHealth<=0||health.invulnerable)return;
   var game=OnlineSession.Instance?OnlineSession.Instance.game:null;if(!game||game.Phase!=ArenaPhase.Fighting)return;
   regen+=Time.deltaTime;if(regen>=1){int ticks=(int)regen;regen-=ticks;health.Heal(ticks*progression.items[3]);}}
  void OnDestroy(){if(summoned)Destroy(summoned);}
  public void Apply(HeroProgression model){progression=model;GetComponent<ArenaCharacterMotor>().moveSpeed=MoveSpeed(model.items[0]);var load=GetComponent<SpellLoadout>();load.EnsureCatalog();for(int i=0;i<load.slots.Length;i++){load.slots[i].level=model.spells[i];load.slots[i].readyAt=0;}}
 }
}
