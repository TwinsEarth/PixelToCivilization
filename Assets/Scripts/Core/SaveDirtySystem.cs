using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.7.0 存档脏标记系统（dirty flag 自动存档节流）。
    /// 原则：只有数据变更才触发自动存档，避免无变化时反复写盘。
    /// 实现：对关键状态（年份/人口/资源头部/建筑数/天数/朝代）做轻量哈希指纹，
    /// 每次 UpdateCheck 对比指纹，变化即 MarkDirty；SaveSystem 自动档触发前查询
    /// ShouldSkipAutoSave()，非脏则跳过本轮自动保存（同时重置定时器，避免忙轮询）。
    /// 兼容铁律：不改变存档格式/schema；纯节流优化，手动存档与紧急存档不受影响。
    /// </summary>
    public static class SaveDirtySystem
    {
        /// <summary>是否启用脏检查（默认启用；Debug 面板可关）。</summary>
        public static bool Enabled = true;

        static bool _dirty = true;          // 初始为脏（首次必然存档）
        static long _lastHash;
        static int _skipCount, _saveCount;

        /// <summary>是否已脏（有未保存变更）。</summary>
        public static bool IsDirty => _dirty;

        /// <summary>累计跳过的自动存档次数（探针/调试）。</summary>
        public static int SkipCount => _skipCount;

        /// <summary>累计执行的自动存档次数。</summary>
        public static int SaveCount => _saveCount;

        /// <summary>对关键状态算轻量指纹（FNV-1a64 思路，纯 long 运算零分配）。</summary>
        public static long Fingerprint(GameState state)
        {
            if (state == null) return 0L;
            long h = 1469598103934665603L;
            h ^= state.Year; h *= 1099511628211L;
            h ^= (int)(state.Day * 10f);
            h ^= state.Pop; h *= 1099511628211L;
            h ^= (long)(state.Housing * 100d); h *= 1099511628211L;
            double gold = state.Res.TryGetValue("gold", out var g) ? g : 0d;
            double food = state.Res.TryGetValue("food", out var f) ? f : 0d;
            h ^= (long)(gold * 1000d) ^ (long)(food * 1000d);
            h *= 1099511628211L;
            h ^= (long)state.DynastyIdx << 8;
            return h;
        }

        /// <summary>每帧调用：计算指纹，变化即标记脏。</summary>
        public static void UpdateCheck(GameState state)
        {
            if (!Enabled || state == null) return;
            long h = Fingerprint(state);
            if (h != _lastHash)
            {
                _lastHash = h;
                _dirty = true;
            }
        }

        /// <summary>自动档触发前查询：非脏则跳过（返回 true=跳过本轮自动保存）。</summary>
        public static bool ShouldSkipAutoSave()
        {
            if (!Enabled) { _saveCount++; return false; }
            if (!_dirty) { _skipCount++; return true; }
            _saveCount++;
            return false;
        }

        /// <summary>存档完成后调用：清除脏标记。</summary>
        public static void MarkClean()
        {
            _dirty = false;
        }

        /// <summary>手动存档/紧急存档前调用：强制视为脏（不跳过）。</summary>
        public static void ForceDirty()
        {
            _dirty = true;
        }

        /// <summary>探针：脏状态 + 跳过/保存计数。</summary>
        public static string Probe()
        {
            return "dirty:on=" + (Enabled ? "1" : "0") +
                   "|d=" + (_dirty ? "1" : "0") +
                   "|skip=" + _skipCount +
                   "|save=" + _saveCount;
        }
    }
}
