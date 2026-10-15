using System;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Culture
{
    /// <summary>
    /// 文化服务：依据累积文化值刷新文化等级，计算影响力与文化胜利进度。
    /// </summary>
    public sealed class CultureService : ICultureService
    {
        /// <summary>
        /// 文化等级里程碑（累计文化值）。索引=等级，最高等级=数组长度-1。
        /// </summary>
        private static readonly float[] _thresholds =
        {
            0f, 500f, 2000f, 6000f, 15000f, 35000f,
            70000f, 150000f, 300000f, 600000f, 1000000f
        };

        /// <summary>最高文化等级（文化胜利目标）。</summary>
        public static readonly int MaxLevel = _thresholds.Length - 1;

        private readonly CultureState _state;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public CultureService(CultureState state, IServiceLocator services, Action<string> log)
        {
            _state = state;
            _services = services;
            _log = log;
        }

        public float Value(int faction)
        {
            if (_services.TryResolve<IEconomyService>(out var eco))
                return eco.Get(faction, ResourceType.Culture);
            return 0f;
        }

        public int Level(int faction) => _state.GetLevel(faction);

        public float Influence(int faction)
        {
            float influence = _state.GetLevel(faction) * 10f;
            return influence > 100f ? 100f : influence;
        }

        public float VictoryProgress(int faction)
        {
            float target = _thresholds[MaxLevel];
            float p = Value(faction) / target;
            if (p < 0f) p = 0f;
            if (p > 1f) p = 1f;
            return p;
        }

        public bool YearlyUpdate(int faction)
        {
            float value = Value(faction);
            int newLevel = LevelForValue(value);
            int oldLevel = _state.GetLevel(faction);
            if (newLevel <= oldLevel) return false;

            _state.SetLevel(faction, newLevel);
            _services.Resolve<IEventBus>()?.Publish(new CultureLevelUpEvent
            {
                Faction = faction,
                NewLevel = newLevel,
                OldLevel = oldLevel,
                GameYear = ResolveYear(),
            });
            _log?.Invoke($"[文化] 势力 {faction} 文化升至 {newLevel} 级（影响力 {Influence(faction):0}）");
            return true;
        }

        public static int LevelForValue(float value)
        {
            int level = 0;
            for (int i = 0; i < _thresholds.Length; i++)
            {
                if (value >= _thresholds[i]) level = i;
                else break;
            }
            return level;
        }

        private int ResolveYear()
        {
            if (_services.TryResolve<IClock>(out var clock))
                return clock.GameYear;
            return 0;
        }
    }
}
