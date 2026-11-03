using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GuerraDeEmpujes
{
 public sealed partial class MobileRoundGame
 {
  public OnlineSession Network{get;private set;}
  public bool Online=>Network&&Network.Playing;
  public int LocalActor{get;private set;}
  public readonly List<HeroProgression> OnlineProgress=new List<HeroProgression>();
  public int OnlineWins(int i)=>onlineWins[i];public int OnlineKills(int i)=>onlineKills[i];public int OnlineEarned(int i)=>onlineEarned[i];
  readonly int[] onlineWins=new int[ShrinkingArena.MaxPlayers],onlineKills=new int[ShrinkingArena.MaxPlayers],onlineEarned=new int[ShrinkingArena.MaxPlayers];
  public void OnlineStart(OnlinePeer[] peers,int local)
  {
   StopAllCoroutines();LocalActor=local;Round=1;Wins=Eliminations=EarnedGold=0;RoundResults.Clear();OnlineProgress.Clear();System.Array.Clear(onlineWins,0,onlineWins.Length);System.Array.Clear(onlineKills,0,onlineKills.Length);System.Array.Clear(onlineEarned,0,onlineEarned.Length);
   foreach(var peer in peers)OnlineProgress.Add(new HeroProgression());progress=OnlineProgress[local];SelectedHero=peers[local].hero;BattleMode=LocalBattleMode.FreeForAll;Phase=ArenaPhase.Shop;Message="";OnlineSpawn(peers,local);if(Network.IsHost)Network.NewGeneration();if(Hud)Hud.RefreshShop();
  }
  public void OnlineSpawn(OnlinePeer[] peers,int local)
  {
   ClearFighters();if(Network.IsHost&&World)World.ResetRound();LocalActor=local;Arena.Configure(peers.Length,Round);World.PlaceCovers();
   for(int i=0;i<peers.Length;i++){Vector3 spawn=Arena.SpawnPoint(i);int actor=i;var p=Instantiate(heroPrefabs[Mathf.Clamp(peers[i].hero,0,3)],spawn,Quaternion.LookRotation(-new Vector3(spawn.x,0,spawn.z)));p.name=(i==local?"Tú · ":"J"+(i+1)+" · ")+MobileFrontEnd.HeroNames[peers[i].hero];Fighters.Add(p);var stats=p.GetComponent<HeroBattleStats>();if(!stats)stats=p.AddComponent<HeroBattleStats>();stats.Apply(OnlineProgress[i]);var health=p.GetComponent<ArenaHealth>();health.invulnerable=true;if(Network.IsHost)health.onDeath.AddListener(()=>OnlineEliminated(actor));else{p.GetComponent<Rigidbody>().isKinematic=true;foreach(var c in p.GetComponentsInChildren<Collider>())c.enabled=false;}MobileLightweight.Dress(p,i==local);p.AddComponent<MobileHealthBar>().localPlayer=i==local;if(Network.IsHost&&!peers[i].connected)health.ApplyNetworkHealth(0);}
  }
  public bool OnlineBuy(int actor,bool item,int index)
  {
   if(Phase!=ArenaPhase.Shop||actor<0||actor>=OnlineProgress.Count||Network.RoundReadies[actor])return false;var model=OnlineProgress[actor];if(!(item?model.BuyItem(index):model.BuySpell(index)))return false;Fighters[actor].GetComponent<HeroBattleStats>().Apply(model);if(actor==LocalActor&&Hud)Hud.RefreshShop();return true;
  }
  public void OnlineBeginRound(){if(Phase==ArenaPhase.Shop){foreach(var model in OnlineProgress)model.shopOpen=false;StartCoroutine(BeginRound());}}
  void OnlineUpdate()
  {
   if(!Network.IsHost||Phase!=ArenaPhase.Fighting)return;TimeLeft=Mathf.Max(0,TimeLeft-Time.deltaTime);if(TimeLeft<=0){float best=-1;int winner=-1;for(int i=0;i<Fighters.Count;i++){float hp=Fighters[i].GetComponent<ArenaHealth>().CurrentHealth;if(hp>best){best=hp;winner=i;}}OnlineEndRound(winner,"Tiempo agotado");}
  }
  void OnlineEliminated(int index)
  {
   if(Phase!=ArenaPhase.Fighting)return;var health=Fighters[index].GetComponent<ArenaHealth>();if(health.LastAttacker&&Time.time-health.LastHitTime<8){int killer=Fighters.FindIndex(p=>p.GetComponent<ArenaHealth>()==health.LastAttacker);if(killer>=0&&killer!=index){OnlineProgress[killer].Earn(75);onlineKills[killer]++;onlineEarned[killer]+=75;if(killer==LocalActor)Hud.Toast("+75 oro · eliminación");}}
   int alive=0,winner=-1;for(int i=0;i<Fighters.Count;i++)if(Fighters[i].GetComponent<ArenaHealth>().CurrentHealth>0){alive++;winner=i;}if(alive<=1)OnlineEndRound(winner,"Último mago en pie");
  }
  void OnlineEndRound(int winner,string reason)
  {
   if(Phase!=ArenaPhase.Fighting)return;Phase=ArenaPhase.Result;for(int i=0;i<Fighters.Count;i++){int reward=i==winner?400:250;OnlineProgress[i].Earn(reward);onlineEarned[i]+=reward;if(i==winner)onlineWins[i]++;Fighters[i].GetComponent<ArenaHealth>().invulnerable=true;Fighters[i].GetComponent<ArenaCharacterMotor>().SetMoveDirection(Vector3.zero);Fighters[i].GetComponent<Rigidbody>().isKinematic=true;}
   Wins=onlineWins[LocalActor];Eliminations=onlineKills[LocalActor];EarnedGold=onlineEarned[LocalActor];Message=(winner>=0?"J"+(winner+1)+" GANA LA RONDA":"EMPATE")+"\n"+reason;RoundResults.Add("RONDA "+Round+" · "+(winner>=0?"J"+(winner+1)+" VICTORIA":"EMPATE"));StartCoroutine(OnlineNextRound());
  }
  IEnumerator OnlineNextRound(){yield return new WaitForSeconds(2.5f);Round++;Phase=ArenaPhase.Shop;Message="";foreach(var model in OnlineProgress)model.shopOpen=true;Network.NewGeneration();OnlineSpawn(Network.Peers,LocalActor);Hud.RefreshShop();}
  public void OnlineDisconnected(int actor){if(Phase==ArenaPhase.Fighting){var h=Fighters[actor].GetComponent<ArenaHealth>();h.invulnerable=false;h.Protect(0);h.TakeDamage(10000);}else if(Phase==ArenaPhase.Shop){Network.RoundReadies[actor]=true;Fighters[actor].GetComponent<ArenaHealth>().ApplyNetworkHealth(0);}Fighters[actor].GetComponent<ArenaCharacterMotor>().SetMoveDirection(Vector3.zero);}
  public void OnlineState(OnlinePacket m){bool phaseChanged=Phase!=(ArenaPhase)m.phase;Phase=(ArenaPhase)m.phase;Round=m.round;Countdown=m.countdown;TimeLeft=m.time;Message=m.message;Wins=m.actors[LocalActor].wins;Eliminations=m.actors[LocalActor].kills;EarnedGold=m.actors[LocalActor].earned;bool shopChanged=phaseChanged||progress.gold!=m.actors[LocalActor].progress.gold;progress=m.actors[LocalActor].progress;OnlineProgress.Clear();foreach(var a in m.actors)OnlineProgress.Add(a.progress);RoundResults.Clear();if(m.results!=null)RoundResults.AddRange(m.results);if(Hud&&Phase==ArenaPhase.Shop&&shopChanged){Hud.RefreshShop();if(FrontEnd&&FrontEnd.IsOpen&&FrontEnd.Page=="Arsenal")FrontEnd.ShowArsenal();}}
  public void OnlineStop(){StopAllCoroutines();LocalActor=0;OnlineProgress.Clear();NewMatch();}
 }
}
