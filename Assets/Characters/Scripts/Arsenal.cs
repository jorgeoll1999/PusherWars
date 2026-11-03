using UnityEngine;
namespace GuerraDeEmpujes
{
 public static class Arsenal
 {
  public const int Powers=11,Items=5,MaxEquipped=5;
  public static readonly string[] Names={"Fuego","Rayo","Teleport","Campo de fuerza","Ariete","Ricochet","Égida","Metralla","Luz curva","Monolito","Búmeran","Botas","Capa firme","Orbe expansivo","Anillo vital","Talismán"};
  public static readonly string[] Help={
   "Una bola de fuego que daña y empuja al primer objetivo.","Un rayo instantáneo en la dirección elegida.","Viaja al destino o intercambia lugar con el mago que esté allí.",
   "Una burbuja protectora que estalla y empuja a los rivales cercanos.","Te conviertes en un proyectil. Rebotas y empujas sin hacer daño.","Un cristal que rebota en rivales, roca y borde de la arena.",
   "Durante exactamente 2 s ignoras el daño y los empujes.","Al primer tercio del alcance se abre en tres balas con la mitad de empuje.","Un orbe de luz que curva antes de llegar a la dirección elegida y ralentiza al impactar.",
   "Levanta una roca u obelisco temporal a distancia fija frente al mago.","Empuja a la ida. Al regresar te sigue y arrastra al rival hacia ti.",
   "+12 % de velocidad por nivel.","Resiste un 8 % del empuje por nivel.","Amplía el radio de impacto un 3,5 % por nivel.","Recupera 1 punto de vida por segundo y nivel durante el combate.","Acorta ralentizaciones y la duración del impulso: 30 % en nivel 1, hasta 70 %."};
  static SpellDefinition[] definitions=new SpellDefinition[Powers];
  public static bool Self(int id)=>id==3||id==6;
  public static bool Ground(int id)=>id==2;
  public static SpellKind Animation(SpellKind kind)=>(int)kind<4?kind:SpellKind.Fireball;
  public static SpellDefinition Extra(int id,SpellProjectile prefab){
   if(definitions[id])return definitions[id];
   var d=ScriptableObject.CreateInstance<SpellDefinition>();d.kind=(SpellKind)id;d.displayName=Names[id];d.projectilePrefab=prefab;d.levels=new SpellLevel[5];
   for(int i=0;i<5;i++)d.levels[i]=new SpellLevel{damage=id==4||id==6||id==9?0:10+2*i,knockback=4+i*.7f,cooldown=(id==6?13:id==9?11:7)-.55f*i,
    range=(id==4?7:id==9?6:id==8?8:id==10?9:12)+i,speed=id==8?8.5f+i*.4f:(id==4?12:10)+i,hitRadius=.22f+i*.015f,radius=.85f+i*.05f,duration=id==6?2:id==9?4+i*.5f:1.5f,upgradeCost=100+i*50};
   definitions[id]=d;return d;
  }
 }
}