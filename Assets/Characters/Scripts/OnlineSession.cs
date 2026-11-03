using System;

using System.Collections;

using System.Collections.Generic;

using System.Collections.Concurrent;

using System.Net.WebSockets;

using System.Text;

using System.Threading;

using System.Threading.Tasks;

using UnityEngine;

namespace GuerraDeEmpujes

{

 [Serializable] public class OnlinePeer {public int id,hero;public bool ready,connected=true;}

 [Serializable] public class OnlineActor {public Vector3 position;public float yaw,hp,speed;public int wins,kills,earned;public bool bubble,teleporting,ramming;public float ward;public int coverKind,coverLife;public Vector3 coverAt;public HeroProgression progress;public float[] cooldown;}

 [Serializable] public class OnlineShot {public int id,kind;public Vector3 position;public Quaternion rotation;}

 [Serializable] public class OnlinePacket

 {

  public string type,code,message,name;public int hero,version=2,id,from,index,kind,round,phase,countdown,wins,earned,kills,generation;public bool ready,item;public Vector3 position,end;public Color color;public float value,time,volume,arenaRadius;public int[] coverHits;public OnlinePeer[] players;public OnlineActor[] actors;public OnlineShot[] shots;public string[] results;

 }

 [Serializable] public class OnlineConfig {public string url;}

 public sealed class OnlineSession:MonoBehaviour

 {

  public static OnlineSession Instance{get;private set;}

  public static bool Replica=>Instance&&Instance.Playing&&!Instance.IsHost;

  public MobileRoundGame game;public bool Playing{get;private set;}public bool Connected=>socket!=null&&socket.State==WebSocketState.Open;public bool IsHost=>LocalId==0;

  public int LocalId{get;private set;}=-1;public string Code{get;private set;}="";public string Status{get;private set;}="Crea una sala o pega una invitación.";

  string endpointPreference;public string Endpoint{get;private set;}="";public OnlinePeer[] Peers=new OnlinePeer[0];public Action Changed;

  public string Invitation=>Endpoint.Replace("wss://","https://").Replace("ws://","http://").TrimEnd('/')+"/r/"+Code;

  ClientWebSocket socket;CancellationTokenSource cancellation;readonly ConcurrentQueue<string> incoming=new ConcurrentQueue<string>();readonly SemaphoreSlim sendLock=new SemaphoreSlim(1,1);

  float nextInput,nextSnapshot,nextPing,lastSnapshot;int epoch,receivedGeneration=-1,sequence;Vector3[] positions;float[] yaws;readonly Dictionary<int,GameObject> replicas=new Dictionary<int,GameObject>();readonly Dictionary<int,GameObject> bubbles=new Dictionary<int,GameObject>();

  readonly Dictionary<int,SpellProjectile> projectiles=new Dictionary<int,SpellProjectile>();public int Generation{get;private set;}

  public bool RoundReady;public readonly bool[] RoundReadies=new bool[ShrinkingArena.MaxPlayers];float[] lastInput=new float[ShrinkingArena.MaxPlayers];

  void Awake(){Instance=this;game=GetComponent<MobileRoundGame>();Application.runInBackground=true;var config=Resources.Load<TextAsset>("OnlineConfig");if(config)Endpoint=JsonUtility.FromJson<OnlineConfig>(config.text).url;endpointPreference="guerra.online.endpoint."+Endpoint;Endpoint=PlayerPrefs.GetString(endpointPreference,Endpoint);}

  public async void Connect(bool create,string invitation,int hero)

