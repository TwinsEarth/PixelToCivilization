using NUnit.Framework;
using UnityEngine;
using PxC.Host;
using PxC.Gameplay.Entities;
using PxC.Plugins.Naval;

namespace PxC.Tests.EditMode
{
    public sealed class NavalTests
    {
        private UnitRegistry _units;
        private GridGeography _geo;
        private ServiceRegistry _services;
        private NavalState _state;
        private NavalService _service;

        [SetUp]
        public void SetUp()
        {
            _units = new UnitRegistry();
            _geo = new GridGeography();
            _services = new ServiceRegistry();
            _services.Register<UnitRegistry>(_units);
            _state = new NavalState(_units.Capacity);
            _service = new NavalService(_units, _state, _geo, _services, _ => { });
        }

        [Test]
        public void Spawn_OnLand_IsRejected()
        {
            // 默认全陆地
            var id = _service.Spawn(0, NavalModel.Raft, new Vector3(0f, 0f, 0f));
            Assert.IsFalse(id.IsValid);
        }

        [Test]
        public void Spawn_OnWater_Succeeds()
        {
            _geo.SetWater(0, 0);
            var id = _service.Spawn(0, NavalModel.Raft, new Vector3(0f, 0f, 0f));
            Assert.IsTrue(id.IsValid);
            Assert.AreEqual(1, _service.Count);
        }

        [Test]
        public void Rally_OnLand_FailsSafe()
        {
            _geo.SetWater(0, 0);
            _service.Spawn(0, NavalModel.Raft, new Vector3(0f, 0f, 0f));
            // (5,0) 未标记，为陆地
            int n = _service.Rally(0, new Vector3(5f, 0f, 0f), 100f, false);
            Assert.AreEqual(0, n);
        }

        [Test]
        public void Rally_OnWater_OnlyOrdersNoTeleport()
        {
            _geo.SetWater(0, 0);
            _geo.SetWater(5, 0);
            var id = _service.Spawn(0, NavalModel.Raft, new Vector3(0f, 0f, 0f));
            Vector3 before = _units.GetRef(id).Position;

            int n = _service.Rally(0, new Vector3(5f, 0f, 0f), 100f, false);
            Assert.AreEqual(1, n);

            ref var r = ref _units.GetRef(id);
            Assert.AreEqual(before, r.Position, "海上集结不得瞬时搬运");
            Assert.IsTrue(r.HasMoveTarget);
        }

        [Test]
        public void FindWaterNear_ReturnsWaterWithinRadius()
        {
            _geo.SetWater(3, 2);
            bool found = _service.FindWaterNear(0f, 0f, 10f, out var water);
            Assert.IsTrue(found);
            Assert.IsTrue(_geo.IsWater(water.x, water.z));
        }
    }
}
