using System;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;

namespace PxC.Plugins.Victory
{
    /// <summary>
    /// 胜利服务：每年检测四种胜利条件，计算各路线进度。
    /// </summary>
    public sealed class VictoryService : IVictoryService
    {
        public const int PlayerFaction = 0;

        // 发展胜利阈值
        public const int DevelopmentPopulation = 3000;
        public const int DevelopmentBuildings = 400;

        private readonly VictoryState _state;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public VictoryService(VictoryState state, IServiceLocator services, Action<string> log)
        {
            _state = state;
            _services = services;
            _log = log;
        }

        public VictoryType AchievedType => (VictoryType)_state.AchievedType;
        public bool IsAchieved => _state.IsAchieved;

        public VictoryType Check(int gameYear)
        {
            if (_state.IsAchieved) return (VictoryType)_state.AchievedType;

            var factions = _services.Resolve<FactionRegistry>();
            if (factions == null) return VictoryType.None;

            // 玩家若已被淘汰，不触发胜利（保持 None）。
            var player = factions.Get(PlayerFaction);
            if (player == null || !player.Alive) return VictoryType.None;

            // 1. 征服：仅玩家存活
            if (factions.AliveCount == 1)
                return Complete(VictoryType.Conquest, gameYear, factions);

            // 2. 科技：进入太空时代
            if (_services.TryResolve<ITechService>(out var tech) &&
                tech.GetEra(PlayerFaction) == Era.Space)
                return Complete(VictoryType.Technology, gameYear, factions);

            // 3. 文化：达到最高文化等级
            if (_services.TryResolve<ICultureService>(out var culture) &&
                culture.VictoryProgress(PlayerFaction) >= 1f)
                return Complete(VictoryType.Culture, gameYear, factions);

            // 4. 发展：人口与建筑均达阈值
            if (_services.TryResolve<IPopulationService>(out var pop) &&
                _services.TryResolve<IBuildingService>(out var bld) &&
                pop.Total(PlayerFaction) >= DevelopmentPopulation &&
                bld.TotalCount(PlayerFaction) >= DevelopmentBuildings)
                return Complete(VictoryType.Development, gameYear, factions);

            return VictoryType.None;
        }

        private VictoryType Complete(VictoryType type, int gameYear, FactionRegistry factions)
        {
            _state.Set((int)type, gameYear);
            _services.Resolve<IEventBus>()?.Publish(new VictoryEvent
            {
                Faction = PlayerFaction,
                Type = (int)type,
                GameYear = gameYear,
                VictoryName = type.ToString(),
            });
            _log?.Invoke($"[胜利] 势力 {PlayerFaction} 于 {gameYear} 年达成 {type} 胜利！");
            return type;
        }

        public float ConquestProgress()
        {
            if (!_services.TryResolve<FactionRegistry>(out var factions)) return 0f;
            int total = factions.Count;
            if (total <= 1) return 1f;
            int eliminated = total - factions.AliveCount;
            float p = eliminated / (float)(total - 1);
            return Clamp01(p);
        }

        public float TechnologyProgress()
        {
            if (!_services.TryResolve<ITechService>(out var tech)) return 0f;
            float p = (int)tech.GetEra(PlayerFaction) / (float)Era.Space;
            return Clamp01(p);
        }

        public float CultureProgress()
        {
            if (!_services.TryResolve<ICultureService>(out var culture)) return 0f;
            return Clamp01(culture.VictoryProgress(PlayerFaction));
        }

        public float DevelopmentProgress()
        {
            float p = 0f;
            if (_services.TryResolve<IPopulationService>(out var pop))
                p += Clamp01(pop.Total(PlayerFaction) / (float)DevelopmentPopulation) * 0.5f;
            if (_services.TryResolve<IBuildingService>(out var bld))
                p += Clamp01(bld.TotalCount(PlayerFaction) / (float)DevelopmentBuildings) * 0.5f;
            return Clamp01(p);
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
