using UnityEngine;
using UnityEngine.UI;
namespace GuerraDeEmpujes
{
 [RequireComponent(typeof(CanvasRenderer))]
 public sealed class CitadelFrame:Image
 {
  public bool crimson;
  protected override void OnPopulateMesh(VertexHelper v)
  {
   if(sprite){base.OnPopulateMesh(v);if(crimson){var item=new UIVertex();for(int i=0;i<v.currentVertCount;i++){v.PopulateUIVertex(ref item,i);if(item.uv0.x>.085f&&item.uv0.x<.915f&&item.uv0.y>.085f&&item.uv0.y<.915f){item.color=(Color)item.color*new Color(1,.25f,.22f,1);v.SetUIVertex(item,i);}}}return;}
   v.Clear();var r=rectTransform.rect;float w=r.width,h=r.height;
   Quad(v,r.xMin,r.yMin,w,h,new Color(.055f,.045f,.035f),new Color(.21f,.17f,.115f));
   Quad(v,r.xMin+3,r.yMin+3,w-6,h-6,new Color(.22f,.18f,.12f),new Color(.53f,.40f,.23f));
   Quad(v,r.xMin+6,r.yMin+6,w-12,h-12,crimson?new Color(.15f,.018f,.022f,.98f):new Color(.035f,.035f,.035f,.98f),crimson?new Color(.39f,.075f,.047f,.98f):new Color(.095f,.087f,.070f,.98f));
   Quad(v,r.xMin+11,r.yMax-13,w-22,1,new Color(.66f,.48f,.25f),new Color(.8f,.62f,.34f));
   Quad(v,r.xMin+11,r.yMin+12,w-22,1,new Color(.34f,.27f,.17f),new Color(.34f,.27f,.17f));
   for(int i=0;i<4;i++){float x=i%2==0?r.xMin+9:r.xMax-9,y=i<2?r.yMin+9:r.yMax-9;Diamond(v,x,y,5,new Color(.13f,.11f,.08f));Diamond(v,x,y,2.5f,new Color(.76f,.56f,.29f));}
  }
  void Quad(VertexHelper v,float x,float y,float w,float h,Color bottom,Color top){if(w<=0||h<=0)return;int n=v.currentVertCount;v.AddVert(new Vector2(x,y),bottom*color,Vector2.zero);v.AddVert(new Vector2(x+w,y),bottom*color,Vector2.zero);v.AddVert(new Vector2(x+w,y+h),top*color,Vector2.zero);v.AddVert(new Vector2(x,y+h),top*color,Vector2.zero);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);}
  void Diamond(VertexHelper v,float x,float y,float s,Color c){int n=v.currentVertCount;v.AddVert(new Vector2(x-s,y),c*color,Vector2.zero);v.AddVert(new Vector2(x,y+s),c*color,Vector2.zero);v.AddVert(new Vector2(x+s,y),c*color,Vector2.zero);v.AddVert(new Vector2(x,y-s),c*color,Vector2.zero);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);}
 }
 public static class CitadelUi
 {
  static Sprite[] spells;
  public static Sprite Spell(int index){if(spells==null){var t=Resources.Load<Texture2D>("Interface/SpellAtlas");spells=new Sprite[4];if(t)for(int i=0;i<4;i++){float w=t.width*.5f,h=t.height*.5f;spells[i]=Sprite.Create(t,new Rect(i%2*w,i<2?h:0,w,h),Vector2.one*.5f,100);}}return spells[Mathf.Clamp(index,0,3)];}
  static Font title;public static Font Title=>title?title:(title=Resources.Load<Font>("Interface/Fonts/Cinzel"));
  public static readonly Color Gold=new Color(.91f,.72f,.40f),Ivory=new Color(.93f,.88f,.77f);
  public static CitadelFrame Frame(RectTransform parent,bool crimson=false){var go=new GameObject("Forged frame",typeof(RectTransform),typeof(CanvasRenderer),typeof(CitadelFrame));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.SetAsFirstSibling();var f=go.GetComponent<CitadelFrame>();f.crimson=crimson;f.sprite=Resources.Load<Sprite>("Interface/ForgedPanel");f.type=Image.Type.Sliced;f.pixelsPerUnitMultiplier=5;f.raycastTarget=false;return f;}
 }
}

