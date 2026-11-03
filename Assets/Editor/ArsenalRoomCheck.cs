using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GuerraDeEmpujes;

[InitializeOnLoad]
public static class ArsenalRoomCheck
{
 const string Key="guerra.test.arsenal-room";
 static float started;static int step;
 static ArsenalRoomCheck(){EditorApplication.update+=Tick;}
 public static void Run(){EditorApplication.delayCall+=()=>{EditorSceneManager.OpenScene(CitadelInterfaceBuilder.Scene);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();};}
 static void Tick(){
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
  try{if(started==0)started=Time.realtimeSinceStartup;if(Time.realtimeSinceStartup-started>75)throw new Exception("Sin respuesta de la prueba local, paso "+step);
   var g=UnityEngine.Object.FindAnyObjectByType<MobileRoundGame>();if(!g||!g.FrontEnd||!g.Network)return;var net=g.Network;
   switch(step){
    case 0:if(!g.FrontEnd.IsOpen||!File.Exists("Logs/arsenal-local.txt"))return;
     var endpoint=File.ReadAllText("Logs/arsenal-local.txt").Trim();if(!endpoint.StartsWith("ws://127.0.0.1:"))throw new Exception("Solo servidor de prueba local");
     if(Resources.Load<TextAsset>("OnlineConfig").text.Contains("sslip.io"))throw new Exception("Ejecutar en copia con OnlineConfig local para aislar preferencias");
     net.Connect(true,endpoint,0);step++;break;
    case 1:if(net.Code.Length!=8)return;File.WriteAllText("Logs/arsenal-code.txt",net.Code);net.SetReady();step++;break;
    case 2:if(net.Peers.Length!=2||!net.Peers.All(x=>x.ready))return;net.StartMatch();step++;break;
    case 3:if(!net.Playing)return;g.Ready();step++;break;
    case 4:if(!File.Exists("Logs/arsenal-peer-pass.txt"))return;Debug.Log("ARSENAL NET COMPLETE");End(0);break;
   }
  }catch(Exception e){Debug.LogException(e);End(1);}
 }
 static void End(int code){SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
}