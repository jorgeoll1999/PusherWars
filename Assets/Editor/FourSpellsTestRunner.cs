using UnityEngine;
using UnityEditor;
using System.IO;
using GuerraDeEmpujes;
[InitializeOnLoad]
public static class FourSpellsTestRunner
{
 static FourSpellsTestRunner(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Poll;}
 [MenuItem("Guerra de empujes/Run Four Spell Checks")]
 public static void Run(){File.WriteAllText("ArtSource/Characters/FourSpellsTests.txt","RUNNING\n");SessionState.SetString("FourSpellsChecks","entering");EditorApplication.isPlaying=true;}
 static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetString("FourSpellsChecks","")=="entering"){Application.runInBackground=true;SessionState.SetString("FourSpellsChecks","running");SessionState.SetFloat("FourSpellsCheckTime",(float)EditorApplication.timeSinceStartup);new GameObject("FourSpellsTest").AddComponent<FourSpellsSmokeTest>();}}
 static void Poll(){if(SessionState.GetString("FourSpellsChecks","")!="running")return;var path="ArtSource/Characters/FourSpellsTests.txt";if((File.Exists(path) && File.ReadAllText(path).Contains("ALL CHECKS PASSED")) || EditorApplication.timeSinceStartup-SessionState.GetFloat("FourSpellsCheckTime",0)>20){SessionState.SetString("FourSpellsChecks","");EditorApplication.isPlaying=false;}}
}
