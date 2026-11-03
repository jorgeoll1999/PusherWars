using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace GuerraDeEmpujes
{
 public sealed class MobileLightweight:MonoBehaviour
 {
  public bool lightweight=true;
  public RenderPipelineAsset pipeline;
  public bool allowQualitySwitch;
  public int qualityMode=1;
  public static MobileLightweight Instance{get;private set;}
  public static bool DynamicSpellLights=>!Instance || Instance.qualityMode==2;
  public static bool CanSwitch=>Instance && Instance.allowQualitySwitch;
  public static string ModeLabel=>Active?"LIGERO":Instance && Instance.qualityMode==2?"CALIDAD":"EQUILIBRADO";
  public static bool Active{get;private set;}
  public static float Fps{get;private set;}
  float elapsed;int frames;UniversalRenderPipelineAsset runtimePipeline;
  void Awake(){Instance=this;var source=pipeline?pipeline:QualitySettings.renderPipeline;if(source is UniversalRenderPipelineAsset){runtimePipeline=Instantiate((UniversalRenderPipelineAsset)source);QualitySettings.renderPipeline=runtimePipeline;}QualitySettings.vSyncCount=0;Screen.sleepTimeout=SleepTimeout.NeverSleep;QualitySettings.skinWeights=SkinWeights.FourBones;SetMode(lightweight?0:qualityMode);}
  public void CycleMode(){if(allowQualitySwitch)SetMode((qualityMode+1)%3);}
  public void SetMode(int mode)
  {
   qualityMode=Mathf.Clamp(mode,0,2);Active=qualityMode==0;Application.targetFrameRate=qualityMode==2?60:30;QualitySettings.shadows=Active?UnityEngine.ShadowQuality.Disable:UnityEngine.ShadowQuality.All;
   if(runtimePipeline){runtimePipeline.renderScale=Active?.9f:1;runtimePipeline.msaaSampleCount=qualityMode==2?4:2;runtimePipeline.supportsHDR=qualityMode==2;runtimePipeline.shadowDistance=Active?0:42;runtimePipeline.shadowCascadeCount=1;}
   if(Camera.main){Camera.main.allowHDR=qualityMode==2;var data=Camera.main.GetComponent<UniversalAdditionalCameraData>();if(data)data.renderPostProcessing=qualityMode==2;}
   foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None)){if(light.type==LightType.Directional)light.shadows=Active?LightShadows.None:qualityMode==2?LightShadows.Soft:LightShadows.Hard;else if(light.GetComponent<FireballCometVfx>())light.enabled=DynamicSpellLights;}
   var game=GetComponent<MobileRoundGame>();if(game)foreach(var p in game.Fighters){if(!p)continue;foreach(var r in p.GetComponentsInChildren<SkinnedMeshRenderer>()){r.shadowCastingMode=Active?ShadowCastingMode.Off:ShadowCastingMode.On;r.receiveShadows=!Active;}var shadow=p.transform.Find("Soft contact shadow");if(shadow)shadow.gameObject.SetActive(Active);}
   elapsed=0;frames=0;Fps=0;
  }
  void Update(){elapsed+=Time.unscaledDeltaTime;frames++;if(elapsed>=1){Fps=frames/elapsed;frames=0;elapsed=0;}}
  void OnDestroy(){if(Instance==this){Active=false;Instance=null;if(runtimePipeline){QualitySettings.renderPipeline=pipeline;Destroy(runtimePipeline);}}}
  public static void Dress(GameObject hero,bool local)
  {
   // Enlarge only the model and rig; movement and collision dimensions stay intact.
   foreach(Transform child in hero.transform)if(child.name.EndsWith("_Mesh") || child.name.EndsWith("_Rig"))child.localScale*=1.18f;
   foreach(var r in hero.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=Active?ShadowCastingMode.Off:ShadowCastingMode.On;r.receiveShadows=!Active;}
   var shadow=new GameObject("Soft contact shadow",typeof(MeshFilter),typeof(MeshRenderer));shadow.transform.SetParent(hero.transform,false);shadow.transform.localPosition=Vector3.up*.025f;shadow.transform.localRotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(1.15f,.85f,1);shadow.GetComponent<MeshFilter>().sharedMesh=Resources.Load<Mesh>("MobileArt/ContactQuad");var renderer=shadow.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("MobileArt/ContactShadow");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;shadow.SetActive(Active);
   if(local){var ring=SpellVfx.Ring(hero.transform,new Color(.25f,.9f,1,.8f),.49f,.045f,Quaternion.identity);ring.name="Your character marker";ring.transform.localPosition=Vector3.up*.045f;}
  }
 }
}


