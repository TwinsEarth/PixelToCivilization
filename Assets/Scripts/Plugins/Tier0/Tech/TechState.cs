namespace PxC.Plugins.Tech
{
    /// <summary>时代数（=Era 枚举项数）。</summary>
    public static class Eras
    {
        public const int Count = 10;
    }

    /// <summary>
    /// 科技 SoA：每势力当前时代（int[]）与当前时代内已投入研究（float[]）。
    /// 纯数据、连续存储；研究点由 economy 产出 Research，科技消耗其推进时代。
    /// </summary>
    public sealed class TechState
    {
        private int[] _era;       // 每势力当前时代
        private float[] _progress; // 每势力当前时代内进度
        private int _factionCount;

        public void Bind(int factionCount)
        {
            _factionCount = factionCount;
            _era = new int[factionCount];
            _progress = new float[factionCount];
        }

        public int FactionCount => _factionCount;
        public int[] EraArray => _era;
        public float[] ProgressArray => _progress;

        public void Restore(int[] era, float[] progress, int factionCount)
        {
            _factionCount = factionCount;
            _era = era ?? new int[factionCount];
            _progress = progress ?? new float[factionCount];
        }

        public int GetEra(int faction) => _era[faction];

        public float GetProgress(int faction) => _progress[faction];

        /// <summary>是否已达到某时代（当前时代 >= era）。</summary>
        public bool HasEra(int faction, int era) => _era[faction] >= era;

        /// <summary>向当前时代投入研究点，返回溢出（可用于跨时代）。</summary>
        public void AddProgress(int faction, float amount)
        {
            _progress[faction] += amount;
            if (_progress[faction] < 0f) _progress[faction] = 0f;
        }

        /// <summary>直接设置时代（迁移/推进），并重置新时代进度。</summary>
        public void SetEra(int faction, int era, float carryProgress)
        {
            _era[faction] = era;
            _progress[faction] = carryProgress < 0f ? 0f : carryProgress;
        }
    }
}
