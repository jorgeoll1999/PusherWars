using UnityEngine;
using UnityEditor;
using GuerraDeEmpujes;
using System.IO;
public static class SpellVfxBuilder
{
 [MenuItem("Guerra de empujes/Build Spell Visual Effects")]
 public static void Build()
 {
  string dir="Assets/Characters/Resources/SpellVfx";Directory.CreateDirectory(dir);AssetDatabase.Refresh();
  string[] names={"Glow","Fire","Arcane","Bubble"};Color[] colors={Color.white,new Color(3,.7f,.045f),new Color(1.3f,.3f,3),new Color(.15f,1.3f,2.5f)};
  for(int i=0;i<4;i++){string path=dir+"/"+names[i]+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find(i==0?"GuerraDeEmpujes/SpellGlow":"GuerraDeEmpujes/SpellEnergy");if(!shader)throw new System.Exception("Spell VFX shader missing");if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}else mat.shader=shader;mat.SetColor("_Color",colors[i]);if(i>0){mat.SetFloat("_Style",i-1);mat.SetFloat("_Opacity",i==3?.65f:.85f);}EditorUtility.SetDirty(mat);}
  AssetDatabase.SaveAssets();Debug.Log("Four spell VFX materials built and referenced through Resources.");
 }
 [MenuItem("Guerra de empujes/Preview All Spell Effects")]
 public static void Preview()
 {
  if(!Application.isPlaying){Debug.LogWarning("Press Play before previewing spell effects.");return;}
  var all=Object.FindObjectsByType<SpellLoadout>(FindObjectsSortMode.None);if(all.Length==0)return;
  var owner=all[0];var p=owner.transform.position;
  owner.Release(owner.slots[0].spell,owner.slots[0].spell.levels[0],p+Vector3.right*8);
  owner.Release(owner.slots[2].spell,owner.slots[2].spell.levels[0],p+new Vector3(-6,0,4));
  SpellVfx.Lightning(p+Vector3.up,p+new Vector3(5,1,5));SpellVfx.BubblePop(p+new Vector3(-3,.3f,-2),2.5f);
 }
}
