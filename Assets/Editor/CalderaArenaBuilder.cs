using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CalderaArenaBuilder
{
    const string Root="Assets/Arena/";
    [MenuItem("Guerra de empujes/Validate and Capture Caldera")]
    public static void ValidateAndCapture()
    {
        Physics.SyncTransforms();
        var arena=GameObject.Find("CALDERA • Four Seals Arena");
        var markers=arena.transform.Find("Spawn Points");
        if(markers.childCount!=4)throw new Exception("Expected four spawn points");
        for(int i=0;i<4;i++)
        {
            var p=markers.GetChild(i).position;
            if(Mathf.Abs(new Vector2(p.x,p.z).magnitude-8.15f)>.02f)throw new Exception("Spawn radius mismatch");
            if(!Physics.Raycast(p+Vector3.up*4,Vector3.down,out var hit,10,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y)>.01f)throw new Exception("Missing spawn floor");
        }
        if(!Physics.Raycast(new Vector3(13,4,0),Vector3.down,out var lavaHit,10,~0,QueryTriggerInteraction.Ignore) || !lavaHit.collider.GetComponent<GuerraDeEmpujes.LavaSurface>() || Mathf.Abs(lavaHit.point.y+.08f)>.01f)throw new Exception("Missing solid lava floor");
        var cam=GameObject.Find("Main Camera").GetComponent<Camera>();
        Capture(cam,"Previews/Caldera_Unity_Isometric.png");
        Capture(GameObject.Find("Tactical Camera").GetComponent<Camera>(),"Previews/Caldera_Unity_Top.png");
        Debug.Log("CALDERA VALIDATED: four equidistant spawns, level floor, solid damaging lava, captures saved.");
    }
    [MenuItem("Guerra de empujes/Build Caldera Arena")]
    public static void Build()
    {
        foreach(var dir in new[]{"Materials","Meshes","Prefabs","Scenes"}) Directory.CreateDirectory(Root+dir);
        AssetDatabase.Refresh();
        var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"Textures/Caldera_PaintedAtlas.png");
        ti.maxTextureSize=512;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.Compressed;ti.SaveAndReimport();
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/Caldera_PaintedAtlas.png");
        var painted=Material("Arena_Painted",atlas,false);var ember=Material("Arena_Ember",atlas,true);
        var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"Models/Caldera_Arena.fbx");
        importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
        importer.isReadable=true;importer.generateSecondaryUV=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        AssetDatabase.ImportAsset(Root+"Models/Caldera_Arena.fbx",ImportAssetOptions.ForceUpdate);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/Caldera_Arena.fbx");
        if(model==null)throw new Exception("FBX missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var source=(GameObject)PrefabUtility.InstantiatePrefab(model);source.name="Source";
        var arena=new GameObject("CALDERA • Four Seals Arena");
        var buckets=new Dictionary<string,List<CombineInstance>>();
        MeshFilter floor=null;MeshFilter lavaFloor=null;var spawnPos=new List<Vector3>();
        foreach(var mf in source.GetComponentsInChildren<MeshFilter>())
        {
            var mr=mf.GetComponent<MeshRenderer>();if(!mr)continue;
            string n=mf.name;
            if(n=="Walkable_Platform")floor=mf; if(n=="Lava_Abyss")lavaFloor=mf;
            if(n.StartsWith("SpawnPad_")&&n.EndsWith("_Face"))spawnPos.Add(mr.bounds.center);
            string group=n.StartsWith("Lava_")||n.StartsWith("Cooled_")?"Lava":n.StartsWith("Floating_")?"Background":n.StartsWith("Ruins_")||n.StartsWith("Brazier_")||n.StartsWith("Rubble_")?"Ruins":n.StartsWith("Plant_")?"Vegetation":n.StartsWith("SpawnPad_")||n.StartsWith("Pad_")?"SpawnPads":n.StartsWith("Basalt")||n.StartsWith("Hanging")||n.StartsWith("Walkable")?"Platform":"Surface";
            for(int s=0;s<mf.sharedMesh.subMeshCount;s++)
            {
                bool glow=mr.sharedMaterials[Mathf.Min(s,mr.sharedMaterials.Length-1)].name.Contains("Ember");
                string key=group+(glow?"_Emissive":"_Painted");
                if(!buckets.ContainsKey(key))buckets[key]=new List<CombineInstance>();
                buckets[key].Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=s,transform=mf.transform.localToWorldMatrix});
            }
        }
        int tris=0;
        foreach(var kv in buckets)
        {
            var mesh=new Mesh{name=kv.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(kv.Value.ToArray(),true,true);mesh.RecalculateBounds();tris+=mesh.triangles.Length/3;
            string path=Root+"Meshes/"+kv.Key+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){old.Clear();old.indexFormat=mesh.indexFormat;old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;old.uv2=mesh.uv2;old.triangles=mesh.triangles;old.RecalculateBounds();old.UploadMeshData(false);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(kv.Key,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(arena.transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=kv.Key.EndsWith("Emissive")?ember:painted;
            go.isStatic=true;if(kv.Key.StartsWith("Lava")||kv.Key.StartsWith("Background"))go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        var collider=new GameObject("Walkable surface • 24 m");collider.transform.SetParent(arena.transform,false);
        var cm=new Mesh{name="WalkableCollider"};cm.CombineMeshes(new[]{new CombineInstance{mesh=floor.sharedMesh,transform=floor.transform.localToWorldMatrix}},true,true);
        string cp=Root+"Meshes/WalkableCollider.asset";var oldCollider=AssetDatabase.LoadAssetAtPath<Mesh>(cp);if(oldCollider){EditorUtility.CopySerialized(cm,oldCollider);UnityEngine.Object.DestroyImmediate(cm);cm=oldCollider;}else AssetDatabase.CreateAsset(cm,cp);
        collider.AddComponent<MeshCollider>().sharedMesh=cm;
        var markers=new GameObject("Spawn Points");markers.transform.SetParent(arena.transform,false);
        for(int i=0;i<spawnPos.Count;i++){var go=new GameObject("Spawn_"+(i+1));go.transform.SetParent(markers.transform,false);go.transform.position=new Vector3(spawnPos[i].x,.15f,spawnPos[i].z);go.transform.LookAt(new Vector3(0,.15f,0));}
        var lavaGround=new GameObject("Solid Lava Floor • 20 HP per second");lavaGround.transform.SetParent(arena.transform,false);
        var lm=new Mesh{name="LavaCollider"};lm.CombineMeshes(new[]{new CombineInstance{mesh=lavaFloor.sharedMesh,transform=lavaFloor.transform.localToWorldMatrix}},true,true);
        string lp=Root+"Meshes/LavaCollider.asset";var previousLava=AssetDatabase.LoadAssetAtPath<Mesh>(lp);if(previousLava){EditorUtility.CopySerialized(lm,previousLava);UnityEngine.Object.DestroyImmediate(lm);lm=previousLava;}else AssetDatabase.CreateAsset(lm,lp);
        lavaGround.AddComponent<MeshCollider>().sharedMesh=lm;lavaGround.AddComponent<GuerraDeEmpujes.LavaSurface>();
        UnityEngine.Object.DestroyImmediate(source);
        PrefabUtility.SaveAsPrefabAsset(arena,Root+"Prefabs/Caldera_Arena.prefab");
        var sun=new GameObject("Daylight",typeof(Light));sun.transform.rotation=Quaternion.Euler(48,-35,0);var l=sun.GetComponent<Light>();l.type=LightType.Directional;l.color=new Color(1,.9f,.78f);l.intensity=1.8f;l.shadows=LightShadows.Soft;l.shadowBias=.03f;l.shadowNormalBias=.15f;
        RenderSettings.sun=l;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.60f,.68f,.84f);RenderSettings.ambientEquatorColor=new Color(.40f,.43f,.56f);RenderSettings.ambientGroundColor=new Color(.38f,.23f,.19f);RenderSettings.fog=false;
        var camera=Camera("Main Camera",new Vector3(27,30,34),new Vector3(0,-1.8f,0),14.55f);camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();
        var top=Camera("Tactical Camera",new Vector3(0,35,0),Vector3.zero,14);top.enabled=false;
        var volume=new GameObject("Soft color and lava glow",typeof(Volume));var vol=volume.GetComponent<Volume>();vol.isGlobal=true;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.18f);bloom.threshold.Override(1.2f);var color=profile.Add<ColorAdjustments>(true);color.saturation.Override(8);color.postExposure.Override(.25f);
        string pp=Root+"Materials/CalderaVolume.asset";var op=AssetDatabase.LoadAssetAtPath<VolumeProfile>(pp);if(op){vol.sharedProfile=op;}else{AssetDatabase.CreateAsset(profile,pp);foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);vol.sharedProfile=profile;}
        EditorSceneManager.SaveScene(scene,Root+"Scenes/Caldera_Arena.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/Caldera_Arena.unity",true)};
        AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,-1,0),camera.transform.rotation,26,true);
        Selection.activeGameObject=arena;
        File.WriteAllText("ArtSource/UnityImportReport.json",JsonUtility.ToJson(new Report{triangles=tris,renderers=buckets.Count,spawns=spawnPos.Count,floorBounds=cm.bounds.ToString(),scene=scene.path},true));
        Capture(camera,"Previews/Caldera_Unity_Isometric.png");Capture(top,"Previews/Caldera_Unity_Top.png");
        Debug.Log("CALDERA READY: "+tris+" triangles, "+buckets.Count+" renderers, "+spawnPos.Count+" spawn points.");
    }
    [Serializable]class Report{public int triangles,renderers,spawns;public string floorBounds,scene;}
    static Material Material(string name,Texture2D atlas,bool glow){string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.name=name;m.SetTexture("_BaseMap",atlas);m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.12f);m.SetFloat("_Metallic",0);if(glow){m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",atlas);m.SetColor("_EmissionColor",Color.white*.75f);}return m;}
    static Camera Camera(string name,Vector3 pos,Vector3 target,float size){var go=new GameObject(name,typeof(Camera));go.transform.position=pos;go.transform.LookAt(target);var c=go.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=size;c.nearClipPlane=.1f;c.farClipPlane=200;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.20f,.16f,.25f);c.allowHDR=true;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;return c;}
    public static void Capture(Camera c,string path){var rt=new RenderTexture(1400,1100,24,RenderTextureFormat.ARGB32);var prev=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1400,1100,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1400,1100),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());c.targetTexture=prev;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(t);}
}



