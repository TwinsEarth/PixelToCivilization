using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI / 命令行构建入口（-executeMethod BuildScript.BuildWebGL）。
/// 入口场景：Assets/Scenes/MainScene.unity（空场景，GameBootstrap 以
/// RuntimeInitializeOnLoadMethod 自动组装宿主，无需场景脚本）。
/// </summary>
public static class BuildScript
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";

    /// <summary>构建 WebGL（IL2CPP）。输出到 Build/WebGL。</summary>
    public static void BuildWebGL()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        var s = report.summary;
        Debug.Log($"[BuildScript] WebGL result={s.result} size={s.totalSize} errors={s.totalErrors} " +
                  $"warnings={s.totalWarnings} duration={s.totalTime.TotalSeconds:F1}s path={s.outputPath}");
        if (s.result != BuildResult.Succeeded)
            throw new Exception("WebGL build failed: " + s.result);
    }
}
