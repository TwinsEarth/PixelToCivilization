using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Host;
using PxC.Plugins.CoercedPeace;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Military;
using PxC.Plugins.Naval;
using PxC.Plugins.Building;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 压制性和约测试：战争持续满 3 年、双方都有单位、实力悬殊（&gt;3 倍），
    /// 且弱者有非首府聚落时，强者强制弱者割让最近聚落并停战。
    /// </summary>
    public sealed class CoercedPeaceTests
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
            public BuildingRegistry Buildings;
            public StrongEventBus Bus;
            public CoercedPeacePlugin Plugin;
            public List<CoercedPeaceEvent> Events;
        }

        private static Harness Build()
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < 2; i++)
                factions.Add("F" + i, false, i * 1000f, 0f, i);

            var geo = new GridGeography();
            services.Register<IGeographyQuery>(geo);

            var units = new UnitRegistry();
            services.Register<UnitRegistry>(units);

            var groundState = new GroundForceState(units.Capacity);
            var ground = new GroundForceService(units, groundState, geo, services, _ => { });
            services.Register<GroundForceService>(ground);

            var navalState = new NavalState(units.Capacity);
            var naval = new NavalService(units, navalState, geo, services, _ => { });
            services.Register<NavalService>(naval);

            var military = new MilitaryService(ground, naval, null);
            services.Register<IMilitaryService>(military);
            services.Register<FactionRegistry>(factions);

            var settlements = new SettlementRegistry();
            services.Register<SettlementRegistry>(settlements);
            var buildings = new BuildingRegistry();
            services.Register<BuildingRegistry>(buildings);

            var bus = new StrongEventBus();
            var events = new List<CoercedPeaceEvent>();
            bus.Subscribe<CoercedPeaceEvent>(events.Add);
            services.Register<IEventBus>(bus);

            var plugin = new CoercedPeacePlugin();
            var ctx = new PluginContext(
                CoercedPeacePlugin.PluginIdValue,
                bus,
                services,
                new ChunkedWorld(),
                new CapabilitySystem(),
                _ => { });
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
                Buildings = buildings,
                Bus = bus,
                Plugin = plugin,
                Events = events,
            };
        }

        private static void SpawnGround(Harness h, int faction, int count, float baseX)
        {
            for (int i = 0; i < count; i++)
                h.Ground.Spawn(faction, GroundForceModel.Cavalry,
                    new Vector3(baseX + i * 4f, 0f, 0f));
        }

        private static int FoundVillage(Harness h, int faction, float x)
        {
            int slot = h.Settlements.Found(
                faction, SettlementKind.Village, "v",
                new Vector3(x, 0f, 0f), 1);
            return slot;
        }

        // ------------------------------------------------------------
        // 1. 满 3 年、4:1 悬殊、弱者有村庄 → 割村庄、停战、发事件
        // ------------------------------------------------------------
        [Test]
        public void OverwhelmingAdvantage_CedesVillageAndPeace()
        {
            var h = Build();
            // 强者首府 (0,0)，弱者首府 (100,0)，弱者村庄 (50,0)
            h.Settlements.Found(0, SettlementKind.Capital, "cap",
                new Vector3(0f, 0f, 0f), 1);
            h.Settlements.Found(1, SettlementKind.Capital, "cap",
                new Vector3(100f, 0f, 0f), 1);
            FoundVillage(h, 1, 50f);

            SpawnGround(h, 0, 4, 0f);   // 强者 4 单位
            SpawnGround(h, 1, 1, 100f); // 弱者 1 单位
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(4); // 满 3 年

            Assert.AreEqual(1, h.Events.Count);
            Assert.AreEqual(0, h.Events[0].Stronger);
            Assert.AreEqual(1, h.Events[0].Weaker);
            Assert.AreEqual(1, h.Events[0].SettlementsCeded);
            Assert.IsFalse(h.Military.IsAtWar(0, 1));

            // 村庄 (50,0) 已归属强者
            int villageSlot = h.Settlements.Capacity;
            bool found = false;
            for (int i = 0; i < h.Settlements.Capacity; i++)
            {
                var r = h.Settlements.GetSafe(i);
                if (r.Alive && r.Position.x == 50f)
                {
                    Assert.AreEqual(0, r.Faction, "被割让村庄应归属强者");
                    found = true;
                }
            }
            Assert.IsTrue(found);
        }

        // ------------------------------------------------------------
        // 2. 战争持续不足 3 年 → 不割
        // ------------------------------------------------------------
        [Test]
        public void WarTooShort_NoCoercion()
        {
            var h = Build();
            h.Settlements.Found(0, SettlementKind.Capital, "cap",
                Vector3.zero, 1);
            h.Settlements.Found(1, SettlementKind.Capital, "cap",
                new Vector3(100f, 0f, 0f), 1);
            FoundVillage(h, 1, 50f);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(3); // 仅 2 年

            Assert.AreEqual(0, h.Events.Count);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 3. 实力比不足 3 倍（2:1）→ 不割
        // ------------------------------------------------------------
        [Test]
        public void NotOverwhelming_NoCoercion()
        {
            var h = Build();
            h.Settlements.Found(0, SettlementKind.Capital, "cap",
                Vector3.zero, 1);
            h.Settlements.Found(1, SettlementKind.Capital, "cap",
                new Vector3(100f, 0f, 0f), 1);
            FoundVillage(h, 1, 50f);
            SpawnGround(h, 0, 2, 0f);
            SpawnGround(h, 1, 1, 100f); // 2:1
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(10);

            Assert.AreEqual(0, h.Events.Count);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 4. 弱者只有首府、无可割聚落 → 不割、战争继续
        // ------------------------------------------------------------
        [Test]
        public void NoCedableSettlement_NoCoercion()
        {
            var h = Build();
            h.Settlements.Found(0, SettlementKind.Capital, "cap",
                Vector3.zero, 1);
            h.Settlements.Found(1, SettlementKind.Capital, "cap",
                new Vector3(100f, 0f, 0f), 1);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(10);

            Assert.AreEqual(0, h.Events.Count);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 5. 割地时村庄半径内的弱者建筑随之转移
        // ------------------------------------------------------------
        [Test]
        public void Cession_TransfersNearbyBuildings()
        {
            var h = Build();
            h.Settlements.Found(0, SettlementKind.Capital, "cap",
                Vector3.zero, 1);
            h.Settlements.Found(1, SettlementKind.Capital, "cap",
                new Vector3(100f, 0f, 0f), 1);
            FoundVillage(h, 1, 50f);
            // 村庄附近的弱者农田（半径 6 内）
            h.Buildings.Spawn(1, (int)BuildingType.Farm,
                new Vector3(51f, 0f, 0f), 50f, false);
            SpawnGround(h, 0, 4, 0f);
            SpawnGround(h, 1, 1, 100f);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(4);

            Assert.AreEqual(1, h.Events.Count);
            Assert.AreEqual(1, h.Buildings.CountByType(0, (int)BuildingType.Farm),
                "被割让村庄附近的建筑应归属强者");
        }
    }
}
