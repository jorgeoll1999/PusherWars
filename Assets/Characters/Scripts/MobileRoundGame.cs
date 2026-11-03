using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace GuerraDeEmpujes
{
 public enum ArenaPhase{Shop,Countdown,Fighting,Result,MatchEnd}
 public sealed partial class MobileRoundGame:MonoBehaviour
 {
  public GameObject[] heroPrefabs;
  public bool useFrontEnd;public bool MenuOpen;public int SelectedHero;public LocalBattleMode BattleMode;public int Eliminations{get;private set;}public int EarnedGold{get;private set;}int roundStartEliminations;public readonly List<string> RoundResults=new List<string>();
  public MobileFrontEnd FrontEnd{get;private set;}
  public HeroProgression progress=new HeroProgression();public ArenaPhase Phase{get;private set;}
  public int Round{get;private set;}=1;public int Wins{get;private set;}public int Countdown{get;private set;}
  public float TimeLeft{get;private set;}public string Message{get;private set;}="";
  public readonly List<GameObject> Fighters=new List<GameObject>();
  public GameObject Player=>Fighters.Count>LocalActor?Fighters[LocalActor]:null;
  public MobileArenaHud Hud{get;private set;}
  public bool CanControl=>!MenuOpen && Phase==ArenaPhase.Fighting && Player && Player.GetComponent<ArenaHealth>().CurrentHealth>0;
  public BattlefieldWorld World{get;private set;}
  public ShrinkingArena Arena{get;private set;}
  void Start(){Arena=gameObject.AddComponent<ShrinkingArena>();World=gameObject.AddComponent<BattlefieldWorld>();Network=gameObject.AddComponent<OnlineSession>();Network.game=this;if(!GetComponent<MobileLightweight>())gameObject.AddComponent<MobileLightweight>();Screen.sleepTimeout=SleepTimeout.NeverSleep;Hud=gameObject.AddComponent<MobileArenaHud>();Hud.game=this;OpenShop();if(useFrontEnd){FrontEnd=gameObject.AddComponent<MobileFrontEnd>();FrontEnd.game=this;}}
  void ClearFighters(){foreach(var p in Fighters)if(p)Destroy(p);Fighters.Clear();foreach(var shot in FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None))Destroy(shot.gameObject);}
  void Spawn()
  {
   ClearFighters();if(World)World.ResetRound();roundStartEliminations=Eliminations;int count=BattleMode==LocalBattleMode.Duel?2:BattleMode==LocalBattleMode.GrandMelee?8:4;Arena.Configure(count,Round);World.PlaceCovers();
   for(int i=0;i<count;i++)
   {
    Vector3 spawn=Arena.SpawnPoint(i);int actor=i;int hero=(Mathf.Clamp(SelectedHero,0,3)+i)%4;var p=Instantiate(heroPrefabs[hero],spawn,Quaternion.LookRotation(-new Vector3(spawn.x,0,spawn.z)));p.name=(i==0?"Tú · ":"")+MobileFrontEnd.HeroNames[hero];Fighters.Add(p);
    var stats=p.GetComponent<HeroBattleStats>();if(!stats)stats=p.AddComponent<HeroBattleStats>();
    var model=i==0?progress:new HeroProgression();if(i>0){for(int k=0;k<4;k++)model.spells[k]=Mathf.Clamp(1+(Round-1)/2,1,5);model.items[0]=(Round-1)/2;model.items[1]=(Round-1)/3;model.items[2]=(Round-1)/3;}
    stats.Apply(model);p.GetComponent<ArenaHealth>().invulnerable=true;p.GetComponent<ArenaHealth>().onDeath.AddListener(()=>Eliminated(actor));
    MobileLightweight.Dress(p,i==0);p.AddComponent<MobileHealthBar>().localPlayer=i==0;
    if(i>0){var bot=p.AddComponent<MobileArenaBot>();bot.game=this;bot.enabled=BattleMode!=LocalBattleMode.Training;}
   }
  }
  void OpenShop(){Phase=ArenaPhase.Shop;progress.shopOpen=true;Spawn();if(Hud)Hud.RefreshShop();}
  public void ApplyPurchases(){if(Player)Player.GetComponent<HeroBattleStats>().Apply(progress);if(Hud)Hud.RefreshShop();}
  public bool BuySpell(int i){if(Online){if(Network.RoundReady)return false;if(Network.IsHost)return OnlineBuy(LocalActor,false,i);Network.Send(new OnlinePacket{type="buy",index=i});return true;}if(Phase!=ArenaPhase.Shop || !progress.BuySpell(i))return false;ApplyPurchases();return true;}
  public bool BuyItem(int i){if(Online){if(Network.RoundReady)return false;if(Network.IsHost)return OnlineBuy(LocalActor,true,i);Network.Send(new OnlinePacket{type="buy",index=i,item=true});return true;}if(Phase!=ArenaPhase.Shop || !progress.BuyItem(i))return false;ApplyPurchases();return true;}
  public void Ready(){if(Online){Network.ReadyRound();return;}if(Phase==ArenaPhase.Shop)StartCoroutine(BeginRound());}
  IEnumerator BeginRound(){progress.shopOpen=false;Phase=ArenaPhase.Countdown;for(int n=3;n>0;n--){Countdown=n;yield return new WaitForSeconds(1);}Countdown=0;TimeLeft=90;foreach(var p in Fighters)p.GetComponent<ArenaHealth>().invulnerable=BattleMode==LocalBattleMode.Training && p==Player;Phase=ArenaPhase.Fighting;}
  void Update(){if(Online){OnlineUpdate();return;}if(Phase!=ArenaPhase.Fighting || MenuOpen)return;TimeLeft=Mathf.Max(0,TimeLeft-Time.deltaTime);if(TimeLeft<=0){float best=-1;int winner=-1;for(int i=0;i<Fighters.Count;i++){float hp=Fighters[i].GetComponent<ArenaHealth>().CurrentHealth;if(hp>best){best=hp;winner=i;}}EndRound(winner==0,"Tiempo agotado");}}
  void Eliminated(int index)
  {
   if(Phase!=ArenaPhase.Fighting)return;var health=Fighters[index].GetComponent<ArenaHealth>();
   if(index!=0 && health.LastAttacker==Player.GetComponent<ArenaHealth>() && Time.time-health.LastHitTime<8){progress.Earn(75);Eliminations++;EarnedGold+=75;Hud.Toast("+75 oro · eliminación");}
   int alive=0;foreach(var p in Fighters)if(p.GetComponent<ArenaHealth>().CurrentHealth>0)alive++;
   if(index==0)EndRound(false,"Has sido eliminado");else if(alive==1)EndRound(true,"Último mago en pie");
  }
  void EndRound(bool win,string reason)
  {
   if(Phase!=ArenaPhase.Fighting)return;Phase=ArenaPhase.Result;int reward=win?400:250;progress.Earn(reward);EarnedGold+=reward;RoundResults.Add("RONDA "+Round+"   ·   "+(win?"VICTORIA":"DERROTA")+"   ·   +"+(reward+(Eliminations-roundStartEliminations)*75)+" ORO");if(win)Wins++;
   Message=(win?"VICTORIA":"RONDA TERMINADA")+"\n"+reason+"\n+"+reward+" oro";
   foreach(var p in Fighters){p.GetComponent<ArenaHealth>().invulnerable=true;p.GetComponent<ArenaCharacterMotor>().SetMoveDirection(Vector3.zero);p.GetComponent<Rigidbody>().isKinematic=true;}
   StartCoroutine(NextShop());
  }
  IEnumerator NextShop(){yield return new WaitForSeconds(2.5f);if(Round>=5){Phase=ArenaPhase.MatchEnd;Message=(Wins>=3?"VICTORIA":"BATALLA COMPLETADA")+"\n"+Wins+" victorias en 5 rondas";}else{Round++;OpenShop();}}
  public void NewMatch(){if(Online){Hud.Toast("Sigues en la arena. Compra entre rondas y pulsa Listo.");return;}StopAllCoroutines();Round=1;Wins=0;Eliminations=0;EarnedGold=0;RoundResults.Clear();progress=new HeroProgression();OpenShop();}
  public void ConfigureBattle(int hero,LocalBattleMode mode){SelectedHero=Mathf.Clamp(hero,0,3);BattleMode=mode;if(Round!=1||Phase!=ArenaPhase.Shop)NewMatch();else{Spawn();if(Hud)Hud.RefreshShop();}}
  void OnDestroy(){ClearFighters();}
 }
}






