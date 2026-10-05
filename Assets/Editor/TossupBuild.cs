using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma; // blend like LÖVE does
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            AssetDatabase.SaveAssets();
            Debug.Log("Tossup: project set up (" + ScenePath + ")");
        }

        [MenuItem("Tossup/Build Windows Player")]
        public static void BuildWindows()
        {
            Setup();
            var output = OutputPath(WindowsOutput);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Tossup: build " + report.summary.result + " -> " + Path.GetFullPath(output));
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        [MenuItem("Tossup/Build macOS Player")]
        public static void BuildMacOS()
        {
            Setup();
            var output = OutputPath(MacOSOutput);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                AdHocSignMacOSBuild(output);
            Debug.Log("Tossup: build " + report.summary.result + " -> " + Path.GetFullPath(output));
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
