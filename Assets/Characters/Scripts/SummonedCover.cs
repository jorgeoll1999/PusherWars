using UnityEngine;
namespace GuerraDeEmpujes
{
 public static class SummonedCover
 {
  public static void Place(SpellLoadout caster,Vector3 target,SpellLevel level){
   var session=OnlineSession.Instance;if(!session||!session.game.World)return;
   var game=session.game;var hero=caster.GetComponent<HeroBattleStats>();
   var feet=caster.transform.position;var dir=Vector3.ProjectOnPlane(target-feet,Vector3.up).normalized;if(dir.sqrMagnitude<.1f)dir=caster.transform.forward;
   for(int i=0;i<1;i++){
    var p=feet+dir*2.8f;p.y=0;
    if(!game.Arena.Inside(p,1.4f)||Physics.CheckBox(p+Vector3.up*1.1f,new Vector3(1,1,1),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
    if(hero.summoned)Object.Destroy(hero.summoned);
    var type=Random.Range(0,2);hero.summoned=Create(game,p,type);hero.summonType=type;Object.Destroy(hero.summoned,level.duration);
    SpellAudio.Play("MonolithRise",p,.6f);SpellVfx.Burst(p+Vector3.up*.3f,new Color(.8f,.56f,.28f),.8f);return;
   }
  }
  public static GameObject Create(MobileRoundGame game,Vector3 p,int type){
   var go=Object.Instantiate(Resources.Load<GameObject>("Battlefield/"+(type==0?"WeatheredRock":"SealObelisk")),p,Quaternion.identity,game.World.transform);
   var c=go.AddComponent<ArenaCover>();c.capacity=c.hits=3;var box=go.AddComponent<BoxCollider>();box.size=new Vector3(1.62f,type==0?1.6f:2.3f,1.52f);box.center=Vector3.up*box.size.y*.5f;c.solid=box;c.visuals=go.GetComponentsInChildren<Renderer>();c.Refresh();return go;
  }
 }
}