using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using GuerraDeEmpujes;
public static class MobilePresentationBuilder
{
 public static void Prepare()
 {
  Directory.CreateDirectory("Assets/Characters/Resources/MobileArt");
  var texture=new Texture2D(512,512,TextureFormat.RGB24,false);var colors=new Color[512*512];
  for(int y=0;y<512;y++)for(int x=0;x<512;x++){
   float u=x/512f,v=y/512f;
   float n=TileNoise(u,v,7)*.65f+TileNoise(u,v,18)*.25f+TileNoise(u,v,43)*.10f;
   float fissure=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.006f,.027f,Mathf.Abs(n-.50f)));
   var crust=Color.Lerp(new Color(.075f,.028f,.04f),new Color(.28f,.065f,.045f),Mathf.Clamp01(n*1.3f));
   var molten=Color.Lerp(new Color(.65f,.12f,.025f),new Color(1,.48f,.08f),fissure);
   colors[y*512+x]=Color.Lerp(crust,molten,fissure*fissure);
  }
  texture.SetPixels(colors);texture.Apply();File.WriteAllBytes("Assets/Characters/Resources/MobileArt/LavaCrust.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.Refresh();
  var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Characters/Resources/MobileArt/LavaCrust.png");importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
  var lava=Material("MobileLava","Guerra/Mobile Lava");lava.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Characters/Resources/MobileArt/LavaCrust.png");EditorUtility.SetDirty(lava);
  Material("ContactShadow","Guerra/Mobile Contact");
  string meshPath="Assets/Characters/Resources/MobileArt/ContactQuad.asset";
  if(!AssetDatabase.LoadAssetAtPath<Mesh>(meshPath)){var mesh=new Mesh{name="Contact quad"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
  foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
   r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
   if(r.name=="Lava_Painted")r.sharedMaterial=lava;
   if(r.name=="Lava_Emissive"){r.sharedMaterial=lava;r.enabled=true;}
  }
  foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){light.shadows=LightShadows.None;if(light.type==LightType.Directional){light.intensity=1.35f;light.color=new Color(1,.94f,.87f);}}
  var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");rp.renderScale=.9f;rp.msaaSampleCount=2;rp.supportsHDR=false;rp.shadowDistance=0;
  var serialized=new SerializedObject(rp);serialized.FindProperty("m_MainLightShadowsSupported").boolValue=false;serialized.FindProperty("m_AdditionalLightsRenderingMode").intValue=0;serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;serialized.FindProperty("m_RequireDepthTexture").boolValue=false;serialized.FindProperty("m_RequireOpaqueTexture").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(rp);
  QualitySettings.SetQualityLevel(0,true);QualitySettings.shadows=UnityEngine.ShadowQuality.Disable;
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.65f,.71f,.84f);RenderSettings.ambientEquatorColor=new Color(.48f,.51f,.60f);RenderSettings.ambientGroundColor=new Color(.35f,.31f,.30f);
  var manager=UnityEngine.Object.FindFirstObjectByType<MobileRoundGame>();if(manager && !manager.GetComponent<MobileLightweight>())manager.gameObject.AddComponent<MobileLightweight>();
  if(Camera.main){var cameraData=Camera.main.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=false;Camera.main.allowHDR=false;}
  PlayerSettings.bundleVersion="0.2.2";PlayerSettings.Android.bundleVersionCode=3;
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
 }
 static float TileNoise(float u,float v,float f){float a=Mathf.PerlinNoise(u*f+13.37f,v*f+29.51f),b=Mathf.PerlinNoise((u-1)*f+13.37f,v*f+29.51f),c=Mathf.PerlinNoise(u*f+13.37f,(v-1)*f+29.51f),d=Mathf.PerlinNoise((u-1)*f+13.37f,(v-1)*f+29.51f);return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);}
 static Material Material(string name,string shader){string path="Assets/Characters/Resources/MobileArt/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}return m;}
}




