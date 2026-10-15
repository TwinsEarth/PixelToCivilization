using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using PxC.Bootstrap;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Core.Time;
using PxC.Core.World;
using PxC.Host;
using PxC.Infra.Assets;
using PxC.Infra.Budget;
using PxC.Infra.Crash;
using PxC.Infra.Memory;
using PxC.Infra.Pool;
using PxC.Infra.Save;
using PxC.Infra.Time;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Plugins.Economy;
using PxC.Plugins.Population;
using PxC.Plugins.Building;
using PxC.Plugins.Tech;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;
using PxC.Plugins.Combat;
using PxC.Plugins.Military;
using PxC.Plugins.Rally;
using PxC.Plugin.Smoke;
using PxC.Plugins.Settlement;
using PxC.Plugins.Colonization;
using PxC.Plugins.Culture;
using PxC.Plugins.History;
using PxC.Plugins.Policy;
using PxC.Plugins.Weather;
using PxC.Plugins.Disaster;
using PxC.Plugins.Victory;
using PxC.Plugins.AutoGovernor;
using PxC.Plugins.AutoMilitary;
using PxC.Plugins.AutoDiplomacy;
using PxC.Plugins.WarResolution;
using PxC.Plugins.Conquest;
using PxC.Plugins.AutoPeace;
using PxC.Plugins.CoercedPeace;
using PxC.Content.Towers;
using PxC.Content.Events;
using PxC.Content.Dynasties;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 确定性多年模拟节奏测试（v10.7.1）。把全 29 个插件真实装配后，
    /// 按"年结 + 该年帧模拟"推进 N 年，统计扩张/战争/灭国/和约/割地节奏，
    /// 用于平衡自动扩张节奏、验证战争能否真正打响，并作为长期回归。
    /// 无 Unity 环境下反射真实执行（非官方测试运行器），时间尺度为确定性常量。
    /// </summary>
    public sealed class DeterministicSimPaceTests
    {
        private sealed class GameWorld
        {
            public StrongEventBus Events;
            public ServiceRegistry Services;
            public ChunkedWorld World;
            public CapabilitySystem Capability;
            public GameClock Clock;
            public SaveSystem Saves;
            public FactionRegistry Factions;
            public UnitRegistry Units;
            public BuildingRegistry Buildings;
            public SettlementRegistry Settlements;
            public PluginRuntime Runtime;
            public AutoGovernorPlugin AutoGovernor;
            public AutoMilitaryPlugin AutoMilitary;
        }

        private sealed class Counters
        {
            public int Wars;
            public int Eliminations;
            public int Conquests;
            public int WhitePeace;
            public int CoercedPeace;
            public int SettlementsFounded;
        }

        // 时间尺度：1 游戏年（1x≈60 真实秒）在测试里用 FramesPerYear×FrameDt 秒模拟，
        // 让地面/海军 AI 有一整年的"移动+战斗"时间，结果与现实帧率解耦、可复现。
        private const int FramesPerYear = 600;
        private const float FrameDt = 0.1f;
        private const int SimYears = 120;

        private static GameWorld AssembleWorld(string rootDir, Action<string> log)
        {
            var w = new GameWorld
            {
                Events = new StrongEventBus(),
                Services = new ServiceRegistry(),
                World = new ChunkedWorld(),
                Capability = new CapabilitySystem(),
            };
            w.Capability.SetLogger(log);
            w.Clock = new GameClock();
            w.Clock.SnapToStart();

            var frameBudget = new FrameBudget(10f);
            var scheduler = new SimScheduler(QualityTier.High);
            IAssetLoader assets = new ResourcesAssetLoader();

            var poolRoot = new GameObject("PxC_PaceTestPoolRoot");
            var pool = new ObjectPool(poolRoot.transform);
            var memory = new MemoryMonitor(1100f, 1500f, log);
            w.Saves = new SaveSystem(rootDir);
            var crashReport = new LocalCrashReport(log);
            var crashGuard = new CrashGuard(log);

            w.Factions = new FactionRegistry();
            w.Units = new UnitRegistry();
            w.Buildings = new BuildingRegistry();
            w.Settlements = new SettlementRegistry();

            w.Services.Register<IClock>(new ClockAdapter(w.Clock));
            w.Services.Register<IObjectPool>(pool);
            w.Services.Register<IAssetLoader>(assets);
            w.Services.Register<IMemoryMonitor>(memory);
            w.Services.Register<ICrashGuard>(crashGuard);
            w.Services.Register<ICrashReport>(crashReport);
            w.Services.Register<ISaveService>(w.Saves);
            w.Services.Register<IFrameBudget>(frameBudget);
            w.Services.Register<ISimScheduler>(scheduler);
            w.Services.Register<FactionRegistry>(w.Factions);
            w.Services.Register<UnitRegistry>(w.Units);
            w.Services.Register<BuildingRegistry>(w.Buildings);
            w.Services.Register<SettlementRegistry>(w.Settlements);
            w.Services.Register<IGeographyQuery>(new PermissiveGeography());

            w.Runtime = new PluginRuntime(
                w.Events, w.Services, w.World, w.Capability,
                GameBootstrap.HostVersion, log);

            w.Runtime.Discover(new EconomyPlugin());
            w.Runtime.Discover(new PopulationPlugin());
            w.Runtime.Discover(new BuildingPlugin());
            w.Runtime.Discover(new TechPlugin());
            w.Runtime.Discover(new GroundForcePlugin());
            w.Runtime.Discover(new NavalPlugin());
            w.Runtime.Discover(new CombatPlugin());
            w.Runtime.Discover(new MilitaryPlugin());
            w.Runtime.Discover(new RallyCommandPlugin());
            w.Runtime.Discover(new SmokePlugin());
            w.Runtime.Discover(new SettlementPlugin());
            w.Runtime.Discover(new ColonizationPlugin());
            w.Runtime.Discover(new CulturePlugin());
            w.Runtime.Discover(new HistoryPlugin());
            w.Runtime.Discover(new PolicyPlugin());
            w.Runtime.Discover(new WeatherPlugin());
            w.Runtime.Discover(new DisasterPlugin());
            var autoGovernor = new AutoGovernorPlugin();
            var autoMilitary = new AutoMilitaryPlugin();
            w.AutoGovernor = autoGovernor;
            w.AutoMilitary = autoMilitary;
            w.Runtime.Discover(autoGovernor);
            w.Runtime.Discover(autoMilitary);
            w.Runtime.Discover(new AutoDiplomacyPlugin());
            w.Runtime.Discover(new WarResolutionPlugin());
            w.Runtime.Discover(new ConquestPlugin());
            w.Runtime.Discover(new AutoPeacePlugin());
            w.Runtime.Discover(new CoercedPeacePlugin());
            w.Runtime.Discover(new VictoryPlugin());
            w.Runtime.Discover(new TowersPack());
            w.Runtime.Discover(new EventsPack());
            w.Runtime.Discover(new DynastiesPack());

            return w;
        }

        /// <summary>推进 N 年：每年先年结（经济/建造/征兵/宣战/战争结算），再跑该年帧模拟（单位移动/战斗）。
        /// onYear(k,year) 在年结后、帧模拟前执行（用于测试场景注入：增兵/制造战争）。</summary>
        private static void Run(GameWorld w, int years, Counters c, StringBuilder trace,
            Action<int, int> onYear)
        {
            var pop = w.Services.Resolve<IPopulationService>();
            int startYear = w.Clock.Year;

            for (int k = 1; k <= years; k++)
            {
                int year = startYear + k;
                w.Clock.Restore(year, 0f);

                // 年结（含宣战/灭国/和约/割地/自动建造/征兵）
                w.Runtime.TickYear(year);
                if (w.Services.TryResolve<IEconomyYearFinalizer>(out var fin))
                    fin.FinalizeYear(year);

                // 测试场景注入（年结后、帧模拟前）
                onYear?.Invoke(k, year);

                // 该年帧模拟：驱动地面/海军 AI（行军/追击/攻击）
                var sims = w.Runtime.SimPlugins;
                for (int frame = 0; frame < FramesPerYear; frame++)
                {
                    for (int i = 0; i < sims.Count; i++)
                        w.Runtime.TickSim(sims[i], FrameDt);
                }
                w.Events.DispatchPending();

                if (k % 20 == 0 || k == 1 || k == years)
                {
                    int alive = 0;
                    for (int f = 0; f < w.Factions.Count; f++)
                        if (w.Factions.Get(f).Alive) alive++;
                    int totalPop = 0;
                    for (int f = 0; f < w.Factions.Count; f++)
                        totalPop += pop.Total(f);
                    trace.AppendLine(
                        $"y{k:D3} year{year} alive={alive} settlements={w.Settlements.Count} " +
                        $"buildings={w.Buildings.Count} ground={w.Units.Count} pop={totalPop} " +
                        $"wars={c.Wars} elim={c.Eliminations} conquer={c.Conquests} " +
                        $"whitePeace={c.WhitePeace} coerced={c.CoercedPeace} founded={c.SettlementsFounded}");
                }
            }
        }

        private static void Subscribe(GameWorld w, Counters c)
        {
            w.Events.Subscribe<WarDeclaredEvent>(_ => c.Wars++);
            w.Events.Subscribe<FactionEliminatedEvent>(e =>
            {
                c.Eliminations++;
                Console.Out.WriteLine(
                    $"  [灭国] 势力 {e.Faction} 被势力 {e.ByFaction} 所灭（年 {e.GameYear}）");
            });
            w.Events.Subscribe<TerritoryConqueredEvent>(_ => c.Conquests++);
            w.Events.Subscribe<PeaceTreatyEvent>(_ => c.WhitePeace++);
            w.Events.Subscribe<CoercedPeaceEvent>(_ => c.CoercedPeace++);
            w.Events.Subscribe<SettlementFoundedEvent>(_ => c.SettlementsFounded++);
        }

        // ----------------------------------------------------------------
        // 用例：120 年自动节奏冒烟——世界必须发展；人工制造一场军力不均衡的战争，
        // 验证"宣战 → 行军接敌 → 战斗减员 → 灭国/占领"完整战争链能走完。
        // ----------------------------------------------------------------
        [Test]
        public void AutoRun_120Years_WorldDevelopsAndWarsResolve()
        {
            using var tmp = new TempDir();
            var logs = new List<string>();
            var trace = new StringBuilder();
            var w = AssembleWorld(tmp.Path, m => logs.Add(m));
            var counters = new Counters();

            // 世界发展部分：开启玩家自动（本测试不模拟玩家操作，让世界整体发展）。
            w.AutoGovernor.EnablePlayerAutomation = true;
            w.AutoMilitary.EnablePlayerAutomation = true;

            Assert.IsTrue(w.Runtime.BuildOrder(out var errors),
                "装配失败: " + string.Join(" | ", errors));
            NewGameSetup.PopulateNewWorld(w.Factions);
            Assert.IsTrue(w.Runtime.InitializeAll(), "InitializeAll 失败");
            w.Runtime.StartAll();
            Subscribe(w, counters);

            int startSettlements = w.Settlements.Count;
            int startBuildings = w.Buildings.Count;

            // 战争链注入：在第 InjectYearK 年，给 attacker 增援 Formation（制造军力优势），
            // 并对 defender 宣战；战争发生在两个 AI 之间，玩家（faction 0）不被卷入。
            // 增援数校准：增援后 attacker 约 10、defender 5，比值 ≈2（>1.5 不触发 AutoPeace、
            // 又 ≤3 不触发 CoercedPeace 提前割地），从而迫使部队真正行军、接敌、战斗、灭国。
            const int attacker = 1;   // 东夷 (60,10)
            const int defender = 3;   // 南蛮 (10,60)，距 attacker 约 70.7
            const int injectYearK = 10;
            const int reinforcements = 5;

            Action<int, int> onYear = (k, year) =>
            {
                if (k == injectYearK)
                {
                    var ground = w.Services.Resolve<GroundForceService>();
                    var military = (MilitaryService)w.Services.Resolve<IMilitaryService>();
                    var att = w.Factions.Get(attacker);

                    // 在 attacker 首府附近环布增援（陆地），随后 Idle→战略行军。
                    const float goldenAngle = 2.399963f;
                    for (int i = 0; i < reinforcements; i++)
                    {
                        float ang = i * goldenAngle;
                        var pos = new Vector3(
                            att.HomeX + Mathf.Cos(ang) * 4f,
                            0f,
                            att.HomeZ + Mathf.Sin(ang) * 4f);
                        ground.Spawn(attacker, GroundForceModel.Formation, pos, attacker * 10 + 2);
                    }
                    // 立即置战争矩阵（使本年帧模拟即可行军），并补发事件（计数器/接线）。
                    military.DeclareWar(attacker, defender, year);
                    w.Events.PublishDeferred(new WarDeclaredEvent
                    {
                        AttackerFaction = attacker,
                        DefenderFaction = defender,
                    });
                    Console.Out.WriteLine(
                        $"[注入] 第{year}年：势力 {attacker} 增援 {reinforcements} 并对势力 {defender} 宣战");
                }

                // 每年追踪行军：注入后打印 attacker/defender 单位数与位置（看是否接敌/减员）。
                if (k >= injectYearK && (k % 2 == 0 || k < injectYearK + 4))
                {
                    var units = w.Services.Resolve<UnitRegistry>();
                    int nAtt = 0, nDef = 0;
                    var attPos = new StringBuilder();
                    var defPos = new StringBuilder();
                    int cap = units.Capacity;
                    for (int s = 0; s < cap; s++)
                    {
                        ref var u = ref units.GetSlotRef(s);
                        if (!u.Alive || u.Domain != EntityDomain.Land) continue;
                        if (u.Faction == attacker)
                        {
                            nAtt++;
                            if (nAtt <= 3) attPos.Append($"#{s}({u.Position.x:F0},{u.Position.z:F0},{u.State}) ");
                        }
                        else if (u.Faction == defender)
                        {
                            nDef++;
                            if (nDef <= 3) defPos.Append($"#{s}({u.Position.x:F0},{u.Position.z:F0},{u.State}) ");
                        }
                    }
                    Console.Out.WriteLine(
                        $"  k{k} att={nAtt} def={nDef} | attPos {attPos}| defPos {defPos}");
                }
            };

            Run(w, SimYears, counters, trace, onYear);

            Console.Out.WriteLine(trace.ToString());
            Console.Out.WriteLine(
                $"TOTAL wars={counters.Wars} elim={counters.Eliminations} conquer={counters.Conquests} " +
                $"whitePeace={counters.WhitePeace} coerced={counters.CoercedPeace} founded={counters.SettlementsFounded}");

            // 世界必须发展：聚落/建筑增长（初始 5 个首都、0 栋自动建筑）
            Assert.Greater(w.Settlements.Count, startSettlements,
                "120 年内应自动建立新聚落（殖民/扩张）");
            Assert.Greater(w.Buildings.Count, startBuildings,
                "120 年内应自动建造建筑");

            // 战争必须能发起（人工注入的宣战，计数器仍记录）
            Assert.Greater(counters.Wars, 0, "120 年内应至少发生 1 次宣战");

            // 战争必须能分出结果：行军接敌后战斗减员，触发灭国/占领/割地/和约。
            int resolutions = counters.Eliminations + counters.CoercedPeace + counters.WhitePeace;
            Console.Out.WriteLine($"战争结束数 resolutions={resolutions}");
            Assert.Greater(resolutions, 0,
                "战争应能结束（灭国/割地/和约），不应只宣战而永不接触");

            // 玩家华夏（faction 0）始终存活（战争在 AI 之间），世界不被灭到只剩 0 方
            Assert.IsTrue(w.Factions.Get(0).Alive,
                "玩家势力不应被灭（战争注入发生在两个 AI 之间）");
            int aliveCount = 0;
            for (int f = 0; f < w.Factions.Count; f++)
                if (w.Factions.Get(f).Alive) aliveCount++;
            Assert.GreaterOrEqual(aliveCount, 1, "世界至少应保留 1 个存活势力");

            // 资源不为 NaN / 负无穷
            var economy = w.Services.Resolve<IEconomyService>();
            for (int f = 0; f < w.Factions.Count; f++)
            {
                for (int rt = 0; rt < PxC.Gameplay.Resources.ResourceTypes.Count; rt++)
                {
                    float v = economy.Get(f, (ResourceType)rt);
                    Assert.IsFalse(float.IsNaN(v), $"势力 {f} 资源 {rt} 为 NaN");
                    Assert.IsFalse(float.IsInfinity(v), $"势力 {f} 资源 {rt} 为 Infinity");
                }
            }
        }
    }
}
