using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using GuerraDeEmpujes;

public static class UserWalkingBuilder
{
 public const string SourcePath="Assets/Characters/AnimationLibrary/UserWalkingV2.fbx";
 public const string ClipName="UserWalkingV2";
 public const float WalkingSpeed=1.4f;
 [MenuItem("Guerra de empujes/Apply User Walking")]
 public static void Build()
 {
  var importer=(ModelImporter)AssetImporter.GetAtPath(SourcePath);
  if(importer==null)throw new Exception("Missing user walking FBX");
  importer.animationType=ModelImporterAnimationType.Human;
  importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
  var takes=importer.defaultClipAnimations;
  if(takes.Length!=1)throw new Exception("Expected a single walking take");
  takes[0].name=ClipName;takes[0].loopTime=true;takes[0].loopPose=true;
  takes[0].lockRootPositionXZ=true;takes[0].lockRootRotation=true;takes[0].lockRootHeightY=true;
  takes[0].keepOriginalPositionY=true;importer.clipAnimations=takes;importer.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Avatar>().FirstOrDefault();
  if(!avatar || !avatar.isValid || !avatar.isHuman)throw new Exception("Invalid user walking avatar");
  HumanoidLegCalibration.Calibrate(SourcePath);Apply();
 }
 public static void Apply()
 {
  var clip=AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().First(c=>c.name==ClipName);
  foreach(var name in new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"})
  {
   string folder="Assets/Characters/"+name+"/";
   HumanoidLegCalibration.Calibrate(folder+name+".fbx");
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+name+".controller");
   var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();
   var walk=states.First(s=>s.name=="Walk");walk.motion=clip;walk.iKOnFeet=false;
   var locomotion=states.First(s=>s.name=="Locomotion");var tree=(BlendTree)locomotion.motion;
   var idle=states.First(s=>s.name=="Idle").motion;
   // Normal movement uses the supplied walk. Run remains available as its own state.
   tree.children=new[]{new ChildMotion{motion=idle,threshold=0,timeScale=1},new ChildMotion{motion=clip,threshold=WalkingSpeed,timeScale=1}};
   tree.name="Idle and User Walking";tree.useAutomaticThresholds=false;locomotion.iKOnFeet=false;
   EditorUtility.SetDirty(walk);EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);
   var prefab=PrefabUtility.LoadPrefabContents(folder+name+".prefab");
   try{prefab.GetComponent<ArenaCharacterMotor>().moveSpeed=WalkingSpeed;prefab.GetComponent<Animator>().applyRootMotion=false;PrefabUtility.SaveAsPrefabAsset(prefab,folder+name+".prefab");}
   finally{PrefabUtility.UnloadPrefabContents(prefab);}
  }
  AssetDatabase.SaveAssets();Debug.Log("User walking animation applied to all four wizards.");
 }
}
