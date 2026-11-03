using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GuerraDeEmpujes;
public static class CharacterGameplayBuilder
{
 [MenuItem("Guerra de empujes/Prepare Animated Characters")]
 public static void Build()
 {
  var report=new List<string>();
  foreach(var n in new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"})
  {
   string dir="Assets/Characters/"+n,path=dir+"/"+n+".fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=true;
   var takes=importer.defaultClipAnimations;
   foreach(var take in takes)
   {
    string[] names={"Idle","Run","Cast01","Cast02","Cast03","Cast04","Death"};
    string match=names.FirstOrDefault(x=>take.name.Contains(x));if(match==null)continue;
    take.name=match;take.loopTime=match=="Idle" || match=="Run";take.loopPose=take.loopTime;
    take.lockRootRotation=true;take.lockRootPositionXZ=true;take.lockRootHeightY=true;take.keepOriginalPositionY=true;
   }
   importer.clipAnimations=takes;importer.SaveAndReimport();
   var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
   foreach(var expected in new[]{"Idle","Run","Cast01","Cast02","Cast03","Cast04","Death"})
    if(!clips.Any(c=>c.name==expected))throw new Exception(n+" missing clip "+expected);
   string cp=dir+"/"+n+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);
   if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(cp);
   var sm=controller.layers[0].stateMachine;
   foreach(var state in sm.states)sm.RemoveState(state.state);
   foreach(var clip in clips){var state=sm.AddState(clip.name);state.motion=clip;state.writeDefaultValues=false;if(clip.name=="Idle")sm.defaultState=state;}
   EditorUtility.SetDirty(controller);
   string prefabPath=dir+"/"+n+".prefab";var go=PrefabUtility.LoadPrefabContents(prefabPath);
   try
   {
    var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
    if(!animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)throw new Exception(n+" invalid avatar");
    var capsule=go.GetComponent<CapsuleCollider>();if(!capsule)capsule=go.AddComponent<CapsuleCollider>();capsule.center=new Vector3(0,.9f,0);capsule.height=1.8f;capsule.radius=.28f;capsule.direction=1;
    var body=go.GetComponent<Rigidbody>();if(!body)body=go.AddComponent<Rigidbody>();body.mass=1;body.constraints=RigidbodyConstraints.FreezeRotation;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
    if(!go.GetComponent<ArenaHealth>())go.AddComponent<ArenaHealth>();
    var lava=go.GetComponent<LavaContactDamage>();if(!lava)lava=go.AddComponent<LavaContactDamage>();lava.bodyCollider=capsule;
    if(!go.GetComponent<CharacterAnimationDriver>())go.AddComponent<CharacterAnimationDriver>();
    if(!go.GetComponent<ArenaCharacterMotor>())go.AddComponent<ArenaCharacterMotor>();
    if(!go.GetComponent<SpellLoadout>())go.AddComponent<SpellLoadout>();
    PrefabUtility.SaveAsPrefabAsset(go,prefabPath);
   }finally{PrefabUtility.UnloadPrefabContents(go);}
   report.Add(n+": 7 clips; valid humanoid; character capsule; health/lava; four empty selectable spell slots.");
  }
  Directory.CreateDirectory("Assets/Characters/Combat");
  var shot=new GameObject("Projectile_Base");
  try {
   var c=shot.AddComponent<CapsuleCollider>();c.direction=2;c.radius=.18f;c.height=.6f;c.isTrigger=true;
   var rb=shot.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
   shot.AddComponent<SpellProjectile>();
   var visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);visual.name="Placeholder_Visual";visual.transform.SetParent(shot.transform,false);visual.transform.localScale=Vector3.one*.3f;UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
   var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.55f,.25f,1);string mp="Assets/Characters/Combat/Projectile_Base.mat";
   var old=AssetDatabase.LoadAssetAtPath<Material>(mp);if(!old)AssetDatabase.CreateAsset(mat,mp);else{UnityEngine.Object.DestroyImmediate(mat);mat=old;}visual.GetComponent<Renderer>().sharedMaterial=mat;
   PrefabUtility.SaveAsPrefabAsset(shot,"Assets/Characters/Combat/Projectile_Base.prefab");
  }finally{UnityEngine.Object.DestroyImmediate(shot);}
  AssetDatabase.SaveAssets();File.WriteAllLines("ArtSource/Characters/GameplayValidation.txt",report);Debug.Log(string.Join("\n",report));
 }
}
