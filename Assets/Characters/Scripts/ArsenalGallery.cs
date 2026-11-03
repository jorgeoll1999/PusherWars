using UnityEngine;
using UnityEngine.UI;
namespace GuerraDeEmpujes
{
 public sealed class ArsenalGallery:MonoBehaviour
 {
  MobileRoundGame game;Text detail,offer,count;Image preview;int selected=-1;
  readonly Image[] marks=new Image[Arsenal.Powers+Arsenal.Items];
  static RectTransform Box(Transform parent,string name,float l,float b,float r,float t){
   var a=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();a.SetParent(parent,false);a.anchorMin=new Vector2(l,b);a.anchorMax=new Vector2(r,t);a.offsetMin=a.offsetMax=Vector2.zero;return a;}
  static Text Words(Transform parent,string value,float l,float b,float r,float t,int size,bool title=false){
   var x=Box(parent,"Label",l,b,r,t).gameObject.AddComponent<Text>();x.font=title&&CitadelUi.Title?CitadelUi.Title:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
   x.color=title?CitadelUi.Gold:CitadelUi.Ivory;x.fontSize=size;x.alignment=TextAnchor.MiddleCenter;x.resizeTextForBestFit=true;x.resizeTextMinSize=14;x.resizeTextMaxSize=size;x.raycastTarget=false;x.text=value;return x;}
  static Image Art(Transform parent,int id,float l,float b,float r,float t){
   var x=Box(parent,"Icon",l,b,r,t).gameObject.AddComponent<Image>();x.sprite=CitadelUi.Spell(id);x.raycastTarget=false;x.preserveAspect=true;return x;}
  public static ArsenalGallery Build(RectTransform parent,MobileRoundGame game){
   var g=parent.gameObject.AddComponent<ArsenalGallery>();g.game=game;g.Draw();return g;}
  void Draw(){
   Words(transform,"HECHIZOS",0,.91f,.16f,1,22,true);count=Words(transform,"",.44f,.91f,.7f,1,20);
   Words(transform,"OBJETOS",0,.27f,.16f,.35f,22,true);
   for(int i=0;i<marks.Length;i++){
    bool item=i>=Arsenal.Powers;int k=item?i-Arsenal.Powers:i,col=k%6,row=k/6;float l=.006f+col*.117f,b=item?.008f:.633f-row*.285f;
    var tile=Box(transform,Arsenal.Names[i],l,b,l+.111f,b+.265f);
    var bg=tile.gameObject.AddComponent<Image>();bg.color=new Color(.055f,.067f,.09f,.96f);var btn=tile.gameObject.AddComponent<Button>();int id=i;btn.onClick.AddListener(()=>Choose(id));btn.targetGraphic=bg;
    Art(tile,i,.06f,.23f,.94f,.96f);Words(tile,Arsenal.Names[i],.01f,0,.99f,.23f,20);
    marks[i]=Box(tile,"Estado",.08f,0,.92f,.018f).gameObject.AddComponent<Image>();marks[i].raycastTarget=false;
   }
   var panel=Box(transform,"Ficha",.73f,.01f,.995f,.96f);CitadelUi.Frame(panel);
   preview=Art(panel,0,.17f,.55f,.83f,.93f);detail=Words(panel,"",.07f,.24f,.93f,.55f,26);offer=Words(panel,"",.06f,.035f,.94f,.24f,24,true);Refresh();
  }
  void OnDisable(){selected=-1;}
  void OnEnable(){if(detail)Refresh();}
  public void Choose(int id){
   if(selected==id){bool item=id>=Arsenal.Powers;int k=item?id-Arsenal.Powers:id;
    if(game.Phase==ArenaPhase.Shop&&!(game.Online&&game.Network.RoundReady)){
     if(item)game.BuyItem(k);else game.BuySpell(k);}
    selected=-1;
   }else selected=id;
   Refresh();
  }
  public void Refresh(){
   var p=game.progress;count.text=p.Count+" / 5 equipados";
   for(int i=0;i<marks.Length;i++){int n=i<Arsenal.Powers?p.spells[i]:p.items[i-Arsenal.Powers];marks[i].color=i==selected?Color.white:n>0?CitadelUi.Gold:Color.clear;}
   preview.enabled=selected>=0;
   if(selected<0){detail.text="Toca un icono para conocerlo.";offer.text="";return;}
   int id=selected;bool item=id>=Arsenal.Powers;int k=item?id-Arsenal.Powers:id;int level=item?p.items[k]:p.spells[k];int cost=item?p.ItemCost(k):p.SpellCost(k);
   preview.sprite=CitadelUi.Spell(id);detail.text=Arsenal.Help[id];
   string prompt=game.Phase!=ArenaPhase.Shop?"Disponible entre rondas":game.Online&&game.Network.RoundReady?"Ya estás listo":level==5?"Nivel máximo":cost==0?"Cinco poderes equipados":p.gold<cost?"Oro insuficiente":"Toca otra vez · "+(level==0?"Comprar":"Mejorar");
   offer.text=Arsenal.Names[id]+"  ·  "+level+"/5\n"+prompt+(cost>0?"  ·  "+cost+" oro":"");
  }
 }
}