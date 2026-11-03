using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using GuerraDeEmpujes;

public static class SpellCastAnimationBuilder
{
 public static readonly string[] Sources={"castfireball","castRay","casttp","castShield"};
 public static readonly string[] States={"CastFireball","CastLightning","CastTeleport","CastBubble"};
 // Frames selected from the supplied 30 fps gestures: palm thrust, discharge, throw, expansion.
 public static readonly int[] ReleaseFrames={8,15,15,0};
 public static string PathFor(int kind){return "Assets/Characters/AnimationLibrary/SpellCasts/"+Sources[kind]+".fbx";}
 [MenuItem("Guerra de empujes/Apply User Spell Casts")]
 public static void Build()
 {
  for(int kind=0;kind<4;kind++)
  {
   var importer=(ModelImporter)AssetImporter.GetAtPath(PathFor(kind));
   if(!importer)throw new Exception("Missing spell animation: "+Sources[kind]);
   importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
   importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
   var takes=importer.defaultClipAnimations;if(takes.Length!=1)throw new Exception("Expected a single spell take");
   var take=takes[0];take.name=Sources[kind];take.loopTime=take.loopPose=false;
   take.lockRootRotation=take.lockRootPositionXZ=take.lockRootHeightY=true;take.keepOriginalPositionY=true;
   // ModelImporter event times are normalized; imported AnimationClip event times are seconds.
   var events=new List<AnimationEvent>{new AnimationEvent{functionName="OnSpellRelease",time=ReleaseFrames[kind]/(take.lastFrame-take.firstFrame),intParameter=kind}};
   if(kind==3)events.Add(new AnimationEvent{functionName="OnShieldBurst",time=CharacterAnimationDriver.ShieldOpeningTime*30/(take.lastFrame-take.firstFrame)});
   take.events=events.ToArray();
   importer.clipAnimations=takes;importer.SaveAndReimport();HumanoidLegCalibration.Calibrate(PathFor(kind));
  }
  Apply();
 }
 public static void Apply()
 {
  var profiles=Sources.Select((source,kind)=>new SpellCastAnimation{kind=(SpellKind)kind,state=States[kind],clip=AssetDatabase.LoadAllAssetsAtPath(PathFor(kind)).OfType<AnimationClip>().First(c=>c.name==source),releaseTime=ReleaseFrames[kind]/30f}).ToArray();
  foreach(var p in profiles)if(p.clip.events.Count(e=>e.functionName=="OnSpellRelease" && e.intParameter==(int)p.kind && Mathf.Abs(e.time-p.releaseTime)<.002f)!=1 || p.releaseTime>=p.clip.length)throw new Exception("Invalid imported release event: "+p.kind);
  foreach(var name in new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"})
  {
   string folder="Assets/Characters/"+name+"/";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+name+".controller");var sm=controller.layers[0].stateMachine;var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;
   foreach(var profile in profiles){var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==profile.state);if(!state)state=sm.AddState(profile.state);state.motion=profile.clip;state.speed=1;state.iKOnFeet=false;state.writeDefaultValues=true;EditorUtility.SetDirty(state);}
   EditorUtility.SetDirty(controller);var prefab=PrefabUtility.LoadPrefabContents(folder+name+".prefab");
   try{prefab.GetComponent<CharacterAnimationDriver>().spellCasts=profiles;var animator=prefab.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;PrefabUtility.SaveAsPrefabAsset(prefab,folder+name+".prefab");}
   finally{PrefabUtility.UnloadPrefabContents(prefab);}
  }
  AssetDatabase.SaveAssets();Directory.CreateDirectory("ArtSource/Characters/Review");
  File.WriteAllLines("ArtSource/Characters/Review/SpellCastTimings.txt",profiles.Select(p=>p.kind+": "+p.clip.name+"; release="+p.releaseTime.ToString("F3")+"s; duration="+p.clip.length.ToString("F3")+"s; OnSpellRelease event"));
 }
}
