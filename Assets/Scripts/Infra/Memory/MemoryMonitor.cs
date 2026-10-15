using System;
using System.Collections.Generic;
using PxC.Core.Services;

namespace PxC.Infra.Memory
{
    /// <summary>
    /// 内存监控：三级水位 High=1.1GB / Critical=1.5GB，
    /// 活动计数达上限 90% 或水位 High/Critical 时暂停生成。
    /// 内存取数用 Profiler（含 native）；快照需 Memory Profiler 包，未安装标未验证。
    /// </summary>
    public sealed class MemoryMonitor : IMemoryMonitor
    {
        private readonly Dictionary<string, Func<int>> _providers;
        private readonly Dictionary<string, int> _max;
        private readonly float _highMB;
        private readonly float _criticalMB;
        private Action<string> _logger;
        private long _managedBytes;
        private MemoryPressure _pressure;

        private const float CapacityPauseRatio = 0.9f;

        public MemoryMonitor(float highMB = 1100f, float criticalMB = 1500f, Action<string> logger = null)
        {
            _highMB = highMB;
            _criticalMB = criticalMB;
            _logger = logger;
            _providers = new Dictionary<string, Func<int>>(32);
            _max = new Dictionary<string, int>(32);
        }

        public MemoryPressure Pressure => _pressure;
        public long ManagedBytes => _managedBytes;
        public float UsedMB => _managedBytes / (1024f * 1024f);
        public float HighMB => _highMB;
        public float CriticalMB => _criticalMB;

        public bool CanSpawn(int estimatedCostMB = 0)
        {
            if (_pressure == MemoryPressure.High || _pressure == MemoryPressure.Critical)
                return false;
            if (UsedMB + estimatedCostMB >= _highMB)
                return false;

            foreach (var kv in _max)
            {
                if (!_providers.TryGetValue(kv.Key, out var provider)) continue;
                int count = provider();
                if (count >= (int)(kv.Value * CapacityPauseRatio))
                    return false;
            }
            return true;
        }

        public void RegisterLiveCount(string label, Func<int> provider)
        {
            _providers[label] = provider;
        }

        /// <summary>注册带容量上限的计数（达到 90% 暂停生成）。</summary>
        public void RegisterCapacity(string label, Func<int> provider, int max)
        {
            _providers[label] = provider;
            if (max > 0) _max[label] = max;
        }

        public void Tick()
        {
            long bytes = GC.GetTotalMemory(false);
            long profiler = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            if (profiler > 0) bytes = profiler;
            _managedBytes = bytes;

            float used = UsedMB;
            if (used >= _criticalMB) _pressure = MemoryPressure.Critical;
            else if (used >= _highMB) _pressure = MemoryPressure.High;
            else if (used >= _highMB * 0.8f) _pressure = MemoryPressure.Warning;
            else _pressure = MemoryPressure.Normal;
        }

        public void CaptureSnapshot(string label)
        {
            // Memory Profiler 包（com.unity.memoryprofiler）未安装，无法生成 .snap 快照
            _logger?.Invoke(
                $"[Memory] 快照请求 '{label}'：Memory Profiler 未安装（未验证）；Profiler={UsedMB:F1}MB，压力={_pressure}");
        }
    }
}
