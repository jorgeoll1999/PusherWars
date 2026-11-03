#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace GuerraDeEmpujes
{
 public sealed class SpellCastSyncReview:MonoBehaviour
 {
  readonly List<string> results=new List<string>();readonly List<GameObject> cleanup=new List<GameObject>();
  readonly GameObject[] players=new GameObject[4];readonly int[] calls=new int[4];readonly float[] releasedAt=new float[4];
  readonly bool[] effectsReady=new bool[4],correctState=new bool[4];float started;Camera reviewCamera;
  void Check(bool ok,string text){results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("ArtSource/Characters/Review/SpellCastSyncChecks.txt",results);}
  IEnumerator Start()
  {
   Application.runInBackground=true;Time.timeScale=1;Directory.CreateDirectory("ArtSource/Characters/Review");
   File.WriteAllText("ArtSource/Characters/Review/SpellCastSyncChecks.txt","RUNNING\n");
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);cleanup.Add(floor);floor.transform.position=new Vector3(1207.5f,9.5f,3);floor.transform.localScale=new Vector3(40,1,30);
   reviewCamera=new GameObject("Spell synchronization review camera").AddComponent<Camera>();cleanup.Add(reviewCamera.gameObject);reviewCamera.orthographic=true;reviewCamera.orthographicSize=2;reviewCamera.aspect=1.4f;reviewCamera.clearFlags=CameraClearFlags.SolidColor;reviewCamera.backgroundColor=new Color(.12f,.14f,.18f);
   var names=new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"};
   for(int i=0;i<4;i++)
   {
    int actor=i;var p=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/"+names[i]+"/"+names[i]+".prefab"),new Vector3(1200+i*5,10,0),Quaternion.identity);players[i]=p;cleanup.Add(p);p.GetComponent<LavaContactDamage>().enabled=false;
    var input=p.GetComponent<CombatPlayerInput>();if(input)input.enabled=false;
    p.GetComponent<CharacterAnimationDriver>().SpellReleased+=kind=>{
     calls[actor]++;releasedAt[actor]=Time.time-started;var driver=p.GetComponent<CharacterAnimationDriver>();var profile=driver.GetCastAnimation(kind);var origin=driver.GetCastOrigin(kind);
     correctState[actor]=p.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName(profile.state);
     var sounds=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
     if(kind==SpellKind.Fireball || kind==SpellKind.Lightning){string clip=kind==SpellKind.Fireball?"FireCast":"LightningCast";effectsReady[actor]=sounds.Any(s=>s.clip && s.clip.name==clip && s.isPlaying && Vector3.Distance(s.transform.position,origin)<.1f);}
     else if(kind==SpellKind.TeleportSwap)effectsReady[actor]=!sounds.Any(s=>s.clip && s.clip.name=="TeleportCast");
     else effectsReady[actor]=p.GetComponent<BubbleShield>() && p.GetComponent<BubbleShield>().Active && sounds.Any(s=>s.clip && s.clip.name=="BubbleCast12" && Vector3.Distance(s.transform.position,p.transform.position)<.1f);
     if(kind==SpellKind.Fireball || kind==SpellKind.TeleportSwap)effectsReady[actor]&=UnityEngine.Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Any(s=>Vector3.Distance(s.transform.position,origin)<.1f);
     driver.OnSpellRelease((int)kind);driver.OnSpellRelease((int)kind); // Duplicate animation events must not duplicate a spell.
     if(actor==0)StartCoroutine(Capture(p,kind));
    };
   }
   yield return new WaitForSeconds(.15f);
   for(int k=0;k<4;k++)
   {
    var kind=(SpellKind)k;Array.Clear(calls,0,4);Array.Clear(effectsReady,0,4);Array.Clear(correctState,0,4);
    started=Time.time;
    for(int i=0;i<4;i++){var p=players[i];p.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(1200+i*5,10,0));p.transform.rotation=Quaternion.identity;Check(p.GetComponent<SpellLoadout>().TryCastAt(k,new Vector3(1200+i*5,10,6)),names[i]+" starts "+kind);}
    var profile=players[0].GetComponent<CharacterAnimationDriver>().GetCastAnimation(kind);
    if(profile.releaseTime>.1f){yield return new WaitForSeconds(profile.releaseTime-.1f);Check(calls.All(c=>c==0),kind+" has no release before the gesture event");}else{yield return null;yield return null;Check(calls.All(c=>c==1),kind+" activates with the animation start");}
    while(Time.time-started<profile.clip.length+.3f)yield return null;
    for(int i=0;i<4;i++)
    {
     bool time=Mathf.Abs(releasedAt[i]-profile.releaseTime)<.07f;var driver=players[i].GetComponent<CharacterAnimationDriver>();bool idle=!driver.IsCasting && players[i].GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Locomotion");
     Check(calls[i]==1 && time && correctState[i] && effectsReady[i] && idle,names[i]+" "+kind+" exactly once; release="+releasedAt[i].ToString("F3")+"s expected="+profile.releaseTime.ToString("F3")+"s; pose="+correctState[i]+" effects/audio="+effectsReady[i]+" returns="+idle);
     if(kind==SpellKind.TeleportSwap)Check(Vector2.Distance(new Vector2(players[i].transform.position.x,players[i].transform.position.z),new Vector2(1200+i*5,6))<.12f,names[i]+" hand-origin teleport reaches the selected point");
    }
   }
   var first=players[0];int before=calls[0];Check(first.GetComponent<SpellLoadout>().TryCastAt(0,first.transform.position+Vector3.forward*6),"Final pending cast starts");first.GetComponent<ArenaHealth>().TakeDamage(1000);yield return new WaitForSeconds(.5f);
   Check(calls[0]==before && !first.GetComponent<CharacterAnimationDriver>().IsCasting,"Death cancels the animation event and pending spell");
   bool failed=results.Any(r=>r.StartsWith("FAIL"));results.Add(failed?"FAILED":"ALL CHECKS PASSED");File.WriteAllLines("ArtSource/Characters/Review/SpellCastSyncChecks.txt",results);Destroy(gameObject);
  }
  IEnumerator Capture(GameObject player,SpellKind kind)
  {
   yield return null;yield return null;
   reviewCamera.transform.position=player.transform.position+new Vector3(4,2.4f,4.5f);reviewCamera.transform.LookAt(player.transform.position+Vector3.up*.95f+Vector3.forward*.5f);
   var rt=new RenderTexture(840,600,24);var texture=new Texture2D(840,600,TextureFormat.RGB24,false);reviewCamera.targetTexture=rt;reviewCamera.Render();var old=RenderTexture.active;RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,840,600),0,0);texture.Apply();File.WriteAllBytes("Previews/SpellCast_"+kind+".png",texture.EncodeToPNG());RenderTexture.active=old;reviewCamera.targetTexture=null;Destroy(rt);Destroy(texture);
  }
  void OnDestroy(){foreach(var go in cleanup)if(go)Destroy(go);}
 }
}
#endif
