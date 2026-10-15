using System;
using System.Collections.Generic;
using PxC.Core.Services;

namespace PxC.Infra.Crash
{
    /// <summary>
    /// 本地崩溃上报：记录异常等级、自定义键、面包屑。
    /// 第三方（如 Crashlytics）当前未引入，远程上报未验证；可在此实现一个 ICrashReport 适配。
    /// </summary>
    public sealed class LocalCrashReport : ICrashReport
    {
        private readonly Dictionary<string, string> _keys;
        private readonly Action<string> _logger;

        public LocalCrashReport(Action<string> logger = null)
        {
            _logger = logger;
            _keys = new Dictionary<string, string>(16);
        }

        public void LogException(Exception e, bool fatal = false)
        {
            _logger?.Invoke(
                $"[CrashReport] {(fatal ? "FATAL" : "NON-FATAL")} {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
        }

        public void SetCustomKey(string key, string value) => _keys[key] = value;

        public void LogBreadcrumb(string message) => _logger?.Invoke($"[breadcrumb] {message}");

        public string GetKey(string key) =>
            _keys.TryGetValue(key, out var v) ? v : null;
    }
}
