#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.IO;
namespace GuerraDeEmpujes
{
 public sealed class MotionReviewCapture:MonoBehaviour
 {
  public void Begin(Animator animator,Camera camera,string state="Run"){StartCoroutine(Capture(animator,camera,state));}
  IEnumerator Capture(Animator animator,Camera camera,string state)
  {
   string folder="Previews/"+state+"ReviewFrames";Directory.CreateDirectory(folder);Time.timeScale=0;var rt=new RenderTexture(560,560,24);var tex=new Texture2D(560,560,TextureFormat.RGB24,false);
   for(int i=0;i<24;i++){
    animator.Play(state,0,i/24f);animator.Update(.00001f);animator.speed=0;
    yield return null;yield return null;
    camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,560,560),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+i.ToString("00")+".png",tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;
   }
   Destroy(rt);Destroy(tex);File.WriteAllText("Previews/"+state+"ReviewDone.txt","24 real-frame skinning samples captured");Time.timeScale=1;Destroy(this);
  }
 }
 public sealed class MotorMotionReview:MonoBehaviour
 {
  public void Begin(GameObject prefab){StartCoroutine(Test(prefab));}
  IEnumerator Test(GameObject prefab)
  {
   var root=new GameObject("Motor review temporary objects");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(1100,9.5f,0);floor.transform.localScale=new Vector3(20,1,20);
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(1100,11,3);wall.transform.localScale=new Vector3(4,3,.5f);
   var go=Instantiate(prefab,new Vector3(1100,10,0),Quaternion.identity);go.transform.SetParent(root.transform);go.GetComponent<LavaContactDamage>().enabled=false;var motor=go.GetComponent<ArenaCharacterMotor>();var animator=go.GetComponent<Animator>();yield return new WaitForSeconds(.1f);motor.SetMoveDirection(Vector3.forward);
   yield return new WaitForSeconds(.3f);bool moving=go.transform.position.z>motor.moveSpeed*.15f&&animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")&&animator.GetFloat("MoveSpeed")>motor.moveSpeed*.6f;
   yield return new WaitForSeconds(3/motor.moveSpeed+.4f);bool blocked=animator.GetFloat("MoveSpeed")<.1f;var before=go.transform.rotation;motor.SetMoveDirection(Vector3.back);yield return new WaitForSeconds(.02f);bool turning=Quaternion.Angle(before,go.transform.rotation)<60;yield return new WaitForSeconds(.4f);bool reversed=Vector3.Dot(go.transform.forward,Vector3.back)>.9f;
   File.WriteAllLines("ArtSource/Characters/Review/MotorMotionChecks.txt",new[]{(moving?"PASS":"FAIL")+" actual travel blends into configured locomotion",(blocked?"PASS":"FAIL")+" wall obstruction blends to idle",(turning&&reversed?"PASS":"FAIL")+" turn is gradual and completes"});Destroy(root);Destroy(gameObject);
  }
 }
 public sealed class FlightReviewCapture:MonoBehaviour
 {
  public void Begin(Camera camera){StartCoroutine(Capture(camera));}
  IEnumerator Capture(Camera camera)
  {
   Time.timeScale=1;var start=new Vector3(1000,2,0);transform.position=start;gameObject.AddComponent<ProjectileVfx>().Setup(SpellKind.Fireball);
   camera.orthographic=true;camera.orthographicSize=2.3f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.24f,.28f);
   float elapsed=0;int frame=0;Directory.CreateDirectory("Previews/FireballFlightFrames");var rt=new RenderTexture(840,560,24);var tex=new Texture2D(840,560,TextureFormat.RGB24,false);
   while(elapsed<.8f){
    elapsed+=Time.deltaTime;transform.position=start+Vector3.forward*(elapsed*12);camera.transform.position=transform.position+new Vector3(4,3,-1);camera.transform.LookAt(transform.position-Vector3.forward*1.4f);
    yield return new WaitForEndOfFrame();
    if(elapsed>.15f){camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,840,560),0,0);tex.Apply();File.WriteAllBytes("Previews/FireballFlightFrames/"+frame.ToString("000")+".png",tex.EncodeToPNG());frame++;RenderTexture.active=old;camera.targetTexture=null;}
   }
   Destroy(rt);Destroy(tex);File.WriteAllText("Previews/FireballFlightDone.txt",frame+" live flight frames captured");Destroy(gameObject);
  }
 }
}
#endif
