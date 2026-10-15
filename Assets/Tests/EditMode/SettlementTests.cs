using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PxC.Host;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;
using PxC.Plugins.Population;
using PxC.Plugins.Settlement;
using PxC.Plugins.Colonization;

namespace PxC.Tests.EditMode
{
    // ----------------------------------------------------------------
    // 测试替身
    // ----------------------------------------------------------------
    public sealed class FakeEconomy : IEconomyService
    {
        private readonly Dictionary<ResourceType, float> _r =
            new Dictionary<ResourceType, float>();

        public void Set(ResourceType t, float v) => _r[t] = v;

        public float Get(int faction, ResourceType type) =>
            _r.TryGetValue(type, out float v) ? v : 0f;

        public void Add(int faction, ResourceType type, float amount)
        {
            _r.TryGetValue(type, out float v);
            _r[type] = v + amount;
        }

        public bool CanAfford(int faction, ResourceType type, float amount) =>
            Get(faction, type) >= amount;

        public bool TrySpend(int faction, ResourceType type, float amount)
        {
            if (!CanAfford(faction, type, amount)) return false;
            Add(faction, type, -amount);
            return true;
        }

        public bool CanAffordBundle(int faction, ResourceCost[] costs)
        {
            for (int i = 0; i < costs.Length; i++)
                if (Get(faction, costs[i].Type) < costs[i].Amount) return false;
            return true;
        }

        public bool TrySpendBundle(int faction, ResourceCost[] costs)
        {
            if (!CanAffordBundle(faction, costs)) return false;
            for (int i = 0; i < costs.Length; i++)
                Add(faction, costs[i].Type, -costs[i].Amount);
            return true;
        }
    }

    public sealed class FakeTech : ITechService
    {
        public int EraValue;
        public bool IsResearched(int faction, string techId) => false;
        public Era GetEra(int faction) => (Era)EraValue;
        public float GetProgress(int faction, string techId) => 0f;
        public bool HasEra(int faction, Era era) => EraValue >= (int)era;
    }

    // ----------------------------------------------------------------
    // 测试
    // ----------------------------------------------------------------
    public sealed class SettlementTests
    {
        private const int FactionCount = 2;

        private ServiceRegistry _services;
        private FactionRegistry _factions;
        private SettlementRegistry _settlements;
        private FakeEconomy _economy;
        private PopulationService _population;
        private FakeTech _tech;
        private GridGeography _geo;
        private StrongEventBus _events;
        private SettlementService _settlementService;
        private ColonizationService _colonization;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceRegistry();
            _factions = new FactionRegistry();
            _settlements = new SettlementRegistry();
            _economy = new FakeEconomy();
            _tech = new FakeTech();
            _geo = new GridGeography();
            _events = new StrongEventBus();

            // 真实人口：settlement × layer（v10.4.0 唯一权威）
            var populationState = new PopulationState();
            _population = new PopulationService(populationState, _settlements, _services);

            _factions.Add("华夏", true, 0, 0, 0);
            _factions.Add("东夷", false, 60, 10, 1);

            _services.Register<FactionRegistry>(_factions);
            _services.Register<SettlementRegistry>(_settlements);
            _services.Register<IEconomyService>(_economy);
            _services.Register<IPopulationService>(_population);
            _services.Register<ITechService>(_tech);
            _services.Register<IGeographyQuery>(_geo);
            _services.Register<IEventBus>(_events);

            _settlementService = new SettlementService(_settlements, _services, null);
            _colonization = new ColonizationService(_settlements, _services, _events, null);
        }

        /// <summary>模拟一个完整年份：先人口结算，再发展度（与插件 OnYear 同序）。</summary>
        private void RunYear(int year)
        {
            var births = new int[FactionCount];
            var deaths = new int[FactionCount];
            _population.SettlePopulationYear(FactionCount, births, deaths);
            _settlementService.AdvanceYear(year);
        }

        // 1. 初始首都：每个势力一个首都，统一人口 100
        [Test]
        public void InitialCapitals_OnePerFaction()
        {
            _settlementService.FoundInitialCapitals(_factions, 0);

            Assert.AreEqual(FactionCount, _settlements.Count);
            Assert.AreEqual(1, _settlementService.Count(0));
            Assert.AreEqual(1, _settlementService.Count(1));
            Assert.AreEqual(0, _settlementService.GetCapital(0));
            Assert.AreEqual(1, _settlementService.GetCapital(1));
            Assert.AreEqual(100, _settlementService.GetPopulation(0),
                "首都统一初始人口为 100（20/55/8/8/4/5）");
            Assert.AreEqual(100, _population.Total(0),
                "势力总人口=首都人口，无独立账本");
        }

        // 2. 年度发展：人口由 population 结算增长，发展度自动升级
        [Test]
        public void AdvanceYear_PopulationGrowsAndUpgrades()
        {
            _settlementService.FoundInitialCapitals(_factions, 0);
            int popBefore = _settlementService.GetPopulation(0);

            for (int y = 1; y <= 60; y++) RunYear(y);

            Assert.IsTrue(_settlementService.GetPopulation(0) > popBefore,
                "首都人口应逐年增长");
            Assert.IsTrue(_settlementService.GetLevel(0) >= 2,
                "60 年内首都应至少升级到 2 级");
        }

