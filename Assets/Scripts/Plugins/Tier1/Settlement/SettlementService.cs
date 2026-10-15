using System;
using UnityEngine;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Settlement
{
    /// <summary>
    /// ISettlementService 实现。聚落人口年度增长（受天气农业系数影响）、
    /// 发展度累积、达到阈值自动升级。新游戏时为每个势力建立首都。
    /// </summary>
    public sealed class SettlementService : ISettlementService
    {
        private readonly SettlementRegistry _settlements;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public SettlementService(
            SettlementRegistry settlements,
            IServiceLocator services,
            Action<string> log)
        {
            _settlements = settlements;
            _services = services;
            _log = log;
        }

        // ------------------------------------------------------------
        // 新游戏：为每个势力建立首都
        // ------------------------------------------------------------
        public void FoundInitialCapitals(FactionRegistry factions, int gameYear)
        {
            for (int i = 0; i < factions.Count; i++)
            {
                var f = factions.Get(i);
                if (f == null) continue;
                var pos = new Vector3(f.HomeX, 0f, f.HomeZ);
                string name = f.Name + "聚落";
                _settlements.Found(
                    f.Id,
                    SettlementKind.Capital,
                    name,
                    pos,
                    gameYear,
                    SettlementProfile.InitialCapitalPopulation,
                    SettlementProfile.InitialCapitalProsperity);
            }
        }

        // ------------------------------------------------------------
        // 查询
        // ------------------------------------------------------------
        public int Count(int faction) => _settlements.CountByFaction(faction);

        public int GetCapital(int faction) => _settlements.FindCapital(faction);

        public int GetLevel(int slot) =>
            _settlements.GetSafe(slot).Alive ? _settlements.GetSafe(slot).Level : 0;

        public int GetPopulation(int slot) => _settlements.GetSafe(slot).Population;

        public int TotalPopulation(int faction)
        {
            int sum = 0;
            int cap = _settlements.Capacity;
            for (int i = 0; i < cap; i++)
            {
                var r = _settlements.GetSafe(i);
                if (r.Alive && r.Faction == faction) sum += r.Population;
            }
            return sum;
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
            float score = 0f;
            int cap = _settlements.Capacity;
            for (int i = 0; i < cap; i++)
            {
                var r = _settlements.GetSafe(i);
                if (r.Alive && r.Faction == faction)
                    score += r.Level * r.Population / 100f;
            }
            return score;
        }

        // ------------------------------------------------------------
        // 年度发展
        // ------------------------------------------------------------
        public void AdvanceYear(int gameYear)
        {
            IWeatherService weather = null;
            if (_services != null) _services.TryResolve<IWeatherService>(out weather);

            int cap = _settlements.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                if (!_settlements.IsAlive(slot, _settlements.GetGeneration(slot))) continue;
                ref var r = ref _settlements.GetSlotRef(slot);

                // 人口增长（天气好则增长快；天气插件缺失用基准）
                float growth = SettlementProfile.BaseGrowthRate;
                if (weather != null)
                {
                    float crop = weather.CropYieldModifier(r.Faction);
                    if (!float.IsNaN(crop) && !float.IsInfinity(crop) && crop > 0f)
                        growth *= crop;
                }
                // logistic 饱和：接近人口软上限时增长降到最低系数，防止长跨度爆炸。
                float setSat = Math.Max(0f,
                    1f - (float)r.Population / (float)SettlementProfile.PopulationSoftCap);
                growth *= SettlementProfile.MinimumGrowthFactor +
                    (1f - SettlementProfile.MinimumGrowthFactor) * setSat;
                int added = (int)Math.Floor(r.Population * growth);
                if (added < 1) added = 1;
                r.Population += added;

                // 发展度累积
                r.Prosperity +=
                    SettlementProfile.ProsperityBase +
                    r.Population * SettlementProfile.ProsperityPerCapita;

                // 自动升级
                while (r.Level < SettlementProfile.MaxLevel &&
                       r.Prosperity >= SettlementProfile.UpgradeRequirement(r.Level))
                {
                    r.Prosperity -= SettlementProfile.UpgradeRequirement(r.Level);
                    r.Level++;
                    _log?.Invoke($"[聚落] {r.Name} 于 {gameYear} 年升至 {r.Level} 级（人口 {r.Population}）");
                }
            }
        }
    }
}
