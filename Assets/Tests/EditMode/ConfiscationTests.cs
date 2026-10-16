using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Host;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Plugins.Confiscation;
using PxC.Plugins.Economy;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 灭国没收测试（v10.7.7）：
    /// 败者被灭国后全部国库资源转移给胜者；发布 ConfiscationEvent；
    /// 败者无资源时不产生任何转移与负值。
    /// </summary>
    public sealed class ConfiscationTests
    {
        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public EconomyState EcoState;
            public StrongEventBus Bus;
            public ConfiscationPlugin Plugin;
            public List<ConfiscationEvent> ConfiscationEvents;
        }

        private static Harness Build(int factionCount)
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < factionCount; i++)
                factions.Add("F" + i, false, i * 1000f, 0f, i);

            var ecoState = new EconomyState();
            ecoState.Bind(factionCount);
            var economy = new EconomyService(ecoState);
            services.Register<IEconomyService>(economy);
            services.Register<FactionRegistry>(factions);

            var bus = new StrongEventBus();
            var events = new List<ConfiscationEvent>();
            bus.Subscribe<ConfiscationEvent>(events.Add);

            var plugin = new ConfiscationPlugin();
            var ctx = new PluginContext(
                ConfiscationPlugin.PluginIdValue,
                bus, services, new ChunkedWorld(),
                new CapabilitySystem(), _ => { });
            plugin.OnRegister(ctx);
            plugin.OnInitialize();

            return new Harness
            {
                Services = services,
                Factions = factions,
                EcoState = ecoState,
                Bus = bus,
                Plugin = plugin,
                ConfiscationEvents = events,
            };
        }

        // ------------------------------------------------------------
        // 1. 灭国没收：败者全部国库资源转移给胜者，余额归零
        // ------------------------------------------------------------
        [Test]
        public void Eliminated_AllTreasuryTransferred()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Gold, 50f);
            h.EcoState.Set(1, ResourceType.Food, 100f);
            h.EcoState.Set(1, ResourceType.Iron, 20f);
            h.EcoState.Set(1, ResourceType.Capital, 30f);
            h.EcoState.Set(1, ResourceType.Coal, 7f);

            h.Bus.Publish(new FactionEliminatedEvent
            {
                Faction = 1,
                ByFaction = 0,
                GameYear = 10,
            });

            // 胜者应获得全部资源
            Assert.AreEqual(50f, h.EcoState.Get(0, ResourceType.Gold), 0.001f);
            Assert.AreEqual(100f, h.EcoState.Get(0, ResourceType.Food), 0.001f);
            Assert.AreEqual(20f, h.EcoState.Get(0, ResourceType.Iron), 0.001f);
            Assert.AreEqual(30f, h.EcoState.Get(0, ResourceType.Capital), 0.001f);
            Assert.AreEqual(7f, h.EcoState.Get(0, ResourceType.Coal), 0.001f);

            // 败者余额归零（不得残留死账）
            Assert.AreEqual(0f, h.EcoState.Get(1, ResourceType.Gold), 0.001f);
            Assert.AreEqual(0f, h.EcoState.Get(1, ResourceType.Food), 0.001f);
            Assert.AreEqual(0f, h.EcoState.Get(1, ResourceType.Iron), 0.001f);

            // 事件四类合计正确：Gold=50, Capital=30, Food=100, Strategic=Iron20+Coal7=27
            Assert.AreEqual(1, h.ConfiscationEvents.Count);
            var e = h.ConfiscationEvents[0];
            Assert.AreEqual(1, e.Loser);
            Assert.AreEqual(0, e.Winner);
            Assert.AreEqual(50f, e.Gold, 0.001f);
            Assert.AreEqual(30f, e.Capital, 0.001f);
            Assert.AreEqual(100f, e.Food, 0.001f);
            Assert.AreEqual(27f, e.Strategic, 0.001f);
        }

        // ------------------------------------------------------------
        // 2. 败者无资源：不转移、不发事件（或发零事件均可接受，此处断言不发事件）
        // ------------------------------------------------------------
        [Test]
        public void Eliminated_NoTreasury_NoTransfer()
        {
            var h = Build(2);
            h.EcoState.Set(0, ResourceType.Gold, 999f); // 胜者原有资源不受影响

            h.Bus.Publish(new FactionEliminatedEvent
            {
                Faction = 1,
                ByFaction = 0,
                GameYear = 10,
            });

            Assert.AreEqual(999f, h.EcoState.Get(0, ResourceType.Gold), 0.001f,
                "败者无资源，胜者余额不应变化");
            Assert.AreEqual(0, h.ConfiscationEvents.Count, "无资源可没收，不发布事件");
        }

        // ------------------------------------------------------------
        // 3. 战败国库只有部分资源：只转移存在部分，其余不受影响
        // ------------------------------------------------------------
        [Test]
        public void Eliminated_PartialTreasury_TransfersOnlyExisting()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Food, 42f);
            h.EcoState.Set(0, ResourceType.Gold, 5f);

            h.Bus.Publish(new FactionEliminatedEvent
            {
                Faction = 1,
                ByFaction = 0,
                GameYear = 10,
            });

            Assert.AreEqual(42f, h.EcoState.Get(0, ResourceType.Food), 0.001f);
            Assert.AreEqual(5f, h.EcoState.Get(0, ResourceType.Gold), 0.001f,
                "胜者原有金币不变（败者无金币）");
            Assert.AreEqual(0f, h.EcoState.Get(1, ResourceType.Food), 0.001f);

            Assert.AreEqual(1, h.ConfiscationEvents.Count);
            Assert.AreEqual(0f, h.ConfiscationEvents[0].Gold, 0.001f);
            Assert.AreEqual(42f, h.ConfiscationEvents[0].Food, 0.001f);
            Assert.AreEqual(0f, h.ConfiscationEvents[0].Strategic, 0.001f);
        }
    }
}