  {

   if(socket!=null){Status="Sal de la sala actual primero.";Notify();return;}

   string endpoint=Endpoint,code=invitation.Trim().ToUpperInvariant();

   Uri link;if(Uri.TryCreate(invitation.Trim(),UriKind.Absolute,out link)&&(link.Scheme=="https"||link.Scheme=="wss"||link.IsLoopback)){

    endpoint=(link.Scheme=="http"||link.Scheme=="ws"?"ws://":"wss://")+link.Authority;code=link.AbsolutePath.Trim('/').Replace("r/","").ToUpperInvariant();

   }

   Uri address;if(!Uri.TryCreate(endpoint,UriKind.Absolute,out address)||(address.Scheme!="wss"&&!(address.IsLoopback&&address.Scheme=="ws"))){Status="Servidor no configurado. Pega una invitación completa.";Notify();return;}

   if(!create&&(code.Length!=8||!System.Text.RegularExpressions.Regex.IsMatch(code,"^[A-F0-9]{8}$"))){Status="Pega el código de 8 caracteres o la invitación completa.";Notify();return;}

   Endpoint=endpoint;Status="Conectando…";Notify();int attempt=++epoch;socket=new ClientWebSocket();cancellation=new CancellationTokenSource();var current=socket;var token=cancellation.Token;

   try{using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token)){timeout.CancelAfter(15000);await current.ConnectAsync(new Uri(endpoint.TrimEnd('/')+"/socket"),timeout.Token);}if(attempt!=epoch)return;_ = Receive(current,token,attempt);Send(new OnlinePacket{type=create?"create":"join",code=code,kind=0,index=hero,id=hero,hero=hero,players=null,name="",position=Vector3.zero,version=2,from=hero});

   }catch(Exception){if(attempt==epoch){Disconnect(true);Status="No se pudo conectar. Comprueba Internet y el servidor de pruebas.";Notify();}}

  }

  async Task Receive(ClientWebSocket current,CancellationToken token,int attempt)

  {

   var buffer=new byte[8192];try{while(current.State==WebSocketState.Open&&!token.IsCancellationRequested){using(var stream=new System.IO.MemoryStream()){WebSocketReceiveResult result;do{result=await current.ReceiveAsync(new ArraySegment<byte>(buffer),token).ConfigureAwait(false);if(result.MessageType==WebSocketMessageType.Close)throw new Exception();if(result.MessageType!=WebSocketMessageType.Text||stream.Length+result.Count>65536)throw new Exception();stream.Write(buffer,0,result.Count);}while(!result.EndOfMessage);if(attempt==epoch){if(incoming.Count>120)throw new Exception();incoming.Enqueue(Encoding.UTF8.GetString(stream.ToArray()));}}}}catch{if(attempt==epoch&&!token.IsCancellationRequested)incoming.Enqueue("{\"type\":\"closed\",\"message\":\"Se perdió la conexión. Vuelve a entrar a una sala.\"}");}

  }

  public async void Send(OnlinePacket packet)

  {

   var current=socket;var cancel=cancellation;if(current==null||current.State!=WebSocketState.Open||cancel==null)return;

   string json=JsonUtility.ToJson(packet);

   try{await sendLock.WaitAsync(cancel.Token).ConfigureAwait(false);try{if(current.State==WebSocketState.Open){var bytes=Encoding.UTF8.GetBytes(json);await current.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,cancel.Token).ConfigureAwait(false);}}finally{sendLock.Release();}}catch{if(current==socket&&!cancel.IsCancellationRequested)incoming.Enqueue("{\"type\":\"closed\",\"message\":\"Conexión interrumpida.\"}");}

  }

  void Notify(){if(Changed!=null)Changed();}

  public void SetReady(){bool ready=false;foreach(var p in Peers)if(p.id==LocalId)ready=p.ready;Send(new OnlinePacket{type="ready",ready=!ready});}

  public void StartMatch(){Send(new OnlinePacket{type="start"});}

  public void Leave(){Disconnect(true);Status="Has salido de la sala.";Notify();}

  void Disconnect(bool reset)

  {

   bool wasPlaying=Playing;++epoch;Playing=false;Code="";LocalId=-1;Peers=new OnlinePeer[0];if(cancellation!=null)cancellation.Cancel();if(socket!=null){socket.Abort();socket.Dispose();socket=null;}if(cancellation!=null){cancellation.Dispose();cancellation=null;}while(incoming.TryDequeue(out var unused)){}ClearReplicas();projectiles.Clear();receivedGeneration=-1;RoundReady=false;

   if(wasPlaying&&reset&&game){game.OnlineStop();game.FrontEnd.ShowOnline();}

  }

  void ClearReplicas(){foreach(var x in replicas.Values)if(x)Destroy(x);replicas.Clear();foreach(var x in bubbles.Values)if(x)Destroy(x);bubbles.Clear();}

  void Update()

  {

   string json;int budget=50;while(budget-->0&&incoming.TryDequeue(out json)){try{Handle(JsonUtility.FromJson<OnlinePacket>(json));}catch(Exception e){Debug.LogWarning("Online packet: "+e.GetType().Name);}}

   if(!Connected)return;if(Time.unscaledTime>nextPing){nextPing=Time.unscaledTime+5;Send(new OnlinePacket{type="ping"});}

   if(!Playing)return;

   if(IsHost){for(int i=1;i<game.Fighters.Count;i++)if(Time.unscaledTime-lastInput[i]>.35f)game.Fighters[i].GetComponent<ArenaCharacterMotor>().SetMoveDirection(Vector3.zero);if(Time.unscaledTime>nextSnapshot){nextSnapshot=Time.unscaledTime+.067f;SendSnapshot();}}

   else{if(Time.unscaledTime>nextInput){nextInput=Time.unscaledTime+.05f;Send(new OnlinePacket{type="input",position=game.CanControl?game.Player.GetComponent<ArenaCharacterMotor>().DesiredMove:Vector3.zero});}Interpolate();if(lastSnapshot>0&&Time.unscaledTime-lastSnapshot>8){Status="El anfitrión dejó de responder.";Disconnect(true);Notify();}}

  }

  void Handle(OnlinePacket m)

  {

   if(m==null)return;

   switch(m.type){

    case "welcome":LocalId=m.id;Code=m.code;PlayerPrefs.SetString(endpointPreference,Endpoint);PlayerPrefs.Save();Status="Sala "+Code;Notify();break;

    case "roster":Peers=m.players??new OnlinePeer[0];if(Playing){int count=0;foreach(var p in Peers)if(p.connected)count++;if(count<2){Disconnect(true);Status="La sala terminó: quedan menos de dos jugadores.";}}Notify();break;

    case "error":if(LocalId<0)Disconnect(false);Status=m.message;Notify();break;

    case "closed":Status=m.message;Disconnect(true);Notify();break;

    case "start":Playing=true;Generation=0;RoundReady=false;Array.Clear(RoundReadies,0,RoundReadies.Length);lastSnapshot=Time.unscaledTime;game.OnlineStart(Peers,LocalId);game.FrontEnd.Close();Notify();break;

    case "input":if(IsHost&&Playing&&ValidActor(m.from)&&Finite(m.position)){lastInput[m.from]=Time.unscaledTime;game.Fighters[m.from].GetComponent<ArenaCharacterMotor>().SetMoveDirection(game.Phase==ArenaPhase.Fighting?Vector3.ClampMagnitude(m.position,1):Vector3.zero);}break;

    case "cast":if(IsHost&&Playing&&game.Phase==ArenaPhase.Fighting&&ValidActor(m.from)&&Finite(m.position)&&m.position.sqrMagnitude<100000)game.Fighters[m.from].GetComponent<SpellLoadout>().TryCastAt(m.index,m.position);break;

    case "buy":if(IsHost&&Playing&&ValidActor(m.from))game.OnlineBuy(m.from,m.item,m.index);break;

    case "roundReady":if(IsHost&&Playing&&ValidActor(m.from)&&game.Phase==ArenaPhase.Shop){RoundReadies[m.from]=true;CheckRoundReady();}break;

    case "left":if(IsHost&&Playing&&ValidActor(m.from))game.OnlineDisconnected(m.from);break;

    case "snapshot":if(Playing&&!IsHost)ApplySnapshot(m);break;

    case "event":if(Playing&&!IsHost)Replay(m);break;

   }

  }

  static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);

  bool ValidActor(int i)=>i>=0&&i<game.Fighters.Count;

  public void ReadyRound(){if(RoundReady||game.Phase!=ArenaPhase.Shop)return;RoundReady=true;if(IsHost){RoundReadies[LocalId]=true;CheckRoundReady();}else Send(new OnlinePacket{type="roundReady"});game.Hud.RefreshShop();game.Hud.Toast("Listo · esperando a los demás magos");}

  void CheckRoundReady(){foreach(var p in Peers)if(p.connected&&!RoundReadies[p.id])return;game.OnlineBeginRound();}

  public void NewGeneration(){Generation++;RoundReady=false;Array.Clear(RoundReadies,0,RoundReadies.Length);}

  public bool RequestCast(SpellLoadout load,int index,Vector3 destination){if(!Replica||!game.Player||load.gameObject!=game.Player||!game.CanControl)return false;Send(new OnlinePacket{type="cast",index=index,position=destination});return true;}

  public static void Emit(string name,Vector3 position,Vector3 end=default(Vector3),float value=0,Color color=default(Color),int index=-1,int kind=0,float volume=0){var s=Instance;if(s&&s.Playing&&s.IsHost)s.Send(new OnlinePacket{type="event",name=name,position=position,end=end,value=value,color=color,index=index,kind=kind,volume=volume,generation=s.Generation});}

  public static void CastAccepted(SpellLoadout load,int index,Vector3 target,float duration){var s=Instance;if(s&&s.Playing&&s.IsHost)Emit("cast",target,default(Vector3),duration,default(Color),s.game.Fighters.IndexOf(load.gameObject),index);}

  public static int Register(SpellProjectile p){var s=Instance;if(!s||!s.Playing||!s.IsHost)return 0;int id=++s.sequence;s.projectiles[id]=p;return id;}

  void SendSnapshot()

  {

   var actors=new OnlineActor[game.Fighters.Count];for(int i=0;i<actors.Length;i++){var p=game.Fighters[i];var b=p.GetComponent<BubbleShield>();var load=p.GetComponent<SpellLoadout>();var hero=p.GetComponent<HeroBattleStats>();var cover=hero.summoned?hero.summoned.GetComponent<ArenaCover>():null;var cds=new float[load.slots.Length];for(int k=0;k<cds.Length;k++)cds[k]=Mathf.Max(0,load.slots[k].readyAt-Time.time);actors[i]=new OnlineActor{position=p.transform.position,yaw=p.transform.eulerAngles.y,hp=p.GetComponent<ArenaHealth>().CurrentHealth,speed=p.GetComponent<Rigidbody>().linearVelocity.magnitude,bubble=b&&b.Active,teleporting=load.IsTeleporting,ramming=p.GetComponent<ArenaCharacterMotor>().Ramming,ward=p.GetComponent<ArenaHealth>().WardRemaining,coverKind=hero.summonType,coverLife=cover?cover.hits:0,coverAt=cover?cover.transform.position:Vector3.zero,progress=game.OnlineProgress[i],wins=game.OnlineWins(i),kills=game.OnlineKills(i),earned=game.OnlineEarned(i),cooldown=cds};}

   var shots=new List<OnlineShot>();var dead=new List<int>();foreach(var pair in projectiles){if(!pair.Value){dead.Add(pair.Key);continue;}shots.Add(new OnlineShot{id=pair.Key,kind=(int)pair.Value.Kind,position=pair.Value.transform.position,rotation=pair.Value.transform.rotation});}foreach(int id in dead)projectiles.Remove(id);

   Send(new OnlinePacket{type="snapshot",coverHits=game.World.State(),arenaRadius=game.Arena.Radius,actors=actors,shots=shots.ToArray(),round=game.Round,phase=(int)game.Phase,countdown=game.Countdown,time=game.TimeLeft,generation=Generation,message=game.Message,results=game.RoundResults.ToArray()});

  }

  void ApplySnapshot(OnlinePacket m)

  {

   if(m.actors==null||m.actors.Length!=Peers.Length||m.actors.Length<2||m.actors.Length>ShrinkingArena.MaxPlayers)return;lastSnapshot=Time.unscaledTime;

   foreach(var actor in m.actors)if(actor?.progress?.spells?.Length!=Arsenal.Powers||actor.progress.items?.Length!=Arsenal.Items||actor.cooldown?.Length!=Arsenal.Powers){Disconnect(true);Status="Instalen la misma versión del Arsenal para jugar juntos.";Notify();return;}
   if(receivedGeneration!=m.generation){receivedGeneration=m.generation;RoundReady=false;ClearReplicas();game.OnlineSpawn(Peers,LocalId);positions=new Vector3[m.actors.Length];yaws=new float[m.actors.Length];for(int i=0;i<positions.Length;i++){positions[i]=m.actors[i].position;game.Fighters[i].transform.position=positions[i];}}

   game.OnlineState(m);game.World.Apply(m.coverHits);if(m.arenaRadius>0)game.Arena.SetRadius(m.arenaRadius);for(int i=0;i<m.actors.Length;i++){var a=m.actors[i];var p=game.Fighters[i];positions[i]=a.position;yaws[i]=a.yaw;p.GetComponent<ArenaHealth>().ApplyNetworkHealth(a.hp);p.GetComponent<CharacterAnimationDriver>().SetSpeed(a.speed);var load=p.GetComponent<SpellLoadout>();load.ReplicaTeleporting=a.teleporting;var h=p.GetComponent<HeroBattleStats>();h.ReplicaRamming=a.ramming;p.GetComponent<ArenaHealth>().Protect(a.ward);
    if(a.coverLife>0){if(!h.summoned||h.summonType!=a.coverKind||h.summoned.transform.position!=a.coverAt){if(h.summoned)Destroy(h.summoned);h.summonType=a.coverKind;h.summoned=SummonedCover.Create(game,a.coverAt,a.coverKind);}h.summoned.GetComponent<ArenaCover>().Apply(a.coverLife);}else if(h.summoned){Destroy(h.summoned);h.summoned=null;}
    for(int k=0;k<load.slots.Length;k++){load.slots[k].level=a.progress.spells[k];load.slots[k].readyAt=Time.time+a.cooldown[k];}p.GetComponent<HeroBattleStats>().progression=a.progress;p.GetComponent<ArenaCharacterMotor>().moveSpeed=HeroBattleStats.MoveSpeed(a.progress.items[0]);if(a.bubble&&!bubbles.ContainsKey(i)){var visual=SpellVfx.Sphere("Network protective bubble",p.transform,Vector3.up*.9f,2.1f,SpellVfx.BubbleMaterial);SpellVfx.Ring(visual.transform,SpellVfx.Ice,.5f,.035f,Quaternion.identity);bubbles[i]=visual;}if(!a.bubble&&bubbles.ContainsKey(i)){Destroy(bubbles[i]);bubbles.Remove(i);}}

   var seen=new HashSet<int>();foreach(var shot in m.shots??new OnlineShot[0]){seen.Add(shot.id);GameObject visual;if(!replicas.TryGetValue(shot.id,out visual)){visual=new GameObject("Network projectile");visual.transform.SetPositionAndRotation(shot.position,shot.rotation);visual.AddComponent<ProjectileVfx>().Setup((SpellKind)shot.kind);replicas[shot.id]=visual;}visual.transform.SetPositionAndRotation(shot.position,shot.rotation);}var removed=new List<int>();foreach(var pair in replicas)if(!seen.Contains(pair.Key)){Destroy(pair.Value);removed.Add(pair.Key);}foreach(int id in removed)replicas.Remove(id);

  }

  void Interpolate(){if(positions==null)return;for(int i=0;i<game.Fighters.Count&&i<positions.Length;i++){var p=game.Fighters[i].transform;float t=1-Mathf.Exp(-Time.unscaledDeltaTime*22);p.position=Vector3.Distance(p.position,positions[i])>2?positions[i]:Vector3.Lerp(p.position,positions[i],t);p.rotation=Quaternion.Slerp(p.rotation,Quaternion.Euler(0,yaws[i],0),t);}}

  void Replay(OnlinePacket m)

  {

   if(m.generation!=receivedGeneration)return;

   switch(m.name){case "cast":if(ValidActor(m.index)&&m.kind>=0&&m.kind<Arsenal.Powers){var driver=game.Fighters[m.index].GetComponent<CharacterAnimationDriver>();driver.SetAim(m.position);driver.TryCast(Arsenal.Animation((SpellKind)m.kind),()=>{},m.value);}break;

    case "burst":SpellVfx.Burst(m.position,m.color,m.value);break;case "portal":SpellVfx.Portal(m.position);break;case "lightning":var bolt=new GameObject("Network lightning").AddComponent<VfxLightning>();bolt.Setup(m.position,m.end);break;case "pop":SpellVfx.BubblePop(m.position,m.value);break;default:if(m.name.StartsWith("audio:"))SpellAudio.Play(m.name.Substring(6),m.position,m.volume);break;}

  }

  void OnApplicationFocus(bool focus){if(!focus&&Playing&&game.Player){game.Player.GetComponent<ArenaCharacterMotor>().SetMoveDirection(Vector3.zero);if(!IsHost)Send(new OnlinePacket{type="input",position=Vector3.zero});}}

  void OnApplicationPause(bool paused){if(paused&&Playing)Leave();}

  void OnDestroy(){Disconnect(false);if(Instance==this)Instance=null;}

 }

}

