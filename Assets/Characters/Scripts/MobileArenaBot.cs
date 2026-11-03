using UnityEngine;
namespace GuerraDeEmpujes
{
 public sealed class MobileArenaBot:MonoBehaviour
 {
  public MobileRoundGame game;float nextCast,phase;ArenaCharacterMotor motor;SpellLoadout load;ArenaHealth health;
  void Awake(){phase=Random.value*6.28f;motor=GetComponent<ArenaCharacterMotor>();load=GetComponent<SpellLoadout>();health=GetComponent<ArenaHealth>();nextCast=Time.time+Random.Range(1,3);}
  void Update()
  {
   if(!game || game.Phase!=ArenaPhase.Fighting || game.MenuOpen || health.CurrentHealth<=0){motor.SetMoveDirection(Vector3.zero);nextCast=Time.time+Random.Range(.8f,2);return;}
   GameObject target=null;float nearest=float.PositiveInfinity;foreach(var p in game.Fighters){if(!p || p==gameObject || p.GetComponent<ArenaHealth>().CurrentHealth<=0)continue;float d=(p.transform.position-transform.position).sqrMagnitude;if(d<nearest){nearest=d;target=p;}}
   if(!target)return;var delta=target.transform.position-transform.position;delta.y=0;var center=new Vector3(transform.position.x,0,transform.position.z);
   Vector3 move=!game.Arena.Inside(center,2)?-center.normalized:delta.magnitude>7?delta.normalized:delta.magnitude<3?-delta.normalized:Vector3.Cross(Vector3.up,delta.normalized)*Mathf.Sin(Time.time*.8f+phase);motor.SetMoveDirection(move*.65f);
   if(Time.time<nextCast)return;nextCast=Time.time+Random.Range(1.8f,3.2f);
   int spell=health.CurrentHealth<55 && Random.value<.4f?3:Random.value<.15f?2:Random.Range(0,2);
   var point=spell==2?(!game.Arena.Inside(center,2)?Vector3.zero:transform.position-delta.normalized*3):target.transform.position;
   load.TryCastAt(spell,point);
  }
 }
}

