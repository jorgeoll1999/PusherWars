using UnityEngine;
using UnityEngine.UI;
namespace GuerraDeEmpujes
{
 public sealed partial class MobileFrontEnd
 {
  string onlineInvitation="";
  void OnlineChanged(){if(Page=="Multijugador"&&IsOpen)ShowOnline();}
  public void ShowOnline()
  {
   var network=game.Network;network.Changed-=OnlineChanged;network.Changed+=OnlineChanged;Clear("Multijugador");
   Text("Heading",content,"MULTIJUGADOR ONLINE",40,new Vector2(.29f,.85f),new Vector2(.72f,.97f),true,TextAnchor.MiddleCenter);
   var panel=Panel("Online room",content,new Vector2(.20f,.13f),new Vector2(.97f,.81f));
   Text("Status",panel,network.Status,26,new Vector2(.05f,.78f),new Vector2(.95f,.94f),true,TextAnchor.MiddleCenter);
   if(!network.Connected||network.LocalId<0){
    Text("Guide",panel,"2–8 jugadores por Internet · tienda entre rondas\nPuedes pegar un enlace de servidor antes de CREAR SALA.",24,new Vector2(.06f,.59f),new Vector2(.94f,.77f),false,TextAnchor.MiddleCenter);
    Button("Create online room",panel,"CREAR SALA",new Vector2(.06f,.42f),new Vector2(.46f,.57f),()=>network.Connect(true,onlineInvitation,selectedHero),true);
    var r=Rect("Invitation input",panel,new Vector2(.06f,.23f),new Vector2(.94f,.39f));var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.025f,.025f,.035f,.95f);var input=r.gameObject.AddComponent<InputField>();var text=Text("Input text",r,"",23,new Vector2(.025f,.04f),new Vector2(.975f,.96f));text.supportRichText=false;input.textComponent=text;var placeholder=Text("Placeholder",r,"Código o enlace de invitación",23,new Vector2(.025f,.04f),new Vector2(.975f,.96f));placeholder.color=new Color(.7f,.7f,.7f);input.placeholder=placeholder;input.characterLimit=256;input.lineType=InputField.LineType.SingleLine;input.text=onlineInvitation;input.onValueChanged.AddListener(v=>onlineInvitation=v);
    Button("Join online room",panel,"UNIRME",new Vector2(.54f,.42f),new Vector2(.94f,.57f),()=>network.Connect(false,onlineInvitation,selectedHero),true);
    Text("Server note",panel,"SERVIDOR DE PRUEBA · El equipo anfitrión del servicio debe seguir encendido.",18,new Vector2(.06f,.03f),new Vector2(.94f,.18f),false,TextAnchor.MiddleCenter);
    if(network.Connected)Button("Cancel connection",content,"CANCELAR",new Vector2(.02f,.58f),new Vector2(.18f,.70f),network.Leave,false,20);
   }else{
    Text("Code",panel,"SALA  "+network.Code,35,new Vector2(.06f,.61f),new Vector2(.94f,.79f),true,TextAnchor.MiddleCenter);
    for(int i=0;i<network.Peers.Length;i++){var p=network.Peers[i];float x=(i/4)*.48f;float y=.51f-(i%4)*.09f;Text("Peer "+i,panel,"J"+(p.id+1)+" · "+HeroNames[p.hero]+(p.id==network.LocalId?"  (TÚ)":"")+(!p.connected?" · DESCONECTADO":p.ready?" · LISTO":" · PREPARANDO"),22,new Vector2(.015f+x,y),new Vector2(.50f+x,y+.085f),false,TextAnchor.MiddleCenter);}
    if(network.Playing)Button("Return online battle",panel,"VOLVER A LA PARTIDA",new Vector2(.30f,.035f),new Vector2(.94f,.16f),Close,true);
    else{Button("Ready online",panel,"LISTO / NO LISTO",new Vector2(.06f,.035f),new Vector2(.49f,.16f),network.SetReady,true,22);if(network.IsHost){var b=Button("Start online",panel,"INICIAR PARTIDA",new Vector2(.52f,.035f),new Vector2(.94f,.16f),network.StartMatch,true,22);bool ready=network.Peers.Length>=2;foreach(var p in network.Peers)ready&=p.ready;b.interactable=ready;}}
    Button("Copy invitation",content,"COPIAR\nINVITACIÓN",new Vector2(.02f,.43f),new Vector2(.18f,.60f),()=>{GUIUtility.systemCopyBuffer=network.Invitation;game.Hud.Toast("Invitación copiada");},false,19);
    Button("Leave online room",content,"SALIR DE SALA",new Vector2(.02f,.22f),new Vector2(.18f,.37f),network.Leave,false,20);
   }
  }
 }
}
