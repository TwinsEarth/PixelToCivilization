using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Host;
using PxC.Plugins.AutoDiplomacy;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 自动外交 / 主动宣战测试：按军力优势发布 WarDeclaredEvent，
    /// 门槛/优势比/战争数量/玩家跳过等边界。
    /// </summary>
    public sealed class AutoDiplomacyTests
    {
        /// <summary>可控的军事服务桩：手动设置火力与战争状态。</summary>
        private sealed class FakeMilitary : IMilitaryService
        {
            private readonly int[] _firepower;
            private readonly bool[,] _atWar;

            public FakeMilitary(int factions)
            {
                _firepower = new int[factions];
                _atWar = new bool[factions, factions];
            }

            public void SetFirepower(int faction, int power)
            {
                _firepower[faction] = power;
            }

            /// <summary>置双向交战状态（模拟 military 插件 DeclareWar 后的矩阵）。</summary>
            public void SetAtWar(int a, int b)
            {
                _atWar[a, b] = true;
                _atWar[b, a] = true;
            }

            public int TotalSoldiers(int faction) => _firepower[faction];
            public int Firepower(int faction) => _firepower[faction];
            public bool TryRecruit(int faction, int count) => true;

            public bool IsAtWar(int faction, int otherFaction)
            {
                return _atWar[faction, otherFaction];
            }

            public int GetWarStartYear(int faction, int otherFaction) => 1;
            public void MakePeace(int faction, int otherFaction) { }
        }

        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public FakeMilitary Military;
            public StrongEventBus Bus;
            public AutoDiplomacyPlugin Plugin;
            public List<WarDeclaredEvent> Wars;
        }

        private static Harness Build(int factionCount, bool playerAt0)
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < factionCount; i++)
                factions.Add("F" + i, playerAt0 && i == 0, i * 1000f, 0f, i);

            var military = new FakeMilitary(factionCount);
            services.Register<IMilitaryService>(military);
            services.Register<FactionRegistry>(factions);

            var bus = new StrongEventBus();
            var wars = new List<WarDeclaredEvent>();
            bus.Subscribe<WarDeclaredEvent>(wars.Add);

            var plugin = new AutoDiplomacyPlugin();
            var ctx = new PluginContext(
                AutoDiplomacyPlugin.PluginIdValue,
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
                Military = military,
                Bus = bus,
                Plugin = plugin,
                Wars = wars,
            };
        }

        // ------------------------------------------------------------
        // 1. 军力优势显著（60 vs 20，ratio 3.0）→ 宣战
        // ------------------------------------------------------------
        [Test]
        public void OverwhelmingPower_DeclaresWar()
        {
            var h = Build(2, false);
            h.Military.SetFirepower(0, 60);
            h.Military.SetFirepower(1, 20);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Wars.Count);
            Assert.AreEqual(0, h.Wars[0].AttackerFaction);
            Assert.AreEqual(1, h.Wars[0].DefenderFaction);
        }

        // ------------------------------------------------------------
        // 2. 攻方火力低于最低门槛（10 < 20）→ 不宣战
        // ------------------------------------------------------------
        [Test]
        public void BelowMinimumFirepower_NoWar()
        {
            var h = Build(2, false);
            h.Military.SetFirepower(0, 10);
            h.Military.SetFirepower(1, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Wars.Count);
        }

        // ------------------------------------------------------------
        // 3. 优势比不足（25 vs 20，ratio 1.25 < 1.5）→ 不宣战
        // ------------------------------------------------------------
        [Test]
        public void InsufficientAdvantageRatio_NoWar()
        {
            var h = Build(2, false);
            h.Military.SetFirepower(0, 25);
            h.Military.SetFirepower(1, 20);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Wars.Count);
        }

        // ------------------------------------------------------------
        // 4. 已与唯一目标交战 → 不重复宣战
        // ------------------------------------------------------------
        [Test]
        public void AlreadyAtWar_NoRepeat()
        {
            var h = Build(2, false);
            h.Military.SetFirepower(0, 60);
            h.Military.SetFirepower(1, 20);
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Wars.Count);
        }

        // ------------------------------------------------------------
        // 5. 玩家势力（attacker=0）默认不主动宣战
        // ------------------------------------------------------------
        [Test]
        public void PlayerFaction_DoesNotDeclare()
        {
            var h = Build(2, true);
            h.Military.SetFirepower(0, 60);
            h.Military.SetFirepower(1, 20);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Wars.Count);
        }

        // ------------------------------------------------------------
        // 6. 显式开启 AiMayDeclareOnHumanPlayer 后，AI 可以对玩家宣战（高难度）
        // ------------------------------------------------------------
        [Test]
        public void AiCanDeclareOnPlayer_WhenEnabled()
        {
            var h = Build(2, true);
            h.Military.SetFirepower(0, 20);   // 玩家
            h.Military.SetFirepower(1, 60);  // AI
            h.Plugin.AiMayDeclareOnHumanPlayer = true;

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Wars.Count);
            Assert.AreEqual(1, h.Wars[0].AttackerFaction);
            Assert.AreEqual(0, h.Wars[0].DefenderFaction);
        }

        // ------------------------------------------------------------
        // 6b. 默认 AI 不主动对玩家宣战（保护主角）
        // ------------------------------------------------------------
        [Test]
        public void AiDoesNotDeclareOnPlayer_ByDefault()
        {
            var h = Build(2, true);
            h.Military.SetFirepower(0, 20);   // 玩家
            h.Military.SetFirepower(1, 60);  // AI（军力优势）

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Wars.Count, "默认 AI 不应主动对玩家宣战");
        }

        // ------------------------------------------------------------
        // 7. 已在打一场战争（MaxConcurrentWars=1）→ 不向第三方宣战
        // ------------------------------------------------------------
        [Test]
        public void AtMaxConcurrentWars_NoMoreWar()
        {
            var h = Build(3, false);
            h.Military.SetFirepower(0, 80);
            h.Military.SetFirepower(1, 40);  // 已交战目标
            h.Military.SetFirepower(2, 10);  // 潜在弱小目标
            h.Military.SetAtWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Wars.Count,
                "势力 0 已在打 1 场战争，不应再向势力 2 宣战");
        }

        // ------------------------------------------------------------
        // 8. 多目标：每年最多宣战 1 次（break，选择首个满足条件者）
        // ------------------------------------------------------------
        [Test]
        public void DeclaresAtMostOneWarPerYear()
        {
            var h = Build(3, false);
            h.Military.SetFirepower(0, 90);
            h.Military.SetFirepower(1, 10);
            h.Military.SetFirepower(2, 10);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Wars.Count);
            Assert.AreEqual(1, h.Wars[0].DefenderFaction,
                "应选择第一个满足条件的势力 1");
        }
    }
}
