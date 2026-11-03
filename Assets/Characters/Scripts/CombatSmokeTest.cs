#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace GuerraDeEmpujes
{
 public sealed class CombatSmokeTest:MonoBehaviour
 {
  readonly List<Object> cleanup=new List<Object>();readonly List<string> results=new List<string>();bool failed;
  void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);failed|=!ok;File.WriteAllLines("ArtSource/Characters/CombatSmokeTest.txt",results);}
  GameObject Player(string name,Vector3 p){var go=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/"+name+"/"+name+".prefab"),p,Quaternion.identity);cleanup.Add(go);return go;}
  IEnumerator Start()
  {
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,9.5f,3);floor.transform.localScale=new Vector3(20,1,20);cleanup.Add(floor);
   var a=Player("MagaArcana",new Vector3(1000,10,0));var b=Player("MagaFuego",new Vector3(1000,10,3));
   var health=b.GetComponent<ArenaHealth>();var loadout=a.GetComponent<SpellLoadout>();
   var spell=ScriptableObject.CreateInstance<SpellDefinition>();cleanup.Add(spell);
   spell.projectilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Combat/Projectile_Base.prefab").GetComponent<SpellProjectile>();
   spell.levels=new[]{new SpellLevel{damage=15,knockback=5,speed=30,range=12,cooldown=1},new SpellLevel{damage=20,knockback=7,speed=30,range=12,cooldown=1,upgradeCost=3}};
   loadout.slots[0].spell=spell;loadout.upgradePoints=2;Check(!loadout.TryUpgrade(0),"Insufficient upgrade points rejected");loadout.upgradePoints=3;Check(loadout.TryUpgrade(0)&&loadout.slots[0].level==2&&loadout.upgradePoints==0,"Upgrade applies level and cost");Check(!loadout.TryUpgrade(0),"Maximum level enforced");loadout.slots[0].level=1;
   yield return new WaitForSeconds(.1f);
   Check(loadout.TryCast(0,Vector3.forward),"Casting starts");Check(!loadout.TryCast(0,Vector3.forward),"Casting/cooldown rejects repeat");
   yield return new WaitForSeconds(.25f);Check(health.CurrentHealth==100,"No damage before release frame");
   yield return new WaitForSeconds(.4f);Check(Mathf.Abs(health.CurrentHealth-85)<.01f,"Capsule projectile applies damage once");Check(b.transform.position.z>3.05f,"Projectile pushes target");Check(a.GetComponent<ArenaHealth>().CurrentHealth==100,"Caster immune to own projectile");
   yield return new WaitForSeconds(1.1f);var driver=a.GetComponent<CharacterAnimationDriver>();
   for(int i=0;i<4;i++){var kind=(SpellKind)i;Check(driver.TryCast(kind,null),"Spell-specific cast starts");Check(driver.ActiveSpellKind==kind,"Correct spell-specific animation selected");yield return new WaitForSeconds(driver.GetCastDuration(kind)+.12f);}
   bool released=false;driver.TryCast(()=>released=true);a.GetComponent<ArenaHealth>().TakeDamage(1000);yield return new WaitForSeconds(.5f);
   Check(!released&&!driver.TryCast(null),"Death cancels pending release and further casting");Check(a.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Death"),"Death animation connected");
   var lava=Player("MagoHielo",new Vector3(13,0,0));yield return new WaitForSeconds(.5f);Check(lava.GetComponent<ArenaHealth>().CurrentHealth<100,"Lava damages character capsule");Check(lava.transform.position.y>-.15f,"Solid lava supports character");
   results.Add(failed?"FAILED":"ALL CHECKS PASSED");File.WriteAllLines("ArtSource/Characters/CombatSmokeTest.txt",results);Debug.Log(results[results.Count-1]);Destroy(gameObject);
  }
  void OnDestroy(){foreach(var obj in cleanup)if(obj)Destroy(obj);}
 }
}
#endif

