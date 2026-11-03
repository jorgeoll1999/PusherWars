using UnityEngine;
using UnityEngine.UI;
namespace GuerraDeEmpujes
{
 public sealed class MobileHealthBar:MonoBehaviour
 {
  public bool localPlayer;ArenaHealth health;Canvas canvas;Image fill;Text label;
  void Start()
  {
   health=GetComponent<ArenaHealth>();var root=new GameObject("Health bar",typeof(RectTransform),typeof(Canvas));root.transform.SetParent(transform,false);root.transform.localPosition=Vector3.up*2.7f;root.transform.localScale=Vector3.one*.006f;canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=30;root.GetComponent<RectTransform>().sizeDelta=new Vector2(210,50);
   var back=new GameObject("Health background",typeof(RectTransform),typeof(Image));back.transform.SetParent(root.transform,false);var br=back.GetComponent<RectTransform>();br.sizeDelta=new Vector2(210,20);back.GetComponent<Image>().color=new Color(.04f,.02f,.025f,.95f);
   var f=new GameObject("Red health",typeof(RectTransform),typeof(Image));f.transform.SetParent(back.transform,false);var rect=f.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,1);rect.offsetMin=new Vector2(3,3);rect.offsetMax=new Vector2(-3,-3);fill=f.GetComponent<Image>();fill.color=new Color(.9f,.09f,.14f);
   var text=new GameObject("Player label",typeof(RectTransform),typeof(Text));text.transform.SetParent(root.transform,false);var tr=text.GetComponent<RectTransform>();tr.sizeDelta=new Vector2(280,26);tr.anchoredPosition=new Vector2(0,26);label=text.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=22;label.alignment=TextAnchor.MiddleCenter;label.color=localPlayer?new Color(1,.85f,.4f):Color.white;label.text=localPlayer?"TÚ":gameObject.name.ToUpperInvariant();text.AddComponent<Shadow>();
  }
  void LateUpdate(){if(!health || !canvas)return;canvas.gameObject.SetActive(health.CurrentHealth>0);if(Camera.main)canvas.transform.rotation=Camera.main.transform.rotation;var rect=fill.rectTransform;rect.anchorMax=new Vector2(Mathf.Clamp01(health.CurrentHealth/health.maxHealth),1);}
 }
}


