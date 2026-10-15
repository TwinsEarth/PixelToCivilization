using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Host;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Plugins.Economy;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Military;
using PxC.Plugins.Naval;
using PxC.Plugins.Vassalization;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 战争附庸与纳贡测试：
    /// 极端碾压（&gt;3.5 倍）、弱者只剩首府（无可割让）、满 3 年 → 附庸化；
    /// 附庸每年纳贡、宗主保护附庸、宗主被灭附庸解放。
    /// 使用真实 GroundForceService / MilitaryService / EconomyService / NavalService。
    /// </summary>
    public sealed class VassalizationTests
    {
        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public GridGeography Geo;
            public UnitRegistry Units;
            public GroundForceService Ground;
            public NavalService Naval;
            public MilitaryService Military;
            public SettlementRegistry Settlements;
            public EconomyState EcoState;
            public StrongEventBus Bus;
            public VassalizationPlugin Plugin;
            public List<VassalizedEvent> VassalEvents;
            public List<WarDeclaredEvent> WarEvents;
        }

        private static Harness Build(int factionCount)
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < factionCount; i++)
                factions.Add("F" + i, false, i * 1000f, 0f, i);

            var geo = new GridGeography();
            services.Register<IGeographyQuery>(geo);

            var units = new UnitRegistry();
            services.Register<UnitRegistry>(units);

            var groundState = new GroundForceState(units.Capacity);
            var ground = new GroundForceService(units, groundState, geo, services, _ => { });
            services.Register<GroundForceService>(ground);

            var ecoState = new EconomyState();
            ecoState.Bind(factionCount);
            var economy = new EconomyService(ecoState);
            services.Register<IEconomyService>(economy);

            var navalState = new NavalState(units.Capacity);
            var naval = new NavalService(units, navalState, geo, services, _ => { });
            services.Register<NavalService>(naval);

            var pop = new FakePopulation(factionCount, 100, 0);
            services.Register<IPopulationService>(pop);

            var military = new MilitaryService(ground, naval, pop);
            services.Register<IMilitaryService>(military);

            var settlements = new SettlementRegistry();
            services.Register<SettlementRegistry>(settlements);

            services.Register<FactionRegistry>(factions);

            var bus = new StrongEventBus();
            var vassalEvents = new List<VassalizedEvent>();
            var warEvents = new List<WarDeclaredEvent>();
            bus.Subscribe<VassalizedEvent>(vassalEvents.Add);
            bus.Subscribe<WarDeclaredEvent>(warEvents.Add);

            var plugin = new VassalizationPlugin();
            var ctx = new PluginContext(
                VassalizationPlugin.PluginIdValue,
                bus, services, new ChunkedWorld(),
                new CapabilitySystem(), _ => { });
            plugin.OnRegister(ctx);
            plugin.OnInitialize();

            return new Harness
            {
                Services = services,
                Factions = factions,
                Geo = geo,
                Units = units,
                Ground = ground,
                Naval = naval,
                Military = military,
                Settlements = settlements,
                EcoState = ecoState,
                Bus = bus,
                Plugin = plugin,
                VassalEvents = vassalEvents,
                WarEvents = warEvents,
            };
        }

        private static void SpawnGround(Harness h, int faction, int count, float baseX)
        {
            for (int i = 0; i < count; i++)
                h.Ground.Spawn(faction, GroundForceModel.Cavalry,
                    new Vector3(baseX + i * 4f, 0f, 0f));
        }

        private static void Capitals(Harness h)
        {
            h.Settlements.Found(0, SettlementKind.Capital, "cap",
                new Vector3(0f, 0f, 0f), 1);
            h.Settlements.Found(1, SettlementKind.Capital, "cap",
                new Vector3(100f, 0f, 0f), 1);
        }

        // ------------------------------------------------------------
        // 1. 满 3 年、4:1 极端碾压、弱者只有首府 → 附庸化、停战、发事件
        // ------------------------------------------------------------
        [Test]
        public void ExtremeCrush_OnlyCapital_Vassalized()
        {
            var h = Build(2);
            Capitals(h);
            SpawnGround(h, 0, 4, 0f);   // 强者 4
            SpawnGround(h, 1, 1, 100f); // 弱者 1
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(4); // 满 3 年

            Assert.AreEqual(1, h.VassalEvents.Count);
            Assert.AreEqual(0, h.VassalEvents[0].Suzerain);
            Assert.AreEqual(1, h.VassalEvents[0].Vassal);
            Assert.AreEqual(0, h.Factions.Get(1).Suzerain, "弱者应奉强者为宗主");
            Assert.IsTrue(h.Factions.Get(1).IsVassal);
            Assert.IsFalse(h.Military.IsAtWar(0, 1), "应停战");
        }

        // ------------------------------------------------------------
        // 2. 弱者有村庄 → 不附庸（割地由 CoercedPeace 处理）
        // ------------------------------------------------------------
        [Test]
        public void HasVillage_NotVassalized()
        {
            var h = Build(2);
            Capitals(h);
            h.Settlements.Found(1, SettlementKind.Village, "v",
                new Vector3(50f, 0f, 0f), 1);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(4);

            Assert.AreEqual(0, h.VassalEvents.Count);
            Assert.AreEqual(-1, h.Factions.Get(1).Suzerain, "弱者应保持独立，等待割地");
            Assert.IsTrue(h.Military.IsAtWar(0, 1), "战争继续");
        }

        // ------------------------------------------------------------
        // 3. 纳贡：附庸每年向宗主缴纳金币（存量 100 → 缴 10）
        // ------------------------------------------------------------
        [Test]
        public void Vassal_PaysTribute()
        {
            var h = Build(2);
            Capitals(h);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(4); // 附庸化
            // 给附庸 100 金币，宗主 0 金币
            h.EcoState.Set(1, ResourceType.Capital, 100f);
            h.EcoState.Set(0, ResourceType.Capital, 0f);

            h.Plugin.OnYear(5); // 纳贡

            Assert.AreEqual(90f, h.EcoState.Get(1, ResourceType.Capital), 0.001f,
                "附庸应缴 10 金币（100×10%=10）");
            Assert.AreEqual(10f, h.EcoState.Get(0, ResourceType.Capital), 0.001f,
                "宗主应得 10 金币");
        }

        // ------------------------------------------------------------
        // 4. 宗主保护：第三方宣附庸 → 宗主自动参战
        // ------------------------------------------------------------
        [Test]
        public void Suzerain_DefendsVassal()
        {
            var h = Build(3);
            Capitals(h);
            h.Settlements.Found(2, SettlementKind.Capital, "cap",
                new Vector3(200f, 0f, 0f), 1);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);
            h.Plugin.OnYear(4); // 弱者 1 附庸于强者 0

            h.WarEvents.Clear();
            // 第三方势力 2 宣附庸 1
            h.Bus.Publish(new WarDeclaredEvent
            {
                AttackerFaction = 2,
                DefenderFaction = 1,
            });

            bool protector = false;
            for (int i = 0; i < h.WarEvents.Count; i++)
            {
                if (h.WarEvents[i].AttackerFaction == 0 &&
                    h.WarEvents[i].DefenderFaction == 2)
                    protector = true;
            }
            Assert.IsTrue(protector, "宗主应发布对第三方的参战 WarDeclaredEvent");
        }

        // ------------------------------------------------------------
        // 5. 战争不足 3 年 → 不附庸
        // ------------------------------------------------------------
        [Test]
        public void WarTooShort_NotVassalized()
        {
            var h = Build(2);
            Capitals(h);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(3); // 仅 2 年

            Assert.AreEqual(0, h.VassalEvents.Count);
            Assert.AreEqual(-1, h.Factions.Get(1).Suzerain);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 6. 实力比不足 3.5（3:1）→ 不附庸
        // ------------------------------------------------------------
        [Test]
        public void NotExtremeCrush_NotVassalized()
        {
            var h = Build(2);
            Capitals(h);
            SpawnGround(h, 0, 3, 0f);
            SpawnGround(h, 1, 1, 100f); // 3:1 = 3.0 < 3.5
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(10);

            Assert.AreEqual(0, h.VassalEvents.Count);
            Assert.AreEqual(-1, h.Factions.Get(1).Suzerain);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 7. 宗主被灭 → 附庸自动恢复独立
        // ------------------------------------------------------------
        [Test]
        public void SuzerainEliminated_VassalFreed()
        {
            var h = Build(2);
            Capitals(h);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);
            h.Plugin.OnYear(4); // 弱者 1 附庸于强者 0
            Assert.AreEqual(0, h.Factions.Get(1).Suzerain);

            h.Bus.Publish(new FactionEliminatedEvent
            {
                Faction = 0,
                ByFaction = 2,
                GameYear = 20,
            });

            Assert.AreEqual(-1, h.Factions.Get(1).Suzerain, "宗主被灭，附庸应恢复独立");
            Assert.IsFalse(h.Factions.Get(1).IsVassal);
        }
    }
}
