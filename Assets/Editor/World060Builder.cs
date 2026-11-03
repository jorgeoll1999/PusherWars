using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using GuerraDeEmpujes;
public static class World060Builder
{
 public static void Prepare()
 {
  Directory.CreateDirectory("Assets/Characters/Resources/Battlefield");
  MaterialAsset("Assets/Characters/Resources/Battlefield/Stone.mat","Universal Render Pipeline/Lit",new Color(.42f,.39f,.35f),false);
  MaterialAsset("Assets/Characters/Resources/Battlefield/ArrowEdge.mat","Universal Render Pipeline/Unlit",new Color(.035f,.028f,.018f),true);
  MaterialAsset("Assets/Characters/Resources/Interface/AimArrowYellow.mat","Universal Render Pipeline/Unlit",new Color(1,.76f,.18f),true);
  var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);SaveMesh("Block",Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh));Object.DestroyImmediate(cube);
  float g=(1+Mathf.Sqrt(5))/2;var verts=new[]{new Vector3(-1,g,0),new Vector3(1,g,0),new Vector3(-1,-g,0),new Vector3(1,-g,0),new Vector3(0,-1,g),new Vector3(0,1,g),new Vector3(0,-1,-g),new Vector3(0,1,-g),new Vector3(g,0,-1),new Vector3(g,0,1),new Vector3(-g,0,-1),new Vector3(-g,0,1)};var tris=new[]{0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};var mesh=new Mesh();mesh.vertices=tris.Select(i=>verts[i].normalized*.56f).ToArray();mesh.triangles=Enumerable.Range(0,tris.Length).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh("Rock",mesh);
  Directory.CreateDirectory("Assets/Characters/ClothRepair");var report="";
  foreach(var path in new[]{"Assets/Characters/MagaArcana/MagaArcana.prefab","Assets/Characters/MagaFuego/MagaFuego.prefab","Assets/Characters/MagoHielo/MagoHielo.prefab","Assets/Characters/MagoPlanta/MagoPlanta.prefab"}){
   var root=PrefabUtility.LoadPrefabContents(path);try{var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var original=skin.sharedMesh;var repaired=Object.Instantiate(original);var vertices=original.vertices;var weights=original.boneWeights;int hips=System.Array.FindIndex(skin.bones,b=>b.name=="Hips"),chest=System.Array.FindIndex(skin.bones,b=>b.name=="Chest"),count=0;
    for(int i=0;i<vertices.Length;i++){var w=weights[i];string bone=skin.bones[w.boneIndex0].name;var v=skin.transform.TransformPoint(vertices[i]);bool cloth=bone.Contains("Skirt")||bone.Contains("Cape");if(v.y<1.12f&&v.y>.22f&&(cloth||(v.z<-.17f&&Mathf.Abs(v.x)>.12f))){float upper=Mathf.SmoothStep(0,.3f,Mathf.InverseLerp(.85f,1.12f,v.y));weights[i]=new BoneWeight{boneIndex0=hips,weight0=1-upper,boneIndex1=chest,weight1=upper};count++;}}
    repaired.boneWeights=weights;repaired.RecalculateBounds();string target="Assets/Characters/ClothRepair/"+root.name+"_StableCloth.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(target);if(existing){EditorUtility.CopySerialized(repaired,existing);Object.DestroyImmediate(repaired);repaired=existing;}else AssetDatabase.CreateAsset(repaired,target);skin.sharedMesh=repaired;PrefabUtility.SaveAsPrefabAsset(root,path);report+=root.name+": "+count+" cloth vertices stabilized; source FBX preserved\n";
   }finally{PrefabUtility.UnloadPrefabContents(root);}}
  File.WriteAllText("Logs/Cloth060Repair.txt",report);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static void SaveMesh(string name,Mesh mesh){string path="Assets/Characters/Resources/Battlefield/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);}
 static void MaterialAsset(string path,string shader,Color color,bool arrow){var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.2f);if(arrow){m.SetFloat("_Surface",0);m.SetFloat("_Cull",0);m.SetFloat("_ZWrite",0);m.renderQueue=3000;}EditorUtility.SetDirty(m);}
}
