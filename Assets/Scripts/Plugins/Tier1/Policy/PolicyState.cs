using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Policy
{
    /// <summary>
    /// 政策领域状态（纯数据）：每势力当前国策、上次切换年份。
    /// 系数表在 PolicyService 中，不持有运行时引用。
    /// </summary>
    public sealed class PolicyState
    {
        public const int DefaultPolicy = (int)PolicyType.Rest;
        public const int NeverSwitched = -1;

        private int[] _current;
        private int[] _lastSwitch;
        private int _factionCount;

        public void Bind(int factionCount)
        {
            _factionCount = factionCount;
            _current = new int[factionCount];
            _lastSwitch = new int[factionCount];
            for (int i = 0; i < factionCount; i++)
            {
                _current[i] = DefaultPolicy;
                _lastSwitch[i] = NeverSwitched;
            }
        }

        public int FactionCount => _factionCount;

        public int Current(int f) => _current[f];
        public int LastSwitch(int f) => _lastSwitch[f];

        public void Set(int f, int policy, int gameYear)
        {
            _current[f] = policy;
            _lastSwitch[f] = gameYear;
        }

        /// <summary>读档：恢复两段数组（长度不足按现状）。</summary>
        public void Restore(int[] current, int[] lastSwitch, int factionCount)
        {
            _factionCount = factionCount;
            _current = new int[factionCount];
            _lastSwitch = new int[factionCount];
            for (int i = 0; i < factionCount; i++)
            {
                _current[i] = (current != null && i < current.Length)
                    ? current[i] : DefaultPolicy;
                _lastSwitch[i] = (lastSwitch != null && i < lastSwitch.Length)
                    ? lastSwitch[i] : NeverSwitched;
            }
        }
    }
}
