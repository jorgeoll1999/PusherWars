#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace GuerraDeEmpujes
{
 public sealed class CitadelFlowChecks:MonoBehaviour
 {
  readonly List<string> rows=new List<string>();
  void Check(bool pass,string message){rows.Add((pass?"PASS ":"FAIL ")+message);File.WriteAllLines("ArtSource/Characters/Review/CitadelFlowChecks.txt",rows);}
  void Click(Button b){ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);}
  IEnumerator WaitForCombat(MobileRoundGame game){float until=Time.realtimeSinceStartup+25;while(game.Phase!=ArenaPhase.Fighting&&Time.realtimeSinceStartup<until)yield return null;}
  IEnumerator WaitForShop(MobileRoundGame game){float until=Time.realtimeSinceStartup+20;while(game.Phase==ArenaPhase.Result&&Time.realtimeSinceStartup<until)yield return null;}
  IEnumerator Start()
  {
   Application.runInBackground=true;int oldHero=PlayerPrefs.GetInt("citadel.hero",0),oldMode=PlayerPrefs.GetInt("citadel.mode",0);var g=FindFirstObjectByType<MobileRoundGame>();var f=g.FrontEnd;
   yield return new WaitForSecondsRealtime(.3f);f.ShowHome();yield return new WaitForSecondsRealtime(.15f);
   Check(f.IsOpen && Time.timeScale==0 && g.MenuOpen && !g.CanControl,"Home pauses the arena and gates gameplay input");
   Check(Resources.Load<Sprite>("Interface/CitadelBackground") && Resources.Load<Sprite>("Interface/ForgedPanel") && CitadelUi.Title,"Artwork, sliced frame and title font loaded");
   Check(Enumerable.Range(0,4).All(i=>Resources.Load<Sprite>("Interface/Hero"+i) && Resources.Load<Sprite>("Interface/Face"+i) && CitadelUi.Spell(i)),"All four real hero portraits and spell illustrations loaded");
   f.SelectMage(2);yield return new WaitForSecondsRealtime(.15f);Check(f.Page=="Magos" && PlayerPrefs.GetInt("citadel.hero")==2,"Mage selection is connected and saved");
   f.ShowArsenal();yield return new WaitForSecondsRealtime(.15f);var buy=f.GetComponentsInChildren<Button>().First(b=>b.name=="Upgrade spell 0");var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,buy.transform.position)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0 && (hits[0].gameObject==buy.gameObject || hits[0].gameObject.transform.IsChildOf(buy.transform)),"Arsenal purchase button is reachable through decorative frames");
   Click(buy);yield return new WaitForSecondsRealtime(.15f);Check(g.progress.gold==500 && g.progress.spells[0]==2,"Arsenal button spends real round gold and upgrades spell");
   f.ShowModes();yield return new WaitForSecondsRealtime(.15f);Click(f.GetComponentsInChildren<Button>().Where(b=>b.name=="Select").ToArray()[1]);yield return new WaitForSecondsRealtime(.15f);f.ShowLobby();yield return new WaitForSecondsRealtime(.15f);Click(f.GetComponentsInChildren<Button>().First(b=>b.name=="Start battle"));yield return new WaitForSecondsRealtime(3.4f);
   Check(!f.IsOpen && Time.timeScale==1 && g.CanControl,"Lobby start enters live combat and restores gameplay");Check(g.Fighters.Count==2 && g.BattleMode==LocalBattleMode.Duel,"Duel creates exactly one opponent");Check(g.SelectedHero==2 && g.Player.name.Contains("Nival"),"Selected hero is the playable character");Check(g.progress.gold==500 && g.Player.GetComponent<SpellLoadout>().slots[0].level==2,"Purchased spell survives roster selection and battle start");
   f.ShowHome();float clock=g.TimeLeft;var position=g.Player.transform.position;yield return new WaitForSecondsRealtime(.7f);Check(Mathf.Approximately(clock,g.TimeLeft) && Vector3.Distance(position,g.Player.transform.position)<.001f,"Opening menu freezes timer and physics");f.Close();Check(Time.timeScale==1 && !g.MenuOpen,"Continue restores the same round");
   f.ShowModes();yield return new WaitForSecondsRealtime(.15f);Click(f.GetComponentsInChildren<Button>().Where(b=>b.name=="Select").ToArray()[2]);yield return new WaitForSecondsRealtime(.15f);f.StartBattle();yield return new WaitForSecondsRealtime(3.4f);Check(g.Fighters.Count==4 && g.Fighters.Skip(1).All(p=>!p.GetComponent<MobileArenaBot>().enabled),"Training has three passive targets");var health=g.Player.GetComponent<ArenaHealth>();health.TakeDamage(25);Check(health.invulnerable && health.CurrentHealth==100,"Training protects the player's health");
   g.ConfigureBattle(0,LocalBattleMode.FreeForAll);g.NewMatch();
   for(int n=0;n<5;n++){
    foreach(var p in g.Fighters.Skip(1))p.GetComponent<MobileArenaBot>().enabled=false;
    g.Ready();yield return WaitForCombat(g);Check(g.Phase==ArenaPhase.Fighting,"Round "+(n+1)+" reaches combat before damage");foreach(var p in g.Fighters.Skip(1))p.GetComponent<ArenaHealth>().TakeDamage(10000,g.Player.GetComponent<ArenaHealth>());yield return WaitForShop(g);
   }
   Check(g.Phase==ArenaPhase.MatchEnd && g.Wins==5 && g.RoundResults.Count==5,"Five round outcomes produce the completed battle screen");Check(g.Eliminations==15 && g.EarnedGold==3125,"Results report actual eliminations and earned gold");
   PlayerPrefs.SetInt("citadel.hero",oldHero);PlayerPrefs.SetInt("citadel.mode",oldMode);PlayerPrefs.Save();
   rows.Add(rows.Any(r=>r.StartsWith("FAIL"))?"FAILED":"ALL CHECKS PASSED");File.WriteAllLines("ArtSource/Characters/Review/CitadelFlowChecks.txt",rows);Destroy(gameObject);
  }
 }
}
#endif
