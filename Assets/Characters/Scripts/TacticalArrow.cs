using UnityEngine;
using UnityEngine.Rendering;
namespace GuerraDeEmpujes
{
 public sealed class TacticalArrow:MonoBehaviour
 {
  void Awake(){Make("Contorno",.05f,.025f,Resources.Load<Material>("Battlefield/ArrowEdge"));Make("Oro",0,.035f,Resources.Load<Material>("Interface/AimArrowYellow"));}
  void Make(string label,float border,float height,Material material){var p=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));p.transform.SetParent(transform,false);p.transform.localPosition=Vector3.up*height;var mesh=new Mesh();float shaft=.13f+border,head=.68f+border,start=.55f-border,neck=4.15f,tip=5.5f+border;mesh.vertices=new[]{new Vector3(-shaft,0,start),new Vector3(shaft,0,start),new Vector3(-shaft,0,neck),new Vector3(shaft,0,neck),new Vector3(-head,0,neck),new Vector3(head,0,neck),new Vector3(0,0,tip)};mesh.triangles=new[]{0,2,1,1,2,3,4,6,5};mesh.RecalculateNormals();mesh.RecalculateBounds();p.GetComponent<MeshFilter>().sharedMesh=mesh;var r=p.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
  public void Draw(Vector3 feet,Vector3 direction){transform.SetPositionAndRotation(feet,Quaternion.LookRotation(direction));gameObject.SetActive(true);}
  void OnDestroy(){foreach(var f in GetComponentsInChildren<MeshFilter>())Destroy(f.sharedMesh);}
 }
}
