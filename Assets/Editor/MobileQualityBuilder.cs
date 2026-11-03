using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GuerraDeEmpujes;
using System.IO;
public static class MobileQualityBuilder
{
 public const string Scene="Assets/Characters/Scenes/Mobile_Quality_Playtest.unity";
 [MenuItem("Guerra de empujes/Prepare Mobile Quality")]
 public static void Prepare()
 {
  if(EditorSceneManager.GetActiveScene().path!=Scene){
   if(EditorSceneManager.GetActiveScene().path!=MobilePlaytestBuilder.Scene)EditorSceneManager.OpenScene(MobilePlaytestBuilder.Scene);
   var lightManager=UnityEngine.Object.FindFirstObjectByType<MobileLightweight>();
   if(lightManager){lightManager.lightweight=true;lightManager.pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");EditorUtility.SetDirty(lightManager);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());}
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene,true);EditorSceneManager.OpenScene(Scene);
  }
  const string pipelinePath="Assets/Settings/Mobile_Quality_RPAsset.asset";
  if(!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath))AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset",pipelinePath);
  var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);rp.renderScale=1;rp.msaaSampleCount=2;rp.supportsHDR=true;rp.shadowDistance=42;rp.shadowCascadeCount=1;
  var data=new SerializedObject(rp);
  data.FindProperty("m_MainLightShadowsSupported").boolValue=true;data.FindProperty("m_MainLightShadowmapResolution").intValue=1024;data.FindProperty("m_SoftShadowsSupported").boolValue=true;data.FindProperty("m_SoftShadowQuality").intValue=1;
  data.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;data.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=2;data.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(rp);
  QualitySettings.SetQualityLevel(0,true);QualitySettings.renderPipeline=rp;QualitySettings.shadows=UnityEngine.ShadowQuality.All;
  foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){bool ground=r.name.Contains("Lava")||r.name.Contains("Background");r.shadowCastingMode=ground?ShadowCastingMode.Off:ShadowCastingMode.On;r.receiveShadows=!ground;}
  foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(light.type==LightType.Directional){light.shadows=LightShadows.Soft;light.intensity=1.65f;light.color=new Color(1,.94f,.86f);light.shadowStrength=.65f;light.shadowBias=.035f;light.shadowNormalBias=.3f;}}
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.56f,.64f,.80f);RenderSettings.ambientEquatorColor=new Color(.34f,.39f,.49f);RenderSettings.ambientGroundColor=new Color(.27f,.23f,.22f);
  var manager=UnityEngine.Object.FindFirstObjectByType<MobileLightweight>();manager.lightweight=false;manager.qualityMode=1;manager.allowQualitySwitch=true;manager.pipeline=rp;EditorUtility.SetDirty(manager);
  const string profilePath="Assets/Settings/MobileQualityVolume.asset";
  var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
  Bloom bloom;if(!profile.TryGet<Bloom>(out bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.active=true;bloom.intensity.Override(.3f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.6f);bloom.highQualityFiltering.Override(false);
  Tonemapping tone;if(!profile.TryGet<Tonemapping>(out tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.active=true;tone.mode.Override(TonemappingMode.ACES);
  ColorAdjustments color;if(!profile.TryGet<ColorAdjustments>(out color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}color.active=true;color.contrast.Override(6);color.saturation.Override(5);color.postExposure.Override(.1f);EditorUtility.SetDirty(profile);
  var volume=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);foreach(var v in volume)if(v.isGlobal)v.enabled=false;
  var root=GameObject.Find("Mobile quality atmosphere");if(!root)root=new GameObject("Mobile quality atmosphere");var atmosphere=root.GetComponent<Volume>();if(!atmosphere)atmosphere=root.AddComponent<Volume>();atmosphere.enabled=true;atmosphere.isGlobal=true;atmosphere.priority=10;atmosphere.sharedProfile=profile;
  if(Camera.main){Camera.main.allowHDR=true;Camera.main.allowMSAA=true;Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
  PlayerSettings.bundleVersion="0.2.3";PlayerSettings.Android.bundleVersionCode=4;
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
 }
 [MenuItem("Guerra de empujes/Export Mobile Quality Android")]
 public static void Export()
 {
  QualitySettings.SetQualityLevel(0,true);QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_Quality_RPAsset.asset");
  UnityEditor.Rendering.CoreBuildData.instance.Dispose();
  File.WriteAllText("Builds/Android/ExportResult023.txt","BUILDING");PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;PlayerSettings.bundleVersion="0.2.3";PlayerSettings.Android.bundleVersionCode=4;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
  try{var report=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/Android/GradleQuality",BuildTarget.Android,BuildOptions.AcceptExternalModificationsToPlayer);File.WriteAllText("Builds/Android/ExportResult023.txt",report.summary.result+"; errors="+report.summary.totalErrors);}
  finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=false;}
 }
}




