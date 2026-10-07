using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.6.4 内存架构中枢（只读监控 + 临界钳制，不改玩法）：
    /// 1) 显式预算水位 Normal/High/Critical（WebGL IL2CPP wasm 线性内存 2GB 上限，设 1.1GB 高位 / 1.5GB 临界）；
    /// 2) 临界水位统一钳制运行时自动生成入口：GlobalSpawnGate=false 时敌船镜像/鸟补员/鱼补员暂停，
    ///    避免实体逼近上限后内存顶穿（memory access out of bounds）；
    /// 3) 每 SampleInterval(5s) 采样托管堆(GC.GetTotalMemory)与原生内存(Profiler)，计算窗口 GC 增量≈GC Alloc 速率；
    /// 4) 每系统 Tick 耗时统计（由 GameManager 主循环喂入），每 120 帧汇总 Top5 慢系统（供性能神/调优）；
    /// 5) 对象池统计（每采样周期刷新 PoolReport 文本，供 WebMemoryProbe/调试查看）。
    /// 挂载方式：GameManager.InstallSystems 内 AddComponent；非 GameSystemBase（不参与游戏 Tick，用 Update 现实秒采样）。
    /// </summary>
    public class MemoryBudgetManager : MonoBehaviour
    {
        public static MemoryBudgetManager Instance { get; private set; }

        // ---- 显式预算（WebGL IL2CPP 线性内存上限约 2GB；按水位分级）----
        public long HighWaterBytes = 1_100_000_000L;      // 原生内存 ≥1.1GB → High（>50% 预算告警）
        public long CriticalWaterBytes = 1_500_000_000L;  // 原生内存 ≥1.5GB → Critical（>75% 钳制生成）
        public const float SampleInterval = 5f;           // 采样周期（现实秒）

        public enum MemLevel { Normal, High, Critical }
        public MemLevel Level { get; private set; } = MemLevel.Normal;

        /// <summary>临界水位下自动生成入口统一闸门；null（未挂载）时放行保证兼容。</summary>
        public static bool GlobalSpawnGate => Instance == null || Instance.Level < MemLevel.Critical;

        // ---- V9.7.1 实体上限门控（裁决：实体逼近上限即暂停生成）----
        public enum EntityKind { Ship, Bird, Fish, Ground, Cart, Train, Plane, Building }
        static readonly Dictionary<EntityKind, int> EntityCaps = new Dictionary<EntityKind, int>
        {
            { EntityKind.Ship, 200 }, { EntityKind.Bird, 500 }, { EntityKind.Fish, 300 },
            { EntityKind.Ground, 60 }, { EntityKind.Cart, 120 }, { EntityKind.Train, 40 },
            { EntityKind.Plane, 80 }, { EntityKind.Building, 800 }
        };
        /// <summary>逼近上限 90% 即暂停该类生成（硬上限前留 10% 余量给手动操作/视图重建）。</summary>
        public const float NearCapRatio = 0.9f;

        /// <summary>实体生成统一闸门：内存 Critical 或该类实体逼近上限（90%）即关闭。未登记类别放行。</summary>
        public static bool EntitySpawnGate(EntityKind kind, int currentCount)
        {
            if (!GlobalSpawnGate) return false;
            if (!EntityCaps.TryGetValue(kind, out int cap)) return true;
            return currentCount < cap * NearCapRatio;
        }

        /// <summary>实体上限（探针用，0 表示无登记）。</summary>
        public static int CapOf(EntityKind kind) => EntityCaps.TryGetValue(kind, out int c) ? c : 0;

        // ---- 采样结果（只读，供探针/UI/性能神）----
        public long ManagedBytes { get; private set; }        // 托管堆字节
        public long NativeAllocatedBytes { get; private set; }// 原生已分配字节
        public long LastWindowAlloc { get; private set; }     // 最近采样窗托管增量（≈GC Alloc / 5s）
        public float AllocPerSec => LastWindowAlloc / SampleInterval;
        public long MeshBytes { get; private set; }           // 运行时网格内存（资源卸载校验）
        public int MeshCount { get; private set; }
        public long TextureBytes { get; private set; }        // 运行时纹理内存
        public int TextureCount { get; private set; }

        // ---- 慢系统采样（GameManager 主循环每帧喂 ProfileTick）----
        sealed class SysProfile { public float TotalMs; public int Frames; public float MaxMs; }
        readonly Dictionary<string, SysProfile> _sys = new Dictionary<string, SysProfile>(48);
        readonly List<KeyValuePair<string, float>> _top = new List<KeyValuePair<string, float>>(8);
        const int SysWindow = 120;                            // 每 120 帧汇总一次
        int _sysFrames;
        public string TopSystems { get; private set; } = "";  // "NavalSystem:321,CombatSystem:198,..."
        public int SystemCount => _sys.Count;

        // ---- 对象池报告文本（每采样周期刷新，探针直接拼进返回串）----
        public string PoolReportText { get; private set; } = "";
        public int PoolBuckets { get; private set; }

        float _timer;
        long _prevManaged;
        float _lastGateLog;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Update()
        {
            if (Instance == null) Instance = this;
            _timer += Time.unscaledDeltaTime;
            if (_timer < SampleInterval) return;
            _timer = 0f;
            Sample();
        }

        /// <summary>采样一次内存水位 + 网格/纹理 + 池报告；临界时钳制并限频告警。</summary>
        private void Sample()
        {
            // 托管堆（System.GC 在 IL2CPP WebGL 可用，轻量）
            ManagedBytes = GC.GetTotalMemory(false);
            LastWindowAlloc = Math.Max(0L, ManagedBytes - _prevManaged);
            _prevManaged = ManagedBytes;

            // 原生内存（Unity 分配器总账；WebGL=wasm 线性内存占用）
            try { NativeAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(); }
            catch { NativeAllocatedBytes = ManagedBytes; }   // 极端环境回退

            // 运行时网格/纹理内存（资源卸载与泄漏校验；FindObjectsOfTypeAll 含隐藏资产，采样周期低频可接受）
            long meshB = 0, texB = 0; int meshN = 0, texN = 0;
            try
            {
                foreach (var m in Resources.FindObjectsOfTypeAll<Mesh>())
                {
                    if (m == null) continue;
                    meshN++; meshB += Profiler.GetRuntimeMemorySizeLong(m);
                }
                foreach (var t in Resources.FindObjectsOfTypeAll<Texture>())
                {
                    if (t == null) continue;
                    texN++; texB += Profiler.GetRuntimeMemorySizeLong(t);
                }
            }
            catch { /* Profiler API 受限时保持 0，不阻塞 */ }
            MeshBytes = meshB; MeshCount = meshN; TextureBytes = texB; TextureCount = texN;

            PoolReportText = GameObjectPool.ReportText();
            PoolBuckets = GameObjectPool.BucketCount();

            // 水位判定
            var lvl = NativeAllocatedBytes >= CriticalWaterBytes ? MemLevel.Critical
                    : NativeAllocatedBytes >= HighWaterBytes ? MemLevel.High
                    : MemLevel.Normal;
            if (lvl != Level)
            {
                Level = lvl;
                if (lvl == MemLevel.Critical) LogGate();
                Debug.Log("[MEM] 水位=" + lvl + " 原生=" + NativeAllocatedBytes + "B 托管=" + ManagedBytes + "B 网格=" + meshN + "/" + meshB + "B 纹理=" + texN + "/" + texB + "B 池=" + PoolReportText);
            }
            else if (Level == MemLevel.High && ManagedBytes > _prevManaged)
            {
                // 高位持续增长提示（不刷屏：仅在增速超 2%/窗时记录一次）
                if (LastWindowAlloc > ManagedBytes / 50L)
                    Debug.Log("[MEM] 高位增长中 原生=" + NativeAllocatedBytes + "B 托管+" + LastWindowAlloc + "B/5s");
            }
        }

        /// <summary>临界钳制告警（限频 30s，避免每 5s 刷屏）。</summary>
        private void LogGate()
        {
            if (Time.unscaledTime - _lastGateLog < 30f) return;
            _lastGateLog = Time.unscaledTime;
            Debug.LogWarning("[MEM] 临界水位：自动生成已钳制（敌船/鸟/鱼补员暂停）→ 请降低倍速或存档后重启页面");
        }

        /// <summary>GameManager 主循环每帧喂系统耗时（float 毫秒，用 Time.realtimeSinceStartup 差值，零分配）。</summary>
        public void ProfileTick(string sysName, float ms)
        {
            if (!_sys.TryGetValue(sysName, out var p)) { p = new SysProfile(); _sys[sysName] = p; }
            p.TotalMs += ms; p.Frames++; if (ms > p.MaxMs) p.MaxMs = ms;

            _sysFrames++;
            if (_sysFrames < SysWindow) return;
            _sysFrames = 0;

            _top.Clear();
            foreach (var kv in _sys) _top.Add(new KeyValuePair<string, float>(kv.Key, kv.Value.TotalMs));
            _top.Sort((a, b) => b.Value.CompareTo(a.Value));

            var sb = new StringBuilder(96);
            int n = Mathf.Min(5, _top.Count);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(_top[i].Key).Append(':').Append(Mathf.RoundToInt(_top[i].Value));
            }
            TopSystems = sb.ToString();
            foreach (var kv in _sys) { kv.Value.TotalMs = 0f; kv.Value.MaxMs = 0f; }
        }

        /// <summary>探针串（WebMemoryProbe 与 Debug 共用）：一行文本含水位/内存/钳制/网格纹理/池/慢系统。</summary>
        public string Probe()
        {
            return "lvl:" + Level
                + "|native:" + NativeAllocatedBytes
                + "|managed:" + ManagedBytes
                + "|alloc5s:" + LastWindowAlloc
                + "|gate:" + GlobalSpawnGate
                + "|mesh:" + MeshCount + "/" + MeshBytes
                + "|tex:" + TextureCount + "/" + TextureBytes
                + "|pools:" + PoolReportText
                + "|top:" + TopSystems;
        }
    }
}
