using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GuerraDeEmpujes;
using System.IO;
public static class FourSpellsBuilder
{
 [MenuItem("Guerra de empujes/Build Four Selected Spells")]
 public static void Build()
 {
  Directory.CreateDirectory("Assets/Characters/Combat/Spells");
  string[] names={"Fireball","Lightning","TeleportSwap","Bubble"};var definitions=new SpellDefinition[4];
  var baseProjectile=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Combat/Projectile_Base.prefab");
  for(int i=0;i<4;i++)
  {
   string path="Assets/Characters/Combat/Spells/"+names[i]+".asset";var spell=AssetDatabase.LoadAssetAtPath<SpellDefinition>(path);if(!spell){spell=ScriptableObject.CreateInstance<SpellDefinition>();AssetDatabase.CreateAsset(spell,path);}spell.kind=(SpellKind)i;spell.displayName=new[]{"Bola de fuego","Rayo","Intercambio","Burbuja"}[i];
   if(i==0 || i==2)
   {
    var shot=(GameObject)PrefabUtility.InstantiatePrefab(baseProjectile);
    var matPath="Assets/Characters/Combat/Spells/"+names[i]+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}var color=i==0?new Color(1,.22f,.025f):Color.cyan;mat.color=color;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*2);shot.GetComponentInChildren<Renderer>().sharedMaterial=mat;
    shot.name=names[i]+"_Projectile";spell.projectilePrefab=PrefabUtility.SaveAsPrefabAsset(shot,"Assets/Characters/Combat/Spells/"+shot.name+".prefab").GetComponent<SpellProjectile>();Object.DestroyImmediate(shot);
   }
   spell.levels=new SpellLevel[5];for(int l=0;l<5;l++)spell.levels[l]=new SpellLevel{damage=i==0?14+l*3:i==1?10+l*2:0,knockback=i==0?5+l:i==1?4+l*.8f:i==3?8+l*1.5f:0,cooldown=i==0?2.2f-l*.15f:i==1?4-l*.25f:i==2?8-l*.5f:10-l*.5f,speed=i==2?14+l:12+l,range=i==2?9+l:14+l,radius=2.5f+l*.2f,duration=1.2f+l*.1f,upgradeCost=l==0?0:l+1};EditorUtility.SetDirty(spell);definitions[i]=spell;
  }
  foreach(var n in new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"})
  {
   var path="Assets/Characters/"+n+"/"+n+".prefab";var go=PrefabUtility.LoadPrefabContents(path);try{var loadout=go.GetComponent<SpellLoadout>();for(int i=0;i<4;i++)loadout.slots[i]=new SpellSlot{spell=definitions[i],level=1};PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}
  }
  AssetDatabase.SaveAssets();Debug.Log("Four selected spells configured at five levels on all characters.");
 }
 [MenuItem("Guerra de empujes/Create Combat Playground")]
 public static void Playground()
 {
  Directory.CreateDirectory("Assets/Characters/Scenes");var scene=EditorSceneManager.GetActiveScene();
  if(scene.path!="Assets/Arena/Scenes/Caldera_Arena.unity")scene=EditorSceneManager.OpenScene("Assets/Arena/Scenes/Caldera_Arena.unity");
  EditorSceneManager.SaveScene(scene,"Assets/Characters/Scenes/Combat_Playground.unity",true);scene=EditorSceneManager.OpenScene("Assets/Characters/Scenes/Combat_Playground.unity");
  string[] names={"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"};Vector3[] positions={new Vector3(0,.05f,-3),new Vector3(0,.05f,2),new Vector3(2,.05f,0),new Vector3(-3,.05f,0)};
  for(int i=0;i<4;i++){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/"+names[i]+"/"+names[i]+".prefab"));go.transform.position=positions[i];go.GetComponent<SpellLoadout>().upgradePoints=12;if(i==0){go.AddComponent<CombatPlayerInput>();Selection.activeGameObject=go;}}
  if(Camera.main){Camera.main.transform.position=new Vector3(0,18,-14);Camera.main.transform.LookAt(Vector3.zero);Camera.main.orthographicSize=9;}
  EditorSceneManager.SaveScene(scene);Debug.Log("Combat playground saved. Press Play; use WASD, 1–4 and click, Q and E.");
 }
}
