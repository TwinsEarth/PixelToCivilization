using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PixelToCivilization.EditorTools
{
    public static class WebGLBuilder
    {
        public const string BuildVer = "9.5.4";
        public const string Product = "PixelToCivilization";

        public static void BuildCLI()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(root, "BuildWebGL");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            PlayerSettings.productName = "从像素到文明";
            PlayerSettings.companyName = "TwinsEarth";
            PlayerSettings.bundleVersion = BuildVer;

            var opts = new BuildPlayerOptions();
            opts.target = BuildTarget.WebGL;
            opts.targetGroup = BuildTargetGroup.WebGL;
            opts.scenes = new[] { "Assets/Scenes/Main.unity" };
            opts.locationPathName = Path.Combine(outDir, "BuildWebGL");
            opts.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            string log = string.Format("V{0} build: result={1} size={2} errors={3} warnings={4} time={5:0.0}s",
                BuildVer, summary.result, summary.totalSize, summary.totalErrors, summary.totalWarnings, summary.totalTime.TotalSeconds);
            Debug.Log(log);
            File.WriteAllText(Path.Combine(root, "v954_build_result.txt"), log);

            if (summary.result == BuildResult.Succeeded)
                Debug.Log("BUILD_SUCCESS");
            else
            {
                Debug.LogError("BUILD_FAILED");
                EditorApplication.Exit(1);
            }
        }
    }
}
