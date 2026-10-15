using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PxC.Core.Services;
using UnityEngine;

namespace PxC.Infra.Crash
{
    /// <summary>
    /// 崩溃守护：三路全局捕获（logMessageReceived / AppDomain / TaskScheduler）、
    /// 200 条环形日志、崩溃快照文件、安全模式标记、崩溃恢复检测。
    /// 崩溃标记用 PlayerPrefs（跨平台，含 WebGL）；崩溃明细写 persistentDataPath。
    /// </summary>
    public sealed class CrashGuard : ICrashGuard
    {
        private const int MaxLogs = 200;
        private const string CleanKey = "PxC_CleanRun";

        private readonly Queue<string> _ring;
        private Func<CrashStateSnapshot> _stateProvider;
        private readonly Action<string> _logger;
        private bool _installed;

        public bool CrashedLastRun { get; private set; }

        public CrashGuard(Action<string> logger = null)
        {
            _logger = logger;
            _ring = new Queue<string>(MaxLogs);
        }

        public void Install()
        {
            if (_installed) return;
            // 上次标记为 0=未正常结束 → 异常退出
            CrashedLastRun = PlayerPrefs.GetInt(CleanKey, 1) == 0;
            PlayerPrefs.SetInt(CleanKey, 0);
            PlayerPrefs.Save();

            Application.logMessageReceived += OnLogMessage;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTask;
            _installed = true;
        }

        public void Uninstall()
        {
            if (!_installed) return;
            Application.logMessageReceived -= OnLogMessage;
            AppDomain.CurrentDomain.UnhandledException -= OnDomainException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTask;
            _installed = false;
        }

        public void SetStateProvider(Func<CrashStateSnapshot> provider) => _stateProvider = provider;

        public void Log(string message)
        {
            _ring.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_ring.Count > MaxLogs) _ring.Dequeue();
        }

        public void MarkCleanRun()
        {
            PlayerPrefs.SetInt(CleanKey, 1);
            PlayerPrefs.Save();
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            Log($"[{type}] {condition}");
            if (type == LogType.Exception)
                WriteCrash(condition, stackTrace, type.ToString());
        }

        private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            WriteCrash(ex?.Message ?? "unknown", ex?.StackTrace ?? string.Empty, "UnhandledException");
        }

        private void OnUnobservedTask(object sender, UnobservedTaskExceptionEventArgs e)
        {
            WriteCrash(e.Exception.Message, e.Exception.StackTrace, "UnobservedTask");
            e.SetObserved();
        }

        private void WriteCrash(string message, string stack, string type)
        {
            try
            {
                var snap = _stateProvider?.Invoke() ?? new CrashStateSnapshot();
                snap.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                snap.StackTrace = stack;
                snap.ExceptionType = type;
                snap.ExceptionMessage = message;
                snap.RecentLogs = _ring.ToArray();
                snap.Scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                string dir = Application.persistentDataPath;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
                File.WriteAllText(path, JsonUtility.ToJson(snap, true));
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"[Crash] 写崩溃文件失败: {ex.Message}");
            }
        }
    }
}
