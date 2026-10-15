using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Host;
using PxC.Plugins.AutoPeace;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Military;
using PxC.Plugins.Naval;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 自动和约测试：僵持战争持续若干年、双方都有单位且数量接近时停战；
    /// 持续不足、实力悬殊、一方无单位不签。
    /// </summary>
    public sealed class AutoPeaceTests
    {
        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public GridGeography Geo;
            public GroundForceService Ground;
            public NavalService Naval;
            public MilitaryService Military;
            public StrongEventBus Bus;
            public AutoPeacePlugin Plugin;
            public List<PeaceTreatyEvent> Treaties;
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

            var navalState = new NavalState(units.Capacity);
            var naval = new NavalService(units, navalState, geo, services, _ => { });
            services.Register<NavalService>(naval);

            var military = new MilitaryService(ground, naval, null);
            services.Register<IMilitaryService>(military);
            services.Register<FactionRegistry>(factions);

            var bus = new StrongEventBus();
            var treaties = new List<PeaceTreatyEvent>();
            bus.Subscribe<PeaceTreatyEvent>(treaties.Add);
            services.Register<IEventBus>(bus);

            var plugin = new AutoPeacePlugin();
            var ctx = new PluginContext(
                AutoPeacePlugin.PluginIdValue,
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
                Ground = ground,
                Naval = naval,
                Military = military,
                Bus = bus,
                Plugin = plugin,
                Treaties = treaties,
            };
        }

        private static void SpawnGroundUnits(Harness h, int faction, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector3(faction * 100f + i * 4f, 0f, 0f);
                h.Ground.Spawn(faction, GroundForceModel.Cavalry, pos);
            }
        }

        // ------------------------------------------------------------
        // 1. 双方僵持满 5 年、单位接近 → 签和约
        // ------------------------------------------------------------
        [Test]
        public void BalancedStalemateAfterFiveYears_MakesPeace()
        {
            var h = Build(2);
            SpawnGroundUnits(h, 0, 4);
            SpawnGroundUnits(h, 1, 4);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(6);

            Assert.AreEqual(1, h.Treaties.Count);
            Assert.AreEqual(0, h.Treaties[0].FactionA);
            Assert.AreEqual(1, h.Treaties[0].FactionB);
            Assert.AreEqual(1, h.Treaties[0].WarStartYear);
            Assert.IsFalse(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 2. 持续不足 5 年 → 不签
        // ------------------------------------------------------------
        [Test]
        public void WarTooShort_NoPeace()
        {
            var h = Build(2);
            SpawnGroundUnits(h, 0, 4);
            SpawnGroundUnits(h, 1, 4);
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(4); // 仅 3 年

            Assert.AreEqual(0, h.Treaties.Count);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 3. 实力悬殊（优势比超过 1.5）→ 不签（强势方可能灭国）
        // ------------------------------------------------------------
        [Test]
        public void Unbalanced_NoPeace()
        {
            var h = Build(2);
            SpawnGroundUnits(h, 0, 1);
            SpawnGroundUnits(h, 1, 4); // 4:1 优势比 4 > 1.5
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(10);

            Assert.AreEqual(0, h.Treaties.Count);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 4. 一方无单位 → 不签和约（交给灭国/征服路径）
        // ------------------------------------------------------------
        [Test]
        public void OneSideNoUnits_NoPeace()
        {
            var h = Build(2);
            SpawnGroundUnits(h, 1, 4); // 势力 0 无单位
            h.Military.DeclareWar(0, 1, 1);

            h.Plugin.OnYear(10);

            Assert.AreEqual(0, h.Treaties.Count);
            Assert.IsTrue(h.Military.IsAtWar(0, 1));
        }

        // ------------------------------------------------------------
        // 5. MilitaryService：开始年份与 MakePeace 存档口径
        // ------------------------------------------------------------
        [Test]
        public void MilitaryService_StartYearAndPeace()
        {
            var h = Build(2);
            h.Military.DeclareWar(0, 1, 123);

            Assert.AreEqual(123, h.Military.GetWarStartYear(0, 1));
            Assert.AreEqual(123, h.Military.GetWarStartYear(1, 0));

            int[] pairs = h.Military.GetWarPairs(out int[] years);
            Assert.AreEqual(1, pairs.Length / 2);
            Assert.AreEqual(1, years.Length);
            Assert.AreEqual(123, years[0]);

            h.Military.MakePeace(0, 1);
            Assert.IsFalse(h.Military.IsAtWar(0, 1));
            Assert.AreEqual(0, h.Military.GetWarStartYear(0, 1));
        }
    }
}
