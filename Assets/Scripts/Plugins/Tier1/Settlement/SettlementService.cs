using System;
using UnityEngine;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Settlement
{
    /// <summary>
    /// ISettlementService 实现（v10.4.0 治本统一）。人口由 population 插件统一
    /// （聚落 × 分层）：建立首都后调用 SeedCapital 写入 100 人；本服务只做
    /// 发展度累积与自动升级，人口查询全部委托 IPopulationService。
    /// </summary>
    public sealed class SettlementService : ISettlementService
    {
        private readonly SettlementRegistry _settlements;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;
        private IPopulationService _pop;

        public SettlementService(
            SettlementRegistry settlements,
            IServiceLocator services,
            Action<string> log)
        {
            _settlements = settlements;
            _services = services;
            _log = log;
        }

        /// <summary>缓存人口服务（OnInitialize 时 population 已注册）。</summary>
        public void BindPopulation()
        {
            if (_services != null && _services.TryResolve<IPopulationService>(out var pop))
                _pop = pop;
        }

        // ------------------------------------------------------------
        // 新游戏：为每个势力建立首都，并写入统一初始人口
        // ------------------------------------------------------------
        public void FoundInitialCapitals(FactionRegistry factions, int gameYear)
        {
            BindPopulation();
            for (int i = 0; i < factions.Count; i++)
            {
                var f = factions.Get(i);
                if (f == null) continue;
                var pos = new Vector3(f.HomeX, 0f, f.HomeZ);
                string name = f.Name + "聚落";
                int slot = _settlements.Found(
                    f.Id,
                    SettlementKind.Capital,
                    name,
                    pos,
                    gameYear,
                    SettlementProfile.InitialCapitalProsperity);

                // 首都建立后写入统一初始人口（100 人，20/55/8/8/4/5）。
                _pop?.SeedCapital(slot);
            }
        }

        // ------------------------------------------------------------
        // 查询
        // ------------------------------------------------------------
        public int Count(int faction) => _settlements.CountByFaction(faction);

        public int GetCapital(int faction) => _settlements.FindCapital(faction);

        public int GetLevel(int slot)
        {
            var r = _settlements.GetSafe(slot);
            return r.Alive ? r.Level : 0;
        }

        public int GetPopulation(int slot)
        {
            if (!_settlements.GetSafe(slot).Alive) return 0;
            return _pop != null ? _pop.SettlementTotal(slot) : 0;
        }

        public int TotalPopulation(int faction)
        {
            return _pop != null ? _pop.Total(faction) : 0;
        }

        public float GetProsperity(int slot) => _settlements.GetSafe(slot).Prosperity;

        public float GetTerritoryRadius(int slot)
        {
            var r = _settlements.GetSafe(slot);
            return r.Alive ? SettlementProfile.TerritoryRadius(r.Level) : 0f;
        }

        public float BaseTerritoryRadius => SettlementProfile.BaseTerritoryRadius;

        public float DevelopmentScore(int faction)
        {
            // 发展度 = 势力总人口（统一人口）+ 建筑加权。
            float score = 0f;
            if (_pop != null) score += _pop.Total(faction);
            if (_services != null && _services.TryResolve<IBuildingService>(out var bld))
                score += bld.TotalCount(faction);
            return score;
        }

        // ------------------------------------------------------------
        // 年度发展（只做发展度累积 + 升级；人口已由 population 结算）
        // ------------------------------------------------------------
        public void AdvanceYear(int gameYear)
        {
            BindPopulation();
            int cap = _settlements.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_settlements.IsAlive(slot, _settlements.GetGeneration(slot))) continue;
                ref var r = ref _settlements.GetSlotRef(slot);

                // 发展度累积（人口取自统一人口服务）
                int population = _pop != null ? _pop.SettlementTotal(slot) : 0;
                r.Prosperity +=
                    SettlementProfile.ProsperityBase +
                    population * SettlementProfile.ProsperityPerCapita;

                // 自动升级
                while (r.Level < SettlementProfile.MaxLevel &&
                       r.Prosperity >= SettlementProfile.UpgradeRequirement(r.Level))
                {
                    r.Prosperity -= SettlementProfile.UpgradeRequirement(r.Level);
                    r.Level++;
                    _log?.Invoke($"[聚落] {r.Name} 于 {gameYear} 年升至 {r.Level} 级（人口 {population}）");
                }
            }
        }
    }
}
