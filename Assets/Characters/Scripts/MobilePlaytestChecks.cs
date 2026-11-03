#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace GuerraDeEmpujes
{
 public sealed class MobilePlaytestChecks:MonoBehaviour
 {
  readonly List<string> rows=new List<string>();
  void Check(bool ok,string text){rows.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("ArtSource/Characters/Review/MobilePlaytestChecks.txt",rows);}
  IEnumerator Start()
  {
   Application.runInBackground=true;var model=new HeroProgression{gold=10000};
   for(int i=0;i<4;i++){for(int n=0;n<4;n++)Check(model.BuySpell(i),"Spell "+i+" upgrade "+(n+2));Check(model.spells[i]==5 && !model.BuySpell(i),"Spell level five cap");}
   for(int i=0;i<3;i++){for(int n=0;n<5;n++)Check(model.BuyItem(i),"Item "+i+" upgrade "+(n+1));Check(model.items[i]==5 && !model.BuyItem(i),"Item level five cap");}
   var poor=new HeroProgression{gold=0};Check(!poor.BuyItem(0) && !poor.BuySpell(0) && poor.gold==0,"Insufficient funds cannot buy or go negative");poor.gold=600;poor.shopOpen=false;Check(!poor.BuyItem(0) && !poor.BuySpell(0) && poor.gold==600,"Purchases blocked outside the shop");
   var game=FindFirstObjectByType<MobileRoundGame>();yield return new WaitForSeconds(.2f);Check(game.Phase==ArenaPhase.Shop && game.Fighters.Count==4,"Initial shop and four fighters");
   Check(game.BuySpell(0) && game.BuyItem(0) && game.BuyItem(1) && game.BuyItem(2) && game.progress.gold==180,"Spell and all three inventory items spend exact gold");
   var stats=game.Player.GetComponent<HeroBattleStats>();Check(Mathf.Abs(stats.PushResistance-.08f)<.001f && Mathf.Abs(stats.AreaMultiplier-1.035f)<.001f && game.Player.GetComponent<ArenaCharacterMotor>().moveSpeed>1.4f,"Boots, hood and area bonuses applied without character equipment meshes");
   foreach(var p in game.Fighters){var bot=p.GetComponent<MobileArenaBot>();if(bot)bot.enabled=false;}
   game.Ready();yield return new WaitForSeconds(3.25f);Check(game.CanControl && !game.BuySpell(1),"Countdown starts combat and locks shop");
   var joy=FindFirstObjectByType<MobileJoystick>();var down=new PointerEventData(EventSystem.current){pointerId=1001,position=new Vector2(Screen.width*.2f,Screen.height*.2f)};joy.OnPointerDown(down);down.position+=new Vector2(60,40)*game.Hud.UiScale;joy.OnDrag(down);var before=game.Player.transform.position;yield return new WaitForSeconds(.3f);Check(joy.Axis.magnitude>.4f && Vector3.Distance(before,game.Player.transform.position)>.1f,"Floating joystick moves the character");
   MobileSkillButton fire=null;foreach(var skill in FindObjectsByType<MobileSkillButton>(FindObjectsSortMode.None))if(skill.index==0)fire=skill;
   var press=new PointerEventData(EventSystem.current){pointerId=1002,position=RectTransformUtility.WorldToScreenPoint(null,fire.transform.position)};fire.OnPointerDown(press);press.position+=new Vector2(80,20)*game.Hud.UiScale;fire.OnDrag(press);fire.OnPointerUp(press);
   Check(joy.Axis.magnitude>.4f && game.Player.GetComponent<CharacterAnimationDriver>().IsCasting && game.Player.GetComponent<SpellLoadout>().slots[0].readyAt>Time.time,"Independent second finger aims and casts while joystick remains held");joy.OnPointerUp(down);Check(joy.Axis==Vector2.zero,"Joystick release does not stick");
   var health=game.Player.GetComponent<ArenaHealth>();health.TakeDamage(20);Check(health.CurrentHealth==80,"Health decreases and red bar has live health source");yield return new WaitForSeconds(.45f);
   for(int i=1;i<game.Fighters.Count;i++)game.Fighters[i].GetComponent<ArenaHealth>().TakeDamage(1000,health);
   Check(game.Phase==ArenaPhase.Result && game.progress.gold==805,"Eliminations and round victory grant exact gold");yield return new WaitForSeconds(2.7f);
   Check(game.Phase==ArenaPhase.Shop && game.Round==2 && game.progress.spells[0]==2 && game.progress.items[0]==1 && game.progress.gold==805,"Next-round shop preserves gold, items and spell levels");
   Check(game.Player.GetComponent<ArenaHealth>().CurrentHealth==100,"New round restores the living character and health");
   rows.Add(rows.Exists(r=>r.StartsWith("FAIL"))?"FAILED":"ALL CHECKS PASSED");File.WriteAllLines("ArtSource/Characters/Review/MobilePlaytestChecks.txt",rows);Destroy(gameObject);
  }
 }
}
#endif
