using NUnit.Framework;
using UnityEngine;
using PxC.Host;
using PxC.Gameplay.Entities;
using PxC.Plugins.GroundForce;

namespace PxC.Tests.EditMode
{
    public sealed class GroundForceTests
    {
        private UnitRegistry _units;
        private GridGeography _geo;
        private ServiceRegistry _services;
        private GroundForceState _state;
        private GroundForceService _service;

        [SetUp]
        public void SetUp()
        {
            _units = new UnitRegistry();
            _geo = new GridGeography();
            _services = new ServiceRegistry();
            _services.Register<UnitRegistry>(_units);
            _state = new GroundForceState(_units.Capacity);
            _service = new GroundForceService(_units, _state, _geo, _services, _ => { });
        }

        [Test]
        public void Spawn_OnWater_IsRejected()
        {
            _geo.SetWater(10, 10);
            var id = _service.Spawn(0, GroundForceModel.Cavalry, new Vector3(10f, 0f, 10f));
            Assert.IsFalse(id.IsValid);
        }

        [Test]
        public void Spawn_OnLand_Succeeds()
        {
            var id = _service.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            Assert.IsTrue(id.IsValid);
            Assert.AreEqual(1, _service.Count);
        }

        [Test]
        public void Rally_OnWater_FailsSafeAndMovesNothing()
        {
            _service.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            _geo.SetWater(5, 0);
            int n = _service.Rally(0, new Vector3(5f, 0f, 0f), 100f, false);
            Assert.AreEqual(0, n);
        }

        [Test]
        public void Rally_OnLand_OnlyOrdersNoTeleport()
        {
            var id = _service.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            Vector3 before = _units.GetRef(id).Position;

            int n = _service.Rally(0, new Vector3(5f, 0f, 0f), 100f, false);
            Assert.AreEqual(1, n);

            ref var r = ref _units.GetRef(id);
            Assert.AreEqual(before, r.Position, "集结不得瞬时搬运单位");
            Assert.IsTrue(r.HasMoveTarget);
        }

        [Test]
        public void Rally_Repeated100Times_DoesNotThrowOrDuplicate()
        {
            _service.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            for (int i = 0; i < 100; i++)
            {
                int n = _service.Rally(
                    0, new Vector3(1f + (i % 5), 0f, 0f), 100f, false);
                Assert.AreEqual(1, n);
            }
            Assert.AreEqual(1, _service.Count, "连续插旗不得创建单位");
        }

        [Test]
        public void Experience_ReachesThreshold_LevelsUpAndRaisesAttack()
        {
            var id = _service.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            int slot = id.Index;
            float atk1 = _service.CurrentAttack(slot);

            int levels = _service.AddExperience(slot, 100);
            Assert.AreEqual(1, levels);

            float atk2 = _service.CurrentAttack(slot);
            Assert.Greater(atk2, atk1);
            Assert.AreEqual(2, _units.GetRef(id).Level);
        }
    }
}
