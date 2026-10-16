using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Host;
using PxC.Plugins.Economy;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 粮食饥荒时序测试（v10.4.2 治本）。
    /// 直接驱动真实 EconomyPlugin（PluginContext 为真实实现），用 FakePopulation 精确控制
    /// 劳力/士兵数量，验证两阶段年度结算：
    /// 1) OnYear（在 building 之前）只产出并缓存需求，不判定饥荒；
    /// 2) 建筑产出（farm）在 OnYear 后、FinalizeYear 前到账；
    /// 3) FinalizeYear（所有 sim OnYear 后）统一扣消耗，缺口才发 FamineEvent。
    /// </summary>
    public sealed class FamineTimingTests
    {
        // 人口口径：labor=10, soldiers=20（total=30）。
        // 粮食基础产出 = 10 * 2.5 = 25；需求 = 30*1.0 + 20*0.6 = 42；基础缺口 17。
        private const int Labor = 10;
        private const int Soldiers = 20;
        private const float FoodProduced = 25f;
        private const float FoodDemanded = 42f;
        private const float BaseShortfall = 17f;
        // 2 个农田：每个 12/年，共 24；25 + 24 = 49 > 42，补足（剩 7）。
        private const float TwoFarmsOutput = 24f;
        private const float ExpectedLeftover = 7f;

        private ServiceRegistry _services;
        private StrongEventBus _events;
        private FactionRegistry _factions;
        private FakePopulation _pop;
        private EconomyPlugin _plugin;
        private IEconomyService _economy;
        private readonly List<FamineEvent> _famine = new List<FamineEvent>();

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceRegistry();
            _events = new StrongEventBus();
            _factions = new FactionRegistry();
            _factions.Add("华夏", true, 0, 0, 0);

            _pop = new FakePopulation(1, Labor, Soldiers);

            _services.Register<FactionRegistry>(_factions);
            _services.Register<IPopulationService>(_pop);

            _plugin = new EconomyPlugin();
            IPluginContext ctx = new PluginContext(
                EconomyPlugin.PluginIdValue,
                _events, _services, new ChunkedWorld(),
                new CapabilitySystem(), m => { });
            _plugin.OnRegister(ctx);
            _plugin.OnInitialize();

            _economy = _services.Resolve<IEconomyService>();
            _events.Subscribe<FamineEvent>(e => _famine.Add(e));

            // Unity Test Runner 会复用测试类实例，字段初始化器只在首次执行；
            // SetUp 必须重置可变收集字段，避免上个用例的事件残留污染断言。
            _famine.Clear();

            // 消除新游戏 seed 100 粮的干扰，每个用例从 0 库存开始。
            ZeroFood();
        }

        private void ZeroFood()
        {
            float cur = _economy.Get(0, ResourceType.Food);
            if (cur > 0f) _economy.TrySpend(0, ResourceType.Food, cur);
        }

        // 1. 农田产出（建筑）在 FinalizeYear 判定前到账 → 不饥荒。
        [Test]
        public void FarmOutputArrivesBeforeFamineCheck_NoFamine()
        {
            _plugin.OnYear(1);
            Assert.AreEqual(0, _famine.Count, "OnYear 不应立即判定饥荒");

            // 模拟 2 个农田在 building.OnYear 阶段的产出（economy.Add Food）。
            _economy.Add(0, ResourceType.Food, TwoFarmsOutput);

            _plugin.FinalizeYear(1);

            Assert.AreEqual(0, _famine.Count, "农田产出到账后，总产出 49 >= 需求 42，不应饥荒");
            Assert.AreEqual(ExpectedLeftover, _economy.Get(0, ResourceType.Food), 0.01f,
                "剩余粮食 = 产出 49 - 需求 42 = 7");
        }

        // 2. 无农田、基础产出不足 → 仍饥荒（防止过度放松）。
        [Test]
        public void WithoutFarms_BaseShortfall_StillFamine()
        {
            _plugin.OnYear(1);
            // 不补农田产出。
            _plugin.FinalizeYear(1);

            Assert.AreEqual(1, _famine.Count, "无农田、基础产出 25 < 需求 42，应发 1 次饥荒");
            Assert.AreEqual(BaseShortfall, _famine[0].Shortfall, 0.01f,
                "缺口 = 需求 42 - 基础产出 25 = 17");
            Assert.AreEqual(0f, _economy.Get(0, ResourceType.Food), 0.001f,
                "饥荒后粮食库存钳制为 0");
        }

        // 3. FamineEvent 只在 FinalizeYear 后发出（时序）。
        [Test]
        public void FamineEventOnlyAfterFinalize()
        {
            _plugin.OnYear(1);
            Assert.AreEqual(0, _famine.Count, "OnYear 后、FinalizeYear 前，即使有缺口也不应发饥荒");
            Assert.AreEqual(FoodProduced, _economy.Get(0, ResourceType.Food), 0.01f,
                "OnYear 后粮食只有基础产出 25（尚未扣消耗）");

            _plugin.FinalizeYear(1);
            Assert.AreEqual(1, _famine.Count, "FinalizeYear 后才判定饥荒");
        }
    }
}
