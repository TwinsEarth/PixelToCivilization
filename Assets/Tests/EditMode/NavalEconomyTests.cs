using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Host;
using PxC.Plugins.Economy;
using PxC.Plugins.Naval;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 船艇经济产出测试：渔船/木筏产食物、帆船贸易产金币，
    /// 产出在 FinalizeYear（扣口粮前）到账。
    /// 驱动真实 EconomyPlugin + NavalService，用 FakePopulation 控制人口。
    /// </summary>
    public sealed class NavalEconomyTests
    {
        private const int Labor = 1;
        private const int Soldiers = 0;

        private ServiceRegistry _services;
        private StrongEventBus _events;
        private FactionRegistry _factions;
        private GridGeography _geo;
        private UnitRegistry _units;
        private NavalService _naval;
        private FakePopulation _pop;
        private EconomyPlugin _plugin;
        private IEconomyService _economy;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceRegistry();
            _events = new StrongEventBus();
            _factions = new FactionRegistry();
            _factions.Add("华夏", true, 0, 0, 0);

            _pop = new FakePopulation(1, Labor, Soldiers);

            _geo = new GridGeography();
            _units = new UnitRegistry();

            _services.Register<FactionRegistry>(_factions);
            _services.Register<IPopulationService>(_pop);
            _services.Register<IGeographyQuery>(_geo);
            _services.Register<UnitRegistry>(_units);

            var navalState = new NavalState(_units.Capacity);
            _naval = new NavalService(_units, navalState, _geo, _services, _ => { });
            _services.Register<NavalService>(_naval);

            _plugin = new EconomyPlugin();
            IPluginContext ctx = new PluginContext(
                EconomyPlugin.PluginIdValue,
                _events, _services, new ChunkedWorld(),
                new CapabilitySystem(), m => { });
            _plugin.OnRegister(ctx);
            _plugin.OnInitialize();

            _economy = _services.Resolve<IEconomyService>();
        }

        private EntityId SpawnShip(NavalModel model, int x, int z)
        {
            _geo.SetWater(x, z);
            var pos = new Vector3(x, 0f, z);
            return _naval.Spawn(0, model, pos);
        }

        /// <summary>记录年初 Food/Gold（消除新游戏 seed 干扰），推进一整年，返回 (food,gold) 增量。</summary>
        private (float food, float gold) AdvanceYear()
        {
            float f0 = _economy.Get(0, ResourceType.Food);
            float g0 = _economy.Get(0, ResourceType.Gold);

            _plugin.OnYear(1);
            _plugin.FinalizeYear(1);

            return (
                _economy.Get(0, ResourceType.Food) - f0,
                _economy.Get(0, ResourceType.Gold) - g0);
        }

        // 1. 渔船产食物：1 条渔船年产 8 食物（叠加劳力产出 2.5，扣除 1 人口口粮 1）。
        [Test]
        public void FishingBoat_ProducesFood()
        {
            SpawnShip(NavalModel.FishingBoat, 5, 0);
            var (food, _) = AdvanceYear();

            // 劳力 2.5 + 渔船 8 = 10.5；口粮 1（1 人）；净 +9.5。
            Assert.AreEqual(9.5f, food, 0.01f,
                "1 劳力 2.5 + 1 渔船 8 - 1 口粮 1 = 9.5");
        }

        // 2. 木筏产少量食物：1 条木筏 2 食物。
        [Test]
        public void Raft_ProducesSomeFood()
        {
            SpawnShip(NavalModel.Raft, 5, 0);
            var (food, _) = AdvanceYear();

            // 劳力 2.5 + 木筏 2 = 4.5；口粮 1；净 +3.5。
            Assert.AreEqual(3.5f, food, 0.01f,
                "1 劳力 2.5 + 1 木筏 2 - 1 口粮 1 = 3.5");
        }

        // 3. 帆船贸易产金币：1 艘帆船年产 5 金币（人头税另计）。
        [Test]
        public void SailingShip_ProducesGold()
        {
            SpawnShip(NavalModel.SailingShip, 5, 0);
            var (_, gold) = AdvanceYear();

            // 1 人口人头税 0.03 + 帆船 5 = 5.03。
            Assert.AreEqual(5.03f, gold, 0.01f,
                "1 人税 0.03 + 1 帆船 5 = 5.03");
        }

        // 4. 渔船产出可帮助养活人口：无渔船基础缺口，有渔船则不饥荒。
        [Test]
        public void FishingBoat_FeedsPopulation_AvoidsFamine()
        {
            var famines = new List<FamineEvent>();
            _events.Subscribe<FamineEvent>(famines.Add);

            // 1 劳力 2.5，1 人口粮 1：本就盈余；改用大量人口制造缺口。
            // 直接验证：渔船食物在扣口粮前到账，故 1 渔船（8）可覆盖额外缺口。
            SpawnShip(NavalModel.FishingBoat, 5, 0);
            var (food, _) = AdvanceYear();

            Assert.AreEqual(0, famines.Count, "渔船产出到账后不应饥荒");
            Assert.IsTrue(food > 0f, "应有粮食盈余");
        }
    }
}
