using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.6.5 崩溃架构 · CrashGuard：
    /// 全局异常捕获（Application.logMessageReceived：Exception/Assert/Error）+ WebGL JS window.onerror 桥 +
    /// 崩溃标记（booting/clean/crashed）+ 环形日志（200 条）+ 安全模式判定与降级 + 恢复引导。
    /// 说明：WebGL 进程级崩溃（WASM OOM/栈溢出）无法从 C# 捕获，但托管异常/Unity LogType 异常/JS 错误均可捕获，
    /// 结合正常退出写 clean 标记即可判定"上次是否异常退出"。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CrashGuardSystem : MonoBehaviour, ISaveable   // V9.6.6：环形日志随档（存档携带崩溃诊断历史）
    {
        public static CrashGuardSystem Instance { get; private set; }

        // ---------- V9.6.6 ISaveable：环形日志随档 ----------
        public string SaveKey => "CrashGuard";
        [Serializable]
        public class CrashLogSnapshot { public CrashLogEntry[] Entries; }
        public string Serialize()
        {
            if (_ring.Count == 0) return null;
            var snap = new CrashLogSnapshot { Entries = _ring.ToArray() };
            return JsonUtility.ToJson(snap);
        }
        public void Deserialize(string json)
        {
            try
            {
                var snap = JsonUtility.FromJson<CrashLogSnapshot>(json);
                if (snap == null || snap.Entries == null) return;
                _ring.Clear();
                for (int i = Mathf.Max(0, snap.Entries.Length - LogRing); i < snap.Entries.Length; i++)
                    _ring.Add(snap.Entries[i]);
            }
            catch (Exception e) { Debug.LogWarning("[CrashGuard] 环形日志恢复失败（忽略）: " + e.Message); }
        }

        /// <summary>本次会话是否处于安全模式（上次异常退出自动进入；WebCrashSimulate/WebSafeMode 测试可写）</summary>
        public static bool SafeMode { get; set; }
        /// <summary>上次崩溃摘要（安全模式 UI / 探针展示；测试可写）</summary>
        public static string LastCrashSummary { get; set; }

        // PlayerPrefs 键（WebGL 为 IndexedDB 同步持久化）
        public const string KeyState = "PxC_Crash_State";  // booting / clean / crashed
        public const string KeyLast  = "PxC_Crash_Last";   // 最近异常/崩溃摘要
        public const string KeyGood  = "PxC_LastGoodSave"; // 最近健康自动存档时间戳（UTC 秒）

        public const int LogRing = 200;                    // 环形日志上限
        private readonly List<CrashLogEntry> _ring = new List<CrashLogEntry>(LogRing);

        [Serializable]
        public class CrashLogEntry { public long T; public string L; public string M; }

        public void Init()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.logMessageReceived += OnUnityLog;
#if UNITY_WEBGL && !UNITY_EDITOR
            // JS 侧全局 error 事件 → C#（捕获引擎外部/脚本错误；SendMessage 单字符串参数）
            try
            {
                Application.ExternalEval(
                    "if(!window.__pxcOnError){window.__pxcOnError=function(msg,src,line,col){var s=((msg||'').substring(0,180)+'|'+" +
                    "(src||'').substring(0,60)+':'+line+':'+col).substring(0,220);" +
                    "try{if(window.unityInstance&&window.unityInstance.SendMessage)" +
                    "{window.unityInstance.SendMessage('GameManager','WebJsError',s);}}catch(e){}};" +
                    "window.addEventListener('error',window.__pxcOnError,false);}");
            }
            catch { /* 桥接失败不影响主体 */ }
#endif
        }

        public void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Application.logMessageReceived -= OnUnityLog;
            }
        }

        // ---------- 启动标记 ----------
        /// <summary>Boot 最早阶段调用：上次 crashed 且未 clean → 安全模式；随后写 booting。</summary>
        public static void DetectOnBoot()
        {
            SafeMode = PlayerPrefs.GetString(KeyState, "") == "crashed";
            LastCrashSummary = PlayerPrefs.GetString(KeyLast, "");
            PlayerPrefs.SetString(KeyState, "booting");
            PlayerPrefs.Save();
            if (SafeMode)
                Debug.LogWarning("[CrashGuard] 检测到上次异常退出 → 进入安全模式。摘要=" + LastCrashSummary);
        }

        /// <summary>进入 Playing（新局开始/恢复成功）后调用：标记本次正常。</summary>
        public static void MarkClean()
        {
            SafeMode = false;
            PlayerPrefs.SetString(KeyState, "clean");
            PlayerPrefs.Save();
        }

        // ---------- 异常捕获 ----------
        private void OnUnityLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Assert || type == LogType.Error)
            {
                Record("E", msg + " | " + stack);
                if (type == LogType.Exception)
                {
                    // V9.7.0 崩溃摘要附带游戏状态上下文（朝代/年份/人口/势力/时代/倍速），方便定位崩溃时刻
                    LastCrashSummary = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "|" +
                        (msg.Length > 160 ? msg.Substring(0, 160) : msg) +
                        "|" + LocalCrashReporter.ContextText();
                    PlayerPrefs.SetString(KeyState, "crashed");
                    PlayerPrefs.SetString(KeyLast, LastCrashSummary);
                    PlayerPrefs.Save();
                    Debug.LogWarning("[CrashGuard] 已捕获托管异常并标记崩溃；下次启动进入安全模式并尝试回滚恢复");
                }
            }
            else
            {
                Record(type == LogType.Warning ? "W" : "I", msg);
            }
        }

        /// <summary>WebGL JS 错误入口（window.onerror 转发；SendMessage 无参方法桥接用带参）。</summary>
        public void WebJsError(string payload)
        {
            Record("J", payload);
            LastCrashSummary = "JS:" + payload;
            PlayerPrefs.SetString(KeyState, "crashed");
            PlayerPrefs.SetString(KeyLast, LastCrashSummary);
            PlayerPrefs.Save();
        }

        // ---------- 环形日志 ----------
        private void Record(string lvl, string m)
        {
            _ring.Add(new CrashLogEntry { T = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), L = lvl, M = m.Length > 300 ? m.Substring(0, 300) : m });
            if (_ring.Count > LogRing) _ring.RemoveAt(0);
        }

        /// <summary>最近 n 条日志文本（n∈[1,200]），供探针/UI 导出。</summary>
        public string RecentLog(int n)
        {
            n = Mathf.Clamp(n, 1, Mathf.Max(1, _ring.Count));
            var sb = new System.Text.StringBuilder();
            int start = Mathf.Max(0, _ring.Count - n);
            for (int i = start; i < _ring.Count; i++)
                sb.Append(_ring[i].T).Append(' ').Append(_ring[i].L).Append(' ').Append(_ring[i].M).Append('\n');
            return sb.ToString();
        }

        // ---------- 安全模式降级（实际生效项） ----------
        public static void ApplySafeMode()
        {
            if (!SafeMode) return;
            QualitySettings.shadows = ShadowQuality.Disable;   // 关阴影
            QualitySettings.shadowDistance = 0f;
            QualitySettings.lodBias = 0.5f;                    // LOD 提前降级
            QualitySettings.particleRaycastBudget = 16;        // 粒子预算收紧
            Debug.LogWarning("[CrashGuard] 安全模式已生效：阴影关闭 / LOD 0.5 / 粒子预算 16 / 自动保存 20s");
        }

        // ---------- 探针 ----------
        public string Probe()
        {
            return "safe:" + (SafeMode ? "1" : "0") +
                   "|state:" + PlayerPrefs.GetString(KeyState, "none") +
                   "|last:" + (PlayerPrefs.GetString(KeyLast, "") ?? "") +
                   "|good:" + PlayerPrefs.GetString(KeyGood, "0") +
                   "|ring:" + _ring.Count +
                   "|log:" + RecentLog(8).Replace("\n", "\\n").Replace("|", "¦");
        }
    }
}
