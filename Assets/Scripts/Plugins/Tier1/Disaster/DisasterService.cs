using System;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Disaster
{
    /// <summary>
    /// 灾害服务：订阅 WeatherChangedEvent，在当年天气确认后（同步回调）检测灾害，
    /// 施加人口/建筑/粮食影响并发布 DisasterOccurredEvent。
    /// 灾害为全球性（影响所有存活势力），按 faction 的影响接口保留区域扩展。
    /// </summary>
    public sealed class DisasterService : IDisasterService
    {
        private readonly DisasterState _state;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public DisasterService(DisasterState state, IServiceLocator services, Action<string> log)
        {
            _state = state;
            _services = services;
            _log = log;
        }

        public DisasterType Active => (DisasterType)_state.Active;
        public float Severity => _state.Severity;
        public int ActiveYear => _state.ActiveYear;

        /// <summary>天气事件回调（Weather 当年确认后同步调用）。</summary>
        public void OnWeatherChanged(WeatherChangedEvent e)
        {
            RollAndApply(e.GameYear, (WeatherCondition)e.NewWeather);
        }

        /// <summary>
        /// 年度检测并施加影响。返回是否发生灾害。
        /// 事件驱动为主（OnWeatherChanged）；此方法也可供测试/外部直接调用。
        /// </summary>
        public bool YearlyCheck(int gameYear)
        {
            var w = WeatherCondition.Clear;
            if (_services.TryResolve<IWeatherService>(out var weather))
                w = weather.Current;
            return RollAndApply(gameYear, w);
        }

        private bool RollAndApply(int gameYear, WeatherCondition w)
        {
            // 更新连续无雨年数（降水归零）
            if (w == WeatherCondition.Rain || w == WeatherCondition.Storm || w == WeatherCondition.Snow)
                _state.DryYears = 0;
            else
                _state.DryYears++;

            float lat = PlayerLatitude01();
            var type = PickDisaster(w, lat, out float severity);
            if (type == DisasterType.None)
            {
                _state.Active = (int)DisasterType.None;
                _state.Severity = 0f;
                _state.ActiveYear = gameYear;
                return false;
            }

            _state.Active = (int)type;
            _state.Severity = severity;
            _state.ActiveYear = gameYear;

            ApplyToAllFactions(type, severity, gameYear);
            _log?.Invoke($"[灾害] {gameYear} 年发生 {type}（严重度 {severity:0.00}）");
            return true;
        }

        private DisasterType PickDisaster(WeatherCondition w, float lat, out float severity)
        {
            severity = 0f;

            // 干旱：连续 ≥3 年无有效降水
            if (_state.DryYears >= 3 && _state.Next() < 0.55f)
            {
                severity = _state.Range(0.45f, 0.90f);
                return DisasterType.Drought;
            }

            // 暴风雨 → 低纬度台风 / 否则洪水
            if (w == WeatherCondition.Storm)
            {
                if (lat < 0.45f && _state.Next() < 0.55f)
                {
                    severity = _state.Range(0.45f, 0.95f);
                    return DisasterType.Typhoon;
                }
                if (_state.Next() < 0.50f)
                {
                    severity = _state.Range(0.35f, 0.80f);
                    return DisasterType.Flood;
                }
            }

            // 大雪 + 高纬度 → 寒潮
            if (w == WeatherCondition.Snow && lat > 0.55f && _state.Next() < 0.45f)
            {
                severity = _state.Range(0.35f, 0.80f);
                return DisasterType.ColdWave;
            }

            // 地震（随机，与天气无关）
            if (_state.Next() < 0.05f)
            {
                severity = _state.Range(0.30f, 0.85f);
                return DisasterType.Earthquake;
            }

            // 瘟疫（随机，略高概率，与人口聚集相关）
            if (_state.Next() < 0.06f)
            {
                severity = _state.Range(0.30f, 0.75f);
                return DisasterType.Plague;
            }

            return DisasterType.None;
        }

        // ------------------------------------------------------------
        // 施加影响
        // ------------------------------------------------------------
        private void ApplyToAllFactions(DisasterType type, float severity, int gameYear)
        {
            var factions = _services.Resolve<FactionRegistry>();
            if (factions == null) return;

            bool hasEco = _services.TryResolve<IEconomyService>(out var eco);
            bool hasPop = _services.TryResolve<IPopulationService>(out var pop);
            bool hasBld = _services.TryResolve<IBuildingService>(out var bld);

            for (int f = 0; f < factions.Count; f++)
            {
                if (!factions.Get(f).Alive) continue;

                int deaths = 0;
                int buildings = 0;
                float foodLost = 0f;

                // 粮食损失
                if (hasEco)
                {
                    float foodRatio = FoodLossRatio(type, severity);
                    if (foodRatio > 0f)
                    {
                        float stock = eco.Get(f, ResourceType.Food);
                        foodLost = stock * foodRatio;
                        if (foodLost > 0f) eco.Add(f, ResourceType.Food, -foodLost);
                    }
                }

                // 建筑摧毁
                if (hasBld)
                {
                    int n = BuildingLoss(type, severity);
                    if (n > 0) buildings = bld.DestroyByDisaster(f, n);
                }

                // 人口死亡
                if (hasPop)
                {
                    float popRatio = PopulationLossRatio(type, severity);
                    if (popRatio > 0f)
                    {
                        int total = pop.Total(f);
                        int d = (int)Math.Round(total * popRatio);
                        if (d > 0) deaths = pop.ApplyExcessDeaths(f, d);
                    }
                }

                _services.Resolve<IEventBus>()?.Publish(new DisasterOccurredEvent
                {
                    Type = (int)type,
                    Faction = f,
                    Severity = severity,
                    GameYear = gameYear,
                    Deaths = deaths,
                    BuildingsDestroyed = buildings,
                    FoodLost = foodLost,
                });
            }
        }

        private static float FoodLossRatio(DisasterType t, float s)
        {
            switch (t)
            {
                case DisasterType.Drought: return 0.50f + 0.40f * s;
                case DisasterType.Flood: return 0.20f + 0.25f * s;
                case DisasterType.Typhoon: return 0.15f + 0.25f * s;
                case DisasterType.ColdWave: return 0.10f + 0.20f * s;
                default: return 0f;
            }
        }

        private static int BuildingLoss(DisasterType t, float s)
        {
            switch (t)
            {
                case DisasterType.Earthquake: return 2 + (int)Math.Round(5 * s);
                case DisasterType.Typhoon: return 1 + (int)Math.Round(3 * s);
                case DisasterType.Flood: return 1 + (int)Math.Round(2 * s);
                default: return 0;
            }
        }

        private static float PopulationLossRatio(DisasterType t, float s)
        {
            switch (t)
            {
                case DisasterType.Plague: return 0.06f + 0.16f * s;
                case DisasterType.ColdWave: return 0.03f + 0.10f * s;
                case DisasterType.Earthquake: return 0.02f + 0.05f * s;
                default: return 0f;
            }
        }

        // ------------------------------------------------------------
        // 纬度
        // ------------------------------------------------------------
        private float PlayerLatitude01()
        {
            var factions = _services.Resolve<FactionRegistry>();
            if (factions == null || factions.Count <= 0) return 0.5f;
            var info = factions.Get(0);
            if (info == null) return 0.5f;
            float lat = (-info.HomeZ + 60f) / 120f;
            if (lat < 0f) lat = 0f;
            if (lat > 1f) lat = 1f;
            return lat;
        }
    }
}
