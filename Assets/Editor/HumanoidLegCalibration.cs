using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Calibrate the avatar reference pose without changing mesh bind poses or bone lengths.
public static class HumanoidLegCalibration
{
 public static bool Calibrate(string path)
 {
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);
  var description=importer.humanDescription;
  if(description.human.Length==0 || description.skeleton.Length==0)throw new Exception("Missing humanoid reference pose: "+path);
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  var instance=UnityEngine.Object.Instantiate(model);instance.name=model.name;
  SkeletonBone[] skeleton;
  try
  {
   var transforms=instance.GetComponentsInChildren<Transform>(true);
   foreach(var bone in description.skeleton)
   {
    var t=transforms.FirstOrDefault(x=>x.name==bone.name);
    if(t){t.localPosition=bone.position;t.localRotation=bone.rotation;t.localScale=bone.scale;}
   }
   var human=description.human.ToDictionary(b=>b.humanName,b=>transforms.First(t=>t.name==b.boneName));
   foreach(var side in new[]{"Left","Right"})
   {
    var thigh=human[side+"UpperLeg"];var shin=human[side+"LowerLeg"];
    var foot=human[side+"Foot"];var toes=human[side+"Toes"];
    thigh.rotation=Quaternion.FromToRotation(shin.position-thigh.position,Vector3.down)*thigh.rotation;
    shin.rotation=Quaternion.FromToRotation(foot.position-shin.position,Vector3.down)*shin.rotation;
    // Ankle-to-toe direction includes the anatomical drop from ankle to forefoot.
    foot.rotation=Quaternion.FromToRotation(toes.position-foot.position,new Vector3(0,-.5f,.8660254f))*foot.rotation;
   }
   skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
  }
  finally{UnityEngine.Object.DestroyImmediate(instance);}
  bool changed=description.legStretch!=0 || skeleton.Any(b=>{
   var original=description.skeleton.FirstOrDefault(s=>s.name==b.name);
   return original.name==null || Quaternion.Angle(original.rotation,b.rotation)>.01f;
  });
  if(!changed)return false;
  string backup="ArtSource/Characters/BeforeLegCalibration/"+Path.GetFileName(path)+".meta";
  Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path+".meta",backup);
  description.skeleton=skeleton;description.legStretch=0;
  importer.humanDescription=description;importer.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
  if(!avatar || !avatar.isValid || !avatar.isHuman)throw new Exception("Invalid calibrated avatar: "+path);
  return true;
 }
}
