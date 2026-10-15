using System;
using UnityEngine;
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

namespace PxC.Bootstrap
{
    /// <summary>
    /// 唯一接线层（Composition Root）。负责把宿主内核与内置框架服务组装并启动。
    /// 不在此写任何业务规则。
    /// 时序：BeforeSceneLoad 安装崩溃守护 → AfterSceneLoad 收集插件并启动（插件注册在 BeforeSceneLoad 完成）。
    /// </summary>
    public static class GameBootstrap
    {
        public const int HostVersion = 10;

        /// <summary>应用完整版本号（与 Git tag / Release 对齐）。</summary>
        public const string AppVersion = "v10.4.1";

        /// <summary>启动后的测试上下文（PlayMode 冒烟测试使用；安全模式下为 null）。</summary>
        public static BootstrapTestContext TestContext { get; private set; }

        private static CrashGuard _crashGuard;
        private static GameClock _clock;
        private static StrongEventBus _events;
        private static ServiceRegistry _services;
        private static ChunkedWorld _world;
        private static CapabilitySystem _capability;
        private static PluginRuntime _runtime;
        private static FactionRegistry _factions;
        private static UnitRegistry _units;
        private static BuildingRegistry _buildings;
        private static SettlementRegistry _settlements;
        private static SaveCoordinator _saveCoordinator;

        // ------------------------------------------------------------
        // 阶段 1：最早安装崩溃守护
        // ------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Early()
        {
            _crashGuard = new CrashGuard(Debug.Log);
            _crashGuard.Install();
        }

        // ------------------------------------------------------------
        // 阶段 2：组装并启动
        // ------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            Action<string> Log = m =>
            {
                Debug.Log(m);
                _crashGuard?.Log(m);
            };

            // 核心
            _events = new StrongEventBus();
            _services = new ServiceRegistry();
            _world = new ChunkedWorld();
            _capability = new CapabilitySystem();
            _capability.SetLogger(Log);

            _clock = new GameClock();
            _clock.SnapToStart();

            // 帧预算 / 算力
            float budgetMs =
#if UNITY_WEBGL && !UNITY_EDITOR
                6f;
#else
                10f;
#endif
            var frameBudget = new FrameBudget(budgetMs);
            var scheduler = new SimScheduler(DetectQuality());

            // 资源 / 池 / 内存 / 存档
            IAssetLoader assets = SelectAssetLoader();

            var poolRoot = new GameObject("PxC_PoolRoot");
            UnityEngine.Object.DontDestroyOnLoad(poolRoot);
            var pool = new ObjectPool(poolRoot.transform);

            var memory = new MemoryMonitor(1100f, 1500f, Log);
            var saves = new SaveSystem();
            var clockAdapter = new ClockAdapter(_clock);
            var crashReport = new LocalCrashReport(Log);

            // 玩法层共享 registry（势力 / 可移动单位 / 建筑 / 聚落）
            _factions = new FactionRegistry();
            _units = new UnitRegistry();
            _buildings = new BuildingRegistry();
            _settlements = new SettlementRegistry();

            // 注册服务
            _services.Register<IClock>(clockAdapter);
            _services.Register<IObjectPool>(pool);
            _services.Register<IAssetLoader>(assets);
            _services.Register<IMemoryMonitor>(memory);
            _services.Register<ICrashGuard>(_crashGuard);
            _services.Register<ICrashReport>(crashReport);
            _services.Register<ISaveService>(saves);
            _services.Register<IFrameBudget>(frameBudget);
            _services.Register<ISimScheduler>(scheduler);
            _services.Register<FactionRegistry>(_factions);
            _services.Register<UnitRegistry>(_units);
            _services.Register<BuildingRegistry>(_buildings);
            _services.Register<SettlementRegistry>(_settlements);
            // 默认占位地理（全陆地）；真实地图插件在 OnInitialize 时替换为真实水陆查询
            _services.Register<IGeographyQuery>(new PermissiveGeography());

            // 崩溃状态快照提供者
            _crashGuard.SetStateProvider(() => new CrashStateSnapshot
            {
                GameYear = _clock.Year
            });

            // 插件运行时
            _runtime = new PluginRuntime(_events, _services, _world, _capability, HostVersion, Log);
            var plugins = PluginRegistry.Collect();
            for (int i = 0; i < plugins.Count; i++)
                _runtime.Discover(plugins[i]);

