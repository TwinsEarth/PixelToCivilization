using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI / 命令行构建入口（-executeMethod BuildScript.BuildWebGL）。
/// 入口场景：Assets/Scenes/MainScene.unity（空场景，GameBootstrap 以
/// RuntimeInitializeOnLoadMethod 自动组装宿主，无需场景脚本）。
///
/// 说明（重要）：
/// batchmode 下 -logFile 在 domain reload / bee 编译期间可能缓冲不刷新，
/// 因此把构建状态独立写入 build_status.txt，不依赖日志。
///
/// 不要在构建前删除 Library/Bee。bee_backend 在脚本编译完成后会建立完整的
/// 2000b0aE.dag 与 TundraBuildState.state；BuildPlayer 内的
/// ScriptAssembliesAndTypeDB(SkipCompile) 依赖这些文件，由同一个 backend 直接
/// 完成（ExitCode 0）。一旦清空 Bee（bee_backend.info 缺失），frontend 会
/// 冷启动并再启动一个 backend，触发 "More than one copy of bee_backend"。
/// </summary>
public static class BuildScript
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string StatusFile = "build_status.txt";

    private static void WriteStatus(string text)
    {
        try
        {
            File.WriteAllText(StatusFile,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + Environment.NewLine);
            Debug.Log("[BuildScript] " + text);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BuildScript] cannot write status file: " + e.Message);
        }
    }

    /// <summary>构建 WebGL（IL2CPP）。输出到 Build/WebGL。</summary>
    public static void BuildWebGL()
    {
        WriteStatus("BuildWebGL STARTED");

        if (!File.Exists(ScenePath))
        {
            WriteStatus("FATAL scene not found: " + ScenePath);
            throw new Exception("Scene not found: " + ScenePath);
        }

        try
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            var s = report.summary;
            string result =
                $"result={s.result} size={s.totalSize} errors={s.totalErrors} " +
                $"warnings={s.totalWarnings} duration={s.totalTime.TotalSeconds:F1}s path={s.outputPath}";
            WriteStatus("BuildWebGL FINISHED " + result);

            if (s.result != BuildResult.Succeeded)
                throw new Exception("WebGL build failed: " + s.result);
        }
        catch (Exception e)
        {
            WriteStatus("BuildWebGL EXCEPTION " + e);
            throw;
        }
    }
}
