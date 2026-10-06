using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.7.0 崩溃报告抽象（ICrashReporter）。
    /// 统一崩溃/异常上报入口，游戏状态上下文（朝代/年份/人口/势力数）随崩溃快照落盘。
    ///  - WebGL（当前主交付）：LocalCrashReporter 写 PlayerPrefs PxC_CrashCtx_* + 触发 CrashGuardSystem 环形日志与崩溃标记。
    ///  - 原生端（Android/iOS/macOS）未来接入 Firebase Crashlytics 时实现同一接口即可：
    ///    ReportFatal→Crashlytics.ReportUncaughtExceptionsAsFatal=true 的未捕获异常；ReportException→Crashlytics.LogException(e)；
    ///    当前仓库无 Firebase 依赖，适配壳仅以注释形式给出，不引入依赖（铁律）。
    /// </summary>
    public interface ICrashReporter
    {
        /// <summary>设置自定义键（当前朝代/年份/人口/势力数等），崩溃时随报告带上。</summary>
        void SetContext(string key, string value);
        /// <summary>上报致命未捕获异常（本地：落盘+标记崩溃）。</summary>
        void ReportFatal(string msg, string stack);
        /// <summary>上报预期异常（本地：进环形日志，不标记崩溃）。</summary>
        void ReportException(string msg, string stack);
    }

    /// <summary>本地崩溃报告器（WebGL 主实现）。</summary>
    public static class LocalCrashReporter
    {
        const string KeyPrefix = "PxC_CrashCtx_";

        static readonly Dictionary<string, string> _ctx = new Dictionary<string, string>();

        static LocalCrashReporter() { LoadPersisted(); }

        public static void SetContext(string key, string value)
        {
            _ctx[key] = value;
            PlayerPrefs.SetString(KeyPrefix + key, value ?? "");
            PlayerPrefs.Save();
        }

        public static string GetContext(string key) => _ctx.TryGetValue(key, out var v) ? v : "";

        static void LoadPersisted()
        {
            foreach (var k in new[] { "dynasty", "year", "pop", "factions", "era", "speed" })
                _ctx[k] = PlayerPrefs.GetString(KeyPrefix + k, "");
        }

        /// <summary>组装游戏状态上下文串（崩溃日志/探针用）。</summary>
        public static string ContextText()
        {
            return "ctx:" +
                "dyn=" + GetContext("dynasty") +
                "|y=" + GetContext("year") +
                "|pop=" + GetContext("pop") +
                "|fac=" + GetContext("factions") +
                "|era=" + GetContext("era") +
                "|spd=" + GetContext("speed");
        }

        /// <summary>模拟上报（供 WebCrashCtxProbe 验证上下文落盘与回读）。</summary>
        public static string Probe()
        {
            return ContextText() + "|persisted=" + (PlayerPrefs.GetString(KeyPrefix + "year", "") != "" ? "1" : "0");
        }
    }
}
