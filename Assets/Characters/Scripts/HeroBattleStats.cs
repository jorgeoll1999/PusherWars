using System;
using UnityEngine;
namespace GuerraDeEmpujes
{
 [Serializable] public sealed class HeroProgression
 {
  public int gold=600;public int[] spells={1,1,1,1};public int[] items={0,0,0};public bool shopOpen=true;
  public int SpellCost(int index){return index>=0 && index<4 && spells[index]<5?100+(spells[index]-1)*50:0;}
  public int ItemCost(int index){return index>=0 && index<3 && items[index]<5?100+items[index]*60+(index==0?20:0):0;}
  public bool BuySpell(int index){if(!shopOpen || index<0 || index>=4)return false;int cost=SpellCost(index);if(cost<=0 || gold<cost)return false;gold-=cost;spells[index]++;return true;}
  public bool BuyItem(int index){if(!shopOpen || index<0 || index>=3)return false;int cost=ItemCost(index);if(cost<=0 || gold<cost)return false;gold-=cost;items[index]++;return true;}
  public void Earn(int amount){if(amount>0)gold=Mathf.Min(99999,gold+amount);}
 }
 public sealed class HeroBattleStats:MonoBehaviour
 {
  public const float BaseMoveSpeed=2.72f;
  public static float MoveSpeed(int bootsLevel)=>BaseMoveSpeed*(1+Mathf.Clamp(bootsLevel,0,5)*.12f);
  public HeroProgression progression;
  public float PushResistance=>progression==null?0:progression.items[1]*.08f;
  public float AreaMultiplier=>progression==null?1:1+progression.items[2]*.035f;
  public void Apply(HeroProgression model){progression=model;GetComponent<ArenaCharacterMotor>().moveSpeed=MoveSpeed(model.items[0]);var load=GetComponent<SpellLoadout>();for(int i=0;i<4;i++){load.slots[i].level=model.spells[i];load.slots[i].readyAt=0;}}
 }
}
