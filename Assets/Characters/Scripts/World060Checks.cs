#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace GuerraDeEmpujes
{
 public sealed class World060Checks:MonoBehaviour
 {
  readonly List<string> rows=new List<string>();void Check(bool ok,string text){rows.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Logs/World060Checks.txt",rows);if(!ok)throw new System.Exception(text);}
  IEnumerator Start(){yield return null;var g=FindFirstObjectByType<MobileRoundGame>();g.FrontEnd.Close();g.Hud.SetVisible(false);var p=g.Player;var h=p.GetComponent<ArenaHealth>();var motor=p.GetComponent<ArenaCharacterMotor>();var load=p.GetComponent<SpellLoadout>();var enemy=g.Fighters[1];var eh=enemy.GetComponent<ArenaHealth>();eh.invulnerable=false;var c=g.World.covers[0];
   Check(g.World.covers.Length==4,"four clear destructible covers");Check(!GameObject.Find("Vegetation_Painted")&&!GameObject.Find("Ruins_Painted"),"confusing decoration hidden");
   motor.Warp(new Vector3(-9,.04f,-4.6f));enemy.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(-2,.04f,-4.6f));Physics.SyncTransforms();load.GetStats(1,out var ray);load.Release(load.slots[1].spell,ray,enemy.transform.position);Check(c.hits==c.capacity-1&&eh.CurrentHealth==100,"ray strikes cover without hitting wizard behind");
   load.GetStats(0,out var fire);var shot=Instantiate(load.slots[0].spell.projectilePrefab,new Vector3(-9,1,-4.6f),Quaternion.LookRotation(Vector3.right));shot.Initialize(h,fire,Vector3.right);yield return new WaitForSeconds(.4f);Check(!shot&&c.hits==c.capacity-2&&eh.CurrentHealth==100,"fire stops at cover and does not pierce");load.Release(load.slots[1].spell,ray,enemy.transform.position);Check(!c.Present&&!c.solid.enabled,"third impact destroys three-hit cover");load.Release(load.slots[1].spell,ray,enemy.transform.position);Check(eh.CurrentHealth==90,"after destruction ray can reach wizard");
   c.Apply(c.capacity);enemy.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(-5,.04f,5));motor.Warp(new Vector3(-8,.04f,-4.6f));motor.SetMoveDirection(Vector3.right);yield return new WaitForSeconds(1);Check(p.transform.position.x<-6.55f,"character cannot walk through solid cover");motor.SetMoveDirection(Vector3.zero);motor.Warp(new Vector3(-9,.04f,-4.6f));
   load.GetStats(2,out var tp);tp.range=7;tp.speed=12;shot=Instantiate(load.slots[2].spell.projectilePrefab,new Vector3(-9,1,-4.6f),Quaternion.LookRotation(Vector3.right));shot.Initialize(h,tp,Vector3.right,SpellKind.TeleportSwap);Check(load.IsTeleporting&&!load.TryCastAt(3,p.transform.position),"shield rejected while teleport projectile is in flight");yield return new WaitForSeconds(.8f);Check(!load.IsTeleporting&&p.transform.position.x>-2.5f&&c.hits==c.capacity,"teleport passes intact cover without damaging it");
   motor.Warp(new Vector3(-5,.04f,0));var midway=g.Fighters[2].GetComponent<ArenaCharacterMotor>();midway.Warp(new Vector3(-2,.04f,0));enemy.GetComponent<ArenaCharacterMotor>().Warp(new Vector3(2,.04f,0));
   var crossing=Instantiate(load.slots[0].spell.projectilePrefab,new Vector3(-3,1,0),Quaternion.LookRotation(Vector3.left));crossing.Initialize(eh,fire,Vector3.left);
   shot=Instantiate(load.slots[2].spell.projectilePrefab,new Vector3(-5,1,0),Quaternion.LookRotation(Vector3.right));shot.Initialize(h,tp,Vector3.right,SpellKind.TeleportSwap);
   yield return new WaitForSeconds(.85f);Check(Mathf.Abs(p.transform.position.x-2)<.15f&&Mathf.Abs(enemy.transform.position.x+5)<.15f&&Mathf.Abs(midway.transform.position.x+2)<.15f,"TP ignores crossing fire and intervening wizard, swaps with endpoint occupant");
   var five=g.World.covers[2];Check(five.capacity==5,"five-hit cover variant");for(int i=0;i<4;i++)five.Strike();Check(five.Present,"five-hit cover survives four attacks");five.Strike();Check(!five.Present,"five-hit cover destroyed by fifth attack");var state=JsonUtility.FromJson<OnlinePacket>(JsonUtility.ToJson(new OnlinePacket{coverHits=g.World.State()}));g.World.covers[1].Apply(0);g.World.Apply(state.coverHits);Check(g.World.covers[1].Present,"cover health snapshot restores replicated state");
   g.Hud.AimPreview(0,Vector2.right*80*g.Hud.UiScale);Check(GameObject.Find("Tactical aim arrow")&&!GameObject.Find("Mobile aim"),"symmetric filled tactical arrow replaces freehand line");Check(Resources.Load<Texture2D>("Battlefield/LavaBasalt060"),"new basalt lava texture packaged");g.ConfigureBattle(0,LocalBattleMode.GrandMelee);yield return null;Physics.SyncTransforms();
   Check(g.Fighters.Count==8&&g.Arena.Wide,"eight distinct fighters use grand court");var starts=new HashSet<Vector3>();foreach(var f in g.Fighters)starts.Add(f.transform.position);Check(starts.Count==8,"eight separate spawn positions");
   RaycastHit ground;Check(Physics.Raycast(new Vector3(18,2,10),Vector3.down,out ground,4)&&!ground.collider.GetComponent<LavaSurface>(),"rectangle corner is safe");g.Arena.SetRadius(7);Physics.SyncTransforms();Check(Physics.Raycast(new Vector3(18,2,10),Vector3.down,out ground,4)&&ground.collider.GetComponent<LavaSurface>(),"shrinking rectangle exposes solid lava");rows.Add("COMPLETE");File.WriteAllLines("Logs/World060Checks.txt",rows);Destroy(gameObject);
  }
 }
}
#endif
