#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
namespace GuerraDeEmpujes
{
 public sealed class PalmDirectionReview:MonoBehaviour
 {
  IEnumerator Start()
  {
   Application.runInBackground=true;var names=new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"};var actors=new GameObject[4];var maximum=new float[4];
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1607,9.5f,0);floor.transform.localScale=new Vector3(50,1,50);
   for(int i=0;i<4;i++){actors[i]=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/"+names[i]+"/"+names[i]+".prefab"),new Vector3(1600+i*5,10,0),Quaternion.identity);actors[i].GetComponent<LavaContactDamage>().enabled=false;}
   yield return null;
   for(int kind=1;kind<=2;kind++)for(int direction=0;direction<8;direction++)
   {
    var targets=new Vector3[4];
    for(int i=0;i<4;i++){var p=actors[i];p.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(1600+i*5,10,0));float angle=direction*45*Mathf.Deg2Rad;targets[i]=p.transform.position+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*8;var driver=p.GetComponent<CharacterAnimationDriver>();driver.SetAim(targets[i]);driver.TryCast((SpellKind)kind,null);}
    yield return new WaitForSeconds(.55f);
    for(int i=0;i<4;i++)
    {
     var a=actors[i].GetComponent<Animator>();
     for(int side=0;side<2;side++)
     {
      var hand=a.GetBoneTransform(side==0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
      foreach(var skin in actors[i].GetComponentsInChildren<SkinnedMeshRenderer>())
      {
       int bone=Array.IndexOf(skin.bones,hand);if(bone<0)continue;var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var center=Vector3.zero;int count=0;
       var lower=a.GetBoneTransform(side==0?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);int lowerIndex=Array.IndexOf(skin.bones,lower);
       var forward=mesh.bindposes[bone].inverse.MultiplyPoint3x4(Vector3.zero)-mesh.bindposes[lowerIndex].inverse.MultiplyPoint3x4(Vector3.zero);var reference=mesh.bindposes[bone].MultiplyVector(forward).normalized;
       var points=new List<Vector3>();float minimum=float.PositiveInfinity,maxProjection=float.NegativeInfinity;
       for(int v=0;v<vertices.Length;v++){var w=weights[v];if((w.boneIndex0==bone && w.weight0>.98f)||(w.boneIndex1==bone && w.weight1>.98f)||(w.boneIndex2==bone && w.weight2>.98f)||(w.boneIndex3==bone && w.weight3>.98f)){var point=mesh.bindposes[bone].MultiplyPoint3x4(vertices[v]);points.Add(point);float t=Vector3.Dot(point,reference);minimum=Mathf.Min(minimum,t);maxProjection=Mathf.Max(maxProjection,t);}}
       var basePoint=Vector3.zero;var tip=Vector3.zero;int near=0,far=0;foreach(var point in points){float t=Vector3.Dot(point,reference);if(t<minimum+(maxProjection-minimum)*.15f){basePoint+=point;near++;}if(t>maxProjection-(maxProjection-minimum)*.15f){tip+=point;far++;}}
       if(near>0 && far>0){var actual=hand.TransformDirection(tip/far-basePoint/near);var aim=targets[i]-hand.position;aim.y=0;maximum[i]=Mathf.Max(maximum[i],Vector3.Angle(actual,aim));}

      }
     }
    }
    while(actors[0].GetComponent<CharacterAnimationDriver>().IsCasting)yield return null;yield return new WaitForSeconds(.2f);
   }
   var rows=new List<string>();for(int i=0;i<4;i++){rows.Add((maximum[i]<3?"PASS ":"FAIL ")+names[i]+" actual skinned palm alignment across ray/teleport and eight directions: "+maximum[i].ToString("F2")+" degrees");Destroy(actors[i]);}
   File.WriteAllLines("ArtSource/Characters/Review/PalmDirectionChecks.txt",rows);Destroy(floor);Destroy(gameObject);
  }
 }
}
#endif
