#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace GuerraDeEmpujes
{
 public sealed class CastFeel051Checks:MonoBehaviour
 {
  readonly List<string> rows=new List<string>();void Check(bool ok,string value){rows.Add((ok?"PASS ":"FAIL ")+value);File.WriteAllLines("Logs/CastFeel051Checks.txt",rows);if(!ok)throw new System.Exception(value);}
  IEnumerator Start()
  {
   yield return null;var game=FindFirstObjectByType<MobileRoundGame>();game.FrontEnd.Close();game.Hud.SetVisible(false);var p=game.Player;var driver=p.GetComponent<CharacterAnimationDriver>();var motor=p.GetComponent<ArenaCharacterMotor>();var health=p.GetComponent<ArenaHealth>();
   foreach(var kind in new[]{SpellKind.Fireball,SpellKind.Lightning,SpellKind.TeleportSwap,SpellKind.Bubble})
   {
    motor.SetMoveDirection(Vector3.zero);motor.Warp(new Vector3(0,.04f,0));yield return new WaitForSeconds(.1f);int calls=0;float releaseAt=-1,start=Time.time,burst=-1;BubbleShield shield=null;var profile=driver.GetCastAnimation(kind);driver.SetAim(Vector3.forward*8);
    Check(driver.TryCast(kind,()=>{calls++;releaseAt=Time.time-start;if(kind==SpellKind.Bubble){shield=p.GetComponent<BubbleShield>();if(!shield)shield=p.AddComponent<BubbleShield>();shield.Activate(new SpellLevel{duration=1.2f,radius=2.5f,knockback=8});}}),kind+" cast begins");motor.SetMoveDirection(Vector3.right);yield return new WaitForSeconds(.1f);Check(p.transform.position.x<.04f,kind+" movement blocked during windup");
    float until=Time.time+3;while(driver.IsCasting&&Time.time<until){if(shield&&!shield.Active&&burst<0)burst=Time.time-start;yield return null;}float duration=Time.time-start;
    Check(!driver.IsCasting&&calls==1,kind+" releases exactly once and unlocks");Check(Mathf.Abs(releaseAt-profile.releaseTime)<.10f,kind+" release timing preserved "+releaseAt.ToString("0.000")+"s");
    Check(duration<(kind==SpellKind.Fireball?.58f:kind==SpellKind.Bubble?1.58f:.85f),kind+" shortened total lock "+duration.ToString("0.000")+"s");if(kind==SpellKind.Bubble)Check(burst>1&&burst<1.45f,"shield still explodes with arms opening "+burst.ToString("0.000")+"s");
    var before=p.transform.position;yield return new WaitForSeconds(.15f);Check(p.transform.position.x>before.x+.1f,kind+" character actually moves after recovery");motor.SetMoveDirection(Vector3.zero);yield return new WaitForSeconds(.15f);
   }
   motor.Warp(new Vector3(0,.04f,0));float began=Time.time;var bubble=p.GetComponent<BubbleShield>();driver.TryCast(SpellKind.Bubble,()=>bubble.Activate(new SpellLevel{duration=1.6f,radius=3,knockback=8}),1.6f);while(driver.IsCasting&&Time.time-began<3)yield return null;Check(Time.time-began<1.95f,"level-five shield also has short recovery "+(Time.time-began).ToString("0.000")+"s");
   int pending=0;driver.TryCast(SpellKind.Fireball,()=>pending++);health.invulnerable=false;health.TakeDamage(1000);yield return new WaitForSeconds(.6f);Check(pending==0&&!driver.IsCasting,"death still cancels pending spell");game.NewMatch();game.FrontEnd.Close();game.Hud.AimPreview(0,Vector2.right*80*game.Hud.UiScale);var arrow=GameObject.Find("Mobile aim").GetComponent<LineRenderer>();Check(arrow.startColor.r>.95f&&arrow.startColor.g>.95f&&arrow.startColor.b<.05f&&arrow.startColor.a==1&&arrow.startWidth>=.3f,"yellow opaque thick arrow");Check(Vector3.Distance(arrow.GetPosition(0),arrow.GetPosition(1))>=5,"arrow extends five meters");Check(!GameObject.Find("Selected destination"),"fire arrow remains directional without target cursor");rows.Add("COMPLETE");File.WriteAllLines("Logs/CastFeel051Checks.txt",rows);Destroy(gameObject);
  }
 }
}
#endif
