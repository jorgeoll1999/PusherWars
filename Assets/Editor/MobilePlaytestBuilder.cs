using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using Object = UnityEngine.Object;
using GuerraDeEmpujes;
public static class MobilePlaytestBuilder
{
 public const string Scene="Assets/Characters/Scenes/Mobile_Playtest.unity";
 [MenuItem("Guerra de empujes/Create Mobile Playtest")]
 public static void Create()
 {
  var source=EditorSceneManager.GetActiveScene();if(source.path!=Scene){EditorSceneManager.SaveScene(source,Scene,true);EditorSceneManager.OpenScene(Scene);}
  foreach(var health in Object.FindObjectsByType<ArenaHealth>(FindObjectsSortMode.None))Object.DestroyImmediate(health.gameObject);
  foreach(var game in Object.FindObjectsByType<MobileRoundGame>(FindObjectsSortMode.None))Object.DestroyImmediate(game.gameObject);
  string[] names={"Fireball","Lightning","TeleportSwap","Bubble"};
  for(int kind=0;kind<4;kind++)
  {
   var spell=AssetDatabase.LoadAssetAtPath<SpellDefinition>("Assets/Characters/Combat/Spells/"+names[kind]+".asset");spell.levels=new SpellLevel[5];
   for(int level=0;level<5;level++)spell.levels[level]=new SpellLevel{damage=kind==0?14+level*4:kind==1?10+level*3:0,knockback=kind==0?5+level:kind==1?4+level*.8f:kind==3?8+level*1.5f:0,cooldown=(kind==0?2.2f:kind==1?4:kind==2?8:10)*(1-level*.1f),range=(kind==2?9:14)+level,speed=(kind==2?14:12)+level,radius=2.5f+level*.2f,duration=1.2f+level*.1f,hitRadius=(kind==1?.12f:.18f)+level*.015f,upgradeCost=level==0?0:level+1};EditorUtility.SetDirty(spell);
  }
  var manager=new GameObject("Mobile round game").AddComponent<MobileRoundGame>();manager.heroPrefabs=new GameObject[4];string[] heroes={"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"};for(int i=0;i<4;i++)manager.heroPrefabs[i]=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/"+heroes[i]+"/"+heroes[i]+".prefab");
  if(Camera.main){Camera.main.transform.position=new Vector3(0,20,-15);Camera.main.transform.LookAt(Vector3.zero);Camera.main.orthographic=true;Camera.main.orthographicSize=10.6f;}
  PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
  PlayerSettings.productName="Guerra de empujes";PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.guerradeempujes.playtest");PlayerSettings.bundleVersion="0.2.0";
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Selection.activeGameObject=manager.gameObject;
 }
 [MenuItem("Guerra de empujes/Build Android Tablet APK")]
 public static void BuildAndroid()
 {
  Directory.CreateDirectory("Builds/Android");File.WriteAllText("Builds/Android/BuildResult.txt","BUILDING");PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;PlayerSettings.bundleVersion="0.2.2";PlayerSettings.Android.bundleVersionCode=3;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;EditorUserBuildSettings.buildAppBundle=false;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
  // Windows AF_UNIX sockets cannot reliably use some short-name TEMP paths.
  string sockets=Path.Combine(Environment.GetEnvironmentVariable("PUBLIC")??Path.GetTempPath(),"GuerraJavaSockets");Directory.CreateDirectory(sockets);
  string previous=Environment.GetEnvironmentVariable("JAVA_TOOL_OPTIONS");
  Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS",(previous??"")+" -Djdk.net.unixdomain.tmpdir="+sockets.Replace('\\','/')+" -Djava.net.preferIPv4Stack=true");
  try{var report=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/Android/GuerraDeEmpujes-Tablet.apk",BuildTarget.Android,BuildOptions.None);File.WriteAllText("Builds/Android/BuildResult.txt",report.summary.result+"; errors="+report.summary.totalErrors+"; bytes="+report.summary.totalSize);}
  finally{Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS",previous);}
 }
 public static void ExportAndroid()
 {
  Directory.CreateDirectory("Builds/Android");File.WriteAllText("Builds/Android/ExportResult022.txt","BUILDING");
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
  PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;
  PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;
  PlayerSettings.bundleVersion="0.2.2";PlayerSettings.Android.bundleVersionCode=3;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
  EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=true;
  try{var report=BuildPipeline.BuildPlayer(new[]{Scene},"Builds/Android/GradleProject",BuildTarget.Android,BuildOptions.AcceptExternalModificationsToPlayer);File.WriteAllText("Builds/Android/ExportResult022.txt",report.summary.result+"; errors="+report.summary.totalErrors);}
  finally{EditorUserBuildSettings.exportAsGoogleAndroidProject=false;}
 }
}



