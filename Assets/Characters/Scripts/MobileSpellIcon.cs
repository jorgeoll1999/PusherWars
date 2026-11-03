using UnityEngine;
using UnityEngine.UI;
namespace GuerraDeEmpujes
{
 // Small vector emblems: crisp at tablet resolution, no texture downloads.
 [RequireComponent(typeof(CanvasRenderer))]
 public sealed class MobileSpellIcon:MaskableGraphic
 {
  public int kind;
  protected override void OnPopulateMesh(VertexHelper vh)
  {
   vh.Clear();float s=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
   if(kind==0){Poly(vh,s,new Color(1,.27f,.055f),new Vector2(-.55f,-.12f),new Vector2(-.42f,.30f),new Vector2(-.15f,.12f),new Vector2(.10f,.93f),new Vector2(.40f,.42f),new Vector2(.57f,-.10f),new Vector2(.36f,-.62f),new Vector2(0,-.80f),new Vector2(-.40f,-.62f));Poly(vh,s,new Color(1,.87f,.35f),new Vector2(-.25f,-.23f),new Vector2(.06f,.37f),new Vector2(.29f,-.27f),new Vector2(.15f,-.62f),new Vector2(-.13f,-.64f));}
   if(kind==1){Poly(vh,s,new Color(.28f,.66f,1),new Vector2(-.15f,.92f),new Vector2(.61f,.92f),new Vector2(.12f,.20f),new Vector2(.53f,.20f),new Vector2(-.46f,-.92f),new Vector2(-.16f,-.13f),new Vector2(-.55f,-.13f));Poly(vh,s,new Color(.8f,.98f,1),new Vector2(.03f,.77f),new Vector2(.34f,.77f),new Vector2(-.01f,.04f),new Vector2(.23f,.04f),new Vector2(-.28f,-.53f),new Vector2(-.03f,-.02f),new Vector2(-.30f,-.02f));}
   if(kind==2){Ring(vh,s,new Color(.69f,.38f,1),.49f,.85f,.12f);Ring(vh,s,new Color(.89f,.78f,1),.27f,.57f,.07f);Poly(vh,s,Color.white,new Vector2(-.77f,.04f),new Vector2(.30f,.04f),new Vector2(.30f,.27f),new Vector2(.79f,-.13f),new Vector2(.30f,-.52f),new Vector2(.30f,-.29f),new Vector2(-.77f,-.29f));}
   if(kind==3){Ring(vh,s,new Color(.29f,.8f,1),.58f,.65f,.11f);Poly(vh,s,new Color(.78f,.96f,1),new Vector2(-.39f,.31f),new Vector2(-.34f,.47f),new Vector2(-.12f,.57f),new Vector2(.03f,.55f),new Vector2(-.13f,.40f));for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var side=new Vector2(-d.y,d.x);Poly(vh,s,new Color(.55f,.91f,1),d*.73f-side*.12f,d*.98f,d*.73f+side*.12f);}}
  }
  static void Poly(VertexHelper vh,float s,Color c,params Vector2[] p){int first=vh.currentVertCount;foreach(var v in p)vh.AddVert(v*s,c,Vector2.zero);for(int i=1;i<p.Length-1;i++)vh.AddTriangle(first,first+i,first+i+1);}
  static void Ring(VertexHelper vh,float s,Color c,float x,float y,float width){for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Poly(vh,s,c,new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y),new Vector2(Mathf.Cos(b)*x,Mathf.Sin(b)*y),new Vector2(Mathf.Cos(b)*(x-width),Mathf.Sin(b)*(y-width)),new Vector2(Mathf.Cos(a)*(x-width),Mathf.Sin(a)*(y-width)));}}
 }
}

