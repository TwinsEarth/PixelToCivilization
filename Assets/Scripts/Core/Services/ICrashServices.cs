using System;

namespace PxC.Core.Services
{
    /// <summary>崩溃时保存的游戏状态快照（本地崩溃日志）。</summary>
    [Serializable]
    public sealed class CrashStateSnapshot
    {
        public string Timestamp;
        public string Scene;
        public int GameYear;
        public int Population;
        public int FactionCount;
        public string Dynasty;
        public string[] RecentLogs;
        public string StackTrace;
        public string ExceptionType;
        public string ExceptionMessage;
    }

    /// <summary>
    /// 崩溃守护：三路全局捕获（logMessageReceived / AppDomain / TaskScheduler）、
    /// 环形日志、崩溃快照、安全模式、崩溃恢复检测。
    /// </summary>
    public interface ICrashGuard
    {
        void Install();
        void Uninstall();

        /// <summary>注册状态快照提供者（崩溃时收集当前年份/人口/朝代等）。</summary>
        void SetStateProvider(Func<CrashStateSnapshot> provider);

        /// <summary>记录一条关键日志（进入环形缓冲）。</summary>
        void Log(string message);

        /// <summary>上次是否异常退出（启动时检测，决定是否提供恢复）。</summary>
        bool CrashedLastRun { get; }

        /// <summary>标记本次为正常运行（成功启动/存档后调用）。</summary>
        void MarkCleanRun();
    }

    /// <summary>
    /// 崩溃上报：本地实现先写入崩溃文件；预留第三方（如 Crashlytics）适配。
    /// 当前未引入 Crashlytics，远程上报为未验证。
    /// </summary>
    public interface ICrashReport
    {
        /// <summary>记录异常。fatal=true 作为严重事件。</summary>
        void LogException(Exception e, bool fatal = false);

        /// <summary>设置自定义键（当前朝代/年份/人口/势力数）。</summary>
        void SetCustomKey(string key, string value);

        /// <summary>写入一条面包屑日志（崩溃前的上下文）。</summary>
        void LogBreadcrumb(string message);
    }
}
