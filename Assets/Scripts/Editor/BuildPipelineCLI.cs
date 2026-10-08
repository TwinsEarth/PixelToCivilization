#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V9.6.7 部署与运行：多平台 BuildPipeline 一键构建（WebGL / Windows / Android / macOS / iOS）。
    ///
    /// 命令行用法（Tuanjie 团结引擎 2022.3.62t12）：
    ///   & "E:\Unity\2022.3.62t12\Editor\Tuanjie.exe" -batchmode -quit `
    ///     -projectPath "<工程根>" -logFile build.log `
    ///     -executeMethod PixelToCivilization.EditorTools.BuildPipelineCLI.BuildTargetCLI `
    ///     -buildTarget WebGL -outDir <输出目录>
    ///
    /// buildTarget 枚举：WebGL / Win64 / Android / macOS / iOS（缺省 WebGL）。
    /// 平台约束：
    ///   - WebGL / Win64 / Android 可在 Windows 本机构建（Android 需 Hub 装 Android Build Support + SDK/NDK）。
    ///   - macOS 需 macOS 机器；iOS 需 macOS + Xcode（构建产出 Xcode 工程，再用 xcodebuild 出包）。
    ///   - 本脚本在 Windows 上实测 WebGL；其他平台脚本路径已硬化，产物以平台机器实测为准。
    /// </summary>
    public static class BuildPipelineCLI
    {
        public const string BuildVer = "9.8.0";
        const string ScenePath = "Assets/Scenes/MainScene.unity";
        const string CompanyName = "ToFuture";
        const string ProductName = "从像素到文明";
        const string AndroidBundleId = "com.tofuture.pixelcivilization";
        const string IosBundleId = "com.tofuture.pixelcivilization";

        // ==================== 命令行入口 ====================

        public static void BuildTargetCLI()
        {
            string[] args = Environment.GetCommandLineArgs();
            string target = Arg(args, "-buildTarget") ?? "WebGL";
            string outDir = Arg(args, "-outDir");
            try
            {
                var bt = (BuildTarget)Enum.Parse(typeof(BuildTarget), target, true);
                string dir = Build(bt, outDir);
                Console.WriteLine("BUILD_SUCCESS:" + dir);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildPipeline] 构建异常: " + e);
                Console.WriteLine("BUILD_FAILED:" + e.Message);
                EditorApplication.Exit(1);
            }
        }

        static string Arg(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == key) return args[i + 1];
            return null;
        }

        // ==================== 核心构建 ====================

        /// <summary>切换平台 → PlayerSettings 硬化 → BuildPipeline.BuildPlayer → 报告摘要。返回输出目录。</summary>
        public static string Build(BuildTarget target, string outDirOverride = null)
        {
            // V7.0.6 教训：命令行 -batchmode 下新增脚本若未导入会被漏编译（CS0103/CS0234），构建前强制全量导入
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var group = BuildPipeline.GetBuildTargetGroup(target);
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            if (!switched)
                throw new Exception("切换平台失败: " + target +
                    " —— 请在 Tuanjie Hub 为本编辑器安装对应的 Build Support 模块（WebGL / Windows / Android / macOS / iOS），然后重新执行。");

            ConfigurePlayerSettings(group, target);

            string projRoot = Path.GetDirectoryName(Application.dataPath);
            string outDir = outDirOverride ?? Path.Combine(projRoot, OutDirName(target));
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outDir,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("构建失败: " + report.summary.result + "，见 -logFile 日志尾部。");

            // WebGL 额外步骤：中文全屏加载页 + 本地服务器脚本（复用 WebGLBuilder，产物统一）
            if (target == BuildTarget.WebGL)
                WebGLBuilder.WriteCustomIndex(outDir);

            LogReport(report);
            return outDir;
        }

        static string OutDirName(BuildTarget t)
        {
            switch (t)
            {
                case BuildTarget.WebGL: return "BuildWebGL";
                case BuildTarget.StandaloneWindows64: return "BuildWindows";
                case BuildTarget.Android: return "BuildAndroid";
                case BuildTarget.StandaloneOSX: return "BuildMacOS";
                case BuildTarget.iOS: return "BuildIOS";
                default: return "Build" + t;
            }
        }

        // ==================== PlayerSettings 硬化 ====================

        /// <summary>公共（全平台）设置 + 平台特定设置。只设与部署相关的键，不动玩法/渲染/资源面。</summary>
        public static void ConfigurePlayerSettings(BuildTargetGroup group, BuildTarget target)
        {
            // --- 公共 ---
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName + " " + BuildVer;
            PlayerSettings.bundleVersion = BuildVer;
            PlayerSettings.runInBackground = true;
            try { PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP); } catch { }
            try { PlayerSettings.SetApiCompatibilityLevel(group, ApiCompatibilityLevel.NET_Standard); } catch { }
            try { PlayerSettings.SetIl2CppCompilerConfiguration(group, Il2CppCompilerConfiguration.Release); } catch { }

            // --- 平台特定 ---
            switch (target)
            {
                case BuildTarget.WebGL:
                    // 沿用 WebGLBuilder 的浏览器兼容硬化（单线程免 COOP/COEP、压缩关闭免 Content-Encoding、dataCaching 关防 .data/wasm 错位、异常只抛显式）
                    WebGLBuilder.ConfigurePlayerSettings();
                    break;
                case BuildTarget.StandaloneWindows64:
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, AndroidBundleId); // Standalone 组共用标识
                    break;
                case BuildTarget.Android:
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidBundleId);
                    try { PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22; } catch { }
                    try { PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel32; } catch { }
                    try { PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; } catch { }
                    try { PlayerSettings.Android.forceSDCardPermission = false; } catch { }
                    break;
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, IosBundleId);
                    break;
                case BuildTarget.iOS:
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, IosBundleId);
                    try { PlayerSettings.iOS.targetOSVersionString = "12.0"; } catch { }
                    break;
            }
        }

        // ==================== 构建报告 ====================

        static void LogReport(BuildReport report)
        {
            var s = report.summary;
            Console.WriteLine("[BuildReport] result=" + s.result + " sizeBytes=" + s.totalSize +
                " durationSec=" + Math.Round(s.totalTime.TotalSeconds, 1) + " warnings=" + s.totalWarnings + " errors=" + s.totalErrors);
            foreach (var e in report.steps)
                if (e.depth == 0)
                    Console.WriteLine("  step " + e.name + " (" + Math.Round(e.duration.TotalSeconds, 1) + "s)");
            Debug.Log("[BuildPipeline] 构建完成: " + s.result + " 输出: " + s.outputPath);
        }

        // ==================== Editor 菜单（调试用） ====================

        [MenuItem("像素到文明/⑪ 一键构建 WebGL(部署)")]
        public static void BuildWebGLMenu() { Build(BuildTarget.WebGL); }

        [MenuItem("像素到文明/⑪ 一键构建 Windows x64(部署)")]
        public static void BuildWindowsMenu() { Build(BuildTarget.StandaloneWindows64); }
    }
}
#endif
