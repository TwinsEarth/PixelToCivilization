using NUnit.Framework;
using UnityEngine;
using PxC.Host;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;
using PxC.Plugins.Military;

namespace PxC.Tests.EditMode
{
    /// <summary>测试用人口：内存分层，可在层间调配。</summary>
    public sealed class FakePopulation : IPopulationService
    {
        private const int Layers = 6;
        private readonly int[,] _pop;
        private readonly int _factions;

        public FakePopulation(int factions, int labor, int soldiers)
        {
            _factions = factions;
            _pop = new int[factions, Layers];
            for (int f = 0; f < factions; f++)
            {
                _pop[f, (int)PopulationLayer.Laborer] = labor;
                _pop[f, (int)PopulationLayer.Soldier] = soldiers;
            }
        }

        public int Total(int faction)
        {
            int sum = 0;
            for (int l = 0; l < Layers; l++) sum += _pop[faction, l];
            return sum;
        }

        public int ByLayer(int faction, PopulationLayer layer) =>
            _pop[faction, (int)layer];

        public float BirthRate(int faction) => 0f;
        public float DeathRate(int faction) => 0f;
        public float StarvationRisk(int faction) => 0f;

        public bool TryAssign(int faction, PopulationLayer from, PopulationLayer to, int count)
        {
            if (_pop[faction, (int)from] < count) return false;
            _pop[faction, (int)from] -= count;
            _pop[faction, (int)to] += count;
            return true;
        }

        public int ApplyExcessDeaths(int faction, int count)
        {
            // 按灾害顺序扣减：老、少、劳力、工匠、学者、士兵
            PopulationLayer[] order =
            {
                PopulationLayer.Elder,
                PopulationLayer.Children,
                PopulationLayer.Laborer,
                PopulationLayer.Artisan,
                PopulationLayer.Scholar,
                PopulationLayer.Soldier,
            };

            int remaining = count;
            for (int i = 0; i < order.Length && remaining > 0; i++)
            {
                int have = _pop[faction, (int)order[i]];
                int take = have < remaining ? have : remaining;
                _pop[faction, (int)order[i]] -= take;
                remaining -= take;
            }
            return count - remaining;
        }

        public bool Emigrate(int faction, int count)
        {
            int idx = (int)PopulationLayer.Laborer;
            if (_pop[faction, idx] < count) return false;
            _pop[faction, idx] -= count;
            return true;
        }
    }

    public sealed class MilitaryTests
    {
        private UnitRegistry _units;
        private GridGeography _geo;
        private ServiceRegistry _services;
        private GroundForceService _ground;
        private NavalService _naval;
        private FakePopulation _pop;
        private MilitaryService _military;

        [SetUp]
        public void SetUp()
        {
            _units = new UnitRegistry();
            _geo = new GridGeography();
            _services = new ServiceRegistry();
            _services.Register<UnitRegistry>(_units);
            var groundState = new GroundForceState(_units.Capacity);
            _ground = new GroundForceService(_units, groundState, _geo, _services, _ => { });
            var navalState = new NavalState(_units.Capacity);
            _naval = new NavalService(_units, navalState, _geo, _services, _ => { });
            _pop = new FakePopulation(8, 10, 2);
            _military = new MilitaryService(_ground, _naval, _pop);
        }

        [Test]
        public void TryRecruit_MovesLaborToSoldier()
        {
            bool ok = _military.TryRecruit(0, 5);
            Assert.IsTrue(ok);
            Assert.AreEqual(7, _pop.ByLayer(0, PopulationLayer.Soldier));
            Assert.AreEqual(5, _pop.ByLayer(0, PopulationLayer.Laborer));
        }

        [Test]
        public void TryRecruit_InsufficientLabor_Fails()
        {
            Assert.IsFalse(_military.TryRecruit(0, 100));
            Assert.AreEqual(2, _pop.ByLayer(0, PopulationLayer.Soldier));
        }

        [Test]
        public void TotalSoldiers_AggregatesPopulationAndUnits()
        {
            Assert.AreEqual(2, _military.TotalSoldiers(0));
            _ground.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            Assert.AreEqual(3, _military.TotalSoldiers(0));
        }

        [Test]
        public void DeclareWar_SetsAtWarState()
        {
            Assert.IsFalse(_military.IsAtWar(0, 1));
            _military.DeclareWar(0, 1);
            Assert.IsTrue(_military.IsAtWar(0, 1));
            Assert.IsTrue(_military.IsAtWar(1, 0));
        }

        [Test]
        public void WarPairs_RoundTrip()
        {
            _military.DeclareWar(0, 1);
            _military.DeclareWar(2, 3);
            var pairs = _military.GetWarPairs();
            Assert.AreEqual(4, pairs.Length);

            var rebuilt = new MilitaryService(_ground, _naval, _pop);
            rebuilt.RestoreWarPairs(pairs);
            Assert.IsTrue(rebuilt.IsAtWar(0, 1));
            Assert.IsTrue(rebuilt.IsAtWar(2, 3));
        }
    }
}
