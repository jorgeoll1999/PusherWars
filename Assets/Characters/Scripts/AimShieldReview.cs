#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace GuerraDeEmpujes
{
 [DefaultExecutionOrder(300)]
 public sealed class AimShieldReview:MonoBehaviour
 {
  public bool shieldsOnly;
  readonly List<string> rows=new List<string>();GameObject actor;GameObject floor;
  void Log(string text){rows.Add(text);File.WriteAllLines("ArtSource/Characters/Review/AimShieldChecks.txt",rows);}
  IEnumerator Start()
  {
   Application.runInBackground=true;Time.timeScale=1;
   floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1500,9.5f,0);floor.transform.localScale=new Vector3(40,1,40);
   actor=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/MagaArcana/MagaArcana.prefab"),new Vector3(1500,10,0),Quaternion.identity);actor.GetComponent<LavaContactDamage>().enabled=false;
   yield return null;var driver=actor.GetComponent<CharacterAnimationDriver>();var animator=actor.GetComponent<Animator>();var load=actor.GetComponent<SpellLoadout>();
   for(int kind=1;kind<=(shieldsOnly?0:2);kind++)for(int dir=0;dir<8;dir++)
   {
    actor.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(1500,10,0));float angle=dir*45*Mathf.Deg2Rad;var target=actor.transform.position+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*8;
    driver.SetAim(target);driver.TryCast((SpellKind)kind,()=>load.Release(load.slots[kind].spell,load.slots[kind].spell.levels[0],target));
    yield return new WaitForSeconds(.53f);yield return new WaitForSeconds(.025f);
    float error=0;for(int side=0;side<2;side++){var u=animator.GetBoneTransform(side==0?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var h=animator.GetBoneTransform(side==0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var aim=target-u.position;aim.y=0;var actual=h.position-u.position;actual.y=0;error=Mathf.Max(error,Vector3.Angle(actual,aim));}
    Log((error<2?"PASS ":"FAIL ")+((SpellKind)kind)+" direction "+dir*45+" arm alignment="+error.ToString("F2"));
    while(driver.IsCasting)yield return null;yield return new WaitForSeconds(.2f);
   }
   for(int level=0;level<5;level++)
   {
    var stats=load.slots[3].spell.levels[level];driver.TryCast(SpellKind.Bubble,()=>load.Release(load.slots[3].spell,stats,actor.transform.position),stats.duration);
    yield return null;yield return null;bool immediate=actor.GetComponent<BubbleShield>().Active;
    while(actor.GetComponent<BubbleShield>().Active)yield return null;
    yield return new WaitForSeconds(.025f);float frame=animator.GetCurrentAnimatorStateInfo(0).normalizedTime*98;
    Log((immediate && Mathf.Abs(frame-54)<4?"PASS ":"FAIL ")+"shield level "+(level+1)+" immediate="+immediate+" explosion frame="+frame.ToString("F1")+" expected=54");
    while(driver.IsCasting)yield return null;yield return new WaitForSeconds(.2f);
   }
   File.WriteAllLines("ArtSource/Characters/Review/AimShieldChecks.txt",rows);Destroy(actor);Destroy(floor);Destroy(gameObject);
  }
 }
}
#endif
