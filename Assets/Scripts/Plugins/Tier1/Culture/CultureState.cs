using System;

namespace PxC.Plugins.Culture
{
    /// <summary>
    /// 文化领域状态（纯数据）：每势力当前文化等级（里程碑）。
    /// 文化值本身实时读取 IEconomyService 的 ResourceType.Culture，不重复存储。
    /// </summary>
    public sealed class CultureState
    {
        private int[] _level;
        private int _factionCount;

        public void Bind(int factionCount)
        {
            _factionCount = factionCount;
            _level = new int[factionCount];
        }

        public int FactionCount => _factionCount;

        public int GetLevel(int f) => _level[f];
        public void SetLevel(int f, int level) => _level[f] = level;

        public void Restore(int[] levels, int factionCount)
        {
            _factionCount = factionCount;
            _level = new int[factionCount];
            if (levels != null)
            {
                for (int i = 0; i < factionCount && i < levels.Length; i++)
                    _level[i] = levels[i];
            }
        }
    }
}
