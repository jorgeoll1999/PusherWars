using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace GuerraDeEmpujes
{
 public sealed class MobileArenaHud:MonoBehaviour
 {
  public MobileRoundGame game;
  public bool Editing{get;private set;}HudLayoutEditor layoutEditor;
  void ApplySafeArea(){var a=Screen.safeArea;if(!SafeRoot||Screen.width<=0||Screen.height<=0)return;safe=a;SafeRoot.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height);SafeRoot.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);}
  public void BeginLayoutEdit(){ApplySafeArea();Canvas.ForceUpdateCanvases();if(!layoutEditor){layoutEditor=gameObject.AddComponent<HudLayoutEditor>();layoutEditor.Setup(this);}layoutEditor.Begin();}
  public void SetEditing(bool value){Editing=value;if(joystick)joystick.ResetStick();HideAim();foreach(var s in skills)if(s)s.Cancel();if(value){shop.SetActive(false);result.SetActive(false);if(performance)performance.text=MobileLightweight.ModeLabel+" · EDITANDO HUD";}}
  public int AimingSkill{get;private set;}=-1;
  public RectTransform SafeRoot{get;private set;}public RectTransform CancelZone{get;private set;}
  public float UiScale=>canvas?canvas.scaleFactor:1;
  public Sprite Circle{get;private set;}
  Canvas canvas;Font font;Rect safe;GameObject shop,result;Text shopGold,gold,round,life,message,resultText,toast,performance,heroName;Image lifeFill,heroFace,resultHero;
  public void SetVisible(bool visible){if(canvas)canvas.gameObject.SetActive(visible);if(!visible){if(joystick)joystick.ResetStick();HideAim();}}
  readonly Text[] spellInfo=new Text[4],spellLevel=new Text[4],spellBuy=new Text[4],itemInfo=new Text[3],itemLevel=new Text[3],itemBuy=new Text[3],inventory=new Text[3];
  readonly Button[] spellButtons=new Button[4],itemButtons=new Button[3];readonly MobileSkillButton[] skills=new MobileSkillButton[4];
  TacticalArrow tacticalArrow;MobileJoystick joystick;float toastUntil,nextVisualRefresh;LineRenderer rangeLine,targetLine,targetCircle;GameObject aimingLabel;
  public static readonly string[] SkillNames={"FUEGO","RAYO","TELEPORT","BURBUJA"};
  public static readonly Color[] SkillColors={new Color(1,.38f,.16f),new Color(.3f,.9f,1),new Color(.78f,.5f,1),new Color(.35f,.78f,1)};
  static readonly Color Navy=new Color(.035f,.05f,.09f,.96f),Card=new Color(.075f,.10f,.16f,1),Gold=new Color(1,.76f,.3f);
  void Start(){font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");Build();ApplySafeArea();layoutEditor=gameObject.AddComponent<HudLayoutEditor>();layoutEditor.Setup(this);RefreshShop();}
  RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin=default,Vector2 offsetMax=default)
  {var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;return r;}
  Image Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color)
  {var r=Rect(name,parent,min,max);var image=r.gameObject.AddComponent<Image>();image.color=color;if(color.a>.9f&&!name.Contains("Health")){CitadelUi.Frame(r);image.color=Color.clear;}return image;}
  Text Label(string name,Transform parent,string value,int size,Vector2 min,Vector2 max,TextAnchor align=TextAnchor.MiddleCenter)
  {var r=Rect(name,parent,min,max);var t=r.gameObject.AddComponent<Text>();bool title=name.Contains("title")||name=="Name"||name=="Caption"||name=="Skill name";t.font=title&&CitadelUi.Title?CitadelUi.Title:font;t.text=value;t.fontSize=size;t.color=title?CitadelUi.Gold:CitadelUi.Ivory;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
  Button Action(string name,Transform parent,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction callback)
  {var image=Panel(name,parent,min,max,Gold);var button=image.gameObject.AddComponent<Button>();var frame=image.GetComponentInChildren<CitadelFrame>();if(frame){frame.crimson=true;button.targetGraphic=frame;}else button.targetGraphic=image;var colors=button.colors;colors.disabledColor=new Color(.34f,.34f,.34f,.85f);button.colors=colors;var t=Label("Caption",image.transform,value,21,new Vector2(.035f,.06f),new Vector2(.965f,.94f));t.color=CitadelUi.Gold;button.onClick.AddListener(callback);return button;}
  RectTransform Fixed(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
  {var r=Rect(name,parent,anchor,anchor);r.pivot=anchor;r.sizeDelta=size;r.anchoredPosition=position;return r;}
  Sprite MakeCircle()
  {var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*64);pixels[y*128+x]=new Color(1,1,1,Mathf.Clamp01(63-d));}texture.SetPixels(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,128,128),Vector2.one*.5f,128);}
  public Image Disc(string name,Transform parent,float size,Color color)
  {var r=Fixed(name,parent,Vector2.one*.5f,Vector2.zero,Vector2.one*size);var image=r.gameObject.AddComponent<Image>();image.sprite=Circle;image.color=color;return image;}
  void Build()
  {
   Circle=MakeCircle();var root=new GameObject("Mobile HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
   var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;SafeRoot=Rect("Safe area",root.transform,Vector2.zero,Vector2.one);
   if(!EventSystem.current){var events=new GameObject("Mobile EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
   var touch=Panel("Floating joystick touch zone",SafeRoot,Vector2.zero,new Vector2(.48f,.72f),Color.clear);joystick=touch.gameObject.AddComponent<MobileJoystick>();joystick.hud=this;
   var portrait=Fixed("Life panel",SafeRoot,new Vector2(0,1),new Vector2(26,-20),new Vector2(470,104));portrait.gameObject.AddComponent<Image>().color=Color.clear;CitadelUi.Frame(portrait);
   heroFace=Rect("Mage portrait",portrait,new Vector2(.02f,.08f),new Vector2(.21f,.94f)).gameObject.AddComponent<Image>();heroFace.sprite=Resources.Load<Sprite>("Interface/Face"+game.SelectedHero);heroFace.preserveAspect=true;heroFace.raycastTarget=false;
   heroName=Label("Name",portrait,MobileFrontEnd.HeroNames[game.SelectedHero].ToUpperInvariant(),25,new Vector2(.25f,.5f),new Vector2(.65f,.95f),TextAnchor.MiddleLeft);life=Label("Health value",portrait,"100 / 100",21,new Vector2(.66f,.5f),new Vector2(.95f,.95f));
   var hp=Panel("Health track",portrait,new Vector2(.25f,.18f),new Vector2(.95f,.4f),new Color(.2f,.04f,.07f));lifeFill=Panel("Health red fill",hp.transform,Vector2.zero,Vector2.one,new Color(.9f,.1f,.16f));
   var r=Fixed("Round clock",SafeRoot,new Vector2(.5f,1),new Vector2(0,-20),new Vector2(350,78));r.gameObject.AddComponent<Image>().color=Color.clear;CitadelUi.Frame(r);round=Label("Round",r,"RONDA 1 / 5",24,Vector2.zero,Vector2.one);
   var money=Fixed("Gold",SafeRoot,Vector2.one,new Vector2(-26,-20),new Vector2(250,78));money.gameObject.AddComponent<Image>().color=Color.clear;CitadelUi.Frame(money);gold=Label("Gold value",money,"600 ORO",30,Vector2.zero,Vector2.one);gold.color=Gold;
   if(game.useFrontEnd){var menu=Fixed("Menu",SafeRoot,Vector2.one,new Vector2(-290,-20),new Vector2(155,78));Action("Open menu",menu,"MENÚ",Vector2.zero,Vector2.one,()=>game.FrontEnd.ShowHome());}
   var inv=Fixed("Inventory",SafeRoot,new Vector2(0,1),new Vector2(26,-125),new Vector2(470,52));for(int i=0;i<3;i++){var slot=Rect("Inventory slot "+i,inv,new Vector2(i/3f+.005f,0),new Vector2((i+1)/3f-.005f,1));CitadelUi.Frame(slot);inventory[i]=Label("Item "+i,slot,"",18,new Vector2(.04f,.12f),new Vector2(.96f,.88f));}
   Vector2[] positions={new Vector2(-30,28),new Vector2(-215,35),new Vector2(-35,225),new Vector2(-235,215)};int[] sizes={178,146,146,146};
   for(int i=0;i<4;i++)
   {
    var holder=Fixed("Skill "+SkillNames[i],SafeRoot,new Vector2(1,0),positions[i],Vector2.one*sizes[i]);var edge=Disc("Rim",holder,sizes[i],new Color(.13f,.12f,.11f,1));var metal=Disc("Metal",edge.transform,sizes[i]-4,new Color(.55f,.46f,.30f));metal.raycastTarget=false;var inner=Disc("Skill button",edge.transform,sizes[i]-12,new Color(.025f,.035f,.055f,1));
    skills[i]=inner.gameObject.AddComponent<MobileSkillButton>();skills[i].hud=this;skills[i].index=i;
    var fill=Disc("Cooldown",inner.transform,sizes[i]-10,new Color(0,0,0,.72f));fill.raycastTarget=false;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Radial360;fill.fillAmount=0;skills[i].cooldown=fill;
    var emblem=Rect("Spell emblem",inner.transform,new Vector2(.22f,.37f),new Vector2(.78f,.89f));skills[i].artwork=emblem.gameObject.AddComponent<Image>();skills[i].artwork.sprite=CitadelUi.Spell(i);skills[i].artwork.preserveAspect=true;skills[i].artwork.raycastTarget=false;skills[i].caption=Label("Cooldown number",inner.transform,"",34,new Vector2(.1f,.33f),new Vector2(.9f,.83f));skills[i].caption.color=Color.white;
    Label("Skill name",inner.transform,SkillNames[i],18,new Vector2(0,.15f),new Vector2(1,.34f));skills[i].level=Label("Skill level",inner.transform,"Nv. 1",16,new Vector2(0,.02f),new Vector2(1,.17f));
   }
   CancelZone=Fixed("Cancel cast",SafeRoot,new Vector2(1,0),new Vector2(-135,425),new Vector2(160,68));CancelZone.gameObject.AddComponent<Image>().color=new Color(.6f,.12f,.18f,.9f);Label("Cancel",CancelZone,"CANCELAR",20,Vector2.zero,Vector2.one);CancelZone.GetComponent<Image>().raycastTarget=false;CancelZone.gameObject.SetActive(false);
   aimingLabel=Fixed("Aim hint",SafeRoot,new Vector2(.5f,0),new Vector2(0,28),new Vector2(650,55)).gameObject;Label("Aim help",aimingLabel.transform,"Arrastra para apuntar · suelta para lanzar",23,Vector2.zero,Vector2.one);aimingLabel.SetActive(false);
   message=Label("Center message",SafeRoot,"",66,new Vector2(.25f,.4f),new Vector2(.75f,.66f));message.gameObject.AddComponent<Shadow>();
   toast=Label("Gold toast",SafeRoot,"",26,new Vector2(.3f,.77f),new Vector2(.7f,.84f));toast.color=Gold;
   performance=Label("Performance",SafeRoot,"LIGERO · -- FPS",18,new Vector2(.015f,.015f),new Vector2(.27f,.075f),TextAnchor.MiddleLeft);performance.color=new Color(.68f,.77f,.86f);performance.raycastTarget=MobileLightweight.CanSwitch;var graphicsButton=performance.gameObject.AddComponent<Button>();graphicsButton.targetGraphic=performance;graphicsButton.interactable=MobileLightweight.CanSwitch;graphicsButton.onClick.AddListener(()=>{if(MobileLightweight.Instance)MobileLightweight.Instance.CycleMode();});BuildShop();
   result=Panel("Round result overlay",SafeRoot,Vector2.zero,Vector2.one,new Color(.015f,.025f,.05f,.85f)).gameObject;var mural=Rect("Victory mural",result.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();mural.sprite=Resources.Load<Sprite>("Interface/CitadelBackground");mural.color=new Color(.42f,.42f,.42f,1);mural.raycastTarget=false;var board=Rect("Results board",result.transform,new Vector2(.34f,.27f),new Vector2(.97f,.93f));CitadelUi.Frame(board);resultHero=Rect("Champion",result.transform,new Vector2(.02f,.08f),new Vector2(.33f,.88f)).gameObject.AddComponent<Image>();resultHero.sprite=Resources.Load<Sprite>("Interface/Hero"+game.SelectedHero);resultHero.preserveAspect=true;resultHero.raycastTarget=false;Label("Results title",result.transform,"RESULTADOS DE LA ARENA",37,new Vector2(.38f,.83f),new Vector2(.93f,.91f));resultText=Label("Result",result.transform,"",32,new Vector2(.38f,.30f),new Vector2(.93f,.82f));Action("New match",result.transform,"NUEVA PARTIDA",new Vector2(.37f,.15f),new Vector2(.63f,.25f),()=>game.NewMatch());if(game.useFrontEnd)Action("Return home",result.transform,"VOLVER AL CAMPAMENTO",new Vector2(.67f,.15f),new Vector2(.97f,.25f),()=>game.FrontEnd.ShowHome());result.SetActive(false);performance.transform.SetAsLastSibling();
  }
  Button onlineReady;
  void BuildShop()
  {
   shop=Panel("Between-round shop",SafeRoot,Vector2.zero,Vector2.one,new Color(.01f,.015f,.035f,.88f)).gameObject;
   var panel=Panel("Shop panel",shop.transform,new Vector2(.035f,.055f),new Vector2(.965f,.945f),Navy);
   Label("Shop title",panel.transform,"ARSENAL  /  TIENDA ENTRE RONDAS",32,new Vector2(.03f,.87f),new Vector2(.8f,.98f),TextAnchor.MiddleLeft);
   shopGold=Label("Shop gold",panel.transform,"",30,new Vector2(.76f,.87f),new Vector2(.98f,.98f));shopGold.color=Gold;
   Label("Shop help",panel.transform,"Compra con oro. Las mejoras se conservan mientras sigas en esta partida.",21,new Vector2(.03f,.80f),new Vector2(.97f,.88f),TextAnchor.MiddleLeft);
   for(int i=0;i<4;i++)
   {
    int index=i;float x=.025f+i*.244f;var card=Panel("Upgrade "+SkillNames[i],panel.transform,new Vector2(x,.445f),new Vector2(x+.228f,.795f),Card);
    var art=Rect("Spell illustration",card.transform,new Vector2(.35f,.74f),new Vector2(.65f,.96f)).gameObject.AddComponent<Image>();art.sprite=CitadelUi.Spell(i);art.preserveAspect=true;art.raycastTarget=false;var title=Label("Name",card.transform,SkillNames[i],25,new Vector2(.05f,.61f),new Vector2(.95f,.77f));title.color=SkillColors[i];
    spellLevel[i]=Label("Level",card.transform,"",18,new Vector2(.05f,.52f),new Vector2(.95f,.63f));spellInfo[i]=Label("Statistics",card.transform,"",19,new Vector2(.06f,.23f),new Vector2(.94f,.53f));
    spellButtons[i]=Action("Buy",card.transform,"",new Vector2(.07f,.035f),new Vector2(.93f,.22f),()=>game.BuySpell(index));spellBuy[i]=spellButtons[i].GetComponentInChildren<Text>();
   }
   string[] names={"BOTAS DEL VIAJERO","CAPUCHA FIRME","ORBE DE EXPANSIÓN"};
   for(int i=0;i<3;i++)
   {
    int index=i;float x=.025f+i*.326f;var card=Panel("Item "+i,panel.transform,new Vector2(x,.155f),new Vector2(x+.31f,.42f),Card);
    Label("Name",card.transform,names[i],23,new Vector2(.04f,.74f),new Vector2(.96f,.98f));itemLevel[i]=Label("Level",card.transform,"",17,new Vector2(.04f,.6f),new Vector2(.96f,.76f));itemInfo[i]=Label("Description",card.transform,"",19,new Vector2(.05f,.29f),new Vector2(.95f,.6f));
    itemButtons[i]=Action("Buy",card.transform,"",new Vector2(.08f,.045f),new Vector2(.92f,.25f),()=>game.BuyItem(index));itemBuy[i]=itemButtons[i].GetComponentInChildren<Text>();
   }
   Label("Shop controls",panel.transform,"IZQUIERDA: joystick flotante\nDERECHA: arrastra un poder para apuntar. Burbuja: tocar.",20,new Vector2(.035f,.015f),new Vector2(.65f,.13f),TextAnchor.MiddleLeft);
   onlineReady=Action("Ready",panel.transform,"ENTRAR A LA RONDA",new Vector2(.69f,.025f),new Vector2(.97f,.125f),()=>game.Ready());
  }
  public void RefreshShop()
  {
   if(!shop || !game.Player)return;var model=game.progress;var load=game.Player.GetComponent<SpellLoadout>();
   for(int i=0;i<4;i++)
   {
    var slot=load.slots[i];slot.spell.TryGetLevel(model.spells[i],out var now);slot.spell.TryGetLevel(Mathf.Min(5,model.spells[i]+1),out var next);spellLevel[i].text="NIVEL "+model.spells[i]+" / 5";
    string Change(float a,float b){return a.ToString("0.#")+(model.spells[i]<5?" → "+b.ToString("0.#"):"");}
    spellInfo[i].text=i==2?"Alcance "+Change(now.range,next.range)+" m\nRecarga "+Change(now.cooldown,next.cooldown)+" s\nVelocidad "+Change(now.speed,next.speed):i==3?"Empuje "+Change(now.knockback,next.knockback)+"\nRadio "+Change(now.radius,next.radius)+" m\nDuración "+Change(now.duration,next.duration)+" s\nRecarga "+Change(now.cooldown,next.cooldown)+" s":"Daño "+Change(now.damage,next.damage)+"\nEmpuje "+Change(now.knockback,next.knockback)+"\nAlcance "+Change(now.range,next.range)+" m\nRecarga "+Change(now.cooldown,next.cooldown)+" s";
    int cost=model.SpellCost(i);spellBuy[i].text=cost>0?"MEJORAR  ·  "+cost+" ORO":"NIVEL MÁXIMO";spellButtons[i].interactable=cost>0 && model.gold>=cost && !(game.Online&&game.Network.RoundReady);
   }
   for(int i=0;i<3;i++){int n=model.items[i];itemLevel[i].text=(n==0?"SIN COMPRAR":"EN INVENTARIO · NIVEL "+n)+" / 5";itemInfo[i].text=i==0?"Velocidad +"+(n*12)+"% → +"+(Mathf.Min(5,n+1)*12)+"%":i==1?"Resiste "+(n*8)+"% → "+(Mathf.Min(5,n+1)*8)+"% de empuje":"Zona de impacto +"+(n*3.5f).ToString("0.#")+"% → +"+(Mathf.Min(5,n+1)*3.5f).ToString("0.#")+"%";int cost=model.ItemCost(i);itemBuy[i].text=cost>0?"COMPRAR / MEJORAR  ·  "+cost+" ORO":"NIVEL MÁXIMO";itemButtons[i].interactable=cost>0 && model.gold>=cost && !(game.Online&&game.Network.RoundReady);}
  }
  void Update()
  {
   if(!canvas || !canvas.gameObject.activeSelf || !game.Player)return;if(safe!=Screen.safeArea)ApplySafeArea();if(Editing){game.Player.GetComponent<ArenaCharacterMotor>().SetMoveDirection(Vector3.zero);return;}
   if(onlineReady){bool waiting=game.Online&&game.Network.RoundReady;onlineReady.interactable=!waiting;onlineReady.GetComponentInChildren<Text>().text=waiting?"ESPERANDO MAGOS…":"ENTRAR A LA RONDA";}
   if(safe!=Screen.safeArea){safe=Screen.safeArea;SafeRoot.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);SafeRoot.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);}
   shop.SetActive(game.Phase==ArenaPhase.Shop);result.SetActive(!game.Online&&(game.Phase==ArenaPhase.Result || game.Phase==ArenaPhase.MatchEnd));resultText.text=game.Phase==ArenaPhase.MatchEnd?game.Message+"\n\nELIMINACIONES  "+game.Eliminations+"     ORO GANADO  "+game.EarnedGold+"\n\n"+string.Join("\n",game.RoundResults):game.Message;result.transform.Find("New match").gameObject.SetActive(game.Phase==ArenaPhase.MatchEnd);
   if(Time.unscaledTime>=nextVisualRefresh){nextVisualRefresh=Time.unscaledTime+.1f;performance.text=MobileLightweight.ModeLabel+" · "+Mathf.RoundToInt(MobileLightweight.Fps)+" FPS"+(MobileLightweight.CanSwitch?"  ›":"");heroName.text=MobileFrontEnd.HeroNames[game.SelectedHero].ToUpperInvariant();heroFace.sprite=Resources.Load<Sprite>("Interface/Face"+game.SelectedHero);if(resultHero)resultHero.sprite=Resources.Load<Sprite>("Interface/Hero"+game.SelectedHero);gold.text=game.progress.gold+" ORO";shopGold.text=gold.text;round.text="RONDA "+game.Round+(game.Online?"   ·   ":" / 5   ·   ")+(game.Phase==ArenaPhase.Shop?"TIENDA":Mathf.CeilToInt(game.TimeLeft)+" s");var health=game.Player.GetComponent<ArenaHealth>();life.text=Mathf.CeilToInt(health.CurrentHealth)+" / "+health.maxHealth;lifeFill.rectTransform.anchorMax=new Vector2(health.CurrentHealth/health.maxHealth,1);
   message.text=game.Phase==ArenaPhase.Countdown?game.Countdown.ToString():game.Online&&game.Phase==ArenaPhase.Result?game.Message:"";toast.gameObject.SetActive(Time.unscaledTime<toastUntil);
   for(int i=0;i<3;i++)inventory[i].text=new[]{"BOTAS","CAPUCHA","ORBE"}[i]+"  "+game.progress.items[i]+"/5";
   foreach(var s in skills)s.Refresh();}
   if(game.CanControl)game.Player.GetComponent<ArenaCharacterMotor>().SetMoveDirection(ScreenDirection(joystick.Axis));else{joystick.ResetStick();game.Player.GetComponent<ArenaCharacterMotor>().SetMoveDirection(Vector3.zero);foreach(var s in skills)s.Cancel();}
  }
  public Vector3 ScreenDirection(Vector2 value){var c=Camera.main;var right=c?c.transform.right:Vector3.right;var up=c?c.transform.up:Vector3.forward;right.y=up.y=0;return right.normalized*value.x+up.normalized*value.y;}
  public Vector3 AimPoint(int index,Vector2 drag)
  {
   var p=game.Player;var load=p.GetComponent<SpellLoadout>();load.GetStats(index,out var stats);
   if(drag.magnitude<10*UiScale){GameObject best=null;float distance=float.PositiveInfinity;foreach(var other in game.Fighters){if(other==p||other.GetComponent<ArenaHealth>().CurrentHealth<=0)continue;float d=(other.transform.position-p.transform.position).sqrMagnitude;if(d<distance){distance=d;best=other;}}var direction=best?best.transform.position-p.transform.position:p.transform.forward;direction.y=0;if(direction.sqrMagnitude<.001f)direction=p.transform.forward;return p.transform.position+(index==2?Vector3.ClampMagnitude(direction,stats.range):direction.normalized*stats.range);}
   float length=index==2?Mathf.Lerp(.75f,stats.range,Mathf.Clamp01(drag.magnitude/(125*UiScale))):stats.range;
   return p.transform.position+ScreenDirection(drag.normalized).normalized*length;
  }
  public void AimPreview(int index,Vector2 drag)
  {
   if(!game.Player||Editing)return;AimingSkill=index;if(!rangeLine){rangeLine=SpellVfx.Line("Mobile aiming range",transform,new Color(.3f,1,1,.55f),.045f,true);targetLine=SpellVfx.Line("Mobile aim",transform,Color.white,.07f);targetCircle=SpellVfx.Line("Selected destination",transform,Color.white,.06f,true);}
   bool teleport=index==2;targetLine.sharedMaterial=teleport?SpellVfx.Glow:Resources.Load<Material>("Interface/AimArrowYellow");targetLine.startColor=targetLine.endColor=teleport?Color.white:new Color(1,1,0,1);targetLine.startWidth=targetLine.endWidth=teleport?.07f:.30f;targetLine.numCornerVertices=4;rangeLine.gameObject.SetActive(teleport);targetLine.gameObject.SetActive(teleport);if(tacticalArrow)tacticalArrow.gameObject.SetActive(!teleport);targetCircle.gameObject.SetActive(teleport);CancelZone.gameObject.SetActive(true);aimingLabel.SetActive(true);game.Player.GetComponent<SpellLoadout>().GetStats(index,out var stats);var center=game.Player.transform.position+Vector3.up*.10f;var target=AimPoint(index,drag);target.y=center.y;
   if(teleport){rangeLine.positionCount=64;for(int i=0;i<64;i++){float a=i*Mathf.PI/32;rangeLine.SetPosition(i,center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*stats.range);}targetLine.positionCount=2;targetLine.SetPosition(0,center);targetLine.SetPosition(1,target);targetCircle.positionCount=32;for(int i=0;i<32;i++){float a=i*Mathf.PI/16;targetCircle.SetPosition(i,target+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.55f);}}
   else{var dir=target-center;dir.y=0;if(!tacticalArrow){var root=new GameObject("Tactical aim arrow");root.transform.SetParent(transform,false);tacticalArrow=root.AddComponent<TacticalArrow>();}tacticalArrow.Draw(center,dir.normalized);}
  }
  public void HideAim(){if(tacticalArrow)tacticalArrow.gameObject.SetActive(false);AimingSkill=-1;if(targetCircle)targetCircle.gameObject.SetActive(false);if(rangeLine)rangeLine.gameObject.SetActive(false);if(targetLine)targetLine.gameObject.SetActive(false);if(CancelZone)CancelZone.gameObject.SetActive(false);if(aimingLabel)aimingLabel.SetActive(false);}
  public void Toast(string value){if(!toast)return;toast.text=value;toastUntil=Time.unscaledTime+2;}
  void OnApplicationPause(bool pause){if(pause && joystick){joystick.ResetStick();foreach(var s in skills)if(s)s.Cancel();}}
 }
 public sealed class MobileJoystick:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
 {
  public MobileArenaHud hud;public Vector2 Axis{get;private set;}int pointer=int.MinValue;Vector2 origin;Image pad,knob;
  public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue || !hud.game.CanControl)return;pointer=e.pointerId;origin=e.position;if(!pad){pad=hud.Disc("Floating joystick",hud.SafeRoot,200,new Color(.025f,.035f,.055f,.70f));knob=hud.Disc("Joystick thumb",pad.transform,88,new Color(.55f,.48f,.34f,.95f));var ring=hud.Disc("Joystick bezel",pad.transform,192,new Color(.7f,.58f,.34f,.65f));ring.raycastTarget=false;var core=hud.Disc("Joystick recess",ring.transform,185,new Color(.025f,.035f,.055f,.9f));core.raycastTarget=false;knob.transform.SetAsLastSibling();var thumb=hud.Disc("Thumb inset",knob.transform,74,new Color(.10f,.13f,.16f,1));thumb.raycastTarget=false;pad.raycastTarget=knob.raycastTarget=false;}RectTransformUtility.ScreenPointToLocalPointInRectangle(hud.SafeRoot,e.position,null,out var local);pad.rectTransform.anchoredPosition=local;pad.gameObject.SetActive(true);OnDrag(e);}
  public void OnDrag(PointerEventData e){if(e.pointerId!=pointer)return;var delta=Vector2.ClampMagnitude((e.position-origin)/hud.UiScale,90);Axis=delta/90;if(knob)knob.rectTransform.anchoredPosition=delta;}
  public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)ResetStick();}
  public void ResetStick(){pointer=int.MinValue;Axis=Vector2.zero;if(pad)pad.gameObject.SetActive(false);}
 }
 public sealed class MobileSkillButton:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
 {
  public MobileArenaHud hud;public int index;public Image cooldown,artwork;public MobileSpellIcon icon;public Text caption,level;int pointer=int.MinValue;Vector2 origin,drag;
  void Update(){if(pointer!=int.MinValue && hud.game.CanControl)hud.AimPreview(index,drag);}
  public void Refresh(){if(!hud.game.Player)return;var load=hud.game.Player.GetComponent<SpellLoadout>();var slot=load.slots[index];load.GetStats(index,out var stats);float left=Mathf.Max(0,slot.readyAt-Time.time);cooldown.fillAmount=left/Mathf.Max(.01f,stats.cooldown);caption.text=left>0?left.ToString("0.0"):"";if(artwork)artwork.canvasRenderer.SetAlpha(left>0?.18f:hud.game.CanControl?1:.45f);if(icon)icon.canvasRenderer.SetAlpha(left>0?.18f:hud.game.CanControl?1:.45f);level.text="Nv. "+slot.level+" / 5";}
  public void OnPointerDown(PointerEventData e){if(hud.Editing || pointer!=int.MinValue || hud.AimingSkill>=0 || !hud.game.CanControl)return;var load=hud.game.Player.GetComponent<SpellLoadout>();if(Time.time<load.slots[index].readyAt || load.GetComponent<CharacterAnimationDriver>().IsCasting)return;if(index==3){load.TryCastAt(3,load.transform.position);return;}pointer=e.pointerId;origin=e.position;drag=Vector2.zero;hud.AimPreview(index,drag);}
  public void OnDrag(PointerEventData e){if(e.pointerId!=pointer)return;drag=e.position-origin;hud.AimPreview(index,drag);}
  public void OnPointerUp(PointerEventData e){if(e.pointerId!=pointer)return;bool cancel=RectTransformUtility.RectangleContainsScreenPoint(hud.CancelZone,e.position,null);if(!cancel && hud.game.CanControl)hud.game.Player.GetComponent<SpellLoadout>().TryCastAt(index,hud.AimPoint(index,drag));Cancel();}
  public void Cancel(){if(pointer!=int.MinValue){pointer=int.MinValue;hud.HideAim();}}
 }
}







