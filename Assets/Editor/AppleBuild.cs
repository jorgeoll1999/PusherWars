using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AppleBuild
{
    const string Scene = "Assets/Characters/Scenes/Citadel_Playtest.unity";

    static void Configure()
    {
        QualitySettings.SetQualityLevel(0, true);
        QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
            "Assets/Settings/Mobile_Quality_RPAsset.asset");
        UnityEditor.Rendering.CoreBuildData.instance.Dispose();
        PlayerSettings.companyName = "Guerra de empujes";
        PlayerSettings.productName = "Guerra de empujes";
        PlayerSettings.bundleVersion = "0.8.4";
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
    }

    public static void ExportIos()
    {
        Configure();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.guerradeempujes.playtest");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.targetOSVersionString = "16.0";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.buildNumber = "18";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        string team = Environment.GetEnvironmentVariable("GUERRA_APPLE_TEAM_ID");
        if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        AssetDatabase.SaveAssets();
        Build(BuildTarget.iOS, "Builds/iOS/Xcode", "Builds/iOS/ExportResult.txt");
    }

    public static void ExportMac()
    {
        Configure();
#if UNITY_STANDALONE_OSX
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.ARM64;
        UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = false;
#endif
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.guerradeempujes.playtest");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        AssetDatabase.SaveAssets();
        Build(BuildTarget.StandaloneOSX, "Builds/macOS/Guerra de empujes.app", "Builds/macOS/ExportResult.txt");
    }

    static void Build(BuildTarget target, string path, string resultPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
        File.WriteAllText(resultPath, "BUILDING");
        var report = BuildPipeline.BuildPlayer(new[] { Scene }, path, target, BuildOptions.None);
        File.WriteAllText(resultPath, report.summary.result + "; errors=" + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Apple build failed. See " + resultPath);
    }
}
