#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GuerraDeEmpujes
{
 public sealed class Mobile050Checks:MonoBehaviour
 {
  readonly List<string> rows=new List<string>();
  void Check(bool ok,string text){rows.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Logs/Mobile050Checks.txt",rows);if(!ok)throw new System.Exception(text);}
  IEnumerator Start()
  {
   var game=FindFirstObjectByType<MobileRoundGame>();yield return null;game.FrontEnd.Close();yield return null;var hud=game.Hud;var player=game.Player;
   Check(Mathf.Abs(player.GetComponent<ArenaCharacterMotor>().moveSpeed-2.72f)<.001f,"base movement speed +70 percent");Check(Mathf.Abs(HeroBattleStats.MoveSpeed(5)-4.352f)<.001f,"boots keep proportional upgrades");
   foreach(int spell in new[]{0,1}){float a=Vector3.Distance(player.transform.position,hud.AimPoint(spell,new Vector2(20,0)*hud.UiScale)),b=Vector3.Distance(player.transform.position,hud.AimPoint(spell,new Vector2(120,0)*hud.UiScale));Check(Mathf.Abs(a-b)<.01f,"spell "+spell+" drag controls direction only");hud.AimPreview(spell,new Vector2(70,0)*hud.UiScale);Check(!GameObject.Find("Selected destination")&&!GameObject.Find("Mobile aiming range"),"spell "+spell+" has no target cursor or range circle");Check(GameObject.Find("Tactical aim arrow"),"visible directional mesh arrow");}
   Check(Vector3.Distance(player.transform.position,hud.AimPoint(2,new Vector2(120,0)*hud.UiScale))>Vector3.Distance(player.transform.position,hud.AimPoint(2,new Vector2(20,0)*hud.UiScale))+2,"teleport drag selects distance and direction");hud.AimPreview(2,new Vector2(70,0)*hud.UiScale);Check(GameObject.Find("Selected destination")&&GameObject.Find("Mobile aiming range"),"only teleport has a destination circle");hud.HideAim();
   const string key="guerra.hud.positions.v1";bool existed=PlayerPrefs.HasKey(key);string old=PlayerPrefs.GetString(key,"");
   try{hud.BeginLayoutEdit();Check(hud.Editing&&game.MenuOpen,"layout editing blocks gameplay controls");var target=(RectTransform)hud.SafeRoot.Find("Skill FUEGO");var before=target.position;var handle=GameObject.Find("Move Skill FUEGO").GetComponent<HudLayoutDrag>();handle.OnDrag(new PointerEventData(EventSystem.current){delta=new Vector2(-180,180)*hud.UiScale});Check(Vector3.Distance(before,target.position)>100*hud.UiScale,"real UI drag moves spell holder");var editor=handle.editor;var moved=target.position;editor.End(true);Check(PlayerPrefs.HasKey(key),"layout saved locally");target.anchoredPosition+=new Vector2(-90,0);var loader=new GameObject("Layout reload check").AddComponent<HudLayoutEditor>();loader.Setup(hud);Check(Vector3.Distance(moved,target.position)<2,"saved layout loads in fresh editor component");Destroy(loader.gameObject);game.FrontEnd.Close();hud.BeginLayoutEdit();handle=GameObject.Find("Move Skill FUEGO").GetComponent<HudLayoutDrag>();handle.OnDrag(new PointerEventData(EventSystem.current){delta=new Vector2(-100,0)*hud.UiScale});handle.editor.End(false);Check(Vector3.Distance(moved,target.position)<2,"cancel restores prior layout");game.FrontEnd.Close();hud.BeginLayoutEdit();var fresh=GameObject.Find("Move Skill FUEGO").GetComponent<HudLayoutDrag>();fresh.editor.ResetDefaults();Check(Vector3.Distance(before,target.position)<2,"reset returns default control position");fresh.editor.End(false);}
   finally{if(existed)PlayerPrefs.SetString(key,old);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
   game.FrontEnd.Close();Time.timeScale=1;game.Arena.enabled=false;game.Arena.SetRadius(6);Physics.SyncTransforms();RaycastHit hit;
   Check(Physics.Raycast(new Vector3(9,2,0),Vector3.down,out hit,4)&&hit.collider.GetComponent<LavaSurface>(),"shrinking exposes solid lava floor");Check(Physics.Raycast(new Vector3(4,2,0),Vector3.down,out hit,4)&&hit.collider.name.StartsWith("RoundCourt"),"center remains safe solid rock");Check(ShrinkingArena.RadiusAt(1,0)<ShrinkingArena.RadiusAt(1,60)&&ShrinkingArena.StartingRadius(6)<ShrinkingArena.StartingRadius(1),"arena shrinks during combat and in later rounds");
   var health=player.GetComponent<ArenaHealth>();var oldPosition=player.transform.position;player.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(9,-.06f,0));health.invulnerable=false;player.GetComponent<LavaContactDamage>().TickDamage(.5f);Check(health.CurrentHealth<100,"exposed lava damages character without sinking");health.ResetHealth();health.invulnerable=true;player.GetComponent<ArenaCharacterMotor>().Warp(oldPosition);game.Arena.SetRadius(ShrinkingArena.FullRadius);game.Arena.enabled=true;
   var prefab=player.GetComponent<SpellLoadout>().slots[0].spell.projectilePrefab;var level=new SpellLevel{speed=20,range=20,hitRadius=.18f};
   var aShot=Instantiate(prefab,new Vector3(-.45f,3.5f,0),Quaternion.LookRotation(Vector3.right));var bShot=Instantiate(prefab,new Vector3(.45f,3.5f,0),Quaternion.LookRotation(Vector3.left));aShot.Initialize(health,level,Vector3.right);bShot.Initialize(health,level,Vector3.left);yield return new WaitForSeconds(.08f);Check(!aShot&&!bShot,"opposing projectiles collide and cancel both");
   level.speed=40;aShot=Instantiate(prefab,new Vector3(0,3.5f,0),Quaternion.LookRotation(Vector3.right));aShot.Initialize(health,level,Vector3.right);level.speed=5;bShot=Instantiate(prefab,new Vector3(.6f,3.5f,0),Quaternion.LookRotation(Vector3.right));bShot.Initialize(health,level,Vector3.right);yield return new WaitForSeconds(.08f);Check(!aShot&&!bShot,"same-direction projectile catch collides");
   level.speed=10;aShot=Instantiate(prefab,new Vector3(0,3.5f,2),Quaternion.LookRotation(Vector3.right));bShot=Instantiate(prefab,new Vector3(0,3.5f,4),Quaternion.LookRotation(Vector3.right));aShot.Initialize(health,level,Vector3.right);bShot.Initialize(health,level,Vector3.right);yield return new WaitForSeconds(.08f);Check(aShot&&bShot,"separated parallel projectiles do not collide");Destroy(aShot.gameObject);Destroy(bShot.gameObject);rows.Add("COMPLETE");File.WriteAllLines("Logs/Mobile050Checks.txt",rows);Destroy(gameObject);
  }
 }
}
#endif
