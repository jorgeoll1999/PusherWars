using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Linq;
using System.Collections.Generic;
public static class AuthoredLocomotionBuilder
{
 [MenuItem("Guerra de empujes/Apply Authored Locomotion")]
 public static void Build()
 {
  string path="Assets/Characters/AnimationLibrary/Quaternius_Standard.fbx";var preliminary=(ModelImporter)AssetImporter.GetAtPath(path);preliminary.bakeAxisConversion=true;preliminary.motionNodeName="root";preliminary.SaveAndReimport();var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var ts=model.GetComponentsInChildren<Transform>(true);
  var mapping=new Dictionary<string,string>{{"Hips","DEF-hips"},{"Spine","DEF-spine.001"},{"Chest","DEF-spine.002"},{"UpperChest","DEF-spine.003"},{"Neck","DEF-neck"},{"Head","DEF-head"}};
  foreach(var side in new[]{"Left","Right"}){var suffix=side=="Left"?".L":".R";foreach(var pair in new[]{new[]{"Shoulder","shoulder"},new[]{"UpperArm","upper_arm"},new[]{"LowerArm","forearm"},new[]{"Hand","hand"},new[]{"UpperLeg","thigh"},new[]{"LowerLeg","shin"},new[]{"Foot","foot"},new[]{"Toes","toe"}})mapping[side+pair[0]]="DEF-"+pair[1]+suffix;}
  var human=new List<HumanBone>();for(int i=0;i<(int)HumanBodyBones.LastBone;i++){string bone;if(mapping.TryGetValue(((HumanBodyBones)i).ToString(),out bone))human.Add(new HumanBone{boneName=bone,humanName=HumanTrait.BoneName[i],limit=new HumanLimit{useDefaultValues=true}});}
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
  importer.humanDescription=new HumanDescription{human=human.ToArray(),skeleton=ts.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),armStretch=0,legStretch=0,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f};
  var takes=importer.defaultClipAnimations;foreach(var take in takes){take.name=take.name.Split('|').Last();take.loopTime=take.name.EndsWith("Loop");take.loopPose=false;take.lockRootPositionXZ=true;take.lockRootRotation=true;take.lockRootHeightY=true;take.keepOriginalPositionY=true;}importer.clipAnimations=takes;importer.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();if(!avatar || !avatar.isValid || !avatar.isHuman)throw new Exception("Invalid source animation avatar");
  var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToDictionary(c=>c.name);
  foreach(var name in new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"}){
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Characters/"+name+"/"+name+".controller");var sm=controller.layers[0].stateMachine;
   if(!controller.parameters.Any(p=>p.name=="MoveSpeed"))controller.AddParameter("MoveSpeed",AnimatorControllerParameterType.Float);
   foreach(var entry in new[]{new[]{"Idle","Idle_Loop"},new[]{"Walk","Walk_Loop"},new[]{"Run","Jog_Fwd_Loop"}}){var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==entry[0]);if(!state)state=sm.AddState(entry[0]);state.motion=clips[entry[1]];state.iKOnFeet=false;EditorUtility.SetDirty(state);}
   var locomotion=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Locomotion");if(!locomotion)locomotion=sm.AddState("Locomotion");var tree=locomotion.motion as BlendTree;if(!tree){tree=new BlendTree{name="Authored Idle Walk Jog"};AssetDatabase.AddObjectToAsset(tree,controller);}
   tree.blendType=BlendTreeType.Simple1D;tree.blendParameter="MoveSpeed";tree.useAutomaticThresholds=false;tree.children=new[]{new ChildMotion{motion=clips["Idle_Loop"],threshold=0,timeScale=1},new ChildMotion{motion=clips["Walk_Loop"],threshold=1.4f,timeScale=1},new ChildMotion{motion=clips["Jog_Fwd_Loop"],threshold=3.2f,timeScale=1.15f}};locomotion.motion=tree;locomotion.iKOnFeet=false;sm.defaultState=locomotion;EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);
  }
  var smoke=Resources.Load<Material>("SpellVfx/Smoke");smoke.SetColor("_Color",new Color(.025f,.022f,.02f,.65f));EditorUtility.SetDirty(smoke);
  if(AssetDatabase.LoadAllAssetsAtPath(UserWalkingBuilder.SourcePath).OfType<AnimationClip>().Any(c=>c.name==UserWalkingBuilder.ClipName))UserWalkingBuilder.Apply();
  if(Enumerable.Range(0,4).All(k=>AssetDatabase.LoadAllAssetsAtPath(SpellCastAnimationBuilder.PathFor(k)).OfType<AnimationClip>().Any(c=>c.name==SpellCastAnimationBuilder.Sources[k] && c.events.Any(e=>e.functionName=="OnSpellRelease"))))SpellCastAnimationBuilder.Apply();
  AssetDatabase.SaveAssets();Debug.Log("Authored humanoid locomotion applied; user walking and spell casts preserved when present.");
 }
}