#if UNITY_EDITOR
namespace GuerraDeEmpujes
{
 public sealed class FourSpellsSmokeTest:MonoBehaviour
 {
  List<Object> cleanup=new List<Object>();List<string> results=new List<string>();bool failed;
  void Check(bool ok,string text){failed|=!ok;results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("ArtSource/Characters/FourSpellsTests.txt",results);}
  GameObject Player(Vector3 p){var go=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/MagaArcana/MagaArcana.prefab"),p,Quaternion.identity);cleanup.Add(go);go.GetComponent<LavaContactDamage>().enabled=false;return go;}
  void Warp(GameObject go,Vector3 p){go.GetComponent<ArenaCharacterMotor>().Warp(p);}
  IEnumerator Start()
  {
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,9.5f,0);floor.transform.localScale=new Vector3(40,1,40);cleanup.Add(floor);
   var a=Player(new Vector3(1000,10,0));var b=Player(new Vector3(1000,10,4));var near=Player(new Vector3(1001,10,0));var far=Player(new Vector3(1006,10,0));
   var load=a.GetComponent<SpellLoadout>();var health=b.GetComponent<ArenaHealth>();yield return new WaitForSeconds(.15f);
   load.Release(load.slots[0].spell,load.slots[0].spell.levels[0],b.transform.position);var castSound=GameObject.Find("Spell sound FireCast");Check(castSound && castSound.GetComponent<AudioSource>().isPlaying && castSound.GetComponent<AudioSource>().pitch==1 && castSound.GetComponent<AudioSource>().clip.length>1.3f,"Full fire audio starts at release without pitch shift");yield return new WaitForSeconds(.5f);Check(health.CurrentHealth==86,"Fireball deals configured damage once");Check(b.transform.position.z>4,"Fireball pushes");Check(castSound && castSound.GetComponent<AudioSource>().isPlaying && castSound.GetComponent<AudioSource>().time>.3f && !GameObject.Find("Spell sound FireImpact"),"Fire audio continues through impact without a second sound");
   health.ResetHealth();Warp(b,new Vector3(1000,10,4));load.Release(load.slots[1].spell,load.slots[1].spell.levels[0],b.transform.position);Check(health.CurrentHealth==90,"Lightning impacts instantly");
   Warp(a,new Vector3(1000,10,0));Warp(b,new Vector3(1000,10,4));health.ResetHealth();load.Release(load.slots[2].spell,load.slots[2].spell.levels[0],b.transform.position);Check(!GameObject.Find("Spell sound TeleportCast") && !GameObject.Find("Spell sound TeleportArrival"),"Teleport launch is silent");yield return new WaitForSeconds(.4f);Check(Vector3.Distance(a.transform.position,new Vector3(1000,10,4))<.15f && Vector3.Distance(b.transform.position,new Vector3(1000,10,0))<.15f,"Teleport projectile swaps both characters");Check(health.CurrentHealth==100,"Swap does not damage target");int arrivals=0;foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))if(source.clip && source.clip.name=="TeleportArrival")arrivals++;Check(arrivals==1 && Resources.Load<AudioClip>("SpellAudio/TeleportArrival").length>1,"Full teleport audio plays once on arrival");
   Warp(a,new Vector3(1000,10,0));Warp(b,new Vector3(1000,10,6));load.Release(load.slots[2].spell,load.slots[2].spell.levels[0],new Vector3(1003,10,3));yield return new WaitForSeconds(.45f);Check(Vector2.Distance(new Vector2(a.transform.position.x,a.transform.position.z),new Vector2(1003,3))<.15f,"Empty destination reached at selected point");
   Warp(a,new Vector3(1000,10,0));Warp(b,new Vector3(1000,10,4));var shield=b.AddComponent<BubbleShield>();shield.Activate(load.slots[3].spell.levels[0]);health.ResetHealth();load.Release(load.slots[1].spell,load.slots[1].spell.levels[0],b.transform.position);Check(health.CurrentHealth==100,"Bubble blocks lightning");load.Release(load.slots[0].spell,load.slots[0].spell.levels[0],b.transform.position);yield return new WaitForSeconds(.5f);Check(health.CurrentHealth==100,"Bubble blocks fireball");shield.Detonate();
   Warp(a,new Vector3(1000,10,0));Warp(near,new Vector3(1001,10,0));Warp(far,new Vector3(1006,10,0));var bubble=a.AddComponent<BubbleShield>();bubble.Activate(load.slots[3].spell.levels[0]);bubble.Detonate();Check(near.GetComponent<ArenaCharacterMotor>().KnockbackVelocity.x>0,"Bubble pushes close character outward");Check(far.GetComponent<ArenaCharacterMotor>().KnockbackVelocity.sqrMagnitude<.01f,"Bubble does not push distant character");Check(near.GetComponent<ArenaHealth>().CurrentHealth==100,"Bubble explosion only pushes");
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1004,11,0);wall.transform.localScale=new Vector3(2,20,2);cleanup.Add(wall);Physics.SyncTransforms();Check(!SpellCombat.Teleport(a.GetComponent<ArenaHealth>(),null,wall.transform.position),"Unsafe wall destination rejected");
   load.upgradePoints=2;Check(load.TryUpgrade(0)&&load.slots[0].level==2&&load.upgradePoints==0,"Upgrade changes level and spends points");
   for(int level=0;level<5;level++){var stats=load.slots[3].spell.levels[level];bubble.Activate(stats);var source=GameObject.Find("Spell sound BubbleCast"+Mathf.RoundToInt(stats.duration*10));Check(source && Mathf.Abs(source.GetComponent<AudioSource>().clip.length-stats.duration)<.01f && source.GetComponent<AudioSource>().pitch==1,"Bubble charge audio matches level "+(level+1)+" duration");var audio=source?source.GetComponent<AudioSource>():null;bubble.Detonate();Check(!audio || !audio.isPlaying,"Bubble detonation stops charge at level "+(level+1));yield return null;}
   results.Add(failed?"FAILED":"ALL CHECKS PASSED");File.WriteAllLines("ArtSource/Characters/FourSpellsTests.txt",results);Debug.Log(results[results.Count-1]);Destroy(gameObject);
  }
  void OnDestroy(){foreach(var o in cleanup)if(o)Destroy(o);}
 }
}
#endif
