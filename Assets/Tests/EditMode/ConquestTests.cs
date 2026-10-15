using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;
using PxC.Host;
using PxC.Plugins.Conquest;
using PxC.Plugins.Population;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 灭国领土占领测试：败者聚落/建筑归属胜者，人口随聚落归属，
    /// 发布 TerritoryConqueredEvent（转移数量）。
    /// </summary>
    public sealed class ConquestTests
    {
        private const int Loser = 0;
        private const int Winner = 1;

        private sealed class Harness
        {
            public ServiceRegistry Services;
            public SettlementRegistry Settlements;
            public BuildingRegistry Buildings;
            public PopulationState PopState;
            public PopulationService Pop;
            public StrongEventBus Bus;
            public List<TerritoryConqueredEvent> Conquered;
            public ConquestPlugin Plugin;
        }

        private static Harness Build()
        {
            var services = new ServiceRegistry();
            var settlements = new SettlementRegistry();
            var buildings = new BuildingRegistry();
            var popState = new PopulationState(16);
            var pop = new PopulationService(popState, settlements, services);

            services.Register<SettlementRegistry>(settlements);
            services.Register<BuildingRegistry>(buildings);
            services.Register<IPopulationService>(pop);

            var bus = new StrongEventBus();
            var conquered = new List<TerritoryConqueredEvent>();
            bus.Subscribe<TerritoryConqueredEvent>(conquered.Add);
            services.Register<IEventBus>(bus);

            var plugin = new ConquestPlugin();
            var ctx = new PluginContext(
                ConquestPlugin.PluginIdValue,
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
                Settlements = settlements,
                Buildings = buildings,
                PopState = popState,
                Pop = pop,
                Bus = bus,
                Conquered = conquered,
                Plugin = plugin,
            };
        }

        private static int FoundLoserSettlement(Harness h, int x, int z)
        {
            return h.Settlements.Found(
                Loser,
                SettlementKind.Village,
                "LoserVillage",
                new Vector3(x, 0f, z),
                100);
        }

        private static int SpawnLoserBuilding(Harness h, int x, int z)
        {
            return h.Buildings.Spawn(
                Loser,
                0, // House
                new Vector3(x, 0f, z),
                60f,
                false);
        }

        private static void Eliminate(Harness h)
        {
            h.Bus.Publish(new FactionEliminatedEvent
            {
                Faction = Loser,
                ByFaction = Winner,
                GameYear = 500,
            });
        }

        // ------------------------------------------------------------
        // 1. 灭国后败者聚落/建筑归属胜者，人口随之转移
        // ------------------------------------------------------------
        [Test]
        public void TransfersSettlementsBuildingsAndPopulation()
        {
            var h = Build();
            int slot = FoundLoserSettlement(h, 10, 0);
            h.PopState.Add(slot, PopulationLayer.Laborer, 80);
            h.PopState.Add(slot, PopulationLayer.Children, 20);
            SpawnLoserBuilding(h, 11, 0);

            // 转移前：loser 有 100 人口、1 聚落、1 建筑；winner 为 0
            Assert.AreEqual(100, h.Pop.Total(Loser));
            Assert.AreEqual(1, h.Settlements.CountByFaction(Loser));
            Assert.AreEqual(1, h.Buildings.TotalCount(Loser));

            Eliminate(h);

            // 聚落归属胜者
            Assert.AreEqual(0, h.Settlements.CountByFaction(Loser));
            Assert.AreEqual(1, h.Settlements.CountByFaction(Winner));
            Assert.AreEqual(Winner, h.Settlements.GetSafe(slot).Faction);

            // 建筑归属胜者
            Assert.AreEqual(0, h.Buildings.TotalCount(Loser));
            Assert.AreEqual(1, h.Buildings.TotalCount(Winner));

            // 人口随聚落归属胜者（loser 归零）
            Assert.AreEqual(0, h.Pop.Total(Loser));
            Assert.AreEqual(100, h.Pop.Total(Winner));
        }

        // ------------------------------------------------------------
        // 2. 发布 TerritoryConqueredEvent，数量正确
        // ------------------------------------------------------------
        [Test]
        public void PublishesTerritoryConqueredEventWithCounts()
        {
            var h = Build();
            FoundLoserSettlement(h, 10, 0);
            FoundLoserSettlement(h, 20, 0);
            SpawnLoserBuilding(h, 11, 0);
            SpawnLoserBuilding(h, 12, 0);
            SpawnLoserBuilding(h, 13, 0);

            Eliminate(h);

            Assert.AreEqual(1, h.Conquered.Count);
            Assert.AreEqual(Loser, h.Conquered[0].Loser);
            Assert.AreEqual(Winner, h.Conquered[0].Winner);
            Assert.AreEqual(2, h.Conquered[0].Settlements);
            Assert.AreEqual(3, h.Conquered[0].Buildings);
        }

        // ------------------------------------------------------------
        // 3. 败者无聚落/建筑：事件仍正常发布，数量为 0
        // ------------------------------------------------------------
        [Test]
        public void NoAssets_EventStillPublishedWithZeroCounts()
        {
            var h = Build();

            Eliminate(h);

            Assert.AreEqual(1, h.Conquered.Count);
            Assert.AreEqual(0, h.Conquered[0].Settlements);
            Assert.AreEqual(0, h.Conquered[0].Buildings);
        }

        // ------------------------------------------------------------
        // 4. 胜者自身资产不受影响（只转移败者归属）
        // ------------------------------------------------------------
        [Test]
        public void WinnerAssetsRemainUntouched()
        {
            var h = Build();
            // 胜者已有聚落
            int winnerSlot = h.Settlements.Found(
                Winner, SettlementKind.Capital, "WinnerCapital",
                new Vector3(0, 0f, 0), 100);
            h.PopState.Add(winnerSlot, PopulationLayer.Laborer, 50);
            // 败者有 1 聚落
            int loserSlot = FoundLoserSettlement(h, 30, 0);
            h.PopState.Add(loserSlot, PopulationLayer.Laborer, 10);

            Eliminate(h);

            // 胜者原有 50 + 接收败者 10 = 60，聚落 2
            Assert.AreEqual(60, h.Pop.Total(Winner));
            Assert.AreEqual(2, h.Settlements.CountByFaction(Winner));
        }
    }
}
