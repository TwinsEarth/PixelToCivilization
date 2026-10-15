using System;
using UnityEngine;
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
        public const string AppVersion = "v10.2.0";

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

            // 玩法层共享 registry（势力 / 可移动单位 / 建筑）
            _factions = new FactionRegistry();
            _units = new UnitRegistry();
            _buildings = new BuildingRegistry();

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

            // 新游戏初始世界（开局势力布局）；读档路径后续由存档恢复
            if (!_services.Resolve<ISaveService>().HasSave(0))
                NewGameSetup.PopulateNewWorld(_factions);

            _runtime.InitializeAll();
            _runtime.StartAll();

            TestContext = new BootstrapTestContext(
                _clock, _events, _services, _world, _runtime,
                _factions, _units, _buildings);

            // 主循环
            var loopGo = new GameObject("PxC_GameLoop");
            UnityEngine.Object.DontDestroyOnLoad(loopGo);
            var loop = loopGo.AddComponent<GameLoop>();
            loop.Configure(_clock, _runtime, _events, frameBudget, scheduler);
            loop.AddPerFrame(_ => memory.Tick());
            loop.AddPerFrame(saves.Tick);
            loop.AddQuitHandler(() => _crashGuard.MarkCleanRun());

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
    }
}
