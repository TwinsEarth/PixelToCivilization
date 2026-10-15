using System;
using NUnit.Framework;
using UnityEngine;
using PxC.Host;
using PxC.Core.Events;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;
using PxC.Plugins.Combat;

namespace PxC.Tests.EditMode
{
    public sealed class CombatTests
    {
        private UnitRegistry _units;
        private GridGeography _geo;
        private ServiceRegistry _services;
        private StrongEventBus _events;
        private GroundForceState _groundState;
        private GroundForceService _ground;
        private NavalService _naval;
        private CombatService _combat;

        [SetUp]
        public void SetUp()
        {
            _units = new UnitRegistry();
            _geo = new GridGeography();
            _services = new ServiceRegistry();
            _services.Register<UnitRegistry>(_units);
            _groundState = new GroundForceState(_units.Capacity);
            _ground = new GroundForceService(_units, _groundState, _geo, _services, _ => { });
            var navalState = new NavalState(_units.Capacity);
            _naval = new NavalService(_units, navalState, _geo, _services, _ => { });
            _events = new StrongEventBus();
            _combat = new CombatService(_units, _ground, _naval, _events, _ => { });
        }

        [Test]
        public void SameDomain_LandAttacksLand_DealsDamage()
        {
            var a = _ground.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            var b = _ground.Spawn(1, GroundForceModel.Cavalry, new Vector3(3f, 0f, 0f));
            float hpBefore = _units.GetRef(b).Hp;

            _combat.ResolveAttack(a, b);
            float hpAfter = _units.GetRef(b).Hp;

            Assert.Less(hpAfter, hpBefore);
        }

        [Test]
        public void CrossDomain_LandAttacksNaval_IsRejected()
        {
            var a = _ground.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            _geo.SetWater(5, 0);
            var b = _naval.Spawn(1, NavalModel.Raft, new Vector3(5f, 0f, 0f));
            float hpBefore = _units.GetRef(b).Hp;

            _combat.ResolveAttack(a, b);
            float hpAfter = _units.GetRef(b).Hp;

            Assert.AreEqual(hpBefore, hpAfter, "跨域攻击必须被拒绝");
        }

        [Test]
        public void LethalDamage_KillsTarget()
        {
            var a = _ground.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            var b = _ground.Spawn(1, GroundForceModel.Cavalry, new Vector3(3f, 0f, 0f));

            _combat.DamageUnit(b, 100000f, DamageType.Blunt, a);
            _events.DispatchPending();

            Assert.IsFalse(_units.GetRef(b).Alive);
        }

        [Test]
        public void AwardExperience_ToLand_LevelsUp()
        {
            var a = _ground.Spawn(0, GroundForceModel.Cavalry, new Vector3(0f, 0f, 0f));
            _combat.AwardExperience(a, 100);
            Assert.AreEqual(2, _units.GetRef(a).Level);
        }

        [Test]
        public void Isolated1v1_AutonomousAi_TheyCloseAndFight()
        {
            // 隔离 1v1：两个同型 Formation，相距 5（感知内、攻击范围外）。
            // 由 GroundForceAi 自主驱动：应互相接近、进入攻击、减员，直到一方死亡。
            var a = _ground.Spawn(0, GroundForceModel.Formation, new Vector3(0f, 0f, 0f));
            var b = _ground.Spawn(1, GroundForceModel.Formation, new Vector3(5f, 0f, 0f));
            var ai = new GroundForceAi(
                _units, _groundState, _geo,
                () => _combat, null, null, _ => { });

            int frames = 600;
            float dt = 0.1f;
            for (int f = 0; f < frames; f++)
            {
                ai.Tick(dt);
                _events.DispatchPending();
                if (f % 20 == 0 || f == frames - 1)
                {
                    bool aAlive = _units.GetRef(a).Alive;
                    bool bAlive = _units.GetRef(b).Alive;
                    Console.Out.WriteLine(
                        $"  f{f} aHP={_units.GetSafe(a).Hp:F0}({_units.GetSafe(a).State}) " +
                        $"bHP={_units.GetSafe(b).Hp:F0}({_units.GetSafe(b).State}) " +
                        $"aPos=({_units.GetSafe(a).Position.x:F1}) bPos=({_units.GetSafe(b).Position.x:F1})");
                    if (!aAlive || !bAlive) break;
                }
            }

            bool aDied = !_units.GetRef(a).Alive;
            bool bDied = !_units.GetRef(b).Alive;
            Console.Out.WriteLine($"结果 aDied={aDied} bDied={bDied}");
            Assert.IsTrue(aDied || bDied,
                "1v1 自主战斗应在 600 帧内分出胜负（一方死亡）");
        }
    }
}
