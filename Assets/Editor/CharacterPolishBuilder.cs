using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using GuerraDeEmpujes;
public static class CharacterPolishBuilder
{
 [MenuItem("Guerra de empujes/Apply Character Motion and Audio Polish")]
 public static void Build()
 {
  foreach(var name in new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"})
  {
   var path="Assets/Characters/"+name+"/"+name+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationCompression=ModelImporterAnimationCompression.Off;
   var clips=importer.defaultClipAnimations;foreach(var clip in clips){clip.loopTime=clip.name=="Idle" || clip.name=="Run";clip.loopPose=false;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;clip.keepOriginalPositionY=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Characters/"+name+"/"+name+".controller");var motions=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
   foreach(var state in controller.layers[0].stateMachine.states){if(!state.state.name.StartsWith("Cast") && state.state.name!="Death")continue;var motion=motions.FirstOrDefault(c=>c.name==state.state.name);if(!motion)continue;state.state.motion=motion;state.state.iKOnFeet=false;EditorUtility.SetDirty(state.state);}EditorUtility.SetDirty(controller);
   var prefabPath="Assets/Characters/"+name+"/"+name+".prefab";var go=PrefabUtility.LoadPrefabContents(prefabPath);try{go.GetComponent<ArenaCharacterMotor>().moveSpeed=3.2f;PrefabUtility.SaveAsPrefabAsset(go,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(go);}
  }
  string dir="Assets/Characters/Resources/SpellVfx";
  foreach(var name in new[]{"Flame","FireCore","Smoke"}){string path=dir+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find(name=="Flame"?"GuerraDeEmpujes/SpellFlame":name=="Smoke"?"GuerraDeEmpujes/SpellSmoke":"Universal Render Pipeline/Unlit");if(!shader)throw new System.Exception("Missing fire shader");if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;if(name=="FireCore")material.SetColor("_BaseColor",new Color(5,3,.7f));EditorUtility.SetDirty(material);}
  foreach(var file in Directory.GetFiles("Assets/Characters/Resources/SpellAudio","*.wav")){var importer=(AudioImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();}
  AuthoredLocomotionBuilder.Build();AssetDatabase.SaveAssets();Debug.Log("Four characters: authored humanoid locomotion, custom casts/death, user sounds imported as mono PCM, fireball materials ready.");
 }
}
