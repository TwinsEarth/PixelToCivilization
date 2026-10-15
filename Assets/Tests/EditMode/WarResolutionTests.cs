using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Host;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;
using PxC.Plugins.Victory;
using PxC.Plugins.WarResolution;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 战争结果 / 灭国闭环测试：作战单位归零一方被灭国，
    /// 平局/双方有单位/非交战不灭，含船艇口径与征服胜利联动。
    /// </summary>
    public sealed class WarResolutionTests
    {
        /// <summary>军事服务桩：仅控制战争矩阵（Firepower 不用于灭国判定）。</summary>
        private sealed class FakeMilitary : IMilitaryService
        {
            private readonly bool[,] _atWar;

            public FakeMilitary(int factions)
            {
                _atWar = new bool[factions, factions];
            }

            public void SetAtWar(int a, int b)
            {
                _atWar[a, b] = true;
                _atWar[b, a] = true;
            }

            public int TotalSoldiers(int faction) => 0;
            public int Firepower(int faction) => 0;
            public bool TryRecruit(int faction, int count) => true;
            public bool IsAtWar(int faction, int otherFaction) =>
                _atWar[faction, otherFaction];
        }

        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public UnitRegistry Units;
            public GridGeography Geo;
            public GroundForceService Ground;
            public NavalService Naval;
            public FakeMilitary Military;
            public StrongEventBus Bus;
            public WarResolutionPlugin Plugin;
            public List<FactionEliminatedEvent> Eliminated;
        }

        private static Harness Build(int factionCount, bool playerAt0)
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < factionCount; i++)
                factions.Add("F" + i, playerAt0 && i == 0, i * 1000f, 0f, i);

            var units = new UnitRegistry();
            var geo = new GridGeography();
            services.Register<UnitRegistry>(units);
            services.Register<IGeographyQuery>(geo);

            var groundState = new GroundForceState(units.Capacity);
            var ground = new GroundForceService(units, groundState, geo, services, _ => { });
            services.Register<GroundForceService>(ground);

            var navalState = new NavalState(units.Capacity);
            var naval = new NavalService(units, navalState, geo, services, _ => { });
            services.Register<NavalService>(naval);

            var military = new FakeMilitary(factionCount);
            services.Register<IMilitaryService>(military);
            services.Register<FactionRegistry>(factions);

            var bus = new StrongEventBus();
            var eliminated = new List<FactionEliminatedEvent>();
            bus.Subscribe<FactionEliminatedEvent>(eliminated.Add);
            services.Register<IEventBus>(bus);

            var plugin = new WarResolutionPlugin();
            var ctx = new PluginContext(
                WarResolutionPlugin.PluginIdValue,
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
                Units = units,
                Geo = geo,
                Ground = ground,
                Naval = naval,
                Military = military,
                Bus = bus,
                Plugin = plugin,
                Eliminated = eliminated,
            };
        }

        private static EntityId SpawnGround(Harness h, int faction)
        {
            var pos = new Vector3(faction * 100f, 0f, 0f);
            return h.Ground.Spawn(faction, GroundForceModel.Formation, pos);
        }

        private static EntityId SpawnShip(Harness h, int faction, int x, int z)
        {
            h.Geo.SetWater(x, z);
            var pos = new Vector3(x, 0f, z);
            return h.Naval.Spawn(faction, NavalModel.Raft, pos);
        }

        // ------------------------------------------------------------
        // 1. 一方无单位、对方有单位 → 无单位方被灭国
        // ------------------------------------------------------------
        [Test]
        public void NoUnits_Eliminated()
        {
            var h = Build(2, false);
            SpawnGround(h, 1);            // 势力 1 有单位；势力 0 无单位
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Eliminated.Count);
            Assert.AreEqual(0, h.Eliminated[0].Faction);
            Assert.AreEqual(1, h.Eliminated[0].ByFaction);
            Assert.IsFalse(h.Factions.Get(0).Alive);
            Assert.IsTrue(h.Factions.Get(1).Alive);
        }

        // ------------------------------------------------------------
        // 2. 双方都有单位 → 不消灭
        // ------------------------------------------------------------
        [Test]
        public void BothHaveUnits_NoElimination()
        {
            var h = Build(2, false);
            SpawnGround(h, 0);
            SpawnGround(h, 1);
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Eliminated.Count);
            Assert.IsTrue(h.Factions.Get(0).Alive);
            Assert.IsTrue(h.Factions.Get(1).Alive);
        }

        // ------------------------------------------------------------
        // 3. 双方都无单位 → 平局，互不灭国
        // ------------------------------------------------------------
        [Test]
        public void BothNoUnits_Draw_NoElimination()
        {
            var h = Build(2, false);
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Eliminated.Count);
            Assert.IsTrue(h.Factions.Get(0).Alive);
            Assert.IsTrue(h.Factions.Get(1).Alive);
        }

        // ------------------------------------------------------------
        // 4. 非交战状态 → 即使一方无单位也不被灭
        // ------------------------------------------------------------
        [Test]
        public void NotAtWar_NoElimination()
        {
            var h = Build(2, false);
            SpawnGround(h, 1);
            // 不设置战争状态

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Eliminated.Count);
            Assert.IsTrue(h.Factions.Get(0).Alive);
        }

        // ------------------------------------------------------------
        // 5. 一方无地面单位但有船艇 → 不算被消灭（船艇也是作战单位）
        // ------------------------------------------------------------
        [Test]
        public void HasShipOnly_NotEliminated()
        {
            var h = Build(2, false);
            SpawnShip(h, 0, 5, 0);        // 势力 0 无地面、有船
            SpawnGround(h, 1);
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Eliminated.Count);
            Assert.IsTrue(h.Factions.Get(0).Alive);
        }

        // ------------------------------------------------------------
        // 6. AI 可消灭玩家（玩家无单位）
        // ------------------------------------------------------------
        [Test]
        public void PlayerCanBeEliminated()
        {
            var h = Build(2, true);
            SpawnGround(h, 1);            // 玩家（0）无单位，AI（1）有单位
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Eliminated.Count);
            Assert.AreEqual(0, h.Eliminated[0].Faction);
            Assert.IsFalse(h.Factions.Player.Alive);
        }

        // ------------------------------------------------------------
        // 7. 灭国后当年 VictoryService 检测到征服胜利
        // （玩家为势力 1，灭了势力 0，需把玩家放到非 0 位置不便；
        //  改为：玩家 0 灭了势力 1，玩家有单位 → 仅玩家存活 → 征服胜利）
        // ------------------------------------------------------------
        [Test]
        public void Elimination_TriggersConquestVictory()
        {
            var h = Build(2, true);
            SpawnGround(h, 0);            // 玩家有单位
            // 势力 1 无单位
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.IsFalse(h.Factions.Get(1).Alive);
            Assert.AreEqual(1, h.Factions.AliveCount);

            // 用真实 VictoryService 检测（需注册 ITech/ICulture 等，缺失则走征服分支）
            var vstate = new VictoryState();
            var victory = new VictoryService(vstate, h.Services, _ => { });
            VictoryType t = victory.Check(1);

            Assert.AreEqual(VictoryType.Conquest, t);
            Assert.IsTrue(victory.IsAchieved);
        }
    }
}
