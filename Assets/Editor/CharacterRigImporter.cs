using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class CharacterRigImporter
{
    [MenuItem("Guerra de empujes/Prepare Character Rigs")]
    public static void Build()
    {
        var report = new List<string>();
        foreach (var name in new[]{"MagaArcana","MagaFuego","MagoHielo","MagoPlanta"})
        {
            string dir="Assets/Characters/"+name, path=dir+"/"+name+".fbx";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            var transforms=model.GetComponentsInChildren<Transform>(true);
            var human=new List<HumanBone>();
            for(int i=0;i<(int)HumanBodyBones.LastBone;i++)
            {
                string boneName=((HumanBodyBones)i).ToString();
                if(transforms.Any(t=>t.name==boneName))
                    human.Add(new HumanBone {boneName=boneName,humanName=HumanTrait.BoneName[i],limit=new HumanLimit{useDefaultValues=true}});
            }
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.humanDescription=new HumanDescription {
                human=human.ToArray(),
                skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
                armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,feetSpacing=0,hasTranslationDoF=false
            };
            importer.importAnimation=false;
            importer.isReadable=true;
            importer.SaveAndReimport();
            HumanoidLegCalibration.Calibrate(path);
            model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var materialPath=dir+"/"+name+"_URP.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
            string textureDir=dir+"/"+name+".fbm";
            foreach(var file in Directory.GetFiles(textureDir))
            {
                string texturePath=file.Replace('\\','/');
                if(texturePath.EndsWith(".meta"))continue;
                var ti=AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if(ti==null)continue;
                ti.maxTextureSize=2048;
                if(texturePath.ToLowerInvariant().Contains("normal"))ti.textureType=TextureImporterType.NormalMap;
                else ti.sRGBTexture=texturePath.ToLowerInvariant().Contains("basecolor");
                ti.SaveAndReimport();
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if(texturePath.ToLowerInvariant().Contains("basecolor"))material.SetTexture("_BaseMap",tex);
                if(texturePath.ToLowerInvariant().Contains("normal")){material.SetTexture("_BumpMap",tex);material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.65f);}
            }
            material.SetFloat("_Smoothness",.3f);material.SetFloat("_Metallic",.15f);material.SetFloat("_Cull",0);
            EditorUtility.SetDirty(material);
            model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name=name;
                var animator=instance.GetComponent<Animator>();
                if(!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
                    throw new Exception(name+": invalid humanoid avatar");
                foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    skin.sharedMaterials=skin.sharedMaterials.Select(m=>material).ToArray();
                    skin.quality=SkinQuality.Bone4;
                    if(skin.bones.Any(b=>!b))throw new Exception(name+": missing skin bones");
                    var weights=skin.sharedMesh.boneWeights;
                    if(weights.Any(w=>w.weight0+w.weight1+w.weight2+w.weight3<.99f))throw new Exception(name+": unweighted vertex");
                    report.Add(name+": valid Humanoid; "+skin.bones.Length+" skin bones; "+skin.sharedMesh.vertexCount+" imported vertices; all weights normalized.");
                }
                PrefabUtility.SaveAsPrefabAsset(instance,dir+"/"+name+".prefab");
            }
            finally {UnityEngine.Object.DestroyImmediate(instance);}
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("ArtSource/Characters/UnityRigValidation.txt",report);
        Debug.Log(string.Join("\n",report));
    }
}
