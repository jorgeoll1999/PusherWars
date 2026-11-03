using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using GuerraDeEmpujes;
public static class CitadelInterfaceBuilder
{
 public const string Scene="Assets/Characters/Scenes/Citadel_Playtest.unity";
 public static void Prepare()
 {
  if(EditorSceneManager.GetActiveScene().path!=Scene){EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene,true);EditorSceneManager.OpenScene(Scene);}
  var manager=UnityEngine.Object.FindFirstObjectByType<MobileRoundGame>();manager.useFrontEnd=true;EditorUtility.SetDirty(manager);
  foreach(string p in new[]{"CitadelBackground.png","ForgedPanel.png","ArenaPreview.png","GrandCourtPreview.png","Hero0.png","Hero1.png","Hero2.png","Hero3.png","Face0.png","Face1.png","Face2.png","Face3.png"}){
   string path="Assets/Characters/Resources/Interface/"+p;if(!File.Exists(path))continue;var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.maxTextureSize=p=="CitadelBackground.png"?2048:1024;i.textureCompression=TextureImporterCompression.Compressed;if(p=="ForgedPanel.png")i.spriteBorder=new Vector4(128,128,128,128);i.SaveAndReimport();
  }
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};PlayerSettings.bundleVersion="0.7.2";PlayerSettings.Android.bundleVersionCode=13;PlayerSettings.Android.forceInternetPermission=true;AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
 }
 public static void BakeHeroes()
 {
  Directory.CreateDirectory("Assets/Characters/Resources/Interface");var game=UnityEngine.Object.FindFirstObjectByType<MobileRoundGame>();var preview=EditorSceneManager.NewPreviewScene();
  try{
   var cameraObject=new GameObject("Portrait camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,preview);var camera=cameraObject.AddComponent<Camera>();camera.scene=preview;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.allowHDR=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
   var key=new GameObject("Portrait key");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(key,preview);var light=key.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.65f;key.transform.rotation=Quaternion.Euler(35,-25,0);
   var fill=new GameObject("Portrait fill");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fill,preview);var fl=fill.AddComponent<Light>();fl.type=LightType.Directional;fl.intensity=.6f;fl.color=new Color(.65f,.75f,1);fill.transform.rotation=Quaternion.Euler(10,160,0);
   for(int i=0;i<4;i++){
    var hero=(GameObject)PrefabUtility.InstantiatePrefab(game.heroPrefabs[i],preview);var animator=hero.GetComponent<Animator>();animator.Rebind();animator.Update(0);animator.Play("Locomotion");animator.Update(.2f);var skins=hero.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=skins[0].bounds;foreach(var s in skins){s.updateWhenOffscreen=true;s.shadowCastingMode=ShadowCastingMode.Off;bounds.Encapsulate(s.bounds);}
    var center=bounds.center;camera.transform.position=center+new Vector3(2,.2f,6);camera.transform.LookAt(center);camera.orthographicSize=bounds.extents.y*1.12f;Capture(camera,512,768,"Hero"+i);
    var head=animator.GetBoneTransform(HumanBodyBones.Head);var face=head?head.position:center+Vector3.up*bounds.extents.y*.55f;camera.transform.position=face+new Vector3(.5f,.06f,2);camera.transform.LookAt(face-Vector3.up*.08f);camera.orthographicSize=.43f;Capture(camera,256,256,"Face"+i);UnityEngine.Object.DestroyImmediate(hero);
   }
  }finally{EditorSceneManager.ClosePreviewScene(preview);}
  if(Camera.main)Capture(Camera.main,640,360,"ArenaPreview");AssetDatabase.Refresh();
 }
 static void Capture(Camera c,int w,int h,string name){var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);var old=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(w,h,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,w,h),0,0);t.Apply();File.WriteAllBytes("Assets/Characters/Resources/Interface/"+name+".png",t.EncodeToPNG());c.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}
 [MenuItem("Guerra de empujes/Export Citadel Android")]
 public static void Export(){QualitySettings.SetQualityLevel(0,true);QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_Quality_RPAsset.asset");UnityEditor.Rendering.CoreBuildData.instance.Dispose();Directory.CreateDirectory("Builds/Android");File.WriteAllText("Builds/Android/ExportResult072.txt","BUILDING");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};PlayerSettings.bundleVersion="0.7.2";PlayerSettings.Android.bundleVersionCode=13;PlayerSettings.Android.forceInternetPermission=true;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=true;try{var r=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/Android/GradleCitadel",BuildTarget.Android,BuildOptions.AcceptExternalModificationsToPlayer);File.WriteAllText("Builds/Android/ExportResult072.txt",r.summary.result+"; errors="+r.summary.totalErrors);}finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=false;}}
}