        // 3. 殖民消耗：建立新聚落扣资源、移民聚落间转移（势力总人口守恒）
        [Test]
        public void TryFound_SpendsResourcesAndMigrants()
        {
            _tech.EraValue = 1;
            _economy.Set(ResourceType.Food, 10000f);
            _economy.Set(ResourceType.Wood, 10000f);
            _economy.Set(ResourceType.Gold, 10000f);

            _settlementService.FoundInitialCapitals(_factions, 0);
            float foodBefore = _economy.Get(0, ResourceType.Food);
            int totalBefore = _population.Total(0);
            int capitalLaborBefore = _population.SettlementByLayer(0, PopulationLayer.Laborer);

            var site = new Vector3(200f, 0f, 200f);
            bool ok = _colonization.TryFoundSettlement(0, SettlementKind.Village, site, "新村落");

            Assert.IsTrue(ok, "资源充足应能建立村落");
            Assert.AreEqual(2, _settlements.CountByFaction(0));
            Assert.IsTrue(_economy.Get(0, ResourceType.Food) < foodBefore,
                "建立聚落应消耗粮食");
            Assert.IsTrue(_population.SettlementByLayer(0, PopulationLayer.Laborer) < capitalLaborBefore,
                "首都劳力应减少（派出移民）");
            Assert.AreEqual(8, _population.SettlementByLayer(2, PopulationLayer.Laborer),
                "新聚落应收到 8 名移民");
            Assert.AreEqual(totalBefore, _population.Total(0),
                "移民是聚落间转移，势力总人口守恒");
        }

        // 4. 领土归属
        [Test]
        public void GetOwnerAt_ReturnsFactionWithinTerritory()
        {
            _settlementService.FoundInitialCapitals(_factions, 0);

            Assert.AreEqual(0, _colonization.GetOwnerAt(new Vector3(10f, 0f, 10f)),
                "首都领土内归属势力0");
            Assert.AreEqual(-1, _colonization.GetOwnerAt(new Vector3(150f, 0f, 150f)),
                "领土外无归属");
        }

        // 5. 时代门控：早期不能建岛屿/星球聚落
        [Test]
        public void EraGate_BlocksIslandAndPlanetEarly()
        {
            _tech.EraValue = 0;
            _settlementService.FoundInitialCapitals(_factions, 0);

            Assert.IsFalse(_colonization.CanFound(
                0, SettlementKind.Island, new Vector3(300f, 0f, 300f), out _),
                "原始时代不能建岛屿聚落");
            Assert.IsFalse(_colonization.CanFound(
                0, SettlementKind.Planet, new Vector3(400f, 0f, 400f), out _),
                "原始时代不能建星球殖民地");
        }

        // 6. 太空时代可建星球殖民地
        [Test]
        public void EraGate_AllowsPlanetInSpace()
        {
            _tech.EraValue = 9;
            _economy.Set(ResourceType.Food, 100000f);
            _economy.Set(ResourceType.Steel, 100000f);
            _economy.Set(ResourceType.Data, 100000f);
            _settlementService.FoundInitialCapitals(_factions, 0);

            bool ok = _colonization.TryFoundSettlement(
                0, SettlementKind.Planet, new Vector3(500f, 0f, 500f), "火星基地");
            Assert.IsTrue(ok, "太空时代、资源充足应能建立星球殖民地");
        }

        // 7. 资源不足失败
        [Test]
        public void TryFound_FailsWhenResourcesShort()
        {
            _tech.EraValue = 1;
            _economy.Set(ResourceType.Food, 0f);
            _economy.Set(ResourceType.Wood, 0f);
            _settlementService.FoundInitialCapitals(_factions, 0);

            Assert.IsFalse(_colonization.TryFoundSettlement(
                0, SettlementKind.Village, new Vector3(200f, 0f, 200f), null),
                "资源不足时不能建立聚落");
            Assert.AreEqual(1, _settlements.CountByFaction(0),
                "失败不应增加聚落");
        }

        // 8. 水域不能建聚落
        [Test]
        public void TryFound_FailsOnWater()
        {
            _tech.EraValue = 1;
            _economy.Set(ResourceType.Food, 10000f);
            _economy.Set(ResourceType.Wood, 10000f);
            _settlementService.FoundInitialCapitals(_factions, 0);

            _geo.SetWater(200, 200);
            Assert.IsFalse(_colonization.TryFoundSettlement(
                0, SettlementKind.Village, new Vector3(200f, 0f, 200f), null),
                "水域不能建立聚落");
        }

        // 9. 自动扩张：人口繁衍到门槛后年度新增聚落
        [Test]
        public void AutoExpand_FoundsWhenPopulationReady()
        {
            _tech.EraValue = 1;
            _economy.Set(ResourceType.Food, 100000f);
            _economy.Set(ResourceType.Wood, 100000f);
            _economy.Set(ResourceType.Gold, 100000f);

            _settlementService.FoundInitialCapitals(_factions, 0);

            // 先繁衍 8 年，让首都人口超过 115 门槛（初始 100）。
            for (int y = 1; y <= 8; y++) RunYear(y);

            int founded = _colonization.AutoExpand(9);

            Assert.IsTrue(founded >= 1, "人口达到门槛的势力应在年度自动扩张");
            Assert.IsTrue(_settlements.CountByFaction(0) >= 2,
                "势力0应至少新增一个聚落");
        }
    }
}
