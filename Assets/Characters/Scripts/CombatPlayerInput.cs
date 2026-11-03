using UnityEngine;
using UnityEngine.InputSystem;
namespace GuerraDeEmpujes
{
 public sealed class CombatPlayerInput:MonoBehaviour
 {
  Vector3 aim;int selected;public bool controlsEnabled=true;
  void Update()
  {
   if(!controlsEnabled || Keyboard.current==null || Mouse.current==null)return;
   var k=Keyboard.current;var m=Mouse.current;
   GetComponent<ArenaCharacterMotor>().SetMoveDirection(new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)));
   if(k.digit1Key.wasPressedThisFrame)selected=0;if(k.digit2Key.wasPressedThisFrame)selected=1;if(k.digit3Key.wasPressedThisFrame)selected=2;if(k.digit4Key.wasPressedThisFrame)selected=3;
   if(Camera.main){var ray=Camera.main.ScreenPointToRay(m.position.ReadValue());var plane=new Plane(Vector3.up,transform.position);float t;if(plane.Raycast(ray,out t))aim=ray.GetPoint(t);}
   if(m.leftButton.wasPressedThisFrame && m.position.ReadValue().y<Screen.height-150)GetComponent<SpellLoadout>().TryCastAt(selected,aim);
   if(k.qKey.wasPressedThisFrame)GetComponent<SpellLoadout>().TryCastAt(2,aim);
   if(k.eKey.wasPressedThisFrame)GetComponent<SpellLoadout>().TryCastAt(3,aim);
  }
  void OnGUI()
  {
   if(!controlsEnabled)return;var loadout=GetComponent<SpellLoadout>();GUILayout.BeginArea(new Rect(12,12,620,145),GUI.skin.box);
   GUILayout.Label("PRUEBA DE COMBATE | WASD: mover | 1–4: seleccionar | clic: lanzar | Q: teleport | E: burbuja");
   GUILayout.Label("Vida: "+GetComponent<ArenaHealth>().CurrentHealth.ToString("0")+" | Puntos de mejora: "+loadout.upgradePoints);
   GUILayout.BeginHorizontal();for(int i=0;i<loadout.slots.Length;i++){var slot=loadout.slots[i];if(slot.spell && GUILayout.Button((selected==i?"> ":"")+slot.spell.displayName+" Nv."+slot.level+" ("+Mathf.Max(0,slot.readyAt-Time.time).ToString("0.0")+"s)"))selected=i;}GUILayout.EndHorizontal();
   if(GUILayout.Button("Mejorar poder seleccionado"))loadout.TryUpgrade(selected);GUILayout.EndArea();
  }
 }
}
