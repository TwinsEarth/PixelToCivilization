#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V9.6.7 Addressables 就绪评估：**不强制迁移**（铁律：不随意换架构、不随意加依赖）。
    /// 本项目当前为单包 + Assets/Resources 加载（WebGL 主交付），Resources 资产规模小、
    /// 加载路径全同步、无独立热更/换包需求，故 V9.6.7 保持 Resources 架构，不加 com.unity.addressables。
    ///
    /// 何时才应接入 Addressables：
    ///   1) 需要按需下载/热更/独立更新资源（WebGL 下为 .bundle 拆分 + IndexedDB 缓存）；
    ///   2) 单 .data 超过 WebGL 内存预算（约 1.5GB），需要拆包滚动加载；
    ///   3) 需跨平台复用同一套资产管线并做 Addressable Groups 管理。
    ///
    /// 本脚本提供：菜单「像素到文明/⑫ Addressables 就绪评估」输出评估报告；
    /// 菜单「像素到文明/⑫ 生成 Addressables 接入清单(不启用)」生成接入步骤清单 md（不实际改 manifest）。
    /// </summary>
    public static class AddressablesReadiness
    {
        const string ManifestPath = "Packages/manifest.json";
        const string ReportPath = "AddressablesReadinessReport.txt";

        [MenuItem("像素到文明/⑫ Addressables 就绪评估")]
        public static void RunAssessment()
        {
            bool installed = PackagePresent("com.unity.addressables");
            var resources = AssetDatabase.FindAssets("t:Object", new[] { "Assets/Resources" });
            var allAssets = AssetDatabase.FindAssets("t:Object");
            long resBytes = 0, allBytes = 0;
            foreach (var guid in resources)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var f = new FileInfo(p);
                if (f.Exists) resBytes += f.Length;
            }
            foreach (var guid in allAssets)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var f = new FileInfo(p);
                if (f.Exists) allBytes += f.Length;
            }

            string[] lines =
            {
                "===== Addressables 就绪评估（V9.6.7）=====",
                "包状态: com.unity.addressables = " + (installed ? "已安装" : "未安装"),
                "Assets/Resources 资产数: " + resources.Length,
                "Assets/Resources 体积(未压缩源文件): " + (resBytes / 1048576.0).ToString("F1") + " MB",
                "全工程资产数: " + allAssets.Length,
                "全工程体积(源文件): " + (allBytes / 1048576.0).ToString("F1") + " MB",
                "",
                "结论: " + (resources.Length < 800 && resBytes < 512L * 1024 * 1024
                    ? "当前规模小，保持 Resources 架构即可（WebGL 单包 55MB，无热更需求）。不建议本轮接入。"
                    : "资产规模已到需考虑 Addressables 拆包的阈值，建议在专项版本评估迁移。"),
                "",
                "WebGL + Addressables 注意事项:",
                "  1) Build 拆分为多个 .bundle，loader 按需下载；需配置 BrowserAssetCache/IndexedDB 缓存策略",
                "  2) 每个 bundle 独立压缩；跨 bundle 引用需 Addressable 化，破坏现有 Resources.Load 调用点",
                "  3) 远程 bundle 需 HTTPS + CORS；本地 build 产物保持离线可玩需全部打进 build",
                "  4) 接入需重写所有 Resources.Load / Resources.LoadAll 调用点（本项目约数百处）",
                "  5) 风险：WebGL 内存上限 2GB，bundle 加载峰值叠加需做预算控制（可复用 V9.6.4 内存水位框架）",
                "",
                "生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            File.WriteAllLines(ReportPath, lines, new System.Text.UTF8Encoding(false));
            Debug.Log("[Addressables] 评估完成: " + Path.GetFullPath(ReportPath));
        }

        [MenuItem("像素到文明/⑫ 生成 Addressables 接入清单(不启用)")]
        public static void WriteMigrationChecklist()
        {
            string[] steps =
            {
                "# Addressables 接入清单（V9.6.7 评估版 · 未启用，需用户授权后执行）",
                "1. manifest.json dependencies 追加: \"com.unity.addressables\": \"1.21.21\"（2022.3 兼容线，以 Package Manager 可解析版本为准）",
                "2. 新建 Editor 脚本 AddressableSetup: AddressableAssetSettings.Create -> Default Group 设为 Play Mode Script",
                "3. 迁移顺序（按依赖）：先 UI 图集 -> 模型/材质 -> 音频 -> 配置 ScriptableObject",
                "4. Resources.Load 调用点批量改写为 Addressables.LoadAssetAsync（约数百处，需逐处回归）",
                "5. WebGL 特有：BuildScriptPackedMode，LocalLoadPath 'file:///{UnityEngine.AddressableAssets.Addressables.BuildPath}'，",
                "   RemoteLoadPath 仅用于可热更资产；关闭 Build Remote Catalog 可保纯离线",
                "6. 内存预算：每个 bundle 加载后 Addressables.Release 引用，配合 V9.6.4 水位探针 WebMemoryProbe",
                "7. 回滚：manifest 撤销依赖 + 恢复 Resources.Load 调用点 git 还原，确保可一键回退",
                "（清单由菜单生成，不修改任何工程文件）"
            };
            File.WriteAllLines("AddressablesMigrationChecklist.md", steps, new System.Text.UTF8Encoding(false));
            Debug.Log("[Addressables] 接入清单已生成: " + Path.GetFullPath("AddressablesMigrationChecklist.md"));
        }

        static bool PackagePresent(string id)
        {
            string p = Path.Combine(Application.dataPath, "../", ManifestPath);
            try
            {
                string json = File.ReadAllText(p);
                return json.Contains("\"" + id + "\"");
            }
            catch { return false; }
        }
    }
}
#endif
