using UnityEngine;
namespace GuerraDeEmpujes
{
 public sealed class ArenaCover:MonoBehaviour
 {
  int lastHits=-1;bool lastVisible;Transform healthBar,fill;
  void Start(){healthBar=new GameObject("Vida cobertura").transform;healthBar.SetParent(transform,false);healthBar.localPosition=Vector3.up*2.7f;MakeBar(healthBar,"Fondo",new Vector3(1.2f,.06f,.05f),Resources.Load<Material>("Battlefield/ArrowEdge"));fill=MakeBar(healthBar,"Vida",new Vector3(1.2f,.065f,.06f),Resources.Load<Material>("Interface/AimArrowYellow"));lastHits=-1;Refresh();}
  static Transform MakeBar(Transform parent,string name,Vector3 size,Material mat){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localScale=size;go.GetComponent<MeshFilter>().sharedMesh=Resources.Load<Mesh>("Battlefield/Block");go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go.transform;}
 public int capacity=4,hits=4;public Renderer[] visuals;public Collider solid;public bool Present=>hits>0;
  public void Strike(){if(OnlineSession.Replica||!Present)return;Apply(hits-1);if(hits==0)SpellVfx.Burst(transform.position+Vector3.up*.8f,new Color(.7f,.55f,.35f),.65f);}
  public void Apply(int value){hits=Mathf.Clamp(value,0,capacity);Refresh();}
  public void Refresh(){var game=GetComponentInParent<BattlefieldWorld>().game;bool visible=Present&&game.Arena.Inside(transform.position,.95f);if(lastHits==hits&&lastVisible==visible)return;lastHits=hits;lastVisible=visible;solid.enabled=visible;if(healthBar){healthBar.gameObject.SetActive(visible&&hits<capacity);float ratio=(float)hits/capacity;fill.localScale=new Vector3(1.2f*ratio,.065f,.06f);fill.localPosition=new Vector3(-.6f+.6f*ratio,0,-.01f);}foreach(var r in visuals){r.enabled=visible;}}
 }
 public sealed class BattlefieldWorld:MonoBehaviour
 {
  public MobileRoundGame game;public ArenaCover[] covers;int round=-1;
  void Awake(){game=GetComponent<MobileRoundGame>();covers=new ArenaCover[4];
   foreach(var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
       if(t.name=="Ruins_Painted"||t.name=="Ruins_Emissive"||t.name=="Vegetation_Painted"||t.name=="SpawnPads_Painted"||t.name=="Background_Painted")t.gameObject.SetActive(false);
   for(int i=0;i<covers.Length;i++){
       bool spire=i==0||i==2;
       var root=Instantiate(Resources.Load<GameObject>(spire?"Battlefield/SealObelisk":"Battlefield/WeatheredRock"),transform);
       root.name=(spire?"Obelisco ":"Roca ")+(i+1);
       var cover=root.AddComponent<ArenaCover>();covers[i]=cover;
       cover.capacity=cover.hits=i==0?3:i==2?5:4;
       var collider=root.AddComponent<BoxCollider>();
       collider.center=Vector3.up*(spire?1.15f:.80f);
       collider.size=new Vector3(1.62f,spire?2.3f:1.6f,1.52f);
       cover.solid=collider;cover.visuals=root.GetComponentsInChildren<Renderer>();
   }
  }
  public void PlaceCovers()
  {
      float x=game.Arena.Wide?9.2f:5.9f, z=game.Arena.Wide?4.9f:4.6f;
      // Opposite quadrants mirror each other, leaving both central lanes open.
      for(int i=0;i<covers.Length;i++){
          covers[i].transform.position=new Vector3(i<2?-x:x,0,(i==0||i==3)?-z:z);
          covers[i].transform.rotation=Quaternion.Euler(0,i*90+22.5f,0);
          covers[i].Refresh();
      }
  }
  public void ResetRound(){round=game.Round;foreach(var c in covers)c.Apply(c.capacity);}
  void Update(){if(!OnlineSession.Replica&&round!=game.Round){round=game.Round;foreach(var c in covers)c.Apply(c.capacity);}foreach(var c in covers)c.Refresh();}
  public int[] State(){var values=new int[covers.Length];for(int i=0;i<values.Length;i++)values[i]=covers[i].hits;return values;}
  public void Apply(int[] values){if(values==null||values.Length!=covers.Length)return;for(int i=0;i<values.Length;i++)covers[i].Apply(values[i]);}
 }
}