            if (!_runtime.BuildOrder(out var errors))
            {
                for (int i = 0; i < errors.Count; i++)
                    Debug.LogError($"[Bootstrap] 装配错误: {errors[i]}");
                // 安全模式：玩法未启动，仅保留崩溃守护；UI 层后续提供恢复界面
                return;
            }

            // 启动模式：读档恢复（存档存在）或新游戏开局。
            SaveData loaded = saves.Read(SaveCoordinator.MainSlot);
            bool isRestoring = loaded != null;

            if (isRestoring)
            {
                ApplyFactionSnapshot(_factions, loaded, Log);
                _clock.Restore(loaded.GameYear, loaded.Day);
                Log($"[启动] 检测到存档，准备恢复：游戏年 {loaded.GameYear}、势力 {loaded.FactionCount}、段 {loaded.Segments.Count}");
            }
            else
            {
                NewGameSetup.PopulateNewWorld(_factions);
            }

            // 统一初始化：注册服务、建立空状态容器。读档时 OnInitialize 的新游戏 seed 会被随后 RestoreSegments 覆盖。
            _runtime.InitializeAll();

            if (isRestoring)
                _runtime.RestoreSegments(loaded);

            _runtime.StartAll();

            // 存档协调器：集中存档入口（自动存档/手动存档/退出存档）。
            _saveCoordinator = new SaveCoordinator(
                _runtime, saves, _services, _factions, _clock, AppVersion, Log);

            TestContext = new BootstrapTestContext(
                _clock, _events, _services, _world, _runtime,
                _factions, _units, _buildings, _saveCoordinator);

            // 主循环
            var loopGo = new GameObject("PxC_GameLoop");
            UnityEngine.Object.DontDestroyOnLoad(loopGo);
            var loop = loopGo.AddComponent<GameLoop>();
            loop.Configure(_clock, _runtime, _events, frameBudget, scheduler);
            loop.AddPerFrame(_ => memory.Tick());
            loop.AddPerFrame(saves.Tick);
            loop.AddYearHandler(_saveCoordinator.OnYearAdvanced);
            loop.AddQuitHandler(() =>
            {
                // 退出最终存档（桌面有效；WebGL 关闭事件不可靠，靠自动存档兜底）。
                try { _saveCoordinator.SaveImmediate(SaveCoordinator.MainSlot); }
                catch (Exception e) { Debug.LogWarning($"[退出] 最终存档失败: {e.Message}"); }
                _crashGuard.MarkCleanRun();
            });

            if (_crashGuard.CrashedLastRun)
                Log("检测到上次异常退出；可在安全模式中选择恢复上次进度（UI 恢复界面为后续版本）");

            _crashGuard.MarkCleanRun();
            Log($"{AppVersion} 启动完成，插件数 {_runtime.PluginCount}，资源后端 {assets.BackendName}");
        }

        // ------------------------------------------------------------
        // 探测
        // ------------------------------------------------------------
        private static QualityTier DetectQuality()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return QualityTier.Low;
#else
            if (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 8192)
                return QualityTier.Low;
            return QualityTier.High;
#endif
        }

        private static IAssetLoader SelectAssetLoader()
        {
#if UNITY_ADDRESSABLES
            return new AddressablesAssetLoader();
#else
            return new ResourcesAssetLoader();
#endif
        }

        // ------------------------------------------------------------
        // 存档恢复：势力快照
        // ------------------------------------------------------------
        /// <summary>
        /// 从存档恢复 FactionRegistry（按快照顺序 Add，保证 factionId 对齐）。
        /// 旧存档无势力快照（FactionSnapshot 为 v10.4.1 新增）时按默认布局重建兜底。
        /// </summary>
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

            // 旧存档无势力快照：默认开局按固定布局重建；非默认数量无法安全恢复，兜底新游戏。
            log?.Invoke(data.FactionCount == NewGameSetup.InitialFactionCount
                ? "[启动] 旧存档无势力快照，按默认开局势力布局重建"
                : $"[启动] 旧存档势力数 {data.FactionCount} 无法从快照重建，兜底为新游戏");
            NewGameSetup.PopulateNewWorld(registry);
        }
    }
}
