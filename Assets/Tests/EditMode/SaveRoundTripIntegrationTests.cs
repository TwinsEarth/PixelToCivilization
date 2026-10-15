using System;
using System.Collections.Generic;
using NUnit.Framework;
using PxC.Bootstrap;
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
using PxC.Content.Towers;
using PxC.Content.Events;
using PxC.Content.Dynasties;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 存档闭环集成测试（v10.4.1）。镜像 GameBootstrap 真实装配时序，
    /// 手动 new 全 21 个内置插件并 Discover（不调用 PluginRegistry / 不跑 RuntimeInitializeOnLoadMethod）。
    /// 覆盖两条路径：存档触发→收集→写入；读档→恢复势力/时钟/插件段→启动。
    /// 注意：InitializeAll 内部自动按拓扑顺序 OnRegister + OnInitialize，测试不手动 OnRegister。
    /// </summary>
    public sealed class SaveRoundTripIntegrationTests
    {
        // ----------------------------------------------------------------
        // 装配容器（镜像 GameBootstrap 的字段）
        // ----------------------------------------------------------------
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
            public SaveCoordinator Coordinator;
        }

        // ----------------------------------------------------------------
        // 组装：极薄宿主 + 全 21 插件（仅 Discover，不 BuildOrder）。
        // rootDir 决定 SaveSystem 落盘目录；newGame 时为空目录，读档时同目录。
        // ----------------------------------------------------------------
        private static GameWorld AssembleWorld(string rootDir, Action<string> log)
        {
            var w = new GameWorld();
            w.Events = new StrongEventBus();
            w.Services = new ServiceRegistry();
            w.World = new ChunkedWorld();
            w.Capability = new CapabilitySystem();
            w.Capability.SetLogger(log);

            w.Clock = new GameClock();
            w.Clock.SnapToStart();

            var frameBudget = new FrameBudget(10f);
            var scheduler = new SimScheduler(QualityTier.High);

            IAssetLoader assets = new ResourcesAssetLoader();

            // ObjectPool 仅在构造时持有 root Transform；插件 OnInitialize 不调用池，故桩 root 安全。
            var poolRoot = new GameObject("PxC_TestPoolRoot");
            var pool = new ObjectPool(poolRoot.transform);

            var memory = new MemoryMonitor(1100f, 1500f, log);
            w.Saves = new SaveSystem(rootDir);
            var crashReport = new LocalCrashReport(log);
            // 不 Install：避免订阅全局事件；只注册 ICrashGuard 满足服务装配。
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

            DiscoverAll(w.Runtime);

            w.Coordinator = new SaveCoordinator(
                w.Runtime, w.Saves, w.Services, w.Factions, w.Clock,
                GameBootstrap.AppVersion, log);

            return w;
        }

        // ----------------------------------------------------------------
        // 全 21 插件（顺序无关，BuildOrder 内做拓扑排序）。
        // ----------------------------------------------------------------
        private static void DiscoverAll(PluginRuntime runtime)
        {
            runtime.Discover(new EconomyPlugin());
            runtime.Discover(new PopulationPlugin());
            runtime.Discover(new BuildingPlugin());
            runtime.Discover(new TechPlugin());
            runtime.Discover(new GroundForcePlugin());
            runtime.Discover(new NavalPlugin());
            runtime.Discover(new CombatPlugin());
            runtime.Discover(new MilitaryPlugin());
            runtime.Discover(new RallyCommandPlugin());
            runtime.Discover(new SmokePlugin());
            runtime.Discover(new SettlementPlugin());
            runtime.Discover(new ColonizationPlugin());
            runtime.Discover(new CulturePlugin());
            runtime.Discover(new HistoryPlugin());
            runtime.Discover(new PolicyPlugin());
            runtime.Discover(new WeatherPlugin());
            runtime.Discover(new DisasterPlugin());
            runtime.Discover(new VictoryPlugin());
            runtime.Discover(new TowersPack());
            runtime.Discover(new EventsPack());
            runtime.Discover(new DynastiesPack());
        }

        // ----------------------------------------------------------------
        // 复刻 GameBootstrap.ApplyFactionSnapshot（private，测试无法直接调用）。
        // 按快照顺序 Add 保证 factionId 对齐；无快照时兜底新游戏布局。
        // ----------------------------------------------------------------
        private static void ApplyFactionSnapshot(FactionRegistry registry, SaveData data, Action<string> log)
        {
            if (data.Factions != null && data.Factions.Count > 0)
            {
                for (int i = 0; i < data.Factions.Count; i++)
                {
                    var s = data.Factions[i];
                    var info = registry.Add(
                        s.Name ?? string.Empty,
                        s.IsPlayer,
                        s.HomeX,
                        s.HomeZ,
                        s.ColorIndex);
                    info.Alive = s.Alive;
                }
                return;
            }

            log?.Invoke("[测试] 存档无势力快照，按默认开局布局重建");
            NewGameSetup.PopulateNewWorld(registry);
        }

        // ----------------------------------------------------------------
        // 模拟 GameLoop 年度推进：clock.Restore 设年份 → runtime.TickYear 年结 →
        // coordinator.OnYearAdvanced 自动存档钩子 → saves.Tick 刷异步写队列。
        // ----------------------------------------------------------------
        private static void AdvanceYears(GameWorld w, int fromYear, int years, bool driveAutoSave)
        {
            for (int k = 1; k <= years; k++)
            {
                int y = fromYear + k;
                w.Clock.Restore(y, 0f);
                w.Runtime.TickYear(y);
                if (driveAutoSave)
                {
                    w.Coordinator.OnYearAdvanced(y);
                    w.Saves.Tick(0f);
                }
            }
        }

        /// <summary>全势力总人口（Σ各势力 IPopulationService.Total）。</summary>
        private static int TotalPopulation(GameWorld w)
        {
            var pop = w.Services.Resolve<IPopulationService>();
            int sum = 0;
            for (int f = 0; f < w.Factions.Count; f++)
                sum += pop.Total(f);
            return sum;
        }

        // ----------------------------------------------------------------
        // 用例 1：完整往返 —— 新游戏跑 30 年 → 立即存档 → 全新装配读档 → 恢复断言。
        // ----------------------------------------------------------------
        [Test]
        public void FullGame_SaveThenLoad_RestoresFactionsClockPopulationSettlements()
        {
            using var tmp = new TempDir();
            var logs = new List<string>();
            Action<string> log = m => logs.Add(m);

            // ===== 源世界：新游戏 =====
            var src = AssembleWorld(tmp.Path, log);
            Assert.IsTrue(src.Runtime.BuildOrder(out var buildErrors),
                "源世界装配失败: " + string.Join(" | ", buildErrors));
            NewGameSetup.PopulateNewWorld(src.Factions);
            Assert.IsTrue(src.Runtime.InitializeAll(),
                "源世界 InitializeAll 失败: " + string.Join(" | ", logs));
            src.Runtime.StartAll();

            int startYear = src.Clock.Year;
            AdvanceYears(src, startYear, years: 30, driveAutoSave: false);

            int savedYear = src.Clock.Year;
            int savedPop = TotalPopulation(src);
            int savedSettlements = src.Settlements.Count;
            Assert.Greater(savedPop, 0, "存档前人口应 > 0");
            Assert.GreaterOrEqual(savedSettlements, NewGameSetup.InitialFactionCount,
                "存档前至少应有 5 个首都");

            // 立即同步存档（主槽）
            Assert.IsTrue(src.Coordinator.SaveImmediate(SaveCoordinator.MainSlot),
                "SaveImmediate 应成功");

            // ===== 目标世界：全新装配（同目录，模拟重启） =====
            var dst = AssembleWorld(tmp.Path, log);
            Assert.IsTrue(dst.Runtime.BuildOrder(out var buildErrors2),
                "目标世界装配失败: " + string.Join(" | ", buildErrors2));

            SaveData loaded = dst.Saves.Read(SaveCoordinator.MainSlot);
            Assert.IsNotNull(loaded, "读档应返回 SaveData");

            ApplyFactionSnapshot(dst.Factions, loaded, log);
            dst.Clock.Restore(loaded.GameYear, loaded.Day);

            Assert.IsTrue(dst.Runtime.InitializeAll(),
                "目标世界 InitializeAll 失败: " + string.Join(" | ", logs));
            dst.Runtime.RestoreSegments(loaded);
            dst.Runtime.StartAll();

            // ---- 势力恢复 ----
            Assert.AreEqual(NewGameSetup.InitialFactionCount, dst.Factions.Count,
                "势力数应恢复为 5");
            Assert.AreEqual("华夏部落", dst.Factions.Get(0).Name);
            Assert.AreEqual("东夷部落", dst.Factions.Get(1).Name);
            Assert.AreEqual("西戎部落", dst.Factions.Get(2).Name);
            Assert.AreEqual("南蛮部落", dst.Factions.Get(3).Name);
            Assert.AreEqual("北狄部落", dst.Factions.Get(4).Name);
            Assert.IsNotNull(dst.Factions.Player);
            Assert.IsTrue(dst.Factions.Player.IsPlayer, "玩家势力标记应恢复");

            // ---- 时钟恢复 ----
            Assert.AreEqual(savedYear, dst.Clock.Year, "时钟年份应恢复");
            Assert.AreEqual(loaded.GameYear, savedYear);

            // ---- 人口恢复（存档顶层 Population 与恢复后聚合一致） ----
            Assert.AreEqual(savedPop, loaded.Population,
                "存档顶层 Population 应等于存档前实时聚合");
            int restoredPop = TotalPopulation(dst);
            Assert.AreEqual(savedPop, restoredPop,
                "恢复后总人口应与存档前一致");

            // ---- 聚落恢复 ----
            Assert.AreEqual(savedSettlements, dst.Settlements.Count,
                "聚落数应恢复");
        }

        // ----------------------------------------------------------------
        // 用例 2：自动存档 —— 每 20 游戏年写主槽，经 saves.Tick 落盘。
        // ----------------------------------------------------------------
        [Test]
        public void AutoSave_FiresAtInterval_AndPersists()
        {
            using var tmp = new TempDir();
            var src = AssembleWorld(tmp.Path, m => { });
            Assert.IsTrue(src.Runtime.BuildOrder(out _));
            NewGameSetup.PopulateNewWorld(src.Factions);
            Assert.IsTrue(src.Runtime.InitializeAll());
            src.Runtime.StartAll();

            int startYear = src.Clock.Year;
            Assert.IsFalse(src.Saves.HasSave(SaveCoordinator.MainSlot),
                "开局主槽不应有存档");

            // 跑 25 年（驱动自动存档）
            AdvanceYears(src, startYear, years: 25, driveAutoSave: true);

            Assert.IsTrue(src.Saves.HasSave(SaveCoordinator.MainSlot),
                "跨过 20 年应已自动存档到主槽");
            Assert.AreEqual(startYear + SaveCoordinator.AutoSaveYears,
                src.Coordinator.LastAutoSaveYear,
                "上次自动存档年份应为 startYear + 20");
        }

        // ----------------------------------------------------------------
        // 用例 3：手动异步存档 —— 入队后未落盘，saves.Tick 后落盘。
        // ----------------------------------------------------------------
        [Test]
        public void ManualSave_AsyncQueue_PersistsAfterTick()
        {
            using var tmp = new TempDir();
            const int manualSlot = 1;
            var src = AssembleWorld(tmp.Path, m => { });
            Assert.IsTrue(src.Runtime.BuildOrder(out _));
            NewGameSetup.PopulateNewWorld(src.Factions);
            Assert.IsTrue(src.Runtime.InitializeAll());
            src.Runtime.StartAll();

            bool done = false;
            bool result = false;
            src.Coordinator.SaveGame(manualSlot, ok => { done = true; result = ok; });

            Assert.IsFalse(src.Saves.HasSave(manualSlot),
                "异步存档入队后、Tick 前不应落盘");
            Assert.IsFalse(done);

            src.Saves.Tick(0f);

            Assert.IsTrue(src.Saves.HasSave(manualSlot),
                "Tick 后手动存档应落盘");
            Assert.IsTrue(done, "完成回调应被调用");
            Assert.IsTrue(result, "存档结果应为成功");
        }
    }
}
