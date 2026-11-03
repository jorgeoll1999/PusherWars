using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GuerraDeEmpujes;

// Run in an isolated project: Unity -batchmode -executeMethod ArsenalPlayCheck.Run
[InitializeOnLoad]
public static class ArsenalPlayCheck
{
 const string Key="guerra.test.arsenal";static int step;static float start,when;static Vector3 launch;static bool extended;static bool visualCaptured;static bool aimedEnd;static bool split,curved,returned,bounced,ramBounced;static float priorX;static MobileRoundGame g;
 static ArsenalPlayCheck(){EditorApplication.update+=Tick;}
 public static void Run(){EditorApplication.delayCall+=Begin;} static void Begin(){EditorSceneManager.OpenScene(CitadelInterfaceBuilder.Scene);var root=GameObject.Find("Mobile round game");if(!root||!root.GetComponent<MobileRoundGame>()){Debug.LogError("Falta el controlador. Usa la ruta completa del proyecto, sin alias 8.3.");Exit(1);return;}SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("ARSENAL PASS: "+message);}
 static void Pose(float x,float z){g.Player.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(x,.04f,z));Physics.SyncTransforms();}
 static void Release(int id,Vector3 dir){var load=g.Player.GetComponent<SpellLoadout>();var slot=load.slots[id];slot.spell.TryGetLevel(1,out var stats);launch=g.Player.transform.position;load.Release(slot.spell,stats,launch+dir*stats.range);when=Time.time;}
 static readonly System.Collections.Generic.HashSet<string> heard=new System.Collections.Generic.HashSet<string>();static ArenaCharacterMotor target;static bool hitOut,hitBack,outPlaced,backPlaced;
 static void CaptureShop(){
  var canvas=g.Hud.SafeRoot.GetComponentInParent<Canvas>();var go=new GameObject("Prueba de tienda");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.transform.position=Vector3.up*10000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=~0;
  var rt=new RenderTexture(1440,810,24);var old=RenderTexture.active;camera.targetTexture=rt;
  try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
   var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();System.IO.Directory.CreateDirectory("Previews");System.IO.File.WriteAllBytes("Previews/Arsenal080.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  finally{canvas.worldCamera=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
 static void CaptureWard(){
  var go=new GameObject("Revisión de Égida");var c=go.AddComponent<Camera>();c.enabled=false;c.CopyFrom(Camera.main);c.enabled=false;c.orthographic=true;c.orthographicSize=2.7f;
  var center=g.Player.transform.position+Vector3.up;c.transform.position=center+new Vector3(3,3,-5);c.transform.LookAt(center);
  var rt=new RenderTexture(800,800,24);var old=RenderTexture.active;c.targetTexture=rt;
  try{c.Render();RenderTexture.active=rt;var tex=new Texture2D(800,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,800),0,0);tex.Apply();System.IO.Directory.CreateDirectory("Previews");System.IO.File.WriteAllBytes("Previews/Egida081.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  finally{RenderTexture.active=old;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
 static void Exit(int code){SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
 static void Tick(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
 try{
  if(start==0)start=Time.realtimeSinceStartup;if(Time.realtimeSinceStartup-start>90)throw new Exception("Tiempo de prueba agotado en paso "+step+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+" game="+(bool)g+" front="+(g&&(bool)g.FrontEnd)+" frames="+Time.frameCount);
  if(!g)g=UnityEngine.Object.FindFirstObjectByType<MobileRoundGame>();if(!g||!g.FrontEnd||!g.FrontEnd.IsOpen&&step==0)return;
  var p=g.Player;var h=p.GetComponent<ArenaHealth>();var stats=p.GetComponent<HeroBattleStats>();
  switch(step){
   case 0:
    SpellAudio.Played+=name=>heard.Add(name);g.FrontEnd.StartBattle();Need(g.Phase==ArenaPhase.Shop,"Se abre tienda antes de combatir");
    step=10;when=Time.realtimeSinceStartup;break;
   case 10:
    if(Time.realtimeSinceStartup-when<.25f)return;
    var shop=UnityEngine.Object.FindFirstObjectByType<ArsenalGallery>(FindObjectsInactive.Include);
    Need(g.progress.Count==2&&g.progress.spells[0]==1&&g.progress.spells[3]==1,"Solo Fuego y Campo iniciales");
    foreach(var name in new[]{"RamCast","RamImpact","RicochetCast","RicochetBounce","AegisCast","FragmentCast","FragmentSplit","FragmentImpact","CurveCast","CurveImpact","MonolithRise","BoomerangCast","BoomerangReturn","BoomerangHit","BoomerangCatch"})Need(Resources.Load<AudioClip>("SpellAudio/"+name)!=null,"Audio importado "+name);
    for(int i=0;i<16;i++)Need(CitadelUi.Spell(i)!=null,"Icono "+i);
    int before=g.progress.gold;shop.Choose(10);CaptureShop();Need(g.progress.gold==before,"La consulta no consume oro");shop.Choose(10);
    Need(g.progress.spells[10]==1&&g.progress.gold==before-100,"Segundo toque compra el búmeran");
    g.BuySpell(4);g.BuySpell(7);before=g.progress.gold;Need(!g.BuySpell(8)&&before==g.progress.gold,"No se admite un sexto poder");
    Need(g.BuySpell(10)&&g.progress.spells[10]==2,"Mejoras permitidas aun con cinco equipados");
    Need(g.BuyItem(3)&&g.BuyItem(4),"Anillo y talismán comprables");Need(Mathf.Abs(stats.DebuffTime-.7f)<.001f,"Talismán reduce duración");
    foreach(int id in new[]{8,10}){var def=p.GetComponent<SpellLoadout>().slots[id].spell;Need(def.levels[0].range==(id==8?8:9)&&def.levels[4].range==(id==8?12:13),"Alcance por nivel correcto: "+id);}
    g.Ready();step=1;break;
   case 1:
    if(g.Phase!=ArenaPhase.Fighting)return;
    foreach(var f in g.Fighters.Skip(1)){var bot=f.GetComponent<MobileArenaBot>();if(bot)bot.enabled=false;f.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(-8,.04f,-6+g.Fighters.IndexOf(f)*2));}
    foreach(var cover in g.World.covers)cover.solid.enabled=false;Pose(0,0);h.invulnerable=false;Release(6,Vector3.right);h.TakeDamage(30);Need(h.CurrentHealth==100,"Égida bloquea daño");step++;break;
   case 2:
    if(!visualCaptured&&Time.time-when>.25f){CaptureWard();visualCaptured=true;}
    if(Time.time-when<2.1f)return;h.TakeDamage(10);Need(h.CurrentHealth==90,"Égida caduca después de dos segundos");Release(7,Vector3.right);step++;break;
   case 3:
    if(UnityEngine.Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Count(x=>x.Kind==SpellKind.Fragment)>=3)split=true;
    if(Time.time-when<1.6f)return;Need(split,"Metralla se abre en tres balas");Need(h.CurrentHealth>90,"Regeneración en combate");Release(8,Vector3.right);step++;break;
   case 4:
    foreach(var x in UnityEngine.Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None))if(x.Kind==SpellKind.Curve){if(Mathf.Abs(x.transform.position.z-launch.z)>3)curved=true;if(x.transform.position.x>launch.x+9&&x.transform.position.z>launch.z+1)extended=true;if(x.transform.position.x>launch.x+7.8f&&Mathf.Abs(x.transform.position.z-launch.z)<.32f)aimedEnd=true;}
    if(Time.time-when<1.5f)return;Need(curved&&aimedEnd&&extended,"Luz curva cruza el punto apuntado y prolonga el arco");target=g.Fighters[1].GetComponent<ArenaCharacterMotor>();target.Warp(new Vector3(-8,.04f,0));target.GetComponent<Rigidbody>().isKinematic=true;
    launch=p.transform.position;when=Time.time;Need(p.GetComponent<SpellLoadout>().TryCastAt(10,new Vector3(12,.04f,0)),"Lanzamiento del búmeran con animación");priorX=0;step++;break;
   case 5:
    foreach(var x in UnityEngine.Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None))if(x.Kind==SpellKind.Boomerang){
     bool inbound=x.transform.position.x<priorX-.01f;if(inbound)returned=true;
     if((!outPlaced&&!inbound||!backPlaced&&inbound)&&x.transform.position.x>3&&x.transform.position.x<5){
      var intercept=x.transform.position+x.transform.forward*.30f;intercept.y=.04f;target.Warp(intercept);if(inbound)backPlaced=true;else outPlaced=true;
     }priorX=x.transform.position.x;}
    if(!hitOut&&target.KnockbackVelocity.x>0){hitOut=true;target.Warp(new Vector3(-8,.04f,0));}
    if(hitOut&&target.KnockbackVelocity.x<0)hitBack=true;
    if(Time.time-when<3.7f)return;Need(hitOut&&hitBack,"Búmeran empuja de ida y arrastra al volver");Need(returned,"Búmeran vuelve al lanzador");Pose(12,0);priorX=12;Release(5,Vector3.right);step++;break;
   case 6:
    foreach(var x in UnityEngine.Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None))if(x.Kind==SpellKind.Ricochet){if(x.transform.position.x<priorX-.01f)bounced=true;priorX=x.transform.position.x;}
    if(Time.time-when<1.4f)return;Need(bounced,"Ricochet rebota en el borde");priorX=p.transform.position.x;Release(4,Vector3.right);step++;break;
   case 7:
    if(p.transform.position.x<priorX-.01f)ramBounced=true;priorX=p.transform.position.x;if(Time.time-when<1)return;
    Need(ramBounced,"Ariete rebota con el propio mago");Pose(0,0);Release(9,Vector3.forward);Need(stats.summoned&&stats.summoned.GetComponent<ArenaCover>().solid.enabled,"Monolito crea cobertura sólida");step++;break;
   case 8:
    if(Time.time-when<4.3f)return;Need(!stats.summoned,"Cobertura temporal desaparece");target.Warp(new Vector3(2,.04f,0));target.GetComponent<ArenaHealth>().ResetHealth();Release(4,Vector3.right);step++;break;
   case 9:
    if(Time.time-when<.7f)return;Need(target.KnockbackVelocity.x>0&&target.GetComponent<ArenaHealth>().CurrentHealth==100,"Ariete empuja al rival sin dañarlo");foreach(var name in new[]{"RamCast","RamImpact","RicochetCast","RicochetBounce","AegisCast","FragmentCast","FragmentSplit","CurveCast","MonolithRise","BoomerangCast","BoomerangReturn","BoomerangHit","BoomerangCatch"})Need(heard.Contains(name),"Audio sincronizado "+name);Debug.Log("ARSENAL CHECK COMPLETE");Exit(0);break;
  }
 }catch(Exception e){Debug.LogException(e);Exit(1);}
 }
}