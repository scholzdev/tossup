using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Tossup.UI;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace Tossup.EditorTools
{
    // Project setup and builds. Also usable from the command line:
    //   Unity -batchmode -quit -projectPath . -executeMethod Tossup.EditorTools.TossupBuild.Setup
    //   Unity -batchmode -quit -projectPath . -executeMethod Tossup.EditorTools.TossupBuild.BuildWindows
    //   Unity -batchmode -quit -projectPath . -executeMethod Tossup.EditorTools.TossupBuild.BuildMacOS
    public static class TossupBuild
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/Resources/ui/icon.png";
        const string WindowsOutput = "Builds/Windows/Tossup.exe";
        const string MacOSOutput = "Builds/macOS/Tossup.app";
        const string LinuxOutput = "Builds/Linux/Tossup.x86_64";
        const string VersionPath = "Assets/Resources/version.json";

        [Serializable]
        sealed class VersionData { public string number, build; }

        static string OutputPath(string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-buildOutput") return args[i + 1];
            return fallback;
        }

        static void AdHocSignMacOSBuild(string output)
        {
#if UNITY_EDITOR_OSX
            // Unity 6 may copy vendor-signed Mono dylibs whose signatures no longer
            // validate after player assembly. Re-sign the complete local build so
            // macOS can launch it. Release builds should be Developer ID signed and
            // notarized by the distributor afterwards.
            var escapedOutput = Path.GetFullPath(output).Replace("\\", "\\\\").Replace("\"", "\\\"");
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/codesign",
                Arguments = "--force --deep --sign - \"" + escapedOutput + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            });
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("codesign failed: " + process.StandardError.ReadToEnd());
#endif
        }

        // Creates the main scene (a camera with TossupApp), registers it for builds and applies the player settings.
        [MenuItem("Tossup/Set Up Project")]
        public static void Setup()
        {
            SetupRenderPipeline();
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("Tossup");
                go.AddComponent<Camera>();
                go.AddComponent<TossupApp>();
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "Tossup";
            PlayerSettings.productName = "Tossup";
            PlayerSettings.defaultScreenWidth = TossupApp.DefaultWindowWidth;
            PlayerSettings.defaultScreenHeight = TossupApp.DefaultWindowHeight;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma; // blend like LÖVE does
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            AssetDatabase.SaveAssets();
            Debug.Log("Tossup: project set up (" + ScenePath + ")");
        }

        // Serialized assets make URP work immediately on a fresh checkout. Setup is also
        // idempotent, so CLI builds repair missing assets without duplicating features.
        static void SetupRenderPipeline()
        {
            const string folder = "Assets/Settings";
            const string rendererPath = folder + "/TossupRenderer.asset";
            const string pipelinePath = folder + "/TossupURP.asset";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Settings");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            if (!renderer.TryGetRendererFeature<TossupCanvasFeature>(out _))
            {
                var feature = ScriptableObject.CreateInstance<TossupCanvasFeature>();
                feature.name = "Tossup canvas";
                feature.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
            }
            // Keep URP's recovery map in step with the serialized feature subassets.
            var rendererSettings = new SerializedObject(renderer);
            var featureMap = rendererSettings.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string _, out long id))
                    featureMap.GetArrayElementAtIndex(i).longValue = id;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            // Pixel art is drawn at native resolution, with no lighting or post effects.
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            // URP exposes these Inspector settings as read-only runtime properties.
            var settings = new SerializedObject(pipeline);
            var renderers = settings.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            settings.FindProperty("m_DefaultRendererIndex").intValue = 0;
            settings.FindProperty("m_MainLightRenderingMode").intValue = (int)LightRenderingMode.Disabled;
            settings.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.Disabled;
            settings.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            settings.FindProperty("m_AnyShadowsSupported").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int quality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(quality, false);
        }

        [MenuItem("Tossup/Build Windows Player")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, WindowsOutput);
        }

        [MenuItem("Tossup/Build macOS Player")]
        public static void BuildMacOS()
        {
            Build(BuildTarget.StandaloneOSX, MacOSOutput);
        }

        [MenuItem("Tossup/Build Linux Player")]
        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64, LinuxOutput);
        }

        static void Build(BuildTarget target, string fallback)
        {
            Setup();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                throw new InvalidOperationException("Install Unity's " + target + " build support module for this Editor version.");
            string original=File.ReadAllText(VersionPath);
            var version=JsonUtility.FromJson<VersionData>(original);
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++)if(args[i]=="-buildId")version.build=args[i+1];
            PlayerSettings.bundleVersion=version.number;
            var output = OutputPath(fallback);
            bool success=false;
            try
            {
                File.WriteAllText(VersionPath,JsonUtility.ToJson(version));
                AssetDatabase.ImportAsset(VersionPath,ImportAssetOptions.ForceUpdate);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { ScenePath }, locationPathName = output, target = target, options = BuildOptions.None
                });
                success=report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
                if(success&&target==BuildTarget.StandaloneOSX)AdHocSignMacOSBuild(output);
                Debug.Log("Tossup: build " + report.summary.result + " -> " + Path.GetFullPath(output));
            }
            finally
            {
                File.WriteAllText(VersionPath,original);
                AssetDatabase.ImportAsset(VersionPath,ImportAssetOptions.ForceUpdate);
            }
            if(!success&&Application.isBatchMode)EditorApplication.Exit(1);
        }
    }
}
