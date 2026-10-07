using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PixelToCivilization.Platform
{
    /// <summary>
    /// V9.7.1 混合存档存储层（裁决方案 C）：
    ///  - WebGL：存档正文（PxC_Save_* / PxC_Roll_* / PxC_Bak_* / PxC_Tmp_*）走独立 IndexedDB 库，
    ///    启动时全量预载到 JS 内存缓存 → C# 同步读写（语义与 PlayerPrefs 一致），
    ///    写操作防抖落库、关键节点 BodyFlush 立即落库；
    ///    槽位摘要 / 崩溃状态 / 九神密钥等小键仍走 PlayerPrefs；
    ///    首次启动自动把 PlayerPrefs 中的旧正文一次性迁移到 IndexedDB。
    ///  - 编辑器 / 其他平台：正文直接走 PlayerPrefs，行为与旧版完全一致（零迁移）。
    /// </summary>
    public static class PxcStorage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PxcInitDb();
        [DllImport("__Internal")] private static extern IntPtr PxcGetBody(string key);
        [DllImport("__Internal")] private static extern void PxcSetBody(string key, string val);
        [DllImport("__Internal")] private static extern void PxcDeleteBody(string key);
        [DllImport("__Internal")] private static extern int PxcHasBody(string key);
        [DllImport("__Internal")] private static extern void PxcFlushBody();
        [DllImport("__Internal")] private static extern void PxcFreeBuf(IntPtr ptr);
#endif

        /// <summary>当前是否为 WebGL 混合存储模式（探针/调试用）。</summary>
        public static bool HybridMode { get; private set; }
        /// <summary>存储层是否就绪（WebGL = IndexedDB 预载完成；其余平台恒 true）。</summary>
        public static bool Ready { get; private set; }

        /// <summary>启动初始化：WebGL 打开 IndexedDB（异步，完成后由 GameManager.OnStorageReady 置 Ready）；其余平台直接就绪。</summary>
        public static void BeginInit()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            HybridMode = true;
            Ready = false;
            try { PxcInitDb(); }
            catch (Exception e) { Debug.LogWarning("[PxcStorage] BeginInit failed, fallback memory: " + e.Message); Ready = true; }
#else
            HybridMode = false;
            Ready = true;
#endif
        }

        /// <summary>由 jslib 初始化完成回调（GameManager.OnStorageReady 转发）。</summary>
        public static void MarkReady() => Ready = true;

        // ---------- 正文键（大字符串：完整存档） ----------

        public static bool BodyHasKey(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Ready) return false;
            return PxcHasBody(key) == 1;
#else
            return PlayerPrefs.HasKey(key);
#endif
        }

        public static string BodyGetString(string key, string def = "")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Ready) return def;
            IntPtr ptr = PxcGetBody(key);
            string v = null;
            if (ptr != IntPtr.Zero)
            {
                try { v = Marshal.PtrToStringUTF8(ptr); }
                finally { PxcFreeBuf(ptr); }   // 与 jslib _malloc 配对，防 emscripten 堆泄漏
            }
            return string.IsNullOrEmpty(v) ? def : v;
#else
            return PlayerPrefs.GetString(key, def);
#endif
        }

        public static void BodySetString(string key, string val)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Ready) return;
            PxcSetBody(key, val ?? "");
#else
            PlayerPrefs.SetString(key, val ?? "");
#endif
        }

        public static void BodyDeleteKey(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Ready) return;
            PxcDeleteBody(key);
#else
            PlayerPrefs.DeleteKey(key);
#endif
        }

        /// <summary>立即把正文缓存落库（存档关键节点：原子写提交后）；WebGL 防抖写之外的强同步触发点。</summary>
        public static void BodyFlush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Ready) return;
            PxcFlushBody();
#endif
        }

        // ---------- 旧档迁移（WebGL 首次启动） ----------

        /// <summary>
        /// 把 PlayerPrefs 内的旧正文键一次性迁移到 IndexedDB（正文已存在则不覆盖），
        /// 迁移成功后删除 PlayerPrefs 中的正文副本（摘要/状态键保留）。
        /// 仅 WebGL 混合模式执行；返回迁移键数。
        /// </summary>
        public static int MigrateLegacyFromPlayerPrefs()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!HybridMode || !Ready) return 0;
            int moved = 0;
            Action<string> moveKey = k =>
            {
                if (!PlayerPrefs.HasKey(k)) return;
                if (BodyHasKey(k)) { PlayerPrefs.DeleteKey(k); moved++; return; }  // 新库已有更新档 → 丢弃旧正文
                BodySetString(k, PlayerPrefs.GetString(k));
                PlayerPrefs.DeleteKey(k);
                moved++;
            };
            moveKey("PxC_Save_auto");
            for (int i = 1; i <= 5; i++) moveKey("PxC_Save_" + i);
            for (int i = 0; i < 3; i++) moveKey("PxC_Roll_" + i);
            for (int i = 1; i <= 5; i++) moveKey("PxC_Bak_" + i);
            for (int i = 0; i <= 5; i++) moveKey("PxC_Tmp_" + i);
            if (moved > 0)
            {
                BodyFlush();
                PlayerPrefs.Save();
                Debug.Log("[PxcStorage] 旧档迁移完成：" + moved + " 个正文键已迁入 IndexedDB");
            }
            return moved;
#else
            return 0;
#endif
        }
    }
}
